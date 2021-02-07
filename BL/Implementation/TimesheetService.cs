using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using Core;
using Core.Helpers;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using Microsoft.Extensions.Options;
using Oracle.ManagedDataAccess.Client;
using Core.BL.Interfaces;
using Core.Common;
using Core.Config;
using Core.Extensions;
using Core.Models;
using Core.Models.Timesheet;

namespace BL.Implementation
{
    public class TimesheetService : ITimesheetService
    {
        private readonly IEmployeeService _employeeService;
        private readonly IEmployeeCategoryService _employeeCategoryService;
        private readonly ITSAutoHoursRecordService _tsAutoHoursRecordService;
        private readonly ITSHoursRecordService _tsHoursRecordService;
        private readonly IProjectService _projectService;
        private readonly IProjectReportRecordService _projectReportRecordService;
        private readonly IVacationRecordService _vacationRecordService;
        private readonly IProductionCalendarService _productionCalendarService;
        private readonly IFinanceService _financeService;

        private TimesheetConfig _timesheetConfig;

        public ArrayList PROJECTS = new ArrayList();
        public ArrayList PERMANENT_PROJECTS = new ArrayList();
        public ArrayList HOURS = new ArrayList();
        public ArrayList VACATIONS = new ArrayList();

        public TimesheetService(IEmployeeService employeeService,
            IEmployeeCategoryService employeeCategoryService,
            ITSAutoHoursRecordService tsAutoHoursRecordService,
            ITSHoursRecordService tsHoursRecordService,
            IProjectService projectService,
            IProjectReportRecordService projectReportRecordService,
            IVacationRecordService vacationRecordService, 
            IProductionCalendarService productionCalendarService,
            IFinanceService financeService, IOptions<TimesheetConfig> timesheetOptions)
        {
            _employeeService = employeeService;
            _employeeCategoryService = employeeCategoryService;
            _tsAutoHoursRecordService = tsAutoHoursRecordService;
            _tsHoursRecordService = tsHoursRecordService;
            _projectService = projectService;
            _projectReportRecordService = projectReportRecordService;
            _vacationRecordService = vacationRecordService;
            _productionCalendarService = productionCalendarService;
            _financeService = financeService;
            _timesheetConfig = timesheetOptions.Value;
        }

        private string GetConnectionString()
        {
            string dataSource = string.Format("(DESCRIPTION=(ADDRESS_LIST=(ADDRESS=(PROTOCOL=TCP)(HOST={0})(PORT={1})))(CONNECT_DATA=(SERVER=DEDICATED)(SERVICE_NAME={2})))",
                    _timesheetConfig.OraDBHost,
                    _timesheetConfig.OracleDBPort,
                    _timesheetConfig.OracleDBSN);

            string userID = _timesheetConfig.OracleDBUserID;

            string password = _timesheetConfig.OracleDBPassword;

            // To avoid storing the connection string in your code, 
            // you can retrieve it from a configuration file. 
            return "Data Source=" + dataSource + ";" +
                   "User ID=" + userID + ";Password=" + password;
        }

        public bool IsExternalTSAllowed()
        {
            return (String.IsNullOrEmpty(_timesheetConfig.OraDBHost) == false);
        }

        public void GetDataFromTSAutoHoursRecords(LongRunningTaskBase task)
        {
            PERMANENT_PROJECTS.Clear();
            var projectsDict = new Dictionary<string, PROJECTSRecord>();

            DateTime timesheetProcessingProcessVacationRecordsStartDate = DateTime.MinValue;

            try
            {
                if (String.IsNullOrEmpty(_timesheetConfig.ProcessingProcessVacationRecordsStartDate) == false)
                {
                    timesheetProcessingProcessVacationRecordsStartDate = DateTime.Parse(_timesheetConfig.ProcessingProcessVacationRecordsStartDate);
                }
            }
            catch (Exception)
            {
                timesheetProcessingProcessVacationRecordsStartDate = DateTime.MinValue;
            }


            try
            {
                var records = _tsAutoHoursRecordService.Get(r => r.ToList()).ToList();
                foreach (var item in records)
                {
                    var projectDateEnd = item.Project.EndDate < item.EndDate ? item.Project.EndDate : item.EndDate;

                    if (item.BeginDate <= projectDateEnd
                        && item.BeginDate < timesheetProcessingProcessVacationRecordsStartDate)
                    {
                        PERMANENT_PROJECTSRecord ppr = new PERMANENT_PROJECTSRecord
                        {
                            PROJECT_SHORT_NAME = item.Project.ShortName,
                            PROJECT_DATE_END = projectDateEnd?.ToString("MM/dd/yyyy").Replace(".", "/"),
                            LNAME = item.Employee.LastName,
                            FNAME = item.Employee.FirstName,
                            PATRONYMIC = item.Employee.MidName,
                            tsAutoHoursRecord_BeginDate = item.BeginDate,
                            tsAutoHoursRecord_EndDate = (timesheetProcessingProcessVacationRecordsStartDate > DateTime.MinValue
                                && item.EndDate >= timesheetProcessingProcessVacationRecordsStartDate) ? timesheetProcessingProcessVacationRecordsStartDate.AddDays(-1) : item.EndDate,
                            tsAutoHoursRecord_DayHours = item.DayHours,
                        };

                        PERMANENT_PROJECTS.Add(ppr);

                        try
                        {
                            var project = item.Project;
                            projectsDict[item.Project.ShortName] = new PROJECTSRecord()
                            {
                                PROJECT_SHORT_NAME = project.ShortName,
                                DATE_END = project.EndDate?.ToString("MM/dd/yyyy").Replace(".", "/"),
                                PROJECT_ID = project.ID.ToString(),
                                PROJECT_NAME = project.FullName
                            };
                        }
                        catch (Exception e)
                        {
                            task.SetStatus(-1, "Ошибка: " + e.Message + ", TSAutoHoursRecord.ID = " + item.ID);
                        }
                    }
                }

                foreach (var item in PROJECTS)
                {
                    var key = (item as PROJECTSRecord).PROJECT_SHORT_NAME;
                    if (projectsDict.ContainsKey(key))
                        projectsDict.Remove(key);
                }
                PROJECTS.AddRange(projectsDict.Values);
            }
            catch (Exception e)
            {
                task.SetStatus(-1, "Ошибка: " + e.Message);
            }
        }

        public void GetDataFromTSHoursRecords(LongRunningTaskBase task, DateTime periodStart, DateTime periodEnd,
            bool useTSHoursRecordsOnly)
        {
            var projectsDict = new Dictionary<string, PROJECTSRecord>();
            try
            {
                task.SetStatus(55, "Получение данных из ТШ");

                List<TSHoursRecord> records = null;

                if (useTSHoursRecordsOnly == true)
                {
                    records = _tsHoursRecordService.Get(r => r.Where(
                        x => (x.RecordStatus == TSRecordStatus.PMApproved || x.RecordStatus == TSRecordStatus.HDApproved)
                             && x.RecordDate >= periodStart && x.RecordDate <= periodEnd).ToList()).ToList();
                }
                else
                {
                    records = _tsHoursRecordService.Get(r => r.Where(
                        x => (x.RecordStatus == TSRecordStatus.PMApproved || x.RecordStatus == TSRecordStatus.HDApproved)
                             && x.RecordDate >= periodStart && x.RecordDate <= periodEnd
                             && x.RecordSource != TSRecordSource.ExternalTS
                             && x.RecordSource != TSRecordSource.AutoPercentAssign
                             /*&& x.RecordSource != TSRecordSource.Vacantion*/).ToList()).ToList();
                }

                foreach (var record in records)
                {
                    try
                    {
                        if (record.Hours != null)
                        {
                            HOURS.Add(new HOURSRecord
                            {
                                REC_ID = record.ID.ToString(),
                                USER_ID = record.EmployeeID.ToString(),
                                PROJECT_ID = record.ProjectID.ToString(),
                                PROJECT_SHORT_NAME = record.Project.ShortName,
                                REC_DATE = record.RecordDate?.ToString("MM/dd/yyyy").Replace(".", "/"),
                                LNAME = (record.Employee.LastName != null) ? record.Employee.LastName : "",
                                FNAME = (record.Employee.FirstName != null) ? record.Employee.FirstName : "",
                                PATRONYMIC = (record.Employee.MidName != null) ? record.Employee.MidName : "",
                                EMAIL = (record.Employee.Email != null) ? record.Employee.Email : "",
                                AMOUNT = (int)(record.Hours.Value * 10d), // какой то треш, видимо изначально была завязка на инт, поэтому сдвигают десятые, что бы в отчете получить число с запятой
                                SENT = record.Created?.ToString("MM/dd/yyyy").Replace(".", "/")
                            });
                        }
                    }
                    catch (Exception e)
                    {
                        task.SetStatus(-1, "Ошибка: " + e.Message + ", TSHoursRecords.ID = " + record.ID);
                    }

                    try
                    {
                        var project = record.Project;
                        if (projectsDict.ContainsKey(record.Project.ShortName) == false)
                        {
                            projectsDict[record.Project.ShortName] = new PROJECTSRecord()
                            {
                                PROJECT_SHORT_NAME = project.ShortName,
                                DATE_END = project.EndDate?.ToString("MM/dd/yyyy").Replace(".", "/"),
                                PROJECT_ID = project.ID.ToString(),
                                PROJECT_NAME = project.FullName
                            };
                        }
                    }
                    catch (Exception e)
                    {
                        task.SetStatus(-1, "Ошибка: " + e.Message + ", TSHoursRecord.ID = " + record.ID);
                    }
                }

                try
                {
                    var activeProjectList = _projectService.Get(x => x.Where(p => (p.BeginDate != null && p.BeginDate <= periodEnd) && (p.EndDate == null || p.EndDate >= periodStart)).ToList());

                    if (activeProjectList != null)
                    {
                        foreach (Project project in activeProjectList)
                        {
                            try
                            {
                                if (projectsDict.ContainsKey(project.ShortName) == false)
                                {
                                    projectsDict[project.ShortName] = new PROJECTSRecord()
                                    {
                                        PROJECT_SHORT_NAME = project.ShortName,
                                        DATE_END = project.EndDate?.ToString("MM/dd/yyyy").Replace(".", "/"),
                                        PROJECT_ID = project.ID.ToString(),
                                        PROJECT_NAME = project.FullName
                                    };
                                }
                            }
                            catch (Exception e)
                            {
                                task.SetStatus(-1, "Ошибка: " + e.Message);
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    task.SetStatus(-1, "Ошибка: " + e.Message);
                }

                foreach (var item in PROJECTS)
                {
                    var key = (item as PROJECTSRecord).PROJECT_SHORT_NAME;
                    if (projectsDict.ContainsKey(key))
                        projectsDict.Remove(key);
                }
                PROJECTS.AddRange(projectsDict.Values);
            }
            catch (Exception e)
            {
                task.SetStatus(-1, "Ошибка: " + e.Message);
            }
        }

        //public void GetDataFromTSHoursRecordsForProject(LongRunningTaskBase task, DateTime startPeriod, DateTime endPeriod, string projectShortName)
        //{
        //    var projectsDict = new Dictionary<string, PROJECTSRecord>();
        //    try
        //    {
        //        task.SetStatus(55, "Получение данных из ТШ");

        //        var records = _tsHoursRecordService.Get(r => r.Where(
        //            x => x.Project.ShortName.Equals(projectShortName)
        //                 && (x.RecordStatus == TSRecordStatus.PMApproved || x.RecordStatus == TSRecordStatus.HDApproved)
        //                 && x.RecordDate >= startPeriod && x.RecordDate <= endPeriod
        //                 && x.RecordSource != TSRecordSource.ExternalTS
        //                 && x.RecordSource != TSRecordSource.AutoPercentAssign
        //                 && x.RecordSource != TSRecordSource.Vacantion).ToList());

        //        foreach (var record in records)
        //        {
        //            try
        //            {
        //                if (record.Hours != null)
        //                {
        //                    HOURS.Add(new HOURSRecord
        //                    {
        //                        REC_ID = record.ID.ToString(),
        //                        USER_ID = record.EmployeeID.ToString(),
        //                        PROJECT_ID = record.ProjectID.ToString(),
        //                        PROJECT_SHORT_NAME = record.Project.ShortName,
        //                        REC_DATE = record.RecordDate?.ToString("MM/dd/yyyy").Replace(".", "/"),
        //                        DESCRIPTION = record.Description,
        //                        LNAME = (record.Employee.LastName != null) ? record.Employee.LastName : "",
        //                        FNAME = (record.Employee.FirstName != null) ? record.Employee.FirstName : "",
        //                        PATRONYMIC = (record.Employee.MidName != null) ? record.Employee.MidName : "",
        //                        EMAIL = (record.Employee.Email != null) ? record.Employee.Email : "",
        //                        AMOUNT = (int)(record.Hours.Value * 10d), // какой то треш, видимо изначально была завязка на инт, поэтому сдвигают десятые, что бы в отчете получить число с запятой
        //                        SENT = record.Created?.ToString("MM/dd/yyyy").Replace(".", "/")
        //                    });
        //                }
        //            }
        //            catch (Exception e)
        //            {
        //                task.SetStatus(-1, "Ошибка: " + e.Message + ", TSHoursRecords.ID = " + record.ID);
        //            }

        //            try
        //            {
        //                var project = record.Project;
        //                projectsDict[record.Project.ShortName] = new PROJECTSRecord()
        //                {
        //                    PROJECT_SHORT_NAME = project.ShortName,
        //                    DATE_END = project.EndDate?.ToString("MM/dd/yyyy").Replace(".", "/"),
        //                    PROJECT_ID = project.ID.ToString(),
        //                    PROJECT_NAME = project.FullName
        //                };
        //            }
        //            catch (Exception e)
        //            {
        //                task.SetStatus(-1, "Ошибка: " + e.Message + ", TSHoursRecords.ID = " + record.ID);
        //            }
        //        }

        //        foreach (var item in PROJECTS)
        //        {
        //            var key = (item as PROJECTSRecord).PROJECT_SHORT_NAME;
        //            if (projectsDict.ContainsKey(key))
        //                projectsDict.Remove(key);
        //        }
        //        PROJECTS.AddRange(projectsDict.Values);
        //    }
        //    catch (Exception e)
        //    {
        //        task.SetStatus(-1, "Ошибка: " + e.Message);
        //    }
        //}

        public bool GetDataFromTimeSheetDB(LongRunningTaskBase task, string dateBegin, string dateEnd, int monthCount, bool getVacations)
        {
            bool result = false;

            string connectionString = GetConnectionString();
            try
            {
                using (OracleConnection connection = new OracleConnection())
                {
                    connection.ConnectionString = connectionString;
                    connection.Open();
                    try
                    {
                        task.SetStatus(1, "Состояние подключения к БД: " + connection.State);

                        task.SetStatus(1, "Старт получения списка проектов...");
                        createPROJECTS(task, connection);
                        task.SetStatus(2, "Прочитано из БД проектов:" + PROJECTS.Count);


                        task.SetStatus(2, "Старт получения данных автозагрузки...");
                        createPERMANENT_PROJECTS(task, connection);
                        task.SetStatus(3, "Прочитано из БД записей автозагрузки:" + PERMANENT_PROJECTS.Count);

                        if (getVacations == true)
                        {
                            task.SetStatus(3, "Старт получения данных отпусков...");
                            createVACATIONS(task, connection, dateBegin, dateEnd);
                            task.SetStatus(4, "Прочитано из БД записей отпусков:" + VACATIONS.Count);
                        }

                        task.SetStatus(5, "Старт получения записей о трудозатратах...");
                        createHOURS(task, connection, dateBegin, dateEnd, monthCount);
                        task.SetStatus(50, "Прочитано из БД записей о трудозатратах:" + HOURS.Count);

                    }
                    catch (Exception e)
                    {
                        task.SetStatus(-1, "Ошибка: " + e.Message);
                    }

                    connection.Close();

                    result = true;
                }
            }
            catch (Exception e)
            {
                task.SetStatus(-1, "Ошибка: " + e.Message);
            }

            return result;
        }

        public void GetProjectsFromTimeSheetDB(LongRunningTaskBase task)
        {
            string connectionString = GetConnectionString();
            try
            {
                using (OracleConnection connection = new OracleConnection())
                {
                    connection.ConnectionString = connectionString;
                    connection.Open();
                    try
                    {
                        task.SetStatus(1, "State: " + connection.State);

                        task.SetStatus(1, "Старт получения списка проектов...");
                        createPROJECTS(task, connection);
                        task.SetStatus(50, "Прочитано из БД проектов:" + PROJECTS.Count);
                    }
                    catch (Exception e)
                    {
                        task.SetStatus(-1, "Ошибка: " + e.Message);
                    }

                    connection.Close();
                }
            }
            catch (Exception e)
            {
                task.SetStatus(-1, "Ошибка: " + e.Message);
            }
        }

        public void GetDataFromTimeSheetDBForPM(LongRunningTaskBase task, string dateBegin, string dateEnd,
            ArrayList projectShortNames, int monthCount)
        {
            string connectionString = GetConnectionString();
            try
            {
                using (OracleConnection connection = new OracleConnection())
                {
                    connection.ConnectionString = connectionString;
                    connection.Open();
                    try
                    {
                        task.SetStatus(0, "State: " + connection.State);

                        task.SetStatus(0, "Старт получения списка проектов из Timesheet...");
                        createPROJECTSForPM(task, connection, projectShortNames);
                        task.SetStatus(5, "Старт получения записей о трудозатратах из Timesheet...");
                        createHOURSForPM(task, connection, dateBegin, dateEnd, monthCount);
                        task.SetStatus(50, "Прочитано из БД Timesheet записей о трудозатратах:" + HOURS.Count);
                        //createVACATIONS(connection);
                    }
                    catch (Exception e)
                    {
                        task.SetStatus(-1, "Ошибка: " + e.Message);
                    }

                    connection.Close();
                }
            }
            catch (Exception e)
            {
                task.SetStatus(-1, "Ошибка: " + e.Message);
            }
        }

        private DataTable AddDoubleValueToCell(DataTable dataTable, int rowIndex, string columnName,
            double value)
        {

            if (dataTable.Rows[rowIndex][columnName] == null
                || String.IsNullOrEmpty(dataTable.Rows[rowIndex][columnName].ToString()))
            {
                dataTable.Rows[rowIndex][columnName] = value;
            }
            else
            {
                double currentValue = Convert.ToDouble(dataTable.Rows[rowIndex][columnName]);
                dataTable.Rows[rowIndex][columnName] = currentValue + value;
            }

            return dataTable;
        }

        private int GetPermanentProjectHoursForPeriod(Employee employeeItem, PERMANENT_PROJECTSRecord ppr,
            DateTime periodStart, DateTime periodEnd,
            int periodWorkHours)
        {
            int pprHOURS = 0;
            double pprMonthHoursRatio = 1;
            DateTime employeeAutoHoursBeginDate = DateTime.MinValue;
            DateTime employeeAutoHoursEndDate = DateTime.MaxValue;

            if (ppr.tsAutoHoursRecord_DayHours.HasValue)
            {
                employeeAutoHoursBeginDate = ppr.tsAutoHoursRecord_BeginDate.Value;
                employeeAutoHoursEndDate = ppr.tsAutoHoursRecord_EndDate.Value;
            }
            else
            {
                try
                {
                    if (!String.IsNullOrEmpty(ppr.HIRED))
                        employeeAutoHoursBeginDate = DateTime.ParseExact(ppr.HIRED, "MM/dd/yyyy", CultureInfo.InvariantCulture);

                }
                catch (Exception)
                {
                    employeeAutoHoursBeginDate = DateTime.MinValue;
                }

                try
                {

                    if (!String.IsNullOrEmpty(ppr.TERMINATED))
                        employeeAutoHoursEndDate = DateTime.ParseExact(ppr.TERMINATED, "MM/dd/yyyy", CultureInfo.InvariantCulture);
                }
                catch (Exception)
                {
                    employeeAutoHoursEndDate = DateTime.MaxValue;
                }
            }
            //Важно, чтобы дата начала и окончания автозагрузки не выоходила за период работы сотрудника
            if (employeeItem != null
                && (ppr.tsAutoHoursRecord_DayHours == null
                    || ppr.tsAutoHoursRecord_DayHours.HasValue == false))
            {
                if (employeeItem.EnrollmentDate != null
                    && employeeItem.EnrollmentDate.HasValue == true
                    && employeeItem.EnrollmentDate.Value > employeeAutoHoursBeginDate)
                {
                    employeeAutoHoursBeginDate = employeeItem.EnrollmentDate.Value;
                }

                if (employeeItem.DismissalDate != null
                    && employeeItem.DismissalDate.HasValue == true
                    && employeeItem.DismissalDate.Value < employeeAutoHoursEndDate)
                {
                    employeeAutoHoursEndDate = employeeItem.DismissalDate.Value;
                }
            }

            pprMonthHoursRatio = GetPPRMonthHoursRatio(ppr);
            pprHOURS = (int)Math.Round(pprMonthHoursRatio * 10d * (double)periodWorkHours);

            if (employeeAutoHoursBeginDate > periodEnd)
            {
                pprHOURS = 0;
            }
            else if (employeeAutoHoursEndDate < periodStart)
            {
                pprHOURS = 0;
            }
            else
            {
                DateTime pprStartDate = periodStart;
                DateTime pprEndDate = periodEnd;
                bool needCorrectHours = false;

                if (employeeAutoHoursBeginDate > periodStart)
                {
                    pprStartDate = employeeAutoHoursBeginDate;
                    needCorrectHours = true;
                }

                if (employeeAutoHoursEndDate < periodEnd)
                {
                    pprEndDate = employeeAutoHoursEndDate;
                    needCorrectHours = true;
                }

                if (needCorrectHours == true)
                {
                    pprHOURS = (int)Math.Round(pprMonthHoursRatio * 10d * (double)_productionCalendarService.GetWorkHoursBetweenDates(pprStartDate,
                        pprEndDate));
                }
            }

            return pprHOURS;
        }

        private double GetPPRMonthHoursRatio(PERMANENT_PROJECTSRecord ppr)
        {
            double pprMonthHoursRatio;
            if (ppr.tsAutoHoursRecord_DayHours.HasValue)
                return ppr.tsAutoHoursRecord_DayHours.Value / 8;

            if (ppr.HOURS == 1600)
            {
                pprMonthHoursRatio = 1;
            }
            else
            {
                pprMonthHoursRatio = (double)ppr.HOURS / (double)1600;
            }
            return pprMonthHoursRatio;
        }

        protected static Employee GetEmployeeByTimesheetNames(List<Employee> employeeList, string lastName, string firstName, string midName)
        {
            Employee employeeItem = null;

            if (String.IsNullOrEmpty(midName) == true
                || midName.ToLower().Trim().Equals("null") == true)
            {
                employeeItem = employeeList.Where(e => e.LastName == lastName && e.FirstName == firstName).FirstOrDefault();
            }
            else
            {
                employeeItem = employeeList.Where(e => e.LastName == lastName && e.FirstName == firstName && e.MidName == midName).FirstOrDefault();
            }

            return employeeItem;
        }

        private string GetEmployeeFullNameByTimesheetNames(List<Employee> employeeList, string lastName, string firstName, string midName)
        {
            string employeeName = "";

            if (String.IsNullOrEmpty(midName) == true
                || midName.ToLower().Trim().Equals("null") == true)
            {
                Employee employeeItem = employeeList.Where(e => e.LastName == lastName && e.FirstName == firstName).FirstOrDefault();

                if (employeeItem != null)
                {
                    employeeName = employeeItem.FullName;
                }
                else
                {
                    employeeName = lastName + " " + firstName;
                }
            }
            else
            {
                employeeName = lastName + " " + firstName + " " + midName;
            }

            return employeeName;
        }

        private bool IsEmployeeOfPermanentProjectWorkInPeriod(PERMANENT_PROJECTSRecord ppr,
            DateTime periodStart, DateTime periodEnd)
        {
            bool result = false;
            DateTime employeeEnrollmentDate = DateTime.MinValue;
            DateTime employeeDismissalDate = DateTime.MaxValue;

            try
            {
                if (String.IsNullOrEmpty(ppr.HIRED) == false)
                {
                    employeeEnrollmentDate = DateTime.ParseExact(ppr.HIRED, "MM/dd/yyyy", CultureInfo.InvariantCulture);
                }
            }
            catch (Exception)
            {
                employeeEnrollmentDate = DateTime.MinValue;
            }

            try
            {
                if (String.IsNullOrEmpty(ppr.TERMINATED) == false)
                {
                    employeeDismissalDate = DateTime.ParseExact(ppr.TERMINATED, "MM/dd/yyyy", CultureInfo.InvariantCulture);
                }
            }
            catch (Exception)
            {
                employeeDismissalDate = DateTime.MaxValue;
            }

            if (employeeEnrollmentDate > periodEnd
                || employeeDismissalDate < periodStart)
            {
                result = false;
            }
            else
            {
                result = true;
            }


            return result;
        }

        private DateTime GetPermanentProjectDateEnd(PERMANENT_PROJECTSRecord ppr)
        {
            DateTime permanentProjectDateEnd = DateTime.MaxValue;

            try
            {
                if (String.IsNullOrEmpty(ppr.PROJECT_DATE_END) == false)
                {
                    permanentProjectDateEnd = DateTime.ParseExact(ppr.PROJECT_DATE_END, "MM/dd/yyyy", CultureInfo.InvariantCulture);
                }
            }
            catch (Exception)
            {
                permanentProjectDateEnd = DateTime.MaxValue;
            }

            return permanentProjectDateEnd;
        }

        private double GetWorkHoursCost(double employeePayrollValue, HOURSRecord hr)
        {
            var hoursCost = 0.0;
            if (employeePayrollValue > 0)
            {
                DateTime recDate = DateTime.ParseExact(hr.REC_DATE, "MM/dd/yyyy", CultureInfo.InvariantCulture);
                if (_financeService.GetProjectEmployeePayrollCostsCalcMethod() == "CalcByPercentageActualHours")
                {

                    var employeePeriodHours = GetEmployeeHoursSamePeriod(hr, recDate);
                    hoursCost = employeePayrollValue * (double)hr.AMOUNT / employeePeriodHours;
                }
                else
                {

                    hoursCost = employeePayrollValue * (double)hr.AMOUNT / _productionCalendarService.GetMonthWorkHours(recDate.Month, recDate.Year) / 10d;
                }
            }
            else
            {
                hoursCost = (-employeePayrollValue) * (double)hr.AMOUNT / 10d;
            }
            return hoursCost;
        }

        private double GetEmployeeHoursSamePeriod(HOURSRecord hr, DateTime hrRecDate)
        {
            var result = 0.0;
            foreach (HOURSRecord item in HOURS)
            {
                if (item.LNAME != hr.LNAME || item.FNAME != hr.FNAME)
                    continue;

                DateTime recDate;
                DateTime.TryParseExact(item.REC_DATE, "MM/dd/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out recDate);
                if (recDate == DateTime.MinValue || recDate.Month != hrRecDate.Month || recDate.Year != hrRecDate.Year)
                    continue;

                result += item.AMOUNT;
            }
            return result;
        }

        private double GetPermamentWorkHoursCost(double employeePayrollValue, double pprHOURS, PERMANENT_PROJECTSRecord ppr, int monthWorkHours, Employee employee, DateTime monthStartDate, DateTime monthEndDate)
        {
            var result = 0.0;
            if (employeePayrollValue > 0)
            {
                if (_financeService.GetProjectEmployeePayrollCostsCalcMethod() == "CalcByPercentageActualHours")
                {
                    var summaryHours = 0.0;
                    foreach (PERMANENT_PROJECTSRecord item in PERMANENT_PROJECTS)
                    {
                        if (item.FNAME != ppr.FNAME || item.LNAME != ppr.LNAME || item.PATRONYMIC != ppr.PATRONYMIC)
                            continue;
                        summaryHours += GetPermanentProjectHoursForPeriod(employee, item, monthStartDate, monthEndDate, monthWorkHours);
                    }
                    if (summaryHours != 0.0)
                        result = employeePayrollValue * (double)pprHOURS / summaryHours / 10d;
                }
                else
                    result = employeePayrollValue * (double)pprHOURS / monthWorkHours / 10d;
            }
            else
            {
                result = (-employeePayrollValue) * (double)pprHOURS / 10d;
            }

            return result;
        }

        public byte[] GetProjectsConsolidatedHoursReportExcel(LongRunningTaskBase task, string userIdentityName, string reportPeriodName, string reportTitle,
           bool saveResultsInDB,
           DataTable employeePayrollSheetDataTable, DataTable projectsOtherCostsSheetDataTable,
           DateTime periodStart, DateTime periodEnd,
           List<int> departmentsIDs)
        {
            byte[] binData = null;

            var currentEmployeeList = _employeeService.GetCurrentEmployees(new DateTimeRange(periodStart, periodEnd)).ToList();


            List<Employee> employeeList = null;

            if (departmentsIDs == null)
            {
                employeeList = currentEmployeeList.Where(e => e.Department != null).ToList();
            }
            else
            {
                employeeList = currentEmployeeList.Where(e => e.Department != null && departmentsIDs.Contains(e.Department.ID)).ToList();
            }

            employeeList = employeeList.OrderBy(e => e.Department.ShortName + (e.Department.DepartmentManager != e).ToString() + e.FullName).ToList();
            DataTable dataTableHours = new DataTable();
            dataTableHours.Columns.Add("ProjectShortName", typeof(string)).Caption = "Код проекта";
            dataTableHours.Columns["ProjectShortName"].ExtendedProperties["Width"] = (double)30;
            dataTableHours.Columns.Add("ProjectTotal", typeof(double)).Caption = "Итого (ч)";
            dataTableHours.Columns["ProjectTotal"].ExtendedProperties["Width"] = (double)16;

            DataTable dataTablePayroll = new DataTable();
            dataTablePayroll.Columns.Add("ProjectShortName", typeof(string)).Caption = "Код проекта";
            dataTablePayroll.Columns["ProjectShortName"].ExtendedProperties["Width"] = (double)30;
            dataTablePayroll.Columns.Add("ProjectTotal", typeof(double)).Caption = "Итого ФОТ";
            dataTablePayroll.Columns["ProjectTotal"].ExtendedProperties["Width"] = (double)16;

            DataTable dataTableOvertimePayroll = new DataTable();
            dataTableOvertimePayroll.Columns.Add("ProjectShortName", typeof(string)).Caption = "Код проекта";
            dataTableOvertimePayroll.Columns["ProjectShortName"].ExtendedProperties["Width"] = (double)30;
            dataTableOvertimePayroll.Columns.Add("ProjectTotal", typeof(double)).Caption = "Итого СУ";
            dataTableOvertimePayroll.Columns["ProjectTotal"].ExtendedProperties["Width"] = (double)16;

            DataTable dataTablePerformanceBonus = new DataTable();
            dataTablePerformanceBonus.Columns.Add("ProjectShortName", typeof(string)).Caption = "Код проекта";
            dataTablePerformanceBonus.Columns["ProjectShortName"].ExtendedProperties["Width"] = (double)30;
            dataTablePerformanceBonus.Columns.Add("ProjectTotal", typeof(double)).Caption = "Итого Perf Bonus";
            dataTablePerformanceBonus.Columns["ProjectTotal"].ExtendedProperties["Width"] = (double)16;

            DataTable dataTableOtherCosts = new DataTable();
            dataTableOtherCosts.Columns.Add("ProjectShortName", typeof(string)).Caption = "Код проекта";
            dataTableOtherCosts.Columns["ProjectShortName"].ExtendedProperties["Width"] = (double)30;
            dataTableOtherCosts.Columns.Add("ProjectTotal", typeof(double)).Caption = "Итого Daykassa";
            dataTableOtherCosts.Columns["ProjectTotal"].ExtendedProperties["Width"] = (double)16;

            DataTable dataTableTotalCosts = new DataTable();
            dataTableTotalCosts.Columns.Add("ProjectShortName", typeof(string)).Caption = "Код проекта";
            dataTableTotalCosts.Columns["ProjectShortName"].ExtendedProperties["Width"] = (double)30;
            dataTableTotalCosts.Columns.Add("ProjectTotal", typeof(double)).Caption = "Итого затраты";
            dataTableTotalCosts.Columns["ProjectTotal"].ExtendedProperties["Width"] = (double)16;

            int periodMonthCount = ((periodEnd.Year - periodStart.Year) * 12) + periodEnd.Month - periodStart.Month + 1;

            for (int i = 0; i < periodMonthCount; i++)
            {
                int monthStartYear = periodStart.Year + ((periodStart.Month + i - 1) / 12);
                int monthStartMonth = ((periodStart.Month + i - 1) % 12) + 1;
                DateTime monthStartDate = new DateTime(monthStartYear, monthStartMonth, 1);
                string monthId = monthStartDate.ToString("MM_yyyy");

                dataTableHours.Columns.Add("Month" + monthId, typeof(double)).Caption = monthStartDate.ToString("MMMM yyyy");
                dataTableHours.Columns["Month" + monthId].ExtendedProperties["Width"] = (double)16;

                dataTablePayroll.Columns.Add("Month" + monthId, typeof(double)).Caption = monthStartDate.ToString("MMMM yyyy");
                dataTablePayroll.Columns["Month" + monthId].ExtendedProperties["Width"] = (double)16;

                dataTableOvertimePayroll.Columns.Add("Month" + monthId, typeof(double)).Caption = monthStartDate.ToString("MMMM yyyy");
                dataTableOvertimePayroll.Columns["Month" + monthId].ExtendedProperties["Width"] = (double)16;

                dataTablePerformanceBonus.Columns.Add("Month" + monthId, typeof(double)).Caption = monthStartDate.ToString("MMMM yyyy");
                dataTablePerformanceBonus.Columns["Month" + monthId].ExtendedProperties["Width"] = (double)16;

                dataTableOtherCosts.Columns.Add("Month" + monthId, typeof(double)).Caption = monthStartDate.ToString("MMMM yyyy");
                dataTableOtherCosts.Columns["Month" + monthId].ExtendedProperties["Width"] = (double)16;

                dataTableTotalCosts.Columns.Add("Month" + monthId, typeof(double)).Caption = monthStartDate.ToString("MMMM yyyy");
                dataTableTotalCosts.Columns["Month" + monthId].ExtendedProperties["Width"] = (double)16;
            }

            dataTableHours.Columns.Add("_ISGROUPROW_", typeof(bool)).Caption = "_ISGROUPROW_";
            dataTablePayroll.Columns.Add("_ISGROUPROW_", typeof(bool)).Caption = "_ISGROUPROW_";
            dataTableOvertimePayroll.Columns.Add("_ISGROUPROW_", typeof(bool)).Caption = "_ISGROUPROW_";
            dataTablePerformanceBonus.Columns.Add("_ISGROUPROW_", typeof(bool)).Caption = "_ISGROUPROW_";
            dataTableOtherCosts.Columns.Add("_ISGROUPROW_", typeof(bool)).Caption = "_ISGROUPROW_";
            dataTableTotalCosts.Columns.Add("_ISGROUPROW_", typeof(bool)).Caption = "_ISGROUPROW_";

            SortedDictionary<string, double> notInDBEmplyeesList = new SortedDictionary<string, double>();

            SortedDictionary<string, string> projectShortNameList = new SortedDictionary<string, string>();

            Dictionary<string, double> employeeMonthHoursList = new Dictionary<string, double>();

            for (int k = 0; k < HOURS.Count; k++)
            {
                HOURSRecord hr = HOURS[k] as HOURSRecord;
                string employeeName = GetEmployeeFullNameByTimesheetNames(employeeList, hr.LNAME, hr.FNAME, hr.PATRONYMIC);

                try
                {
                    DateTime recDate = DateTime.ParseExact(hr.REC_DATE, "MM/dd/yyyy", CultureInfo.InvariantCulture);
                    string monthId = recDate.ToString("MM_yyyy");

                    if (employeeMonthHoursList.ContainsKey(employeeName + "_" + monthId) == false)
                    {
                        employeeMonthHoursList.Add(employeeName + "_" + monthId, (double)hr.AMOUNT / 10d);
                    }
                    else
                    {
                        double currentEmployeeMonthHours = employeeMonthHoursList[employeeName + "_" + monthId];
                        employeeMonthHoursList[employeeName + "_" + monthId] = currentEmployeeMonthHours + (double)hr.AMOUNT / 10d;
                    }
                }
                catch (Exception)
                {

                }


                bool employeeFoundInDB = false;

                foreach (Employee employee in employeeList)
                {
                    string name = employee.LastName + " " + employee.FirstName + " " + employee.MidName;
                    if (employeeName.Trim().ToLower().Equals(name.Trim().ToLower()) == true)
                    {
                        if (projectShortNameList.ContainsValue(hr.PROJECT_SHORT_NAME) == false)
                        {
                            string projectShortName = hr.PROJECT_SHORT_NAME;
                            string projectSortKey = ProjectHelper.GetProjectSortKeyByProjectShortName(projectShortName);

                            projectShortNameList.Add(projectSortKey, projectShortName);
                        }
                        employeeFoundInDB = true;
                        break;
                    }
                }

                if (employeeFoundInDB == false)
                {
                    if (notInDBEmplyeesList.ContainsKey(employeeName) == false)
                    {
                        notInDBEmplyeesList.Add(employeeName, 0);
                    }

                    notInDBEmplyeesList[employeeName] = (double)notInDBEmplyeesList[employeeName] + (double)hr.AMOUNT / 10d;
                }
            }

            for (int k = 0; k < PERMANENT_PROJECTS.Count; k++)
            {
                PERMANENT_PROJECTSRecord ppr = PERMANENT_PROJECTS[k] as PERMANENT_PROJECTSRecord;
                string employeeName = GetEmployeeFullNameByTimesheetNames(employeeList, ppr.LNAME, ppr.FNAME, ppr.PATRONYMIC);

                //bool employeeFoundInDB = false;

                foreach (Employee employee in employeeList)
                {
                    string name = employee.LastName + " " + employee.FirstName + " " + employee.MidName;
                    if (employeeName.Trim().ToLower().Equals(name.Trim().ToLower()) == true)
                    {
                        if (projectShortNameList.ContainsValue(ppr.PROJECT_SHORT_NAME) == false)
                        {
                            string projectShortName = ppr.PROJECT_SHORT_NAME;
                            string projectSortKey = ProjectHelper.GetProjectSortKeyByProjectShortName(projectShortName);

                            projectShortNameList.Add(projectSortKey, projectShortName);
                        }
                        //employeeFoundInDB = true;
                        break;
                    }
                }

                /*if(employeeFoundInDB == false)
                {
                    if(notInDBEmplyeesList.ContainsKey(employeeName) == false)
                    {
                        notInDBEmplyeesList.Add(employeeName, 0);
                    }

                    int pprHOURS = 0;

                    if (ppr.HOURS == 1600)
                    {
                        pprHOURS = monthWorkHours * 10;
                    }
                    else
                    {
                        pprHOURS = ppr.HOURS;
                    }

                    notInDBEmplyeesList[employeeName] = notInDBEmplyeesList[employeeName] + (double)pprHOURS / 10d;
                }*/
            }

            dataTableHours.Rows.Add("");
            dataTableHours.Rows[dataTableHours.Rows.Count - 1]["_ISGROUPROW_"] = true;
            dataTablePayroll.Rows.Add("");
            dataTablePayroll.Rows[dataTablePayroll.Rows.Count - 1]["_ISGROUPROW_"] = true;
            dataTableOvertimePayroll.Rows.Add("");
            dataTableOvertimePayroll.Rows[dataTablePayroll.Rows.Count - 1]["_ISGROUPROW_"] = true;
            dataTablePerformanceBonus.Rows.Add("");
            dataTablePerformanceBonus.Rows[dataTablePayroll.Rows.Count - 1]["_ISGROUPROW_"] = true;
            dataTableOtherCosts.Rows.Add("");
            dataTableOtherCosts.Rows[dataTablePayroll.Rows.Count - 1]["_ISGROUPROW_"] = true;
            dataTableTotalCosts.Rows.Add("");
            dataTableTotalCosts.Rows[dataTablePayroll.Rows.Count - 1]["_ISGROUPROW_"] = true;

            int j = 1;
            foreach (var projectShortNameListItem in projectShortNameList)
            {
                task.SetStatus(55 + 40 * j / projectShortNameList.Count, "Расчет для проекта: " + projectShortNameListItem.Value);

                dataTableHours.Rows.Add(projectShortNameListItem.Value);
                dataTablePayroll.Rows.Add(projectShortNameListItem.Value);
                dataTableOvertimePayroll.Rows.Add(projectShortNameListItem.Value);
                dataTablePerformanceBonus.Rows.Add(projectShortNameListItem.Value);
                dataTableOtherCosts.Rows.Add(projectShortNameListItem.Value);
                dataTableTotalCosts.Rows.Add(projectShortNameListItem.Value);

                for (int k = 0; k < HOURS.Count; k++)
                {
                    HOURSRecord hr = HOURS[k] as HOURSRecord;
                    string employeeName = GetEmployeeFullNameByTimesheetNames(employeeList, hr.LNAME, hr.FNAME, hr.PATRONYMIC);

                    if (projectShortNameListItem.Value.Equals(hr.PROJECT_SHORT_NAME) == true)
                    {
                        try
                        {
                            Employee employeeItem = employeeList.Where(e => e.FullName.Equals(employeeName)).FirstOrDefault();

                            DateTime recDate = DateTime.ParseExact(hr.REC_DATE, "MM/dd/yyyy", CultureInfo.InvariantCulture);
                            string monthHoursColumnName = "Month" + recDate.ToString("MM_yyyy");

                            if (employeeItem != null)
                            {
                                dataTableHours = AddDoubleValueToCell(dataTableHours,
                                    0, monthHoursColumnName,
                                    (double)hr.AMOUNT / 10d);

                                dataTableHours = AddDoubleValueToCell(dataTableHours,
                                    dataTableHours.Rows.Count - 1, monthHoursColumnName,
                                    (double)hr.AMOUNT / 10d);

                                dataTableHours = AddDoubleValueToCell(dataTableHours,
                                    dataTableHours.Rows.Count - 1, "ProjectTotal",
                                    (double)hr.AMOUNT / 10d);

                                dataTableHours = AddDoubleValueToCell(dataTableHours,
                                    0, "ProjectTotal",
                                    (double)hr.AMOUNT / 10d);

                                if (employeePayrollSheetDataTable != null
                                    && ProjectHelper.IsNoPaidProject(hr.PROJECT_SHORT_NAME) == false)
                                {
                                    double employeePayrollValue = _financeService.GetEmployeePayrollOnDate(employeePayrollSheetDataTable, employeeItem, recDate, true,
                                        _employeeCategoryService.Get(x => x.Where(ec => ec.EmployeeID == employeeItem.ID).OrderByDescending(ec => ec.CategoryDateBegin).ToList()));
                                    double hoursCost = 0;

                                    if (employeePayrollValue != 0)
                                    {
                                        hoursCost = GetWorkHoursCost(employeePayrollValue, hr);

                                        string monthPayrollColumnName = "Month" + recDate.ToString("MM_yyyy");

                                        dataTablePayroll = AddDoubleValueToCell(dataTablePayroll,
                                            0, monthPayrollColumnName,
                                            hoursCost);

                                        dataTablePayroll = AddDoubleValueToCell(dataTablePayroll,
                                            dataTablePayroll.Rows.Count - 1, monthPayrollColumnName,
                                            hoursCost);

                                        dataTablePayroll = AddDoubleValueToCell(dataTablePayroll,
                                            dataTablePayroll.Rows.Count - 1, "ProjectTotal",
                                            hoursCost);

                                        dataTablePayroll = AddDoubleValueToCell(dataTablePayroll,
                                            0, "ProjectTotal",
                                            hoursCost);


                                        //Total costs
                                        dataTableTotalCosts = AddDoubleValueToCell(dataTableTotalCosts,
                                            0, monthPayrollColumnName,
                                            hoursCost);

                                        dataTableTotalCosts = AddDoubleValueToCell(dataTableTotalCosts,
                                            dataTableTotalCosts.Rows.Count - 1, monthPayrollColumnName,
                                            hoursCost);

                                        dataTableTotalCosts = AddDoubleValueToCell(dataTableTotalCosts,
                                            dataTableTotalCosts.Rows.Count - 1, "ProjectTotal",
                                            hoursCost);

                                        dataTableTotalCosts = AddDoubleValueToCell(dataTableTotalCosts,
                                            0, "ProjectTotal",
                                            hoursCost);
                                    }
                                }
                            }
                        }
                        catch (Exception)
                        {

                        }
                    }
                }

                for (int i = 0; i < periodMonthCount; i++)
                {
                    int monthStartYear = periodStart.Year + ((periodStart.Month + i - 1) / 12);
                    int monthStartMonth = ((periodStart.Month + i - 1) % 12) + 1;
                    DateTime monthStartDate = new DateTime(monthStartYear, monthStartMonth, 1);
                    DateTime monthEndDate = new DateTime(monthStartYear, monthStartMonth, DateTime.DaysInMonth(monthStartYear, monthStartMonth));

                    string monthId = monthStartDate.ToString("MM_yyyy");

                    int monthWorkHours = _productionCalendarService.GetMonthWorkHours(monthStartDate.Month, monthStartDate.Year);

                    for (int k = 0; k < PERMANENT_PROJECTS.Count; k++)
                    {
                        PERMANENT_PROJECTSRecord ppr = PERMANENT_PROJECTS[k] as PERMANENT_PROJECTSRecord;
                        string employeeName = GetEmployeeFullNameByTimesheetNames(employeeList, ppr.LNAME, ppr.FNAME, ppr.PATRONYMIC);

                        int permanentProjectYear = ProjectHelper.GetProjectYear(ppr.PROJECT_SHORT_NAME);
                        DateTime permanentProjectDateEnd = GetPermanentProjectDateEnd(ppr);

                        if (projectShortNameListItem.Value.Equals(ppr.PROJECT_SHORT_NAME) == true
                            && monthEndDate.Year >= permanentProjectYear
                            && monthStartDate <= permanentProjectDateEnd)
                        {
                            Employee employeeItem = employeeList.Where(e => e.FullName.Equals(employeeName)).FirstOrDefault();

                            if (employeeItem != null)
                            {
                                int pprHOURS = GetPermanentProjectHoursForPeriod(employeeItem,
                                    ppr, monthStartDate, monthEndDate,
                                    monthWorkHours);

                                string monthHoursColumnName = "Month" + monthId;

                                dataTableHours = AddDoubleValueToCell(dataTableHours,
                                    0, monthHoursColumnName,
                                    (double)pprHOURS / 10d);

                                dataTableHours = AddDoubleValueToCell(dataTableHours,
                                    dataTableHours.Rows.Count - 1, monthHoursColumnName,
                                    (double)pprHOURS / 10d);

                                dataTableHours = AddDoubleValueToCell(dataTableHours,
                                    dataTableHours.Rows.Count - 1, "ProjectTotal",
                                    (double)pprHOURS / 10d);

                                dataTableHours = AddDoubleValueToCell(dataTableHours,
                                    0, "ProjectTotal",
                                    (double)pprHOURS / 10d);


                                if (employeePayrollSheetDataTable != null
                                    && ProjectHelper.IsNoPaidProject(ppr.PROJECT_SHORT_NAME) == false)
                                {
                                    double employeePayrollValue = _financeService.GetEmployeePayrollOnDate(employeePayrollSheetDataTable, employeeItem, monthEndDate, false, null);

                                    double hoursCost = 0;

                                    if (employeePayrollValue != 0)
                                    {
                                        hoursCost = GetPermamentWorkHoursCost(employeePayrollValue, pprHOURS, ppr, monthWorkHours, employeeItem, monthStartDate, monthEndDate);
                                        string monthPayrollColumnName = "Month" + monthId;

                                        dataTablePayroll = AddDoubleValueToCell(dataTablePayroll,
                                            0, monthPayrollColumnName,
                                            hoursCost);

                                        dataTablePayroll = AddDoubleValueToCell(dataTablePayroll,
                                            dataTablePayroll.Rows.Count - 1, monthPayrollColumnName,
                                            hoursCost);

                                        dataTablePayroll = AddDoubleValueToCell(dataTablePayroll,
                                            dataTablePayroll.Rows.Count - 1, "ProjectTotal",
                                            hoursCost);

                                        dataTablePayroll = AddDoubleValueToCell(dataTablePayroll,
                                            0, "ProjectTotal",
                                            hoursCost);


                                        //Total costs
                                        dataTableTotalCosts = AddDoubleValueToCell(dataTableTotalCosts,
                                            0, monthPayrollColumnName,
                                            hoursCost);

                                        dataTableTotalCosts = AddDoubleValueToCell(dataTableTotalCosts,
                                            dataTableTotalCosts.Rows.Count - 1, monthPayrollColumnName,
                                            hoursCost);

                                        dataTableTotalCosts = AddDoubleValueToCell(dataTableTotalCosts,
                                            dataTableTotalCosts.Rows.Count - 1, "ProjectTotal",
                                            hoursCost);

                                        dataTableTotalCosts = AddDoubleValueToCell(dataTableTotalCosts,
                                            0, "ProjectTotal",
                                            hoursCost);
                                    }
                                }
                            }
                        }
                    }

                    if (employeePayrollSheetDataTable != null
                        && projectsOtherCostsSheetDataTable != null)
                    {
                        foreach (Employee employeeItem in employeeList)
                        {
                            string employeeName = employeeItem.FullName;

                            if (employeeMonthHoursList.ContainsKey(employeeName + "_" + monthId) == true)
                            {
                                double employeeMonthHours = employeeMonthHoursList[employeeName + "_" + monthId];

                                double employeeMonthOverHours = 0;

                                if (employeeMonthHours > monthWorkHours)
                                {
                                    employeeMonthOverHours = employeeMonthHours - (double)monthWorkHours;
                                }

                                double employeeOvertimePayrollValueForEmployee = _financeService.GetProjectEmployeeOvertimePayrollDeltaValueForEmployee(projectsOtherCostsSheetDataTable, employeePayrollSheetDataTable,
                                           projectShortNameListItem.Value,
                                           employeeItem,
                                           employeeMonthOverHours,
                                           monthWorkHours,
                                           monthStartDate, monthEndDate);

                                if (employeeOvertimePayrollValueForEmployee != 0)
                                {
                                    string monthOvertimePayrollColumnName = "Month" + monthId;

                                    dataTableOvertimePayroll = AddDoubleValueToCell(dataTableOvertimePayroll,
                                        0, monthOvertimePayrollColumnName,
                                        employeeOvertimePayrollValueForEmployee);

                                    dataTableOvertimePayroll = AddDoubleValueToCell(dataTableOvertimePayroll,
                                        dataTableOvertimePayroll.Rows.Count - 1, monthOvertimePayrollColumnName,
                                        employeeOvertimePayrollValueForEmployee);

                                    dataTableOvertimePayroll = AddDoubleValueToCell(dataTableOvertimePayroll,
                                        dataTableOvertimePayroll.Rows.Count - 1, "ProjectTotal",
                                        employeeOvertimePayrollValueForEmployee);

                                    dataTableOvertimePayroll = AddDoubleValueToCell(dataTableOvertimePayroll,
                                        0, "ProjectTotal",
                                        employeeOvertimePayrollValueForEmployee);


                                    //Total costs
                                    dataTableTotalCosts = AddDoubleValueToCell(dataTableTotalCosts,
                                         0, monthOvertimePayrollColumnName,
                                         employeeOvertimePayrollValueForEmployee);

                                    dataTableTotalCosts = AddDoubleValueToCell(dataTableTotalCosts,
                                        dataTableTotalCosts.Rows.Count - 1, monthOvertimePayrollColumnName,
                                        employeeOvertimePayrollValueForEmployee);

                                    dataTableTotalCosts = AddDoubleValueToCell(dataTableTotalCosts,
                                        dataTableTotalCosts.Rows.Count - 1, "ProjectTotal",
                                        employeeOvertimePayrollValueForEmployee);

                                    dataTableTotalCosts = AddDoubleValueToCell(dataTableTotalCosts,
                                        0, "ProjectTotal",
                                        employeeOvertimePayrollValueForEmployee);
                                }
                            }
                        }

                        double employeePerformanceBonusValue = _financeService.GetProjectEmployeePerformanceBonusValueForEmployee(projectsOtherCostsSheetDataTable,
                            projectShortNameListItem.Value,
                                null,
                                monthStartDate, monthEndDate);

                        if (employeePerformanceBonusValue != 0)
                        {
                            string monthPerformanceBonusColumnName = "Month" + monthId;

                            dataTablePerformanceBonus = AddDoubleValueToCell(dataTablePerformanceBonus,
                                 0, monthPerformanceBonusColumnName,
                                 employeePerformanceBonusValue);

                            dataTablePerformanceBonus = AddDoubleValueToCell(dataTablePerformanceBonus,
                                dataTablePerformanceBonus.Rows.Count - 1, monthPerformanceBonusColumnName,
                                employeePerformanceBonusValue);

                            dataTablePerformanceBonus = AddDoubleValueToCell(dataTablePerformanceBonus,
                                dataTablePerformanceBonus.Rows.Count - 1, "ProjectTotal",
                                employeePerformanceBonusValue);

                            dataTablePerformanceBonus = AddDoubleValueToCell(dataTablePerformanceBonus,
                                0, "ProjectTotal",
                                employeePerformanceBonusValue);


                            //Total costs
                            dataTableTotalCosts = AddDoubleValueToCell(dataTableTotalCosts,
                                 0, monthPerformanceBonusColumnName,
                                 employeePerformanceBonusValue);

                            dataTableTotalCosts = AddDoubleValueToCell(dataTableTotalCosts,
                                dataTableTotalCosts.Rows.Count - 1, monthPerformanceBonusColumnName,
                                employeePerformanceBonusValue);

                            dataTableTotalCosts = AddDoubleValueToCell(dataTableTotalCosts,
                                dataTableTotalCosts.Rows.Count - 1, "ProjectTotal",
                                employeePerformanceBonusValue);

                            dataTableTotalCosts = AddDoubleValueToCell(dataTableTotalCosts,
                                0, "ProjectTotal",
                                employeePerformanceBonusValue);
                        }

                        double otherCostsValue = _financeService.GetProjectOtherCostsValueForEmployee(projectsOtherCostsSheetDataTable,
                            projectShortNameListItem.Value,
                            null,
                            monthStartDate, monthEndDate);

                        if (otherCostsValue != 0)
                        {
                            string monthOtherCostsColumnName = "Month" + monthId;

                            dataTableOtherCosts = AddDoubleValueToCell(dataTableOtherCosts,
                                 0, monthOtherCostsColumnName,
                                 otherCostsValue);

                            dataTableOtherCosts = AddDoubleValueToCell(dataTableOtherCosts,
                                dataTableOtherCosts.Rows.Count - 1, monthOtherCostsColumnName,
                                otherCostsValue);

                            dataTableOtherCosts = AddDoubleValueToCell(dataTableOtherCosts,
                                dataTableOtherCosts.Rows.Count - 1, "ProjectTotal",
                                otherCostsValue);

                            dataTableOtherCosts = AddDoubleValueToCell(dataTableOtherCosts,
                                0, "ProjectTotal",
                                otherCostsValue);

                            //Total costs
                            dataTableTotalCosts = AddDoubleValueToCell(dataTableTotalCosts,
                                 0, monthOtherCostsColumnName,
                                 otherCostsValue);

                            dataTableTotalCosts = AddDoubleValueToCell(dataTableTotalCosts,
                                dataTableTotalCosts.Rows.Count - 1, monthOtherCostsColumnName,
                                otherCostsValue);

                            dataTableTotalCosts = AddDoubleValueToCell(dataTableTotalCosts,
                                dataTableTotalCosts.Rows.Count - 1, "ProjectTotal",
                                otherCostsValue);

                            dataTableTotalCosts = AddDoubleValueToCell(dataTableTotalCosts,
                                0, "ProjectTotal",
                                otherCostsValue);
                        }
                    }
                }

                j++;
            }

            task.SetStatus(98, "Формирование файла MS Excel...");

            using (MemoryStream stream = new MemoryStream())
            {
                using (SpreadsheetDocument doc = SpreadsheetDocument.Create(stream, SpreadsheetDocumentType.Workbook))
                {
                    string sheetsName = "";

                    if (employeePayrollSheetDataTable != null
                        && projectsOtherCostsSheetDataTable != null)
                    {
                        sheetsName = "Затраты (итого)|Ч-Ч|ФОТ|СУ|Perf Bonus|Daykassa";
                    }
                    else
                    {
                        sheetsName = "Ч-Ч";
                    }

                    WorkbookPart workbookPart = ExcelHelper.CreateWorkbookPart(doc, sheetsName);

                    int partIndex = 1;

                    if (employeePayrollSheetDataTable != null
                       && projectsOtherCostsSheetDataTable != null)
                    {
                        ExcelHelper.CreateWorksheetPartAndImportDataTable(workbookPart, "rId" + partIndex.ToString(), 1, 1, (uint)dataTableTotalCosts.Columns.Count,
                            reportTitle, dataTableTotalCosts, 3, 1);

                        partIndex++;
                    }

                    ExcelHelper.CreateWorksheetPartAndImportDataTable(workbookPart, "rId" + partIndex.ToString(), 1, 1, (uint)dataTableHours.Columns.Count,
                        reportTitle, dataTableHours, 3, 1);

                    partIndex++;

                    if (employeePayrollSheetDataTable != null
                        && projectsOtherCostsSheetDataTable != null)
                    {
                        ExcelHelper.CreateWorksheetPartAndImportDataTable(workbookPart, "rId" + partIndex.ToString(), 1, 1, (uint)dataTablePayroll.Columns.Count,
                            reportTitle, dataTablePayroll, 3, 1);

                        partIndex++;

                        ExcelHelper.CreateWorksheetPartAndImportDataTable(workbookPart, "rId" + partIndex.ToString(), 1, 1, (uint)dataTableOvertimePayroll.Columns.Count,
                            reportTitle, dataTableOvertimePayroll, 3, 1);

                        partIndex++;

                        ExcelHelper.CreateWorksheetPartAndImportDataTable(workbookPart, "rId" + partIndex.ToString(), 1, 1, (uint)dataTablePerformanceBonus.Columns.Count,
                            reportTitle, dataTablePerformanceBonus, 3, 1);

                        partIndex++;

                        ExcelHelper.CreateWorksheetPartAndImportDataTable(workbookPart, "rId" + partIndex.ToString(), 1, 1, (uint)dataTableOtherCosts.Columns.Count,
                            reportTitle, dataTableOtherCosts, 3, 1);

                        partIndex++;
                    }

                    doc.WorkbookPart.Workbook.Save();
                }

                stream.Position = 0;
                BinaryReader b = new BinaryReader(stream);
                binData = b.ReadBytes((int)stream.Length);

            }

            task.SetStatus(100, "Формирование файла MS Excel завершено");

            return binData;
        }

        private DataTable GetProjectHoursReportDataTable(LongRunningTaskBase task, DataTable dataTable,
            string userIdentityName, string reportPeriodName, int monthWorkHours,
            bool groupByMonth,
            bool saveResultsInDB,
            DataTable employeePayrollSheetDataTable, DataTable projectsOtherCostsSheetDataTable,
            DateTime periodStart, DateTime periodEnd,
            List<int> departmentsIDs,
            List<Employee> employeeList)
        {
            dataTable.Rows.Add("", (groupByMonth == true) ? periodStart.ToString("MMMM").ToUpper() : "", "", "ВСЕГО ПО ГК ЗА ПЕРИОД:");
            dataTable.Rows[dataTable.Rows.Count - 1]["_ISGROUPROW_"] = true;
            dataTable.Rows[dataTable.Rows.Count - 1]["ProjectInfo"] = "Трудозатраты:";

            dataTable.Rows.Add("", "", "", "");
            dataTable.Rows[dataTable.Rows.Count - 1]["_ISGROUPROW_"] = true;
            dataTable.Rows[dataTable.Rows.Count - 1]["ProjectInfo"] = "Сотрудников:";

            if (employeePayrollSheetDataTable != null || projectsOtherCostsSheetDataTable != null)
            {
                dataTable.Rows.Add("", "", "", "");
                dataTable.Rows[dataTable.Rows.Count - 1]["_ISGROUPROW_"] = true;
                dataTable.Rows[dataTable.Rows.Count - 1]["ProjectInfo"] = "ФОТ:";

                dataTable.Rows.Add("", "", "", "");
                dataTable.Rows[dataTable.Rows.Count - 1]["_ISGROUPROW_"] = true;
                dataTable.Rows[dataTable.Rows.Count - 1]["ProjectInfo"] = "СУ:";

                dataTable.Rows.Add("", "", "", "");
                dataTable.Rows[dataTable.Rows.Count - 1]["_ISGROUPROW_"] = true;
                dataTable.Rows[dataTable.Rows.Count - 1]["ProjectInfo"] = "Perf Bonus:";

                dataTable.Rows.Add("", "", "", "");
                dataTable.Rows[dataTable.Rows.Count - 1]["_ISGROUPROW_"] = true;
                dataTable.Rows[dataTable.Rows.Count - 1]["ProjectInfo"] = "Daykassa:";

                dataTable.Rows.Add("", "", "", "");
                dataTable.Rows[dataTable.Rows.Count - 1]["_ISGROUPROW_"] = true;
                dataTable.Rows[dataTable.Rows.Count - 1]["ProjectInfo"] = "Сум. затраты:";
            }

            int j = 0;
            var employeeCount = employeeList.Count();
            foreach (var group in employeeList.GroupBy(e => e.Department.ShortName))
            {
                dataTable.Rows.Add(group.Key, group.Count(), "", "");

                int departmentRowIndex = dataTable.Rows.Count - 1;
                double departmentMonthHours = 0;

                dataTable.Rows[departmentRowIndex]["_ISGROUPROW_"] = true;

                foreach (var employeeItem in group)
                {
                    String employeeItemFullName = "";

                    if (String.IsNullOrEmpty(employeeItem.LastName) == false)
                    {
                        employeeItemFullName = employeeItem.LastName;
                    }
                    if (String.IsNullOrEmpty(employeeItem.FirstName) == false)
                    {
                        employeeItemFullName += " " + employeeItem.FirstName;
                    }
                    if (String.IsNullOrEmpty(employeeItem.MidName) == false)
                    {
                        employeeItemFullName += " " + employeeItem.MidName;
                    }

                    employeeItemFullName = employeeItemFullName.Trim();

                    dataTable.Rows.Add(group.Key, ((group.First().Department != null) ? group.First().Department.Title : ""),
                        ((employeeItem.EmployeePositionTitle != null) ? employeeItem.EmployeePositionTitle : ""),
                        employeeItemFullName);

                    try
                    {
                        task.SetStatus(55 + 15 * j / employeeCount, "Расчет трудозатрат для: " + employeeItemFullName);

                        for (int k = 0; k < HOURS.Count; k++)
                        {
                            HOURSRecord hr = HOURS[k] as HOURSRecord;
                            DateTime recDate = DateTime.ParseExact(hr.REC_DATE, "MM/dd/yyyy", CultureInfo.InvariantCulture);
                            if (recDate >= periodStart && recDate <= periodEnd)
                            {
                                string employeeName = GetEmployeeFullNameByTimesheetNames(employeeList, hr.LNAME, hr.FNAME, hr.PATRONYMIC);

                                if (employeeName.Trim().ToLower().Equals(employeeItemFullName.Trim().ToLower()) == true
                                    && dataTable.Columns.Contains(hr.PROJECT_SHORT_NAME) == true)
                                {
                                    //Суммарные часы по проекту
                                    if (dataTable.Rows[0][hr.PROJECT_SHORT_NAME] == null
                                        || String.IsNullOrEmpty(dataTable.Rows[0][hr.PROJECT_SHORT_NAME].ToString()))
                                    {
                                        dataTable.Rows[0][hr.PROJECT_SHORT_NAME] = (double)hr.AMOUNT / 10d;
                                    }
                                    else
                                    {
                                        double hours = Convert.ToDouble(dataTable.Rows[0][hr.PROJECT_SHORT_NAME]);
                                        dataTable.Rows[0][hr.PROJECT_SHORT_NAME] = hours + (double)hr.AMOUNT / 10d;
                                    }

                                    //Суммарные часы сотрудника на проект
                                    if (dataTable.Rows[dataTable.Rows.Count - 1][hr.PROJECT_SHORT_NAME] == null
                                        || String.IsNullOrEmpty(dataTable.Rows[dataTable.Rows.Count - 1][hr.PROJECT_SHORT_NAME].ToString()))
                                    {
                                        dataTable.Rows[dataTable.Rows.Count - 1][hr.PROJECT_SHORT_NAME] = (double)hr.AMOUNT / 10d;
                                    }
                                    else
                                    {
                                        double hours = Convert.ToDouble(dataTable.Rows[dataTable.Rows.Count - 1][hr.PROJECT_SHORT_NAME]);
                                        dataTable.Rows[dataTable.Rows.Count - 1][hr.PROJECT_SHORT_NAME] = hours + (double)hr.AMOUNT / 10d;
                                    }

                                    //Суммарные часы сотрудника по всем проектам
                                    if (dataTable.Rows[dataTable.Rows.Count - 1]["EmployeeMonthHours"] == null
                                        || String.IsNullOrEmpty(dataTable.Rows[dataTable.Rows.Count - 1]["EmployeeMonthHours"].ToString()))
                                    {
                                        dataTable.Rows[dataTable.Rows.Count - 1]["EmployeeMonthHours"] = (double)hr.AMOUNT / 10d;
                                    }
                                    else
                                    {
                                        double hours = Convert.ToDouble(dataTable.Rows[dataTable.Rows.Count - 1]["EmployeeMonthHours"]);
                                        dataTable.Rows[dataTable.Rows.Count - 1]["EmployeeMonthHours"] = hours + (double)hr.AMOUNT / 10d;
                                    }

                                    if (dataTable.Rows[dataTable.Rows.Count - 1]["EmployeeMonthHours"] != null)
                                    {
                                        double employeeMonthHours = Convert.ToDouble(dataTable.Rows[dataTable.Rows.Count - 1]["EmployeeMonthHours"]);

                                        if (employeeMonthHours <= monthWorkHours)
                                        {
                                            dataTable.Rows[dataTable.Rows.Count - 1]["EmployeeMonthUnderHours"] = (double)monthWorkHours - employeeMonthHours;
                                            dataTable.Rows[dataTable.Rows.Count - 1]["EmployeeMonthOverHours"] = 0;
                                        }
                                        else
                                        {
                                            dataTable.Rows[dataTable.Rows.Count - 1]["EmployeeMonthUnderHours"] = 0;
                                            dataTable.Rows[dataTable.Rows.Count - 1]["EmployeeMonthOverHours"] = employeeMonthHours - (double)monthWorkHours;
                                        }
                                    }

                                    departmentMonthHours += (double)hr.AMOUNT / 10d;

                                    //ФОТ
                                    if (employeePayrollSheetDataTable != null
                                        && ProjectHelper.IsNoPaidProject(hr.PROJECT_SHORT_NAME) == false)
                                    {
                                        try
                                        {
                                            double employeePayrollValue = _financeService.GetEmployeePayrollOnDate(employeePayrollSheetDataTable, employeeItem, recDate, true,
                                                _employeeCategoryService.Get(x => x.Where(ec => ec.EmployeeID == employeeItem.ID).OrderByDescending(ec => ec.CategoryDateBegin).ToList()));

                                            double hoursCost = 0;

                                            if (employeePayrollValue != 0)
                                                hoursCost = GetWorkHoursCost(employeePayrollValue, hr);

                                            if (dataTable.Rows[2][hr.PROJECT_SHORT_NAME] == null
                                                    || String.IsNullOrEmpty(dataTable.Rows[2][hr.PROJECT_SHORT_NAME].ToString()))
                                            {
                                                dataTable.Rows[2][hr.PROJECT_SHORT_NAME] = hoursCost;
                                            }
                                            else
                                            {
                                                double hoursCostCurrent = Convert.ToDouble(dataTable.Rows[2][hr.PROJECT_SHORT_NAME]);
                                                dataTable.Rows[2][hr.PROJECT_SHORT_NAME] = hoursCostCurrent + hoursCost;
                                            }

                                            if (dataTable.Rows[dataTable.Rows.Count - 1]["EmployeePayroll"] == null
                                                || String.IsNullOrEmpty(dataTable.Rows[dataTable.Rows.Count - 1]["EmployeePayroll"].ToString()))
                                            {
                                                dataTable.Rows[dataTable.Rows.Count - 1]["EmployeePayroll"] = hoursCost;
                                            }
                                            else
                                            {
                                                double hoursCostCurrent = Convert.ToDouble(dataTable.Rows[dataTable.Rows.Count - 1]["EmployeePayroll"]);
                                                dataTable.Rows[dataTable.Rows.Count - 1]["EmployeePayroll"] = hoursCostCurrent + hoursCost;
                                            }
                                        }
                                        catch (Exception)
                                        {

                                        }
                                    }
                                }
                            }
                        }

                        for (int k = 0; k < PERMANENT_PROJECTS.Count; k++)
                        {
                            PERMANENT_PROJECTSRecord ppr = PERMANENT_PROJECTS[k] as PERMANENT_PROJECTSRecord;
                            string employeeName = GetEmployeeFullNameByTimesheetNames(employeeList, ppr.LNAME, ppr.FNAME, ppr.PATRONYMIC);

                            int permanentProjectYear = ProjectHelper.GetProjectYear(ppr.PROJECT_SHORT_NAME);
                            DateTime permanentProjectDateEnd = GetPermanentProjectDateEnd(ppr);

                            if (employeeName.Trim().ToLower().Equals(employeeItemFullName.Trim().ToLower()) == true
                                && dataTable.Columns.Contains(ppr.PROJECT_SHORT_NAME) == true
                                && periodEnd.Year >= permanentProjectYear
                                && periodStart <= permanentProjectDateEnd)
                            {

                                int pprHOURS = GetPermanentProjectHoursForPeriod(employeeItem,
                                    ppr, periodStart, periodEnd,
                                    monthWorkHours);

                                //Суммарные часы по проекту
                                if (dataTable.Rows[0][ppr.PROJECT_SHORT_NAME] == null
                                    || String.IsNullOrEmpty(dataTable.Rows[0][ppr.PROJECT_SHORT_NAME].ToString()))
                                {
                                    dataTable.Rows[0][ppr.PROJECT_SHORT_NAME] = (double)pprHOURS / 10d;
                                }
                                else
                                {
                                    double hours = Convert.ToDouble(dataTable.Rows[0][ppr.PROJECT_SHORT_NAME]);
                                    dataTable.Rows[0][ppr.PROJECT_SHORT_NAME] = hours + (double)pprHOURS / 10d;
                                }

                                //Суммарные часы сотрудника на проект
                                if (dataTable.Rows[dataTable.Rows.Count - 1][ppr.PROJECT_SHORT_NAME] == null
                                    || String.IsNullOrEmpty(dataTable.Rows[dataTable.Rows.Count - 1][ppr.PROJECT_SHORT_NAME].ToString()))
                                {
                                    dataTable.Rows[dataTable.Rows.Count - 1][ppr.PROJECT_SHORT_NAME] = (double)pprHOURS / 10d;
                                }
                                else
                                {
                                    double hours = Convert.ToDouble(dataTable.Rows[dataTable.Rows.Count - 1][ppr.PROJECT_SHORT_NAME]);
                                    dataTable.Rows[dataTable.Rows.Count - 1][ppr.PROJECT_SHORT_NAME] = hours + (double)pprHOURS / 10d;
                                }

                                //Суммарные часы сотрудника по всем проектам
                                if (dataTable.Rows[dataTable.Rows.Count - 1]["EmployeeMonthHours"] == null
                                    || String.IsNullOrEmpty(dataTable.Rows[dataTable.Rows.Count - 1]["EmployeeMonthHours"].ToString()))
                                {
                                    dataTable.Rows[dataTable.Rows.Count - 1]["EmployeeMonthHours"] = (double)pprHOURS / 10d;
                                }
                                else
                                {
                                    double hours = Convert.ToDouble(dataTable.Rows[dataTable.Rows.Count - 1]["EmployeeMonthHours"]);
                                    dataTable.Rows[dataTable.Rows.Count - 1]["EmployeeMonthHours"] = hours + (double)pprHOURS / 10d;
                                }

                                if (dataTable.Rows[dataTable.Rows.Count - 1]["EmployeeMonthHours"] != null)
                                {
                                    double employeeMonthHours = Convert.ToDouble(dataTable.Rows[dataTable.Rows.Count - 1]["EmployeeMonthHours"]);

                                    if (employeeMonthHours <= monthWorkHours)
                                    {
                                        dataTable.Rows[dataTable.Rows.Count - 1]["EmployeeMonthUnderHours"] = (double)monthWorkHours - employeeMonthHours;
                                        dataTable.Rows[dataTable.Rows.Count - 1]["EmployeeMonthOverHours"] = 0;
                                    }
                                    else
                                    {
                                        dataTable.Rows[dataTable.Rows.Count - 1]["EmployeeMonthUnderHours"] = 0;
                                        dataTable.Rows[dataTable.Rows.Count - 1]["EmployeeMonthOverHours"] = employeeMonthHours - (double)monthWorkHours;
                                    }
                                }

                                departmentMonthHours += (double)pprHOURS / 10d;

                                //ФОТ
                                if (employeePayrollSheetDataTable != null
                                    && ProjectHelper.IsNoPaidProject(ppr.PROJECT_SHORT_NAME) == false)
                                {
                                    try
                                    {
                                        double employeePayrollValue = _financeService.GetEmployeePayrollOnDate(employeePayrollSheetDataTable, employeeItem, periodEnd, false, null);

                                        double hoursCost = 0;

                                        if (employeePayrollValue != 0)
                                            hoursCost = GetPermamentWorkHoursCost(employeePayrollValue, pprHOURS, ppr, monthWorkHours, employeeItem, periodStart, periodEnd);


                                        if (dataTable.Rows[2][ppr.PROJECT_SHORT_NAME] == null
                                                || String.IsNullOrEmpty(dataTable.Rows[2][ppr.PROJECT_SHORT_NAME].ToString()))
                                        {
                                            dataTable.Rows[2][ppr.PROJECT_SHORT_NAME] = hoursCost;
                                        }
                                        else
                                        {
                                            double hoursCostCurrent = Convert.ToDouble(dataTable.Rows[2][ppr.PROJECT_SHORT_NAME]);
                                            dataTable.Rows[2][ppr.PROJECT_SHORT_NAME] = hoursCostCurrent + hoursCost;
                                        }

                                        if (dataTable.Rows[dataTable.Rows.Count - 1]["EmployeePayroll"] == null
                                            || String.IsNullOrEmpty(dataTable.Rows[dataTable.Rows.Count - 1]["EmployeePayroll"].ToString()))
                                        {
                                            dataTable.Rows[dataTable.Rows.Count - 1]["EmployeePayroll"] = hoursCost;
                                        }
                                        else
                                        {
                                            double hoursCostCurrent = Convert.ToDouble(dataTable.Rows[dataTable.Rows.Count - 1]["EmployeePayroll"]);
                                            dataTable.Rows[dataTable.Rows.Count - 1]["EmployeePayroll"] = hoursCostCurrent + hoursCost;
                                        }
                                    }
                                    catch (Exception)
                                    {

                                    }
                                }
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        task.SetStatus(-1, "Ошибка: " + e.Message);
                    }

                    j++;
                }

                dataTable.Rows[departmentRowIndex]["DepartmentMonthHours"] = departmentMonthHours;
            }

            if (saveResultsInDB == true)
            {
                task.SetStatus(70, "Расчет агрегированных данных и запись в БД...");
            }
            else
            {
                task.SetStatus(70, "Расчет агрегированных данных без записи в БД...");
            }

            IEnumerable<Project> projects = _projectService.Get(x => x.ToList());

            if (saveResultsInDB == true)
            {
                IList<ProjectReportRecord> projectReportRecordToDeleteList = _projectReportRecordService.Get(x => x.Where(r => r.ReportPeriodName == reportPeriodName).ToList());

                task.SetStatus(70, "Очистка данных предыдущих расчетов: " + projectReportRecordToDeleteList.Count().ToString());

                try
                {
                    _projectReportRecordService.RemoveRange(projectReportRecordToDeleteList);
                }
                catch (Exception)
                {

                }

                task.SetStatus(70, "Очистка данных предыдущих расчетов завершена");
            }

            for (int k = 0; k < PROJECTS.Count; k++)
            {
                PROJECTSRecord pr = PROJECTS[k] as PROJECTSRecord;

                Project project = null;

                string projectShortName = pr.PROJECT_SHORT_NAME;

                if (String.IsNullOrEmpty(projectShortName) == false)
                {
                    if (saveResultsInDB == true)
                    {
                        task.SetStatus(70 + 28 * k / PROJECTS.Count, "Расчет агрегированных данных и запись в БД для проекта: " + projectShortName);
                    }
                    else
                    {
                        task.SetStatus(70 + 28 * k / PROJECTS.Count, "Расчет агрегированных данных без записи в БД для проекта: " + projectShortName);
                    }

                    project = projects.Where(p => p.ShortName == projectShortName).FirstOrDefault();
                }

                if (dataTable.Columns.Contains(pr.PROJECT_SHORT_NAME) == true)
                {

                    int projectEmployeeCount = 0;

                    int startIndex_i = 2;
                    if (employeePayrollSheetDataTable != null || projectsOtherCostsSheetDataTable != null)
                    {
                        startIndex_i = 7;
                    }

                    for (int i = startIndex_i; i < dataTable.Rows.Count; i++)
                    {
                        if (dataTable.Rows[i]["EmployeeTitle"] != null
                            && String.IsNullOrEmpty(dataTable.Rows[i]["EmployeeTitle"].ToString()) == false)
                        {
                            string employeeTitle = dataTable.Rows[i]["EmployeeTitle"].ToString();
                            Employee employee = employeeList.Where(e => e.FullName == employeeTitle).FirstOrDefault();

                            if (dataTable.Rows[i][projectShortName] != null
                                && String.IsNullOrEmpty(dataTable.Rows[i][projectShortName].ToString()) == false
                                && Convert.ToDouble(dataTable.Rows[i][projectShortName]) != 0)
                            {
                                projectEmployeeCount++;

                                if (saveResultsInDB == true)
                                {
                                    try
                                    {
                                        if (employee != null && project != null)
                                        {
                                            ProjectReportRecord record = _projectReportRecordService.Get(x => x.Where(r => r.ProjectID == project.ID && r.ReportPeriodName == reportPeriodName && r.EmployeeID == employee.ID).ToList()).FirstOrDefault();
                                            if (record != null)
                                            {
                                                record.Hours = Convert.ToDouble(dataTable.Rows[i][projectShortName]);
                                                record.EmployeeID = employee.ID;
                                                record.CalcDate = DateTime.Now;
                                                record.Comments = "Расчет выполнен пользователем: " + userIdentityName;
                                                _projectReportRecordService.Update(record);
                                            }
                                            else
                                            {
                                                record = new ProjectReportRecord();
                                                record.ReportPeriodName = reportPeriodName;
                                                record.ProjectID = project.ID;
                                                record.Hours = Convert.ToDouble(dataTable.Rows[i][projectShortName]);
                                                record.EmployeeID = employee.ID;
                                                record.CalcDate = DateTime.Now;
                                                record.Comments = "Расчет выполнен пользователем: " + userIdentityName;
                                                _projectReportRecordService.Add(record);
                                            }
                                        }
                                    }
                                    catch (Exception ex)
                                    {

                                    }
                                }
                            }


                            //затраты
                            if (projectsOtherCostsSheetDataTable != null)
                            {
                                double employeeMonthOverHoursForEmployee = 0;
                                double employeeOvertimePayrollValueForEmployee = 0;
                                double employeePerformanceBonusValueForEmployee = 0;
                                double otherCostsValueForEmployee = 0;

                                if (dataTable.Rows[i]["EmployeeMonthOverHours"] != null
                                    && String.IsNullOrEmpty(dataTable.Rows[i]["EmployeeMonthOverHours"].ToString()) == false)
                                {
                                    employeeMonthOverHoursForEmployee = Convert.ToDouble(dataTable.Rows[i]["EmployeeMonthOverHours"]);
                                }

                                employeeOvertimePayrollValueForEmployee = _financeService.GetProjectEmployeeOvertimePayrollDeltaValueForEmployee(projectsOtherCostsSheetDataTable, employeePayrollSheetDataTable,
                                    projectShortName,
                                    employee,
                                    employeeMonthOverHoursForEmployee,
                                    monthWorkHours,
                                    periodStart, periodEnd);
                                if (dataTable.Rows[i]["EmployeeOvertimePayroll"] == null
                                   || String.IsNullOrEmpty(dataTable.Rows[i]["EmployeeOvertimePayroll"].ToString()))
                                {
                                    dataTable.Rows[i]["EmployeeOvertimePayroll"] = employeeOvertimePayrollValueForEmployee;
                                }
                                else
                                {
                                    double employeeOvertimePayrollValueForEmployeeCurrent = Convert.ToDouble(dataTable.Rows[i]["EmployeeOvertimePayroll"]);
                                    dataTable.Rows[i]["EmployeeOvertimePayroll"] = employeeOvertimePayrollValueForEmployeeCurrent + employeeOvertimePayrollValueForEmployee;
                                }
                                if (dataTable.Rows[3][projectShortName] == null
                                    || String.IsNullOrEmpty(dataTable.Rows[3][projectShortName].ToString()))
                                {
                                    dataTable.Rows[3][projectShortName] = employeeOvertimePayrollValueForEmployee;
                                }
                                else
                                {
                                    double employeeOvertimePayrollValueForEmployeeCurrent = Convert.ToDouble(dataTable.Rows[3][projectShortName]);
                                    dataTable.Rows[3][projectShortName] = employeeOvertimePayrollValueForEmployeeCurrent + employeeOvertimePayrollValueForEmployee;
                                }

                                employeePerformanceBonusValueForEmployee = _financeService.GetProjectEmployeePerformanceBonusValueForEmployee(projectsOtherCostsSheetDataTable, projectShortName,
                                    employee,
                                    periodStart, periodEnd);
                                if (dataTable.Rows[i]["EmployeePerformanceBonus"] == null
                                    || String.IsNullOrEmpty(dataTable.Rows[i]["EmployeePerformanceBonus"].ToString()))
                                {
                                    dataTable.Rows[i]["EmployeePerformanceBonus"] = employeePerformanceBonusValueForEmployee;
                                }
                                else
                                {
                                    double employeePerformanceBonusValueForEmployeeCurrent = Convert.ToDouble(dataTable.Rows[i]["EmployeePerformanceBonus"]);
                                    dataTable.Rows[i]["EmployeePerformanceBonus"] = employeePerformanceBonusValueForEmployeeCurrent + employeePerformanceBonusValueForEmployee;
                                }

                                otherCostsValueForEmployee = _financeService.GetProjectOtherCostsValueForEmployee(projectsOtherCostsSheetDataTable, projectShortName,
                                    employee,
                                    periodStart, periodEnd);
                                if (dataTable.Rows[i]["EmployeeOtherCosts"] == null
                                    || String.IsNullOrEmpty(dataTable.Rows[i]["EmployeeOtherCosts"].ToString()))
                                {
                                    dataTable.Rows[i]["EmployeeOtherCosts"] = otherCostsValueForEmployee;
                                }
                                else
                                {
                                    double otherCostsValueForEmployeeCurrent = Convert.ToDouble(dataTable.Rows[i]["EmployeeOtherCosts"]);
                                    dataTable.Rows[i]["EmployeeOtherCosts"] = otherCostsValueForEmployeeCurrent + otherCostsValueForEmployee;
                                }
                            }
                        }
                    }

                    if (projectEmployeeCount != 0)
                    {
                        dataTable.Rows[1][projectShortName] = Convert.ToDouble(projectEmployeeCount);
                    }
                    else
                    {
                        dataTable.Rows[1][projectShortName] = 0;
                    }

                    double employeePayrollValue = 0;
                    double employeeOvertimePayrollValue = 0;
                    double employeePerformanceBonusValue = 0;
                    double otherCostsValue = 0;
                    double totalCostsValue = 0;

                    if (employeePayrollSheetDataTable != null
                        && dataTable.Rows[2][projectShortName] != null
                        && String.IsNullOrEmpty(dataTable.Rows[2][projectShortName].ToString()) == false)
                    {
                        employeePayrollValue = Convert.ToDouble(dataTable.Rows[2][projectShortName]);
                    }

                    if (projectsOtherCostsSheetDataTable != null)
                    {
                        if (dataTable.Rows[3][projectShortName] != null
                            && String.IsNullOrEmpty(dataTable.Rows[3][projectShortName].ToString()) == false)
                        {
                            employeeOvertimePayrollValue = Convert.ToDouble(dataTable.Rows[3][projectShortName]);
                        }

                        employeePerformanceBonusValue = _financeService.GetProjectEmployeePerformanceBonusValueForEmployee(projectsOtherCostsSheetDataTable, projectShortName,
                            null,
                            periodStart, periodEnd);
                        dataTable.Rows[4][projectShortName] = employeePerformanceBonusValue;

                        otherCostsValue = _financeService.GetProjectOtherCostsValueForEmployee(projectsOtherCostsSheetDataTable, projectShortName,
                            null,
                            periodStart, periodEnd);
                        dataTable.Rows[5][projectShortName] = otherCostsValue;
                    }

                    if (employeePayrollSheetDataTable != null || projectsOtherCostsSheetDataTable != null)
                    {
                        totalCostsValue = employeePayrollValue + employeeOvertimePayrollValue + employeePerformanceBonusValue + otherCostsValue;
                        dataTable.Rows[6][projectShortName] = totalCostsValue;
                    }

                    if (project != null
                        && ((dataTable.Rows[0][projectShortName] != null
                        && String.IsNullOrEmpty(dataTable.Rows[0][projectShortName].ToString()) == false)
                        || ((project.BeginDate != null && project.BeginDate <= periodEnd) && (project.EndDate == null || project.EndDate >= periodStart))))
                    {

                        if (saveResultsInDB == true)
                        {
                            try
                            {
                                ProjectReportRecord record = _projectReportRecordService.Get(x => x.Where(r => r.ProjectID == project.ID && r.ReportPeriodName == reportPeriodName && r.EmployeeID == null).ToList()).FirstOrDefault();
                                if (record != null)
                                {
                                    if (dataTable.Rows[0][projectShortName] != null
                                        && String.IsNullOrEmpty(dataTable.Rows[0][projectShortName].ToString()) == false)
                                    {
                                        record.Hours = Convert.ToDouble(dataTable.Rows[0][projectShortName]);
                                    }
                                    else
                                    {
                                        record.Hours = 0;
                                    }

                                    if (dataTable.Rows[1][projectShortName] != null)
                                    {
                                        record.EmployeeCount = Convert.ToInt32(dataTable.Rows[1][projectShortName]);
                                    }
                                    else
                                    {
                                        record.EmployeeCount = 0;
                                    }

                                    if (employeePayrollSheetDataTable != null || projectsOtherCostsSheetDataTable != null)
                                    {
                                        //затраты по проекту
                                        record.EmployeePayroll = Convert.ToDecimal(employeePayrollValue);
                                        record.EmployeeOvertimePayroll = Convert.ToDecimal(employeeOvertimePayrollValue);
                                        record.EmployeePerformanceBonus = Convert.ToDecimal(employeePerformanceBonusValue);
                                        record.OtherCosts = Convert.ToDecimal(otherCostsValue);
                                        record.TotalCosts = Convert.ToDecimal(totalCostsValue);
                                    }

                                    record.CalcDate = DateTime.Now;
                                    record.Comments = "Расчет выполнен пользователем: " + userIdentityName;
                                    _projectReportRecordService.Update(record);
                                }
                                else
                                {
                                    record = new ProjectReportRecord();
                                    record.ReportPeriodName = reportPeriodName;
                                    record.ProjectID = project.ID;
                                    if (dataTable.Rows[0][projectShortName] != null
                                        && String.IsNullOrEmpty(dataTable.Rows[0][projectShortName].ToString()) == false)
                                    {
                                        record.Hours = Convert.ToDouble(dataTable.Rows[0][projectShortName]);
                                    }
                                    else
                                    {
                                        record.Hours = 0;
                                    }

                                    if (dataTable.Rows[1][projectShortName] != null)
                                    {
                                        record.EmployeeCount = Convert.ToInt32(dataTable.Rows[1][projectShortName]);
                                    }
                                    else
                                    {
                                        record.EmployeeCount = 0;
                                    }

                                    if (employeePayrollSheetDataTable != null || projectsOtherCostsSheetDataTable != null)
                                    {
                                        //затраты по проекту
                                        record.EmployeePayroll = Convert.ToDecimal(employeePayrollValue);
                                        record.EmployeeOvertimePayroll = Convert.ToDecimal(employeeOvertimePayrollValue);
                                        record.EmployeePerformanceBonus = Convert.ToDecimal(employeePerformanceBonusValue);
                                        record.OtherCosts = Convert.ToDecimal(otherCostsValue);
                                        record.TotalCosts = Convert.ToDecimal(totalCostsValue);
                                    }

                                    record.CalcDate = DateTime.Now;
                                    record.Comments = "Расчет выполнен пользователем: " + userIdentityName;
                                    _projectReportRecordService.Add(record);
                                }
                            }
                            catch (Exception ex)
                            {

                            }
                        }
                    }
                }
                else if (project != null
                    && (project.BeginDate != null && project.BeginDate <= periodEnd) && (project.EndDate == null || project.EndDate >= periodStart))
                {
                    if (saveResultsInDB == true)
                    {
                        try
                        {
                            ProjectReportRecord record = _projectReportRecordService.Get(x => x.Where(r => r.ProjectID == project.ID && r.ReportPeriodName == reportPeriodName && r.EmployeeID == null).ToList()).FirstOrDefault();
                            if (record == null)
                            {
                                record = new ProjectReportRecord();
                                record.ReportPeriodName = reportPeriodName;
                                record.ProjectID = project.ID;
                                record.Hours = 0;
                                record.EmployeeCount = 0;
                                record.EmployeePayroll = 0;

                                record.CalcDate = DateTime.Now;
                                record.Comments = "Расчет выполнен пользователем: " + userIdentityName;
                                _projectReportRecordService.Add(record);
                            }
                        }
                        catch (Exception ex)
                        {

                        }
                    }
                }
            }




            double reportTotalHours = 0;
            double reportDepartmentEmployeeTotalCosts = 0;
            double reportTotalEmployeePayroll = 0;
            double reportTotalEmployeeOvertimePayroll = 0;
            double reportTotalEmployeePerformanceBonus = 0;
            double reportTotalEmployeeOtherCosts = 0;
            double reportTotalEmployeeTotalCosts = 0;

            int startIndex_k = 2;
            if (employeePayrollSheetDataTable != null || projectsOtherCostsSheetDataTable != null)
            {
                startIndex_k = 7;
            }

            reportDepartmentEmployeeTotalCosts = 0;
            int departmentRowIndexForTotalCosts = startIndex_k;
            for (int k = startIndex_k; k < dataTable.Rows.Count; k++)
            {
                if (dataTable.Rows[k]["EmployeeTitle"] != null
                    && String.IsNullOrEmpty(dataTable.Rows[k]["EmployeeTitle"].ToString()) == false)
                {
                    double employeeMonthHours = 0;

                    if (dataTable.Rows[k]["EmployeeMonthHours"] != null
                        && String.IsNullOrEmpty(dataTable.Rows[k]["EmployeeMonthHours"].ToString()) == false)
                    {
                        try
                        {
                            employeeMonthHours = Convert.ToDouble(dataTable.Rows[k]["EmployeeMonthHours"]);
                        }
                        catch (Exception)
                        {
                            employeeMonthHours = 0;
                        }

                        reportTotalHours += employeeMonthHours;
                    }

                    if (employeePayrollSheetDataTable != null || projectsOtherCostsSheetDataTable != null)
                    {
                        double employeePayrollValueForEmployee = 0;
                        double employeeOvertimePayrollValueForEmployee = 0;
                        double employeePerformanceBonusValueForEmployee = 0;
                        double otherCostsValueForEmployee = 0;
                        double totalCostsValueForEmployee = 0;

                        if (dataTable.Rows[k]["EmployeePayroll"] != null
                            && String.IsNullOrEmpty(dataTable.Rows[k]["EmployeePayroll"].ToString()) == false)
                        {
                            employeePayrollValueForEmployee = Convert.ToDouble(dataTable.Rows[k]["EmployeePayroll"]);
                        }

                        if (dataTable.Rows[k]["EmployeeOvertimePayroll"] != null
                            && String.IsNullOrEmpty(dataTable.Rows[k]["EmployeeOvertimePayroll"].ToString()) == false)
                        {
                            employeeOvertimePayrollValueForEmployee = Convert.ToDouble(dataTable.Rows[k]["EmployeeOvertimePayroll"]);
                        }

                        if (dataTable.Rows[k]["EmployeePerformanceBonus"] != null
                            && String.IsNullOrEmpty(dataTable.Rows[k]["EmployeePerformanceBonus"].ToString()) == false)
                        {
                            employeePerformanceBonusValueForEmployee = Convert.ToDouble(dataTable.Rows[k]["EmployeePerformanceBonus"]);
                        }

                        if (dataTable.Rows[k]["EmployeeOtherCosts"] != null
                            && String.IsNullOrEmpty(dataTable.Rows[k]["EmployeeOtherCosts"].ToString()) == false)
                        {
                            otherCostsValueForEmployee = Convert.ToDouble(dataTable.Rows[k]["EmployeeOtherCosts"]);
                        }

                        totalCostsValueForEmployee = employeePayrollValueForEmployee + employeeOvertimePayrollValueForEmployee
                            + employeePerformanceBonusValueForEmployee + otherCostsValueForEmployee;
                        if (dataTable.Rows[k]["EmployeeTotalCosts"] == null
                            || String.IsNullOrEmpty(dataTable.Rows[k]["EmployeeTotalCosts"].ToString()))
                        {
                            dataTable.Rows[k]["EmployeeTotalCosts"] = totalCostsValueForEmployee;
                        }
                        else
                        {
                            double totalCostsValueForEmployeeCurrent = Convert.ToDouble(dataTable.Rows[k]["EmployeeTotalCosts"]);
                            dataTable.Rows[k]["EmployeeTotalCosts"] = totalCostsValueForEmployeeCurrent + totalCostsValueForEmployee;
                        }

                        reportTotalEmployeePayroll += employeePayrollValueForEmployee;
                        reportTotalEmployeeOvertimePayroll += employeeOvertimePayrollValueForEmployee;
                        reportTotalEmployeePerformanceBonus += employeePerformanceBonusValueForEmployee;
                        reportTotalEmployeeOtherCosts += otherCostsValueForEmployee;
                        reportTotalEmployeeTotalCosts += totalCostsValueForEmployee;

                        reportDepartmentEmployeeTotalCosts += totalCostsValueForEmployee;
                    }
                }
                else
                {
                    if (employeePayrollSheetDataTable != null || projectsOtherCostsSheetDataTable != null)
                    {
                        if (dataTable.Rows[departmentRowIndexForTotalCosts]["DepartmentMonthHours"] != null
                            && String.IsNullOrEmpty(dataTable.Rows[departmentRowIndexForTotalCosts]["DepartmentMonthHours"].ToString()) == false)
                        {
                            if (reportDepartmentEmployeeTotalCosts != 0)
                            {
                                dataTable.Rows[departmentRowIndexForTotalCosts]["DepartmentEmployeeTotalCosts"] = reportDepartmentEmployeeTotalCosts;
                            }
                            else
                            {
                                dataTable.Rows[departmentRowIndexForTotalCosts]["DepartmentEmployeeTotalCosts"] = 0;
                            }
                        }
                    }

                    reportDepartmentEmployeeTotalCosts = 0;

                    departmentRowIndexForTotalCosts = k;
                }
            }

            dataTable.Rows[0]["EmployeeMonthHours"] = reportTotalHours;

            if (employeePayrollSheetDataTable != null || projectsOtherCostsSheetDataTable != null)
            {
                dataTable.Rows[0]["EmployeePayroll"] = reportTotalEmployeePayroll;
                dataTable.Rows[0]["EmployeeOvertimePayroll"] = reportTotalEmployeeOvertimePayroll;
                dataTable.Rows[0]["EmployeePerformanceBonus"] = reportTotalEmployeePerformanceBonus;
                dataTable.Rows[0]["EmployeeOtherCosts"] = reportTotalEmployeeOtherCosts;
                dataTable.Rows[0]["EmployeeTotalCosts"] = reportTotalEmployeeTotalCosts;
            }

            return dataTable;
        }

        public byte[] GetProjectsHoursReportExcel(LongRunningTaskBase task, string userIdentityName, string reportPeriodName, string reportTitle, int monthWorkHours,
            bool saveResultsInDB,
            bool addToReportNotInDBEmplyees,
            bool showEmployeeDataInReport,
            bool groupByMonth,
            DataTable employeePayrollSheetDataTable, DataTable projectsOtherCostsSheetDataTable,
            DateTime periodStart, DateTime periodEnd,
            List<int> departmentsIDs)
        {
            byte[] binData = null;

            var currentEmployeeList = _employeeService.GetCurrentEmployees(new DateTimeRange(periodStart, periodEnd)).ToList()
                .Where(e => e.Department != null);


            List<Employee> employeeList = null;

            if (departmentsIDs == null)
            {
                employeeList = currentEmployeeList.Where(e => e.Department != null).ToList();
            }
            else
            {
                employeeList = currentEmployeeList.Where(e => e.Department != null && departmentsIDs.Contains(e.Department.ID)).ToList();
            }

            employeeList = employeeList.OrderBy(e => e.Department.ShortName + (e.Department.DepartmentManager != e).ToString() + e.FullName).ToList();
            DataTable dataTable = new DataTable();

            dataTable.Columns.Add("DepartmentShortName", typeof(string)).Caption = "Код";
            dataTable.Columns["DepartmentShortName"].ExtendedProperties["Width"] = (double)6;
            dataTable.Columns.Add("Department", typeof(string)).Caption = "Подразделение/кол-во";
            dataTable.Columns["Department"].ExtendedProperties["Width"] = (double)71;
            dataTable.Columns.Add("Position", typeof(string)).Caption = "Позиция";
            dataTable.Columns["Position"].ExtendedProperties["Width"] = (double)51;
            dataTable.Columns.Add("EmployeeTitle", typeof(string)).Caption = "Фамилия Имя Отчество";
            dataTable.Columns["EmployeeTitle"].ExtendedProperties["Width"] = (double)39;
            dataTable.Columns.Add("DepartmentMonthHours", typeof(double)).Caption = "Загрузка подразделения (ч)";
            dataTable.Columns["DepartmentMonthHours"].ExtendedProperties["Width"] = (double)11;
            dataTable.Columns.Add("EmployeeMonthHours", typeof(double)).Caption = "Загрузка сотрудника (ч)";
            dataTable.Columns["EmployeeMonthHours"].ExtendedProperties["Width"] = (double)11;
            dataTable.Columns.Add("EmployeeMonthOverHours", typeof(double)).Caption = "Переработка (ч)";
            dataTable.Columns["EmployeeMonthOverHours"].ExtendedProperties["Width"] = (double)11;
            dataTable.Columns.Add("EmployeeMonthUnderHours", typeof(double)).Caption = "Недозагрузка (ч)";
            dataTable.Columns["EmployeeMonthUnderHours"].ExtendedProperties["Width"] = (double)11;
            if (employeePayrollSheetDataTable != null || projectsOtherCostsSheetDataTable != null)
            {
                dataTable.Columns.Add("DepartmentEmployeeTotalCosts", typeof(double)).Caption = "Сум. затраты подразделения";
                dataTable.Columns["DepartmentEmployeeTotalCosts"].ExtendedProperties["Width"] = (double)11;
                dataTable.Columns.Add("EmployeePayroll", typeof(double)).Caption = "ФОТ";
                dataTable.Columns["EmployeePayroll"].ExtendedProperties["Width"] = (double)11;
                dataTable.Columns.Add("EmployeeOvertimePayroll", typeof(double)).Caption = "СУ";
                dataTable.Columns["EmployeeOvertimePayroll"].ExtendedProperties["Width"] = (double)11;
                dataTable.Columns.Add("EmployeePerformanceBonus", typeof(double)).Caption = "Perf Bonus";
                dataTable.Columns["EmployeePerformanceBonus"].ExtendedProperties["Width"] = (double)11;
                dataTable.Columns.Add("EmployeeOtherCosts", typeof(double)).Caption = "Daykassa";
                dataTable.Columns["EmployeeOtherCosts"].ExtendedProperties["Width"] = (double)11;
                dataTable.Columns.Add("EmployeeTotalCosts", typeof(double)).Caption = "Сум. затраты";
                dataTable.Columns["EmployeeTotalCosts"].ExtendedProperties["Width"] = (double)11;
            }
            dataTable.Columns.Add("ProjectInfo", typeof(string)).Caption = "По каждому проекту";
            dataTable.Columns["ProjectInfo"].ExtendedProperties["Width"] = (double)15;
            dataTable.Columns.Add("_ISGROUPROW_", typeof(bool)).Caption = "_ISGROUPROW_";

            SortedDictionary<string, double> notInDBEmplyeesList = new SortedDictionary<string, double>();

            SortedDictionary<string, string> projectShortNameList = new SortedDictionary<string, string>();

            for (int k = 0; k < HOURS.Count; k++)
            {
                HOURSRecord hr = HOURS[k] as HOURSRecord;
                string employeeName = GetEmployeeFullNameByTimesheetNames(employeeList, hr.LNAME, hr.FNAME, hr.PATRONYMIC);

                bool employeeFoundInDB = false;

                foreach (Employee employee in employeeList)
                {
                    string name = employee.LastName + " " + employee.FirstName + " " + employee.MidName;
                    if (employeeName.Trim().ToLower().Equals(name.Trim().ToLower()) == true)
                    {
                        if (projectShortNameList.ContainsValue(hr.PROJECT_SHORT_NAME) == false)
                        {
                            string projectShortName = hr.PROJECT_SHORT_NAME;
                            string projectSortKey = ProjectHelper.GetProjectSortKeyByProjectShortName(projectShortName);

                            projectShortNameList.Add(projectSortKey, projectShortName);
                        }
                        employeeFoundInDB = true;
                        break;
                    }
                }

                if (employeeFoundInDB == false
                    && addToReportNotInDBEmplyees == true)
                {
                    if (notInDBEmplyeesList.ContainsKey(employeeName) == false)
                    {
                        notInDBEmplyeesList.Add(employeeName, 0);
                    }

                    notInDBEmplyeesList[employeeName] = (double)notInDBEmplyeesList[employeeName] + (double)hr.AMOUNT / 10d;
                }
            }

            for (int k = 0; k < PERMANENT_PROJECTS.Count; k++)
            {
                PERMANENT_PROJECTSRecord ppr = PERMANENT_PROJECTS[k] as PERMANENT_PROJECTSRecord;
                string employeeName = GetEmployeeFullNameByTimesheetNames(employeeList, ppr.LNAME, ppr.FNAME, ppr.PATRONYMIC);

                int permanentProjectYear = ProjectHelper.GetProjectYear(ppr.PROJECT_SHORT_NAME);
                DateTime permanentProjectDateEnd = GetPermanentProjectDateEnd(ppr);

                if (IsEmployeeOfPermanentProjectWorkInPeriod(ppr, periodStart, periodEnd) == true
                    && periodEnd.Year >= permanentProjectYear
                    && periodStart <= permanentProjectDateEnd)
                {
                    bool employeeFoundInDB = false;

                    foreach (Employee employee in employeeList)
                    {
                        string name = employee.LastName + " " + employee.FirstName + " " + employee.MidName;
                        if (employeeName.Trim().ToLower().Equals(name.Trim().ToLower()) == true)
                        {
                            if (projectShortNameList.ContainsValue(ppr.PROJECT_SHORT_NAME) == false)
                            {
                                string projectShortName = ppr.PROJECT_SHORT_NAME;
                                string projectSortKey = ProjectHelper.GetProjectSortKeyByProjectShortName(projectShortName);

                                projectShortNameList.Add(projectSortKey, projectShortName);
                            }
                            employeeFoundInDB = true;
                            break;
                        }
                    }

                    if (employeeFoundInDB == false
                        && addToReportNotInDBEmplyees == true)
                    {
                        if (notInDBEmplyeesList.ContainsKey(employeeName) == false)
                        {
                            notInDBEmplyeesList.Add(employeeName, 0);
                        }

                        int pprHOURS = GetPermanentProjectHoursForPeriod(null,
                            ppr, periodStart, periodEnd,
                            monthWorkHours);

                        notInDBEmplyeesList[employeeName] = (double)notInDBEmplyeesList[employeeName] + (double)pprHOURS / 10d;
                    }
                }
            }

            foreach (var projectShortNameListItem in projectShortNameList)
            {
                if (dataTable.Columns.Contains(projectShortNameListItem.Value) == false)
                {
                    dataTable.Columns.Add(projectShortNameListItem.Value, typeof(double)).Caption = projectShortNameListItem.Value;
                    dataTable.Columns[projectShortNameListItem.Value].ExtendedProperties["Width"] = (double)12;
                }
            }



            int startMonth = periodStart.Month;
            int endMonth = periodEnd.Month;

            double reportTotalHours = 0;
            if (groupByMonth == true)
            {
                for (int month = startMonth; month <= endMonth; month++)
                {
                    DataTable dt = dataTable.Clone();

                    DateTime start = (month == startMonth) ? periodStart : new DateTime(periodStart.Year, month, 1);
                    DateTime end = (month == endMonth)
                        ? periodEnd
                        : new DateTime(periodEnd.Year, month, 1).LastDayOfMonth();

                    int mwh = _productionCalendarService.GetWorkHoursBetweenDates(start, end);

                    dt = GetProjectHoursReportDataTable(task, dt,
                        userIdentityName, reportPeriodName, mwh,
                        groupByMonth,
                        saveResultsInDB,
                        employeePayrollSheetDataTable, projectsOtherCostsSheetDataTable,
                        start, end,
                        departmentsIDs,
                        employeeList);

                    foreach (DataRow dr in dt.Rows)
                    {
                        dataTable.Rows.Add(dr.ItemArray);
                    }

                    if (dt.Rows[0]["EmployeeMonthHours"] != null &&
                        String.IsNullOrEmpty(dt.Rows[0]["EmployeeMonthHours"].ToString()) == false)
                    {
                        reportTotalHours += (double) dt.Rows[0]["EmployeeMonthHours"];
                    }

                    //dataTable.Rows.Add("", "", "");
                }

                if (startMonth != endMonth)
                {
                    dataTable.Rows.Add("", "", "", "ИТОГО ЗА ВСЕ ПЕРИОДЫ:");
                    dataTable.Rows[dataTable.Rows.Count - 1]["_ISGROUPROW_"] = true;
                    dataTable.Rows[dataTable.Rows.Count - 1]["EmployeeMonthHours"] = reportTotalHours;
                }
            }
            else
            {
                DataTable dt = dataTable.Clone();

                dt = GetProjectHoursReportDataTable(task, dt,
                    userIdentityName, reportPeriodName, monthWorkHours,
                    groupByMonth,
                    saveResultsInDB,
                    employeePayrollSheetDataTable, projectsOtherCostsSheetDataTable,
                    periodStart, periodEnd,
                    departmentsIDs,
                    employeeList);

                foreach (DataRow dr in dt.Rows)
                {
                    dataTable.Rows.Add(dr.ItemArray);
                }
            }

            dataTable.Rows.Add("", "", "", employeeList.Count());

            if (departmentsIDs == null)
            {
                if (notInDBEmplyeesList.Count != 0)
                {
                    dataTable.Rows.Add("", "", "", "НЕТ В БАЗЕ (" + notInDBEmplyeesList.Count.ToString() + "):");

                    foreach (var notInDBEmplyeesListItem in notInDBEmplyeesList)
                    {
                        dataTable.Rows.Add("", "", "", notInDBEmplyeesListItem.Key, null, notInDBEmplyeesListItem.Value);
                    }
                }
            }

            task.SetStatus(98, "Формирование файла MS Excel...");

            if (showEmployeeDataInReport == true)
            {
                using (MemoryStream stream = new MemoryStream())
                {
                    using (SpreadsheetDocument doc = SpreadsheetDocument.Create(stream, SpreadsheetDocumentType.Workbook))
                    {
                        WorkbookPart workbookPart = ExcelHelper.CreateWorkbookPart(doc, "Отчет " + reportPeriodName);

                        WorksheetPart rId1 = ExcelHelper.CreateWorksheetPartAndImportDataTable(workbookPart, "rId1", 1, 1, (uint)dataTable.Columns.Count,
                            reportTitle, dataTable, 3, 1);

                        doc.WorkbookPart.Workbook.Save();
                    }

                    stream.Position = 0;
                    BinaryReader b = new BinaryReader(stream);
                    binData = b.ReadBytes((int)stream.Length);
                }
            }
            else
            {
                DataTable consolidatedDataTable = new DataTable();
                consolidatedDataTable.Columns.Add("ProjectShortName", typeof(string)).Caption = "Код проекта";
                consolidatedDataTable.Columns["ProjectShortName"].ExtendedProperties["Width"] = (double)40;
                consolidatedDataTable.Columns.Add("ProjectHours", typeof(double)).Caption = "Трудозатраты";
                consolidatedDataTable.Columns["ProjectHours"].ExtendedProperties["Width"] = (double)16;
                consolidatedDataTable.Columns.Add("ProjectEmployeeCount", typeof(double)).Caption = "Сотрудников";
                consolidatedDataTable.Columns["ProjectEmployeeCount"].ExtendedProperties["Width"] = (double)16;
                consolidatedDataTable.Columns.Add("ProjectPayroll", typeof(double)).Caption = "ФОТ";
                consolidatedDataTable.Columns["ProjectPayroll"].ExtendedProperties["Width"] = (double)16;
                consolidatedDataTable.Columns.Add("ProjectOvertimePayroll", typeof(double)).Caption = "СУ";
                consolidatedDataTable.Columns["ProjectOvertimePayroll"].ExtendedProperties["Width"] = (double)16;
                consolidatedDataTable.Columns.Add("ProjectPerformanceBonus", typeof(double)).Caption = "Perf Bonus";
                consolidatedDataTable.Columns["ProjectPerformanceBonus"].ExtendedProperties["Width"] = (double)16;
                consolidatedDataTable.Columns.Add("ProjectOtherCosts", typeof(double)).Caption = "Daykassa";
                consolidatedDataTable.Columns["ProjectOtherCosts"].ExtendedProperties["Width"] = (double)16;
                consolidatedDataTable.Columns.Add("ProjectTotalCosts", typeof(double)).Caption = "Сум. затраты";
                consolidatedDataTable.Columns["ProjectTotalCosts"].ExtendedProperties["Width"] = (double)16;

                int projectColumnStartIndex = -1;
                for (int k = 0; k < dataTable.Columns.Count; k++)
                {
                    if (dataTable.Columns[k].ColumnName != null
                        && dataTable.Columns[k].ColumnName.Equals("ProjectInfo") == true)
                    {
                        projectColumnStartIndex = k + 2;
                        break;
                    }
                }

                if (projectColumnStartIndex != -1)
                {
                    for (int k = projectColumnStartIndex; k < dataTable.Columns.Count; k++)
                    {
                        if (employeePayrollSheetDataTable != null || projectsOtherCostsSheetDataTable != null)
                        {
                            consolidatedDataTable.Rows.Add(dataTable.Columns[k].Caption,
                            dataTable.Rows[0][dataTable.Columns[k].ColumnName],
                            dataTable.Rows[1][dataTable.Columns[k].ColumnName],
                            dataTable.Rows[2][dataTable.Columns[k].ColumnName],
                            dataTable.Rows[3][dataTable.Columns[k].ColumnName],
                            dataTable.Rows[4][dataTable.Columns[k].ColumnName],
                            dataTable.Rows[5][dataTable.Columns[k].ColumnName],
                            dataTable.Rows[6][dataTable.Columns[k].ColumnName]);
                        }
                        else
                        {
                            consolidatedDataTable.Rows.Add(dataTable.Columns[k].Caption,
                            dataTable.Rows[0][dataTable.Columns[k].ColumnName],
                            dataTable.Rows[1][dataTable.Columns[k].ColumnName]);
                        }

                    }
                }

                using (MemoryStream stream = new MemoryStream())
                {
                    using (SpreadsheetDocument doc = SpreadsheetDocument.Create(stream, SpreadsheetDocumentType.Workbook))
                    {
                        WorkbookPart workbookPart = ExcelHelper.CreateWorkbookPart(doc, "Отчет " + reportPeriodName);

                        WorksheetPart rId1 = ExcelHelper.CreateWorksheetPartAndImportDataTable(workbookPart, "rId1", 1, 1, (uint)consolidatedDataTable.Columns.Count,
                            reportTitle, consolidatedDataTable, 3, 1);

                        doc.WorkbookPart.Workbook.Save();
                    }

                    stream.Position = 0;
                    BinaryReader b = new BinaryReader(stream);
                    binData = b.ReadBytes((int)stream.Length);
                }

            }

            task.SetStatus(100, "Формирование файла MS Excel завершено");

            return binData;
        }


        public byte[] GetProjectsHoursForPMReportExcel(LongRunningTaskBase task, string userIdentityName, string reportTitle,
            string projectShortName,
            DateTime periodStart, DateTime periodEnd)
        {
            byte[] binData = null;

            task.SetStatus(55, "Получение данных из ТШ");
            var records = _tsHoursRecordService.Get(r => r.Where(x =>
                x.Project.ShortName.Equals(projectShortName)
                && (x.RecordStatus == TSRecordStatus.PMApproved || x.RecordStatus == TSRecordStatus.HDApproved)
                && x.RecordDate >= periodStart && x.RecordDate <= periodEnd).OrderBy(x => x.RecordDate).ToList());

            var employees = _employeeService.Get(x => x.Where(e => (e.EnrollmentDate == null || e.EnrollmentDate.Value <= periodEnd)
                                                                   && (e.DismissalDate == null || e.DismissalDate >= periodStart) && e.IsVacancy == false).ToList());

            List<Employee> employeeList = _employeeService.Get(x => x.Where(e => e.Department != null && e.IsVacancy == false).ToList()).OrderBy(e => e.FullName).ToList();

            DataTable dataTable = new DataTable();

            dataTable.Columns.Add("EmployeeTitle", typeof(string)).Caption = "Фамилия Имя Отчество";
            dataTable.Columns["EmployeeTitle"].ExtendedProperties["Width"] = (double)32;
            dataTable.Columns.Add("HoursDate", typeof(DateTime)).Caption = "Дата";
            dataTable.Columns["HoursDate"].ExtendedProperties["Width"] = (double)11;
            dataTable.Columns.Add("EmployeeHours", typeof(double)).Caption = "(ч)";
            dataTable.Columns["EmployeeHours"].ExtendedProperties["Width"] = (double)10;
            dataTable.Columns.Add("HoursDescription", typeof(string)).Caption = "Описание";
            dataTable.Columns["HoursDescription"].ExtendedProperties["Width"] = (double)160;
            dataTable.Columns.Add("Created", typeof(DateTime)).Caption = "Создано";
            dataTable.Columns["Created"].ExtendedProperties["Width"] = (double)20;
            dataTable.Columns.Add("RecordStatus", typeof(string)).Caption = "Статус";
            dataTable.Columns["RecordStatus"].ExtendedProperties["Width"] = (double)26;
            dataTable.Columns.Add("RecordSource", typeof(string)).Caption = "Источник";
            dataTable.Columns["RecordSource"].ExtendedProperties["Width"] = (double)26;
            dataTable.Columns.Add("_ISGROUPROW_", typeof(bool)).Caption = "_ISGROUPROW_";

            double totalHoursAmount = 0;

            foreach (var employee in employeeList)
            {
                String employeeFullName = "";

                if (String.IsNullOrEmpty(employee.LastName) == false)
                {
                    employeeFullName = employee.LastName;
                }
                if (String.IsNullOrEmpty(employee.FirstName) == false)
                {
                    employeeFullName += " " + employee.FirstName;
                }
                if (String.IsNullOrEmpty(employee.MidName) == false)
                {
                    employeeFullName += " " + employee.MidName;
                }

                employeeFullName = employeeFullName.Trim();

                int employeeRowIndex = -1;
                double employeeTotalHoursAmount = 0;

                var recordsForEmployee = records.Where(x => x.EmployeeID == employee.ID).OrderBy(x => x.RecordDate).ToList();

                foreach (var tsHoursRecord in recordsForEmployee)
                {

                    if (employeeRowIndex == -1)
                    {
                        dataTable.Rows.Add(employeeFullName,
                            null, 0, "", null, "", "", true);

                        employeeRowIndex = dataTable.Rows.Count - 1;
                    }

                    double hoursAmount = 0;

                    try
                    {
                        hoursAmount = tsHoursRecord.Hours.Value;
                    }
                    catch (Exception)
                    {

                    }

                    employeeTotalHoursAmount += hoursAmount;
                    totalHoursAmount += hoursAmount;

                    DateTime recDate = DateTime.MinValue;
                    try
                    {
                        recDate = tsHoursRecord.RecordDate.Value;
                    }
                    catch (Exception)
                    {

                    }

                    DateTime sentDate = DateTime.MinValue;
                    try
                    {
                        sentDate = tsHoursRecord.Created.Value;
                    }
                    catch (Exception)
                    {

                    }

                    string recordStatusDisplayName = "";

                    try
                    {
                        recordStatusDisplayName = ((DisplayAttribute)(tsHoursRecord.RecordStatus.GetType().GetMember(tsHoursRecord.RecordStatus.ToString()).First().GetCustomAttributes(true)[0])).Name;
                    }
                    catch (Exception)
                    {

                    }

                    string recordSourceDisplayName = "";

                    try
                    {
                        recordSourceDisplayName = ((DisplayAttribute)(tsHoursRecord.RecordSource.GetType().GetMember(tsHoursRecord.RecordSource.ToString()).First().GetCustomAttributes(true)[0])).Name;
                    }
                    catch (Exception)
                    {

                    }

                    dataTable.Rows.Add(employeeFullName,
                        recDate, hoursAmount, RPCSHelper.NormalizeAndTrimString(tsHoursRecord.Description), sentDate,
                        recordStatusDisplayName,
                        recordSourceDisplayName,
                        false);

                }

                if (employeeRowIndex != -1)
                {
                    dataTable.Rows[employeeRowIndex]["EmployeeHours"] = employeeTotalHoursAmount;
                }
            }

            dataTable.Rows.Add("Итого", null, totalHoursAmount, "", null, "", "", true);

            task.SetStatus(98, "Формирование файла MS Excel...");

            using (MemoryStream stream = new MemoryStream())
            {
                using (SpreadsheetDocument doc = SpreadsheetDocument.Create(stream, SpreadsheetDocumentType.Workbook))
                {
                    WorkbookPart workbookPart = ExcelHelper.CreateWorkbookPart(doc, "Отчет");

                    WorksheetPart rId1 = ExcelHelper.CreateWorksheetPartAndImportDataTable(workbookPart, "rId1", 1, 1, (uint)dataTable.Columns.Count,
                        reportTitle, dataTable, 3, 1);

                    doc.WorkbookPart.Workbook.Save();
                }

                stream.Position = 0;
                BinaryReader b = new BinaryReader(stream);
                binData = b.ReadBytes((int)stream.Length);
            }

            task.SetStatus(100, "Формирование файла MS Excel завершено");

            return binData;
        }

        public void SyncTSHoursRecordsWithExternalTimesheet(LongRunningTaskBase task,
            LongRunningTaskReport report,
            DateTime periodStart, DateTime periodEnd,
            bool deleteSyncedRecordsBeforeSync, bool updateAlreadyAddedRecords,
            bool stopOnError,
            int batchSaveRecordsLimit)
        {
            string recId = "";

            try
            {
                var currentEmployeeList = _employeeService.GetCurrentEmployees(new DateTimeRange(periodStart, periodEnd)).ToList()
                    .Where(e => e.Department != null).ToList();

                var projectList = _projectService.Get(x => x.ToList()).ToList();

                if (deleteSyncedRecordsBeforeSync == true)
                {
                    var recordToDeleteList = _tsHoursRecordService.Get(x => x.Where(r =>
                        r.RecordSource == TSRecordSource.ExternalTS
                        && r.RecordDate >= periodStart && r.RecordDate <= periodEnd).ToList());
                    if (recordToDeleteList != null && recordToDeleteList.Count() != 0)
                    {
                        task.SetStatus(50, "Удаление записей, мигрированных из внешнего ТШ, количество записей: " + recordToDeleteList.Count());

                        _tsHoursRecordService.RemoveRange(recordToDeleteList);
                        task.SetStatus(50, "Удаление записей, мигрированных из внешнего ТШ - завершено.");
                    }
                }

                int j = 0;
                bool error = false;

                for (int k = 0; k < HOURS.Count && error == false; k++)
                {
                    HOURSRecord hr = HOURS[k] as HOURSRecord;
                    recId = (hr.REC_ID != null) ? hr.REC_ID : "";

                    try
                    {
                        task.SetStatus(50 + 10 * k / HOURS.Count, "Синхронизация записи внешнего ТШ: " + (k + 1).ToString() + " (" + j.ToString() + ") из " + HOURS.Count.ToString() + ", ID: " + recId);

                        Employee employee = GetEmployeeByTimesheetNames(currentEmployeeList, hr.LNAME, hr.FNAME, hr.PATRONYMIC);

                        if (employee != null)
                        {
                            string projectShortName = hr.PROJECT_SHORT_NAME;

                            Project project = projectList.Where(p => p.ShortName == projectShortName).FirstOrDefault();

                            if (project != null)
                            {
                                DateTime recDate = DateTime.ParseExact(hr.REC_DATE, "MM/dd/yyyy", CultureInfo.InvariantCulture);
                                DateTime recCreated = DateTime.Now;
                                try
                                {
                                    recCreated = DateTime.ParseExact(hr.SENT, "MM/dd/yyyy", CultureInfo.InvariantCulture);
                                }
                                catch (Exception)
                                {

                                }

                                string description = (String.IsNullOrEmpty(hr.DESCRIPTION) == false && String.IsNullOrEmpty(hr.DESCRIPTION.Trim()) == false) ? hr.DESCRIPTION : "-";

                                //tsHoursRecordList.Where(r => r.ExternalSourceElementID == hr.REC_ID).FirstOrDefault();
                                var record = _tsHoursRecordService.Get(x =>
                                    x.Where(r =>
                                        r.RecordSource == TSRecordSource.ExternalTS &&
                                        r.ExternalSourceElementID == hr.REC_ID).ToList()).FirstOrDefault();

                                if (record != null)
                                {
                                    if (updateAlreadyAddedRecords == true)
                                    {
                                        record.RecordDate = recDate;
                                        record.Hours = (double)hr.AMOUNT / 10d;
                                        record.EmployeeID = employee.ID;
                                        record.ProjectID = project.ID;
                                        record.Description = RPCSHelper.NormalizeAndTrimString(description);
                                        record.RecordStatus = TSRecordStatus.PMApproved;
                                        record.Created = recCreated;
                                        _tsHoursRecordService.UpdateWithoutVersion(record);
                                        j++;
                                    }
                                }
                                else
                                {
                                    record = new TSHoursRecord();
                                    record.RecordSource = TSRecordSource.ExternalTS;
                                    record.ExternalSourceElementID = hr.REC_ID;
                                    record.RecordDate = recDate;
                                    record.Hours = (double)hr.AMOUNT / 10d;
                                    record.EmployeeID = employee.ID;
                                    record.ProjectID = project.ID;
                                    record.Description = RPCSHelper.NormalizeAndTrimString(description);
                                    record.RecordStatus = TSRecordStatus.PMApproved;
                                    record.Created = recCreated;
                                    _tsHoursRecordService.Add(record);
                                    j++;
                                }
                            }
                        }

                        if (j >= batchSaveRecordsLimit)
                        {
                            j = 0;
                        }
                    }
                    catch (Exception e)
                    {
                        task.SetStatus(-1, "Ошибка: " + e.Message);
                        report.AddReportEvent(e.Message + " " + e.StackTrace + " " + e.TargetSite.ToString() + ", ID: " + recId);
                        if (stopOnError == true)
                        {
                            error = true;
                        }
                    }
                }

                if (j > 0)
                {
                    j = 0;
                }
            }
            catch (Exception e)
            {
                task.SetStatus(-1, "Ошибка: " + e.Message);
                report.AddReportEvent(e.Message + " " + e.StackTrace + " " + e.TargetSite.ToString() + ", ID: " + recId);
            }
        }

        public void SyncVacationRecordsWithExternalTimesheet(LongRunningTaskBase task,
            LongRunningTaskReport report,
            DateTime periodStart, DateTime periodEnd,
            bool deleteSyncedRecordsBeforeSync, bool updateAlreadyAddedRecords,
            bool stopOnError,
            int batchSaveRecordsLimit)
        {
            string recId = "";

            try
            {
                var currentEmployeeList = _employeeService.GetCurrentEmployees(new DateTimeRange(periodStart, periodEnd)).ToList()
                    .Where(e => e.Department != null).ToList();

                if (deleteSyncedRecordsBeforeSync == true)
                {
                    var recordToDeleteList = _vacationRecordService.Get(x => x.Where(r =>
                        r.RecordSource == VacationRecordSource.ExternalTS
                        && r.VacationBeginDate <= periodEnd && r.VacationEndDate >= periodStart).ToList());
                    if (recordToDeleteList != null && recordToDeleteList.Count() != 0)
                    {
                        task.SetStatus(50, "Удаление записей отпусков, мигрированных из внешнего ТШ, количество записей: " + recordToDeleteList.Count());


                        _vacationRecordService.RemoveRange(recordToDeleteList);
                        task.SetStatus(50, "Удаление записей отпусков, мигрированных из внешнего ТШ - завершено.");
                    }
                }

                var vacationRecordList = _vacationRecordService.Get(x =>
                    x.Where(r => r.RecordSource == VacationRecordSource.ExternalTS).ToList());
                int j = 0;
                bool error = false;
                for (int k = 0; k < VACATIONS.Count && error == false; k++)
                {
                    VACATIONSRecord vr = VACATIONS[k] as VACATIONSRecord;
                    recId = (vr.VACATION_ID != null) ? vr.VACATION_ID : "";

                    try
                    {
                        task.SetStatus(50 + 10 * k / HOURS.Count, "Синхронизация записи отпуска внешнего ТШ: " + (k + 1).ToString() + " (" + j.ToString() + ") из " + VACATIONS.Count.ToString() + ", ID: " + recId);

                        Employee employee = GetEmployeeByTimesheetNames(currentEmployeeList, vr.LNAME, vr.FNAME, vr.PATRONYMIC);

                        if (employee != null)
                        {
                            DateTime vacationBeginDate = DateTime.ParseExact(vr.DATE_START, "MM/dd/yyyy", CultureInfo.InvariantCulture);
                            DateTime vacationEndDate = DateTime.ParseExact(vr.DATE_END, "MM/dd/yyyy", CultureInfo.InvariantCulture);

                            VacationRecord record = vacationRecordList.Where(r => r.ExternalSourceElementID == vr.VACATION_ID).FirstOrDefault();

                            if (record != null)
                            {
                                if (updateAlreadyAddedRecords == true)
                                {
                                    record.EmployeeID = employee.ID;
                                    record.VacationBeginDate = vacationBeginDate;
                                    record.VacationEndDate = vacationEndDate;
                                    record.VacationDays = Convert.ToInt32((vacationEndDate - vacationBeginDate).TotalDays) + 1;
                                    _vacationRecordService.UpdateWithoutVersion(record);
                                    j++;
                                }
                            }
                            else
                            {
                                record = new VacationRecord();
                                record.EmployeeID = employee.ID;
                                record.VacationBeginDate = vacationBeginDate;
                                record.VacationEndDate = vacationEndDate;
                                record.VacationDays = Convert.ToInt32((vacationEndDate - vacationBeginDate).TotalDays) + 1;
                                record.VacationType = VacationRecordType.VacationPaid;
                                record.RecordSource = VacationRecordSource.ExternalTS;
                                record.ExternalSourceElementID = vr.VACATION_ID;
                                _vacationRecordService.Delete(record.ID);
                                j++;
                            }

                        }

                        if (j >= batchSaveRecordsLimit)
                        {
                            j = 0;
                        }
                    }
                    catch (Exception e)
                    {
                        task.SetStatus(-1, "Ошибка: " + e.Message);
                        report.AddReportEvent(e.Message + " " + e.StackTrace + " " + e.TargetSite.ToString() + ", ID: " + recId);
                        if (stopOnError == true)
                        {
                            error = true;
                        }
                    }
                }

                if (j > 0)
                {
                    j = 0;
                }
            }
            catch (Exception e)
            {
                task.SetStatus(-1, "Ошибка: " + e.Message);
                report.AddReportEvent(e.Message + " " + e.StackTrace + " " + e.TargetSite.ToString() + ", ID: " + recId);
            }
        }

        public void createPROJECTS(LongRunningTaskBase task, OracleConnection conn)
        {

            using (OracleCommand command = conn.CreateCommand())
            {
                string sql = "select * from PROJECTS";
                command.CommandText = sql;

                using (OracleDataReader reader = command.ExecuteReader())
                {
                    try
                    {
                        while (reader.Read())
                        {
                            PROJECTSRecord pr = new PROJECTSRecord();
                            PROJECTS.Add(pr);
                            pr.PROJECT_ID = reader.GetOracleValue(0).ToString();
                            pr.PROJECT_SHORT_NAME = reader.GetString(1).Trim();
                            pr.PROJECT_NAME = reader.GetString(2).Trim();
                            pr.PROJECT_LEADER_ID = reader.GetOracleValue(3).ToString();
                            pr.DATE_END = reader.GetOracleValue(4).ToString();
                            pr.UNIT_ID = reader.GetOracleValue(5).ToString();
                            pr.DELETED = reader.GetOracleValue(6).ToString();
                            if (pr.PROJECT_SHORT_NAME.Length == 0)
                            {
                                task.SetStatus(-1, "Отсутствует краткое название проекта для PROJECT_ID == " + pr.PROJECT_ID);
                            }
                            else
                            {
                                task.SetStatus(-1, pr.PROJECT_SHORT_NAME);
                            }
                            if (pr.PROJECT_NAME.Length == 0)
                            {
                                task.SetStatus(-1, "Отсутствует название проекта для PROJECT_ID == " + pr.PROJECT_ID);
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        task.SetStatus(-1, "Ошибка: " + e.Message);
                    }
                    reader.Close();
                }
            }
        }

        public void createPROJECTSForPM(LongRunningTaskBase task, OracleConnection conn,
            ArrayList projectShortNames)
        {
            string sqlINStatement = "";

            if (projectShortNames != null && projectShortNames.Count != 0)
            {
                foreach (string value in projectShortNames)
                {
                    if (String.IsNullOrEmpty(sqlINStatement) == true)
                    {
                        sqlINStatement = "'" + value + "'";
                    }
                    else
                    {
                        sqlINStatement += ", '" + value + "'";
                    }
                }
            }

            if (String.IsNullOrEmpty(sqlINStatement) == false)
            {
                using (OracleCommand command = conn.CreateCommand())
                {
                    string sql = "select * from PROJECTS WHERE project_short_name IN (" + sqlINStatement + ")";

                    command.CommandText = sql;

                    using (OracleDataReader reader = command.ExecuteReader())
                    {
                        try
                        {
                            while (reader.Read())
                            {
                                PROJECTSRecord pr = new PROJECTSRecord();
                                PROJECTS.Add(pr);
                                pr.PROJECT_ID = reader.GetOracleValue(0).ToString();
                                pr.PROJECT_SHORT_NAME = reader.GetString(1).Trim();
                                pr.PROJECT_NAME = reader.GetString(2).Trim();
                                pr.PROJECT_LEADER_ID = reader.GetOracleValue(3).ToString();
                                pr.DATE_END = reader.GetOracleValue(4).ToString();
                                pr.UNIT_ID = reader.GetOracleValue(5).ToString();
                                pr.DELETED = reader.GetOracleValue(6).ToString();
                                if (pr.PROJECT_SHORT_NAME.Length == 0)
                                {
                                    task.SetStatus(-1, "Отсутствует краткое название проекта для PROJECT_ID == " + pr.PROJECT_ID);
                                }
                                else
                                {
                                    task.SetStatus(-1, pr.PROJECT_SHORT_NAME);
                                }
                                if (pr.PROJECT_NAME.Length == 0)
                                {
                                    task.SetStatus(-1, "Отсутствует название проекта для PROJECT_ID == " + pr.PROJECT_ID);
                                }
                            }
                        }
                        catch (Exception e)
                        {
                            task.SetStatus(-1, "Ошибка: " + e.Message);
                        }
                        reader.Close();
                    }
                }
            }
        }


        public void createPERMANENT_PROJECTS(LongRunningTaskBase task, OracleConnection conn)
        {
            using (OracleCommand command = conn.CreateCommand())
            {


                string sql = "select pp.*,\n" +
                  "      u.*,\n" +
                  "      p.*\n" +
                  "      from  permanent_projects pp,\n" +
                  "            users u,\n" +
                  "            projects p\n" +
                  "      where 1=1\n" +
                  "      and pp.PROJECT_ID = p.PROJECT_ID(+)\n" +
                  "      and pp.USER_ID = u.user_id(+)";
                command.CommandText = sql;

                using (OracleDataReader reader = command.ExecuteReader())
                {
                    try
                    {
                        while (reader.Read())
                        {

                            PERMANENT_PROJECTSRecord ppr = new PERMANENT_PROJECTSRecord();
                            PERMANENT_PROJECTS.Add(ppr);
                            ppr.PERMANENT_PROJECT_ID = reader.GetOracleValue(0).ToString();
                            ppr.PROJECT_ID = reader.GetOracleValue(1).ToString().Trim();
                            ppr.USER_ID = reader.GetOracleValue(2).ToString();
                            try
                            {
                                ppr.HOURS = (int)(reader.GetDouble(3) * 10d);
                            }
                            catch (Exception ex)
                            {
                                ppr.HOURS = 0;
                            }
                            ppr.FNAME = reader.GetOracleValue(7).ToString();
                            ppr.PATRONYMIC = reader.GetOracleValue(8).ToString();
                            ppr.LNAME = reader.GetOracleValue(9).ToString();
                            ppr.TERMINATED = (reader.GetOracleValue(15) != null) ? reader.GetOracleValue(15).ToString() : "";
                            try
                            {
                                int i = ppr.TERMINATED.IndexOf(' ');
                                if (i > 0)
                                {
                                    ppr.TERMINATED = ppr.TERMINATED.Substring(0, i);
                                }
                            }
                            catch (Exception ex)
                            {
                                ppr.TERMINATED = "";
                            }
                            ppr.HIRED = (reader.GetOracleValue(16) != null) ? reader.GetOracleValue(16).ToString() : "";
                            try
                            {
                                int i = ppr.HIRED.IndexOf(' ');
                                if (i > 0)
                                {
                                    ppr.HIRED = ppr.HIRED.Substring(0, i);
                                }
                            }
                            catch (Exception ex)
                            {
                                ppr.HIRED = "";
                            }

                            ppr.PROJECT_SHORT_NAME = reader.GetOracleValue(20).ToString().Trim();
                            ppr.PROJECT_NAME = reader.GetOracleValue(21).ToString().Trim();
                            if ((ppr.PROJECT_SHORT_NAME.Length == 0) || (ppr.PROJECT_NAME.Length == 0))
                            {
                                int i0 = 0;
                                for (; i0 < PROJECTS.Count; i0++)
                                {
                                    PROJECTSRecord pr = PROJECTS[i0] as PROJECTSRecord;
                                    if (ppr.PROJECT_ID.Equals(pr.PROJECT_ID) == true)
                                    {
                                        ppr.PROJECT_SHORT_NAME = pr.PROJECT_SHORT_NAME;
                                        ppr.PROJECT_NAME = pr.PROJECT_NAME;
                                        break;
                                    }
                                }
                                if (i0 == PROJECTS.Count)
                                {
                                    task.SetStatus(-1, "Не найдено название проекта для PROJECT_ID == " + ppr.PROJECT_ID);
                                }
                            }

                            ppr.PROJECT_DATE_END = (reader.GetOracleValue(23) != null) ? reader.GetOracleValue(23).ToString() : "";
                            try
                            {
                                int i = ppr.PROJECT_DATE_END.IndexOf(' ');
                                if (i > 0)
                                {
                                    ppr.PROJECT_DATE_END = ppr.PROJECT_DATE_END.Substring(0, i);
                                }
                            }
                            catch (Exception ex)
                            {
                                ppr.PROJECT_DATE_END = "";
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        task.SetStatus(-1, "Ошибка: " + e.Message);
                    }

                    reader.Close();
                }
            }
        }


        public void createHOURS(LongRunningTaskBase task, OracleConnection conn, String dateBegin, String dateEnd, int monthCount)
        {

            using (OracleCommand command = conn.CreateCommand())
            {

                string sql = "select h.*,\n" +
                  "       u.*,\n" +
                  "       p.*\n" +
                  "from  hours h,\n" +
                  "      users u,\n" +
                  "      projects p\n" +
                  "where 1=1\n" +
                  "  and h.PROJECT_ID = p.PROJECT_ID(+)\n" +
                  "  and h.USER_ID = u.user_id(+)\n" +
                  "  and h.rec_date between to_date('" + dateBegin + "', 'YYYY-MM-DD') and to_date('" + dateEnd + "', 'YYYY-MM-DD')";

                command.CommandText = sql;

                using (OracleDataReader reader = command.ExecuteReader())
                {

                    try
                    {
                        int recordsCount = 0;
                        while (reader.Read())
                        {

                            HOURSRecord hr = new HOURSRecord();
                            HOURS.Add(hr);
                            hr.REC_ID = reader.GetOracleValue(0).ToString();
                            hr.USER_ID = reader.GetOracleValue(1).ToString();
                            hr.PROJECT_ID = reader.GetOracleValue(2).ToString();
                            hr.REC_DATE = reader.GetOracleValue(3).ToString();
                            try
                            {
                                int i = hr.REC_DATE.IndexOf(' ');
                                if (i > 0)
                                {
                                    hr.REC_DATE = hr.REC_DATE.Substring(0, i);
                                }
                            }
                            catch (Exception ex)
                            {
                                hr.REC_DATE = "";
                            }
                            try
                            {
                                hr.AMOUNT = ((double)((decimal)(reader.GetDecimal(4) * 100))) / 10;
                            }
                            catch (Exception ex)
                            {
                                hr.AMOUNT = 0;
                            }
                            hr.DESCRIPTION = reader.GetOracleValue(5).ToString();
                            hr.SENT = reader.GetOracleValue(6).ToString();
                            try
                            {
                                int i = hr.SENT.IndexOf(' ');
                                if (i > 0)
                                {
                                    hr.SENT = hr.SENT.Substring(0, i);
                                }
                            }
                            catch (Exception ex)
                            {
                                hr.SENT = "";
                            }
                            hr.ORIG_PROJECT_ID = reader.GetOracleValue(7).ToString();
                            hr.USER_ID_1 = reader.GetOracleValue(8).ToString();
                            hr.USER_DN = reader.GetOracleValue(9).ToString();
                            hr.FNAME = reader.GetOracleValue(10).ToString();
                            hr.PATRONYMIC = reader.GetOracleValue(11).ToString();
                            hr.LNAME = reader.GetOracleValue(12).ToString();
                            hr.LNMTIME = reader.GetOracleValue(13).ToString();
                            try
                            {
                                int i = hr.LNMTIME.IndexOf(' ');
                                if (i > 0)
                                {
                                    hr.LNMTIME = hr.LNMTIME.Substring(0, i);
                                }
                            }
                            catch (Exception ex)
                            {
                                hr.LNMTIME = "";
                            }
                            hr.EMAIL = reader.GetOracleValue(14).ToString();
                            hr.RANK = reader.GetOracleValue(15).ToString();
                            hr.UNIT_ID = reader.GetOracleValue(16).ToString();
                            hr.UID_ = reader.GetOracleValue(20).ToString();
                            hr.PROJECT_ID_1 = reader.GetOracleValue(22).ToString();
                            hr.PROJECT_SHORT_NAME = reader.GetOracleValue(23).ToString().Trim();
                            hr.PROJECT_NAME = reader.GetOracleValue(24).ToString().Trim();
                            hr.PROJECT_END = reader.GetOracleValue(26).ToString();
                            try
                            {
                                int i = hr.PROJECT_END.IndexOf(' ');
                                if (i > 0)
                                {
                                    hr.PROJECT_END = hr.PROJECT_END.Substring(0, i);
                                }
                            }
                            catch (Exception ex)
                            {
                                hr.PROJECT_END = "";
                            }
                            if ((hr.PROJECT_SHORT_NAME.Length == 0) || (hr.PROJECT_NAME.Length == 0))
                            {
                                bool projectRecordFound = false;
                                for (int i0 = 0; i0 < PROJECTS.Count; i0++)
                                {
                                    PROJECTSRecord pr = PROJECTS[i0] as PROJECTSRecord;
                                    if (hr.PROJECT_ID.Equals(pr.PROJECT_ID) == true)
                                    {
                                        hr.PROJECT_SHORT_NAME = pr.PROJECT_SHORT_NAME;
                                        hr.PROJECT_NAME = pr.PROJECT_NAME;
                                        break;
                                    }
                                }
                                if (projectRecordFound == false)
                                {
                                    task.SetStatus(-1, "Не найдено название проекта для PROJECT_ID == " + hr.PROJECT_ID);
                                }
                            }

                            recordsCount++;
                            task.SetStatus(5 + 45 * recordsCount / (7000 * monthCount), "Прочитано записей о трудозатратах: " + recordsCount.ToString());
                        }
                    }
                    catch (Exception e)
                    {
                        task.SetStatus(-1, "Ошибка: " + e.Message);
                    }

                    reader.Close();
                }
            }
        }

        public void createHOURSForPM(LongRunningTaskBase task, OracleConnection conn, String dateBegin, String dateEnd, int monthCount)
        {
            string sqlINStatement = "";

            if (PROJECTS != null && PROJECTS.Count != 0)
            {
                foreach (PROJECTSRecord pr in PROJECTS)
                {
                    if (String.IsNullOrEmpty(sqlINStatement) == true)
                    {
                        sqlINStatement = pr.PROJECT_ID;
                    }
                    else
                    {
                        sqlINStatement += ", " + pr.PROJECT_ID;
                    }
                }
            }


            if (String.IsNullOrEmpty(sqlINStatement) == false)
            {
                using (OracleCommand command = conn.CreateCommand())
                {

                    string sql = "select h.*,\n" +
                      "       u.*,\n" +
                      "       p.*\n" +
                      "from  hours h,\n" +
                      "      users u,\n" +
                      "      projects p\n" +
                      "where 1=1\n" +
                      "  and h.PROJECT_ID = p.PROJECT_ID(+)\n" +
                      "  and h.PROJECT_ID IN (" + sqlINStatement + ")\n" +
                      "  and h.USER_ID = u.user_id(+)\n" +
                      "  and h.rec_date between to_date('" + dateBegin + "', 'YYYY-MM-DD') and to_date('" + dateEnd + "', 'YYYY-MM-DD')\n" +
                      "order by h.rec_date";

                    command.CommandText = sql;

                    using (OracleDataReader reader = command.ExecuteReader())
                    {

                        try
                        {
                            int recordsCount = 0;
                            while (reader.Read())
                            {

                                HOURSRecord hr = new HOURSRecord();
                                HOURS.Add(hr);
                                hr.REC_ID = reader.GetOracleValue(0).ToString();
                                hr.USER_ID = reader.GetOracleValue(1).ToString();
                                hr.PROJECT_ID = reader.GetOracleValue(2).ToString();
                                hr.REC_DATE = reader.GetOracleValue(3).ToString();
                                try
                                {
                                    int i = hr.REC_DATE.IndexOf(' ');
                                    if (i > 0)
                                    {
                                        hr.REC_DATE = hr.REC_DATE.Substring(0, i);
                                    }
                                }
                                catch (Exception ex)
                                {
                                    hr.REC_DATE = "";
                                }
                                try
                                {
                                    hr.AMOUNT = ((double)((decimal)(reader.GetDecimal(4) * 100))) / 10;
                                }
                                catch (Exception ex)
                                {
                                    hr.AMOUNT = 0;
                                }
                                hr.DESCRIPTION = reader.GetOracleValue(5).ToString();
                                hr.SENT = reader.GetOracleValue(6).ToString();
                                try
                                {
                                    int i = hr.SENT.IndexOf(' ');
                                    if (i > 0)
                                    {
                                        hr.SENT = hr.SENT.Substring(0, i);
                                    }
                                }
                                catch (Exception ex)
                                {
                                    hr.SENT = "";
                                }
                                hr.ORIG_PROJECT_ID = reader.GetOracleValue(7).ToString();
                                hr.USER_ID_1 = reader.GetOracleValue(8).ToString();
                                hr.USER_DN = reader.GetOracleValue(9).ToString();
                                hr.FNAME = reader.GetOracleValue(10).ToString();
                                hr.PATRONYMIC = reader.GetOracleValue(11).ToString();
                                hr.LNAME = reader.GetOracleValue(12).ToString();
                                hr.LNMTIME = reader.GetOracleValue(13).ToString();
                                try
                                {
                                    int i = hr.LNMTIME.IndexOf(' ');
                                    if (i > 0)
                                    {
                                        hr.LNMTIME = hr.LNMTIME.Substring(0, i);
                                    }
                                }
                                catch (Exception ex)
                                {
                                    hr.LNMTIME = "";
                                }
                                hr.EMAIL = reader.GetOracleValue(14).ToString();
                                hr.RANK = reader.GetOracleValue(15).ToString();
                                hr.UNIT_ID = reader.GetOracleValue(16).ToString();
                                hr.UID_ = reader.GetOracleValue(20).ToString();
                                hr.PROJECT_ID_1 = reader.GetOracleValue(22).ToString();
                                hr.PROJECT_SHORT_NAME = reader.GetOracleValue(23).ToString().Trim();
                                hr.PROJECT_NAME = reader.GetOracleValue(24).ToString().Trim();
                                hr.PROJECT_END = reader.GetOracleValue(26).ToString();
                                try
                                {
                                    int i = hr.PROJECT_END.IndexOf(' ');
                                    if (i > 0)
                                    {
                                        hr.PROJECT_END = hr.PROJECT_END.Substring(0, i);
                                    }
                                }
                                catch (Exception ex)
                                {
                                    hr.PROJECT_END = "";
                                }
                                if ((hr.PROJECT_SHORT_NAME.Length == 0) || (hr.PROJECT_NAME.Length == 0))
                                {
                                    bool projectRecordFound = false;
                                    for (int i0 = 0; i0 < PROJECTS.Count; i0++)
                                    {
                                        PROJECTSRecord pr = PROJECTS[i0] as PROJECTSRecord;
                                        if (hr.PROJECT_ID.Equals(pr.PROJECT_ID) == true)
                                        {
                                            hr.PROJECT_SHORT_NAME = pr.PROJECT_SHORT_NAME;
                                            hr.PROJECT_NAME = pr.PROJECT_NAME;
                                            break;
                                        }
                                    }
                                    if (projectRecordFound == false)
                                    {
                                        task.SetStatus(-1, "Не найдено название проекта для PROJECT_ID == " + hr.PROJECT_ID);
                                    }
                                }

                                recordsCount++;
                                task.SetStatus(5 + 45 * recordsCount / (7000 * monthCount), "Прочитано записей о трудозатратах: " + recordsCount.ToString());
                            }
                        }
                        catch (Exception e)
                        {
                            task.SetStatus(-1, "Ошибка: " + e.Message);
                        }

                        reader.Close();
                    }
                }
            }
        }


        public void createVACATIONS(LongRunningTaskBase task, OracleConnection conn, String dateBegin, String dateEnd)
        {
            using (OracleCommand command = conn.CreateCommand())
            {

                string sql = "select v.*, u.*\n" +
                    " from vacations v, users u\n" +
                    " where 1=1\n" +
                    " and v.USER_ID = u.user_id(+)\n" +
                    " and v.DATE_START <= to_date('" + dateEnd + "', 'YYYY-MM-DD')\n" +
                    " and v.DATE_END >= to_date('" + dateBegin + "', 'YYYY-MM-DD') ";

                command.CommandText = sql;

                using (OracleDataReader reader = command.ExecuteReader())
                {

                    try
                    {
                        while (reader.Read())
                        {

                            VACATIONSRecord vr = new VACATIONSRecord();
                            VACATIONS.Add(vr);
                            vr.VACATION_ID = reader.GetOracleValue(0).ToString();
                            vr.USER_ID = reader.GetOracleValue(1).ToString();

                            vr.DATE_START = (reader.GetOracleValue(2) != null) ? reader.GetOracleValue(2).ToString() : "";
                            try
                            {
                                int i = vr.DATE_START.IndexOf(' ');
                                if (i > 0)
                                {
                                    vr.DATE_START = vr.DATE_START.Substring(0, i);
                                }
                            }
                            catch (Exception ex)
                            {
                                vr.DATE_START = "";
                            }

                            vr.DATE_END = (reader.GetOracleValue(3) != null) ? reader.GetOracleValue(3).ToString() : "";
                            try
                            {
                                int i = vr.DATE_END.IndexOf(' ');
                                if (i > 0)
                                {
                                    vr.DATE_END = vr.DATE_END.Substring(0, i);
                                }
                            }
                            catch (Exception ex)
                            {
                                vr.DATE_END = "";
                            }

                            try
                            {
                                vr.DAYS_COUNT = (int)(reader.GetInt32(4));
                            }
                            catch (Exception exception)
                            {
                                vr.DAYS_COUNT = 0;
                            }

                            vr.FNAME = reader.GetOracleValue(12).ToString();
                            vr.PATRONYMIC = reader.GetOracleValue(13).ToString();
                            vr.LNAME = reader.GetOracleValue(14).ToString();
                        }

                    }
                    catch (Exception e)
                    {
                        task.SetStatus(-1, "Ошибка: " + e.Message);
                    }

                    reader.Close();
                }

            }
        }

    }
}
