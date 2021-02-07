using System;
using System.Collections.Generic;
using System.Linq;
using Core;
using Core.BL.Interfaces;
using Core.Models;


namespace BL.Implementation
{
    public class EmployeeWorkloadService : IEmployeeWorkloadService
    {
        #region Internal structures

        private class Workload
        {
            public int CurrentProjectPercent { get; set; }

            public int TotalPercent { get; set; }
        }

        #endregion

        protected IEmployeeService EmployeeSvc { get; }

        protected IProjectMembershipService ProjectMembershipSvc { get; }

        protected IProjectService ProjectSvc { get; }

        protected IProductionCalendarService ProductionCalendarSvc { get; }


        public EmployeeWorkloadService(IEmployeeService employeeSvc, IProjectService projectSvc, IProjectMembershipService projectMembershipSvc, IProductionCalendarService productionCalendarSvc)
        {
            if (employeeSvc == null)
                throw new ArgumentNullException(nameof(employeeSvc));
            if (projectSvc == null)
                throw new ArgumentNullException(nameof(projectSvc));
            if (projectMembershipSvc == null)
                throw new ArgumentNullException(nameof(projectMembershipSvc));
            if (productionCalendarSvc == null)
                throw new ArgumentNullException(nameof(productionCalendarSvc));

            EmployeeSvc = employeeSvc;
            ProjectSvc = projectSvc;
            ProjectMembershipSvc = projectMembershipSvc;
            ProductionCalendarSvc = productionCalendarSvc;
        }

        public ProjectWorkload GetProjectWorkload(int projectId, DateTimeRange dateRange)
        {
            var dateOnlyRange = new DateTimeRange(dateRange.Begin.Date, dateRange.End.Date);
            var projectEmployees = ProjectMembershipSvc.GetEmployeesOnProject(projectId, dateOnlyRange);
            var membershipByEmployees = GetProjectMembershipByEmployees(dateOnlyRange, projectEmployees);
            var workhoursByDate = ProductionCalendarSvc.GetWorkHoursByDate(dateOnlyRange);
            var projectWorkload = new ProjectWorkload
            {
                ProjectId = projectId,
                DateFrom = dateOnlyRange.Begin.Date,
                DateTo = dateOnlyRange.End.Date
            };
            var employeeWorkloads = GetEmployeesWorkloadForDateRange(projectEmployees, dateOnlyRange, WorkloadPeriod.Week, membershipByEmployees, workhoursByDate, projectId);
            projectWorkload.EmployeeWorkloads.AddRange(employeeWorkloads);
            projectWorkload.TotalHours = projectWorkload.EmployeeWorkloads.Sum(ew => ew.WorkloadRecords.Sum(wr => wr.CurrentProjectHours));
            return projectWorkload;
        }

        public DepartmentWorkload GetDepartmentWorkload(int departmentId, DateTimeRange dateRange, WorkloadPeriod workloadPeriodStep)
        {
            var dateOnlyRange = new DateTimeRange(dateRange.Begin.Date, dateRange.End.Date);
            var departmentEmployees = EmployeeSvc.GetEmployeesInDepartment(departmentId, dateOnlyRange, true);
            var membershipByEmployees = GetProjectMembershipByEmployees(dateOnlyRange, departmentEmployees);
            var workhoursByDate = ProductionCalendarSvc.GetWorkHoursByDate(dateOnlyRange);
            var departmentWorkload = new DepartmentWorkload
            {
                DepartmentId = departmentId,
                DateFrom = dateOnlyRange.Begin.Date,
                DateTo = dateOnlyRange.End.Date
            };
            var employeeWorkloads = GetEmployeesWorkloadForDateRange(departmentEmployees, dateOnlyRange, workloadPeriodStep, membershipByEmployees, workhoursByDate, 0);
            departmentWorkload.EmployeeWorkloads.AddRange(employeeWorkloads);
            departmentWorkload.TotalHours = departmentWorkload.EmployeeWorkloads.Sum(ew => ew.WorkloadRecords.Sum(wr => wr.CurrentProjectHours));
            return departmentWorkload;
        }

        public SubordinatesWorkload GetPMSubordinatesWorkload(int projectManagerId, DateTimeRange dateRange, WorkloadPeriod workloadPeriodStep)
        {
            // TODO: Логика тяжеловесная и запутанная, надо подумать над возможными улучшениями
            var dateOnlyRange = new DateTimeRange(dateRange.Begin.Date, dateRange.End.Date);
            var employees = GetProjectManagerSubordinates(projectManagerId, dateOnlyRange);
            var membershipByEmployees = GetProjectMembershipByEmployees(dateOnlyRange, employees);
            var workhoursByDate = ProductionCalendarSvc.GetWorkHoursByDate(dateOnlyRange);

            var projectComparer = new FuncBasedComparer<Project>((first, second) =>
            {
                if (first.EmployeePMID == projectManagerId && second.EmployeePMID != projectManagerId)
                    return -1;
                if (first.EmployeePMID != projectManagerId && second.EmployeePMID == projectManagerId)
                    return 1;
                int res = string.Compare(first.FullName, second.FullName);
                if (res != 0)
                    res = first.ID - second.ID;
                return res;
            });

            var workload = new SubordinatesWorkload();
            foreach (var employee in employees)
            {
                var employeeProjects = ProjectMembershipSvc.GetProjectsForEmployee(employee.ID, dateOnlyRange);
                employeeProjects = employeeProjects.OrderBy(e => e, projectComparer).ToList();
                var employeeProjectWorkload = new EmployeeProjectWorkload();
                employeeProjectWorkload.Employee = employee;
                foreach (var project in employeeProjects)
                {
                    if (!membershipByEmployees.TryGetValue(employee.ID, out var projectMemberships))
                        projectMemberships = new List<ProjectMember>();
                    var dailyWorkload = GetEmployeeDailyWorkload(project.ID, dateRange, projectMemberships, workhoursByDate);

                    var employeeWorkload = GetEmployeeWorkloadForDateRange(employee, dateRange, workloadPeriodStep, workhoursByDate, dailyWorkload);
                    var workloadRecord = new EmployeeProjectWorkloadRecord();
                    workloadRecord.Project = project;
                    workloadRecord.WorkloadRecords.AddRange(employeeWorkload.WorkloadRecords);
                    employeeProjectWorkload.WorkloadRecords.Add(workloadRecord);
                }
                if (employeeProjectWorkload.WorkloadRecords.Count > 0)
                    workload.SubordinateEmployeesWorkloads.Add(employeeProjectWorkload);
            }
            return workload;
        }

        private IList<Employee> GetProjectManagerSubordinates(int projectManagerId, DateTimeRange dateOnlyRange)
        {
            var projects = ProjectMembershipSvc.GetProjectsForManager(projectManagerId, dateOnlyRange);
            var comparer = new FuncBasedEqualityComparer<Employee>((first, second) => first.ID == second.ID, e => e.ID.GetHashCode());
            var employeesSet = new HashSet<Employee>(comparer);
            foreach (var project in projects)
            {
                var projectEmployees = ProjectMembershipSvc.GetEmployeesOnProject(project.ID, dateOnlyRange);
                foreach (var employee in projectEmployees)
                    employeesSet.Add(employee);
            }
            var employees = employeesSet.OrderBy(e => e.FullName).ThenBy(e => e.ID).ToList();
            return employees;
        }

        private IList<EmployeeWorkload> GetEmployeesWorkloadForDateRange(IEnumerable<Employee> employees, DateTimeRange dateRange, WorkloadPeriod periodStep, IDictionary<int, List<ProjectMember>> membershipByEmployees, IDictionary<DateTime, int> workhoursByDate, int projectId)
        {
            var employeeWorkloads = new List<EmployeeWorkload>();
            foreach (var employee in employees)
            {
                if (!membershipByEmployees.TryGetValue(employee.ID, out var projectMemberships))
                    projectMemberships = new List<ProjectMember>();
                var dailyWorkload = GetEmployeeDailyWorkload(projectId, dateRange, projectMemberships, workhoursByDate);

                EmployeeWorkload employeeWorkload = GetEmployeeWorkloadForDateRange(employee, dateRange, periodStep, workhoursByDate, dailyWorkload);
                employeeWorkloads.Add(employeeWorkload);
            }
            return employeeWorkloads;
        }

        private EmployeeWorkload GetEmployeeWorkloadForDateRange(Employee employee, DateTimeRange dateRange, WorkloadPeriod periodStep, IDictionary<DateTime, int> workhoursByDate, IDictionary<DateTime, Workload> dailyWorkload)
        {
            var employeeWorkload = new EmployeeWorkload
            {
                Employee = employee
            };

            DateTime dateFrom = dateRange.Begin.Date;

            DateTime dateTo;
            if (periodStep == WorkloadPeriod.Week)
            {
                int daysToSunday = (7 - (int)dateFrom.DayOfWeek) % 7;
                dateTo = dateFrom.AddDays(daysToSunday);
            }
            else if (periodStep == WorkloadPeriod.Month)
            {
                int lastDayOfMonth = DateTime.DaysInMonth(dateFrom.Year, dateFrom.Month);
                dateTo = new DateTime(dateFrom.Year, dateFrom.Month, lastDayOfMonth);
            }
            else
                throw new NotImplementedException($"Обработка периода загрузки {periodStep} не реализована");

            while (dateTo <= dateRange.End.Date)
            {
                EmployeeWorkloadRecord employeeWorkloadRecord = CalculateWorkloadForPeriod(dateFrom, dateTo, dailyWorkload, workhoursByDate);
                employeeWorkload.WorkloadRecords.Add(employeeWorkloadRecord);
                dateFrom = dateTo.AddDays(1);
                if (periodStep == WorkloadPeriod.Week)
                    dateTo = dateTo.AddDays(7);
                else if (periodStep == WorkloadPeriod.Month)
                {
                    dateTo = dateTo.AddMonths(1);
                    int lastDayOfMonth = DateTime.DaysInMonth(dateTo.Year, dateTo.Month);
                    dateTo = new DateTime(dateTo.Year, dateTo.Month, lastDayOfMonth);
                }
            }

            dateTo = dateRange.End.Date;
            if (dateFrom <= dateTo)
            {
                EmployeeWorkloadRecord employeeWorkloadRecord = CalculateWorkloadForPeriod(dateFrom, dateTo, dailyWorkload, workhoursByDate);
                employeeWorkload.WorkloadRecords.Add(employeeWorkloadRecord);
            }

            return employeeWorkload;
        }

        private EmployeeWorkloadRecord CalculateWorkloadForPeriod(DateTime dateFrom, DateTime dateTo, IDictionary<DateTime, Workload> dailyWorkload, IDictionary<DateTime, int> workhours)
        {
            int hoursInWeek = 0;
            int currentProjectHours = 0;
            int totalProjectHours = 0;
            DateTime date = dateFrom;
            while (date <= dateTo)
            {
                int workhoursInDay = workhours[date];
                hoursInWeek += workhoursInDay;
                if (dailyWorkload.TryGetValue(date, out Workload workload))
                {
                    currentProjectHours += (workload.CurrentProjectPercent * workhoursInDay);
                    totalProjectHours += (workload.TotalPercent * workhoursInDay);
                }

                date = date.AddDays(1);
            }
            var employeeWorkloadRecord = new EmployeeWorkloadRecord();
            employeeWorkloadRecord.DateFrom = dateFrom;
            employeeWorkloadRecord.DateTo = dateTo;
            if (hoursInWeek > 0)
            {
                employeeWorkloadRecord.CurrentProjectPercents = Math.Round(currentProjectHours / (double)hoursInWeek);
                employeeWorkloadRecord.TotalPercents = Math.Round(totalProjectHours / (double)hoursInWeek);
            }
            else
            {
                // Обработка ситуации, когда на неделе нет ни одного рабочего дня (с 1 по 7 явнваря)
                // TODO: Нужно подмать, нет ли лучшего варианта
                employeeWorkloadRecord.CurrentProjectPercents = 0;
                employeeWorkloadRecord.TotalPercents = 100;
            }
            employeeWorkloadRecord.CurrentProjectHours = hoursInWeek * employeeWorkloadRecord.CurrentProjectPercents / 100.0;

            return employeeWorkloadRecord;
        }

        private Dictionary<DateTime, Workload> GetEmployeeDailyWorkload(int projectId, DateTimeRange dateRange, IEnumerable<ProjectMember> projectMemberships, IDictionary<DateTime, int> workhours)
        {
            var dailyWorkloads = new Dictionary<DateTime, Workload>();
            foreach (var projectMember in projectMemberships)
            {
                DateTime date = dateRange.Begin.Date;
                if (projectMember.MembershipDateBegin.HasValue)
                    date = projectMember.MembershipDateBegin.Value;
                DateTime dateTo = dateRange.End.Date;
                if (projectMember.MembershipDateEnd.HasValue)
                    dateTo = projectMember.MembershipDateEnd.Value;

                while (date <= dateTo)
                {
                    Workload workload;
                    if (!dailyWorkloads.TryGetValue(date, out workload))
                    {
                        workload = new Workload();
                        dailyWorkloads.Add(date, workload);
                    }

                    // TODO: Уточнить, не надо ли проверять не в отпуске ли человек в этот день
                    workload.TotalPercent += projectMember.AssignmentPercentage;
                    if (projectMember.ProjectID == projectId)
                        workload.CurrentProjectPercent += projectMember.AssignmentPercentage;
                    date = date.AddDays(1);
                }
            }
            return dailyWorkloads;
        }

        private Dictionary<int, List<ProjectMember>> GetProjectMembershipByEmployees(DateTimeRange dateRange, IList<Employee> employees)
        {
            var projectMemberships = ProjectMembershipSvc.GetProjectMembershipForEmployees(dateRange, employees);
            var membershipByEmployees = new Dictionary<int, List<ProjectMember>>(employees.Count);
            foreach (var projectMember in projectMemberships)
            {
                // Членство в проекте в роле КАМ не учитывается в подсчете загрузки
                if (projectMember.ProjectRole != null && projectMember.ProjectRole.RoleType == ProjectRoleType.CAM)
                    continue;

                List<ProjectMember> list;
                if (!membershipByEmployees.TryGetValue(projectMember.Employee.ID, out list))
                {
                    list = new List<ProjectMember>();
                    membershipByEmployees.Add(projectMember.Employee.ID, list);
                }
                list.Add(projectMember);
            }

            return membershipByEmployees;
        }
    }
}