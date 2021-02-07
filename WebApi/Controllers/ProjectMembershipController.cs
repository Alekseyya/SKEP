using System;
using System.Linq;
using Core;
using Core.BL.Interfaces;
using Core.Models;
using Core.Models.RBAC;
using Microsoft.AspNetCore.Mvc;
using WebApi.Dto;
using WebApi.RBAC.Attributes;

namespace WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProjectMembershipController : Controller
    {
        private IProjectService _projectSvc;

        private IProjectMembershipService _projectMembershipSvc;

        public ProjectMembershipController(IProjectMembershipService projectMembershipSvc, IProjectService projectSvc)
        {
            if (projectMembershipSvc == null)
                throw new ArgumentNullException(nameof(projectMembershipSvc));
            if (projectSvc == null)
                throw new ArgumentNullException(nameof(projectSvc));

            _projectMembershipSvc = projectMembershipSvc;
            _projectSvc = projectSvc;
        }

        [HttpGet]
        [Route("api/projects/{projectId}/memberemployees", Name = "ApiProjectMemberEmployees")]
        [OperationApiActionFilter(nameof(Operation.ApiAccessDataView))]
        public IActionResult GetProjectMemberEmployees(int projectId)
        {
            var project = _projectSvc.GetById(projectId);
            if (project == null)
                return NotFound();

            var memebrEmployees = _projectMembershipSvc.GetEmployeesOnProject(projectId);
            var result = memebrEmployees.Select(emp => Employee2Dto(emp)).OrderBy(dto => dto.FullName).ToList();
            return Ok(result);
        }

        [HttpGet]
        [Route("api/project/{projectId}/memberemployees/{dateFrom}/{dateTo}", Name = "ApiProjectMemberEmployeesForPeriod")]
        [OperationApiActionFilter(nameof(Operation.ApiAccessDataView))]
        public IActionResult GetProjectMemberEmployees(int projectId, DateTime dateFrom, DateTime dateTo)
        {
            if (dateFrom > dateTo)
                return BadRequest("Дата начала периода должна быть меньше или равна дате окончания периода");

            var project = _projectSvc.GetById(projectId);
            if (project == null)
                return NotFound();

            var memebrEmployees = _projectMembershipSvc.GetEmployeesOnProject(projectId, new DateTimeRange(dateFrom.Date, dateTo.Date));
            var result = memebrEmployees.Select(emp => Employee2Dto(emp)).OrderBy(dto => dto.FullName).ToList();
            return Ok(result);
        }

        // TODO: Нужно вынести в отдельный класс encoder, возможно, стоит применить automapper или что-то подобное
        private BasicEmployeeDto Employee2Dto(Employee employee)
        {
            var dto = new BasicEmployeeDto
            {
                Id = employee.ID,
                LastName = employee.LastName,
                FirstName = employee.FirstName,
                MidName = employee.MidName,
                FullName = employee.FullName,
                /*Login = employee.ADLogin,*/
                Email = employee.Email,
                DepartmentId = employee.DepartmentID
            };
            return dto;
        }
    }
}
