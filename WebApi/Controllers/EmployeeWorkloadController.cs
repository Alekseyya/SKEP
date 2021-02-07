using System;
using Core;
using Core.BL.Interfaces;
using Core.Models.RBAC;
using Microsoft.AspNetCore.Mvc;
using WebApi.Dto;
using WebApi.Dto.Workload;
using WebApi.RBAC.Attributes;

namespace WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EmployeeWorkloadController : Controller
    {
        private IProjectService _projectSvc;

        private IEmployeeWorkloadService _workloadSvc;

        private IDepartmentService _departmentSvc;

        private IEmployeeService _employeeSvc;

        public EmployeeWorkloadController(IEmployeeWorkloadService workloadSvc, IProjectService projectSvc, IDepartmentService departmentSvc, IEmployeeService employeeSvc)
        {
            if (workloadSvc == null)
                throw new ArgumentNullException(nameof(workloadSvc));
            if (projectSvc == null)
                throw new ArgumentNullException(nameof(projectSvc));
            if (departmentSvc == null)
                throw new ArgumentNullException(nameof(departmentSvc));
            if (employeeSvc == null)
                throw new ArgumentNullException(nameof(employeeSvc));

            _workloadSvc = workloadSvc;
            _projectSvc = projectSvc;
            _departmentSvc = departmentSvc;
            _employeeSvc = employeeSvc;
        }

        [HttpGet]
        [Route("api/projects/{projectId}/employeeworkload/{dateFrom}/{dateTo}", Name = "ApiEmployeesWorkloadProject")]
        [OperationApiActionFilter(nameof(Operation.ApiAccessDataView))]
        public IActionResult GetEmployeesWorkloadForProject(int projectId, DateTime dateFrom, DateTime dateTo)
        {
            if (dateFrom > dateTo)
                return BadRequest("Дата начала периода должна быть меньше или равна дате окончания периода");

            var project = _projectSvc.GetById(projectId);
            if (project == null)
                return NotFound();

            DateTime projectStartDate = project.BeginDate.HasValue ? project.BeginDate.Value : new DateTime(DateTime.Today.Year, 1, 1);
            DateTime projectEndDate = project.EndDate.HasValue ? project.EndDate.Value : new DateTime(DateTime.Today.Year, 12, 31);


            var projectWorkloadForPeriod = _workloadSvc.GetProjectWorkload(projectId, new DateTimeRange(dateFrom.Date, dateTo.Date));
            var projectWorkloadTotal = _workloadSvc.GetProjectWorkload(projectId, new DateTimeRange(projectStartDate.Date, projectEndDate.Date));

            // TODO: Стоит вынести логику в отдельный класс encoder/transformer
            var projectWorkloadDto = new ProjectWorkloadDto()
            {
                ProjectId = project.ID,
                ProjectStartDate = project.BeginDate,
                ProjectEndDate = project.EndDate,
                TotalPossibleHours = (int)Math.Round(projectWorkloadTotal.TotalHours),
                PeriodStartDate = dateFrom.Date,
                PeriodEndDate = dateTo.Date,
                PeriodPossibleHours = (int)Math.Round(projectWorkloadForPeriod.TotalHours)
            };
            if (projectWorkloadForPeriod.EmployeeWorkloads.Count > 0)
            {
                foreach (var workloadRecord in projectWorkloadForPeriod.EmployeeWorkloads[0].WorkloadRecords)
                    projectWorkloadDto.DateRanges.Add(new DateTimeRange(workloadRecord.DateFrom, workloadRecord.DateTo));
                foreach (var employeeWorkload in projectWorkloadForPeriod.EmployeeWorkloads)
                {
                    var employeeWorkloadDto = new ProjectEmployeeWorkloadDto
                    {
                        EmployeeId = employeeWorkload.Employee.ID,
                        EmployeeFullName = employeeWorkload.Employee.FullName
                    };
                    foreach (var workloadRecord in employeeWorkload.WorkloadRecords)
                    {
                        var workloadRecordDto = new ProjectEmployeeWorkloadRecordDto
                        {
                            CurrentProjectPercents = (int)Math.Round(workloadRecord.CurrentProjectPercents),
                            TotalPercents = (int)Math.Round(workloadRecord.TotalPercents)
                        };
                        employeeWorkloadDto.Workloads.Add(workloadRecordDto);
                    }
                    projectWorkloadDto.EmployeesWorkloads.Add(employeeWorkloadDto);
                }
            }

            return Ok(projectWorkloadDto);
        }

        [HttpGet]
        [Route("api/departments/{departmentId}/employeeworkload/{dateFrom}/{dateTo}/{workloadPeriod}", Name = "ApiEmployeesWorkloadDepartment")]
        [OperationApiActionFilter(nameof(Operation.ApiAccessDataView))]
        public IActionResult GetEmployeesWorkloadForDepartment(int departmentId, DateTime dateFrom, DateTime dateTo, WorkloadPeriod workloadPeriod)
        {
            if (dateFrom > dateTo)
                return BadRequest("Дата начала периода должна быть меньше или равна дате окончания периода");

            var department = _departmentSvc.GetById(departmentId);
            if (department == null)
                return NotFound();

            return GetDepartmentWorkloadResult(departmentId, dateFrom, dateTo, workloadPeriod);
        }

        [HttpGet]
        [Route("api/departments/{departmentId}/employeeworkload/{childDepartmentId}/{dateFrom}/{dateTo}/{workloadPeriod}", Name = "ApiEmployeesWorkloadChildDepartment")]
        [OperationApiActionFilter(nameof(Operation.ApiAccessDataView))]
        public IActionResult GetEmployeesWorkloadForChildDepartment(int departmentId, int childDepartmentId, DateTime dateFrom, DateTime dateTo, WorkloadPeriod workloadPeriod)
        {
            if (dateFrom > dateTo)
                return BadRequest("Дата начала периода должна быть меньше или равна дате окончания периода");

            var childDepartment = _departmentSvc.GetById(childDepartmentId);
            if (childDepartment == null)
                return NotFound();

            if (childDepartment.ParentDepartmentID != departmentId)
                return BadRequest("Указанное подразделение не является родительским к указанному дочернему подразделению");

            return GetDepartmentWorkloadResult(childDepartmentId, dateFrom, dateTo, workloadPeriod);
        }

        [HttpGet]
        [Route("api/projectmanagers/{employeeId}/employeeworkload/{dateFrom}/{dateTo}/{workloadPeriod}", Name = "ApiEmployeesWorkloadProjectManager")]
        [OperationApiActionFilter(nameof(Operation.ApiAccessDataView))]
        public IActionResult GetEmployeesWorkloadForProjectManager(int employeeId, DateTime dateFrom, DateTime dateTo, WorkloadPeriod workloadPeriod)
        {
            if (dateFrom > dateTo)
                return BadRequest("Дата начала периода должна быть меньше или равна дате окончания периода");

            var employee = _employeeSvc.GetById(employeeId);
            if (employee == null)
                return NotFound();
            var dateRange = new DateTimeRange(dateFrom, dateTo);

            var subordinatesWorkload = _workloadSvc.GetPMSubordinatesWorkload(employeeId, dateRange, workloadPeriod);
            var subordinatesWorkloadDto = new SubordinatesWorkloadDto
            {
                ManagerEmployeeId = employee.ID
            };
            if (subordinatesWorkload.SubordinateEmployeesWorkloads.Count > 0)
            {
                foreach (var workloadRecord in subordinatesWorkload.SubordinateEmployeesWorkloads[0].WorkloadRecords[0].WorkloadRecords)
                    subordinatesWorkloadDto.DateRanges.Add(new DateTimeRange(workloadRecord.DateFrom, workloadRecord.DateTo));
                foreach (var employeeItem in subordinatesWorkload.SubordinateEmployeesWorkloads)
                {
                    var subordinateEmployeeDto = new EmployeeProjectWorkloadDto
                    {
                        Employee = new BasicEmployeeDto(employeeItem.Employee)
                    };
                    foreach (var workloadItem in employeeItem.WorkloadRecords[0].WorkloadRecords)
                        subordinateEmployeeDto.TotalWorkloadPercents.Add((int)Math.Round(workloadItem.TotalPercents));
                    foreach (var projectItem in employeeItem.WorkloadRecords)
                    {
                        var projectWorkloadDto = new EmployeeProjectWorkloadRecordDto
                        {
                            Project = new BasicProjectDto(projectItem.Project)
                        };
                        foreach (var workloadItem in projectItem.WorkloadRecords)
                            projectWorkloadDto.WorkloadPercents.Add((int)Math.Round(workloadItem.CurrentProjectPercents));
                        subordinateEmployeeDto.ProjectWorkloads.Add(projectWorkloadDto);
                    }
                    subordinatesWorkloadDto.EmployeesWorkloads.Add(subordinateEmployeeDto);
                }

            }

            return Ok(subordinatesWorkloadDto);
        }

        private IActionResult GetDepartmentWorkloadResult(int departmentId, DateTime dateFrom, DateTime dateTo, WorkloadPeriod workloadPeriod)
        {
            var departmentWorkload = _workloadSvc.GetDepartmentWorkload(departmentId, new DateTimeRange(dateFrom.Date, dateTo.Date), workloadPeriod);

            var departmentWorkloadDto = new DepartmentWorkloadDto
            {
                DepartmentId = departmentId,
                PeriodStartDate = dateFrom.Date,
                PeriodEndDate = dateTo.Date
            };
            if (departmentWorkload.EmployeeWorkloads.Count > 0)
            {
                foreach (var workloadRecord in departmentWorkload.EmployeeWorkloads[0].WorkloadRecords)
                    departmentWorkloadDto.DateRanges.Add(new DateTimeRange(workloadRecord.DateFrom, workloadRecord.DateTo));
                foreach (var employeeWorkload in departmentWorkload.EmployeeWorkloads)
                {
                    var employeeWorkloadDto = new CommonEmployeeWorkloadDto
                    {
                        EmployeeId = employeeWorkload.Employee.ID,
                        EmployeeFullName = employeeWorkload.Employee.FullName
                    };
                    foreach (var workloadRecord in employeeWorkload.WorkloadRecords)
                    {
                        int totalPercents = (int)Math.Round(workloadRecord.TotalPercents);
                        employeeWorkloadDto.Workloads.Add(totalPercents);
                    }
                    departmentWorkloadDto.EmployeesWorkloads.Add(employeeWorkloadDto);
                }
            }
            return Ok(departmentWorkloadDto);
        }
    }
}
