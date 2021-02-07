using System;
using System.Linq;
using Core.BL.Interfaces;
using Microsoft.AspNetCore.Mvc;
using WebApi.Dto;

namespace WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DepartmentController : Controller
    {
        private IDepartmentService _departmentSvc;

        public DepartmentController(IDepartmentService departmentSvc)
        {
            if (departmentSvc == null)
                throw new ArgumentNullException(nameof(departmentSvc));

            _departmentSvc = departmentSvc;
        }

        [HttpGet]
        [Route("api/departments/{departmentId}/children", Name = "ApiDepartmentChildren")]
        public IActionResult GetChildDepartments(int departmentId)
        {
            var department = _departmentSvc.GetById(departmentId);
            if (department == null)
                return NotFound();

            var childDepartments = _departmentSvc.GetChildDepartments(departmentId, false);
            var result = childDepartments.Select(d => new BasicDepartmentDto(d)).OrderBy(dto => dto.FullName);
            return Ok(result);
        }
    }
}
