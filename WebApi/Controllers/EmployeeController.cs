using System;
using System.Collections.Generic;
using System.Linq;
using AutoMapper;
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
    public class EmployeeController : Controller
    {
        private IEmployeeService _employeeSvc;
        private IDepartmentService _departmentSvc;
        private IEmployeeCategoryService _employeeCategorySvc;

        public EmployeeController(IEmployeeService employeeSvc, IDepartmentService departmentSvc, IEmployeeCategoryService employeeCategorySvc)
        {
            if (employeeSvc == null)
                throw new ArgumentNullException(nameof(employeeSvc));
            if (departmentSvc == null)
                throw new ArgumentNullException(nameof(departmentSvc));
            if (employeeCategorySvc == null)
                throw new ArgumentNullException(nameof(employeeCategorySvc));

            _employeeSvc = employeeSvc;
            _departmentSvc = departmentSvc;
            _employeeCategorySvc = employeeCategorySvc;
        }

        [HttpGet]
        [Route("{departmentId}/employees", Name = "ApiDepartmentEmployees")]
        [OperationApiActionFilter(nameof(Operation.ApiAccessDataView))]
        public IActionResult GetDepartmentEmployees(int departmentId)
        {
            return GetDepartmentEmployeesInternal(departmentId, false);
        }

        [HttpGet]
        [Route("api/departments/{departmentId}/allemployees", Name = "ApiDepartmentAllEmployees")]
        [OperationApiActionFilter(nameof(Operation.ApiAccessDataView))]
        public IActionResult GetDepartmentEmployeesWithChildDepartments(int departmentId)
        {
            return GetDepartmentEmployeesInternal(departmentId, true);
        }

        private IActionResult GetDepartmentEmployeesInternal(int departmentId, bool includeChildDepartments)
        {
            var department = _departmentSvc.GetById(departmentId);
            if (department == null)
                return NotFound();

            var employees = _employeeSvc.GetEmployeesInDepartment(departmentId, includeChildDepartments);
            var result = employees.Select(emp => new BasicEmployeeDto(emp)).OrderBy(dto => dto.FullName).ToList();
            return Ok(result);
        }

        [Route("api/employee/getbyfullname")]
        [OperationApiActionFilter(nameof(Operation.ApiAccessDataView))]
        public IActionResult GetByFullName([FromQuery]string fullName)
        {
            var employee = _employeeSvc.FindEmployeeByFullName(fullName);

            if (employee != null)
            {
                var result = new BasicEmployeeDto(employee);
                return Ok(result);
            }
            else
            {
                return Ok();
            }
        }

        [Route("api/employee/actualemployeedetailslist")]
        [OperationApiActionFilter(nameof(Operation.ApiAccessDataView))]
        public IActionResult GetActualEmployeeDetailsList(DateTime periodBeginDate, DateTime periodEndDate)
        {
            var currentEmployeeList = _employeeSvc.GetCurrentEmployees(new DateTimeRange(periodBeginDate, periodEndDate)).ToList();

            var config = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile(new AutoMapperConfig.EmployeeGetActualEmployeeDetailsListProfile());
            }).CreateMapper();

            var employeeListResult = config.Map<List<Employee>, List<EmployeeDetailsDto>>(currentEmployeeList.ToList());

            var employeesId = currentEmployeeList.Select(empl => empl.ID);

            var employeeCategories = _employeeCategorySvc.Get(categories => categories
                .Where(category => category.CategoryDateBegin <= DateTime.Today && (!category.CategoryDateEnd.HasValue || category.CategoryDateEnd >= DateTime.Today))
                .ToList()).AsEnumerable();
            employeeCategories = employeeCategories.Where(category => category.EmployeeID.HasValue && employeesId.Contains(category.EmployeeID.Value));

            /*
            var emplDetails = from employee in employees
                              select new EmployeeDetailsDto(employee, employeeCategories.FirstOrDefault(category => category.EmployeeID.Value == employee.ID));
            */

            foreach (var emplDetail in employeeListResult)
            {
                var category = employeeCategories.FirstOrDefault(c => c.EmployeeID.Value == emplDetail.Id);
                if (category == null)
                    emplDetail.CategoryType = null;
                else
                    emplDetail.CategoryType = category.CategoryType;
                // emplDetail.CategoryType = category == null ? null : category.CategoryType;
            }

            return Ok(employeeListResult);
        }
    }
}
