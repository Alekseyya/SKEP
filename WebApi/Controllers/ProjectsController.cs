using System;
using Core.BL.Interfaces;
using Core.Models.RBAC;
using Microsoft.AspNetCore.Mvc;
using WebApi.Dto;
using WebApi.RBAC.Attributes;

namespace WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProjectsController : Controller
    {
        private IProjectService _projectSvc;

        public ProjectsController(IProjectService projectSvc)
        {
            if (projectSvc == null)
                throw new ArgumentNullException(nameof(projectSvc));

            _projectSvc = projectSvc;
        }

        [HttpGet]
        [Route("api/projects/{projectId}", Name = "ApiProjectsGet")]
        [OperationApiActionFilter(nameof(Operation.ApiAccessDataView))]
        public IActionResult GetProject(int projectId)
        {
            var project = _projectSvc.GetById(projectId);
            if (project == null)
                return NotFound();

            var result = new BasicProjectDto(project);
            return Ok(result);
        }
    }
}
