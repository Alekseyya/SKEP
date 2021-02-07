using System.Linq;
using System.Net;
using Core.BL.Interfaces;
using Core.Models;
using Core.Models.RBAC;
using MainApp.RBAC.Attributes;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;






namespace MainApp.Controllers
{
    public class EmployeeDepartmentAssignmentController : Controller
    {
        private readonly IEmployeeDepartmentAssignmentService _employeeDepartmentAssignmentService;
        private readonly IDepartmentService _departmentService;
        private readonly IEmployeeService _employeeService;

        public EmployeeDepartmentAssignmentController(IEmployeeDepartmentAssignmentService employeeDepartmentAssignmentService,
            IDepartmentService departmentService,
            IEmployeeService employeeService)
        {
            _employeeDepartmentAssignmentService = employeeDepartmentAssignmentService;
            _departmentService = departmentService;
            _employeeService = employeeService;
        }

        [OperationActionFilter(nameof(Operation.EmployeeView))]
        [OperationActionFilter(nameof(Operation.DepartmentView))]
        public ActionResult Index()
        {
            var employeeDepartmentAssignments = _employeeDepartmentAssignmentService.Get(x => x.Include(e => e.Department).Include(e => e.Employee).ToArray().OrderByDescending(e => e.BeginDate).ToList());
            return View(employeeDepartmentAssignments);
        }

        [OperationActionFilter(nameof(Operation.EmployeeView))]
        [OperationActionFilter(nameof(Operation.DepartmentView))]
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return StatusCode(StatusCodes.Status400BadRequest);
            }

            EmployeeDepartmentAssignment employeeDepartmentAssignment = _employeeDepartmentAssignmentService.GetById(id.Value);
            if (employeeDepartmentAssignment == null)
            {
                return StatusCode(StatusCodes.Status404NotFound);
            }
            return View(employeeDepartmentAssignment);
        }

        [OperationActionFilter(nameof(Operation.EmployeeCreateUpdate))]
        [OperationActionFilter(nameof(Operation.DepartmentCreateUpdate))]
        public ActionResult Create()
        {
            ViewBag.DepartmentID = new SelectList(_departmentService.Get(x => x.ToList().OrderBy(d => d.ShortName).ToList()), "ID", "FullName");
            ViewBag.EmployeeID = new SelectList(_employeeService.Get(x => x.ToList().OrderBy(e => e.FullName).ToList()), "ID", "FullName");
            return View();
        }

        [OperationActionFilter(nameof(Operation.EmployeeCreateUpdate))]
        [OperationActionFilter(nameof(Operation.DepartmentCreateUpdate))]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(EmployeeDepartmentAssignment employeeDepartmentAssignment)
        {
            if (ModelState.IsValid)
            {
                _employeeDepartmentAssignmentService.Add(employeeDepartmentAssignment);
                return RedirectToAction("Index");
            }

            ViewBag.DepartmentID = new SelectList(_departmentService.Get(x => x.ToList().OrderBy(d => d.ShortName).ToList()), "ID", "FullName", employeeDepartmentAssignment.DepartmentID);
            ViewBag.EmployeeID = new SelectList(_employeeService.Get(x => x.ToList().OrderBy(e => e.FullName).ToList()), "ID", "FullName", employeeDepartmentAssignment.EmployeeID);
            return View(employeeDepartmentAssignment);
        }

        [OperationActionFilter(nameof(Operation.EmployeeCreateUpdate))]
        [OperationActionFilter(nameof(Operation.DepartmentCreateUpdate))]
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return StatusCode(StatusCodes.Status400BadRequest);
            }
            EmployeeDepartmentAssignment employeeDepartmentAssignment = _employeeDepartmentAssignmentService.GetById(id.Value);
            if (employeeDepartmentAssignment == null)
            {
                return StatusCode(StatusCodes.Status404NotFound);
            }
            ViewBag.DepartmentID = new SelectList(_departmentService.Get(x => x.ToList().OrderBy(d => d.ShortName).ToList()), "ID", "FullName", employeeDepartmentAssignment.DepartmentID);
            ViewBag.EmployeeID = new SelectList(_employeeService.Get(x => x.ToList().OrderBy(e => e.FullName).ToList()), "ID", "FullName", employeeDepartmentAssignment.EmployeeID);
            return View(employeeDepartmentAssignment);
        }

        [OperationActionFilter(nameof(Operation.EmployeeCreateUpdate))]
        [OperationActionFilter(nameof(Operation.DepartmentCreateUpdate))]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(EmployeeDepartmentAssignment employeeDepartmentAssignment)
        {
            if (ModelState.IsValid)
            {
                _employeeDepartmentAssignmentService.Update(employeeDepartmentAssignment);
                return RedirectToAction("Index");
            }
            ViewBag.DepartmentID = new SelectList(_departmentService.Get(x => x.ToList().OrderBy(d => d.ShortName).ToList()), "ID", "FullName", employeeDepartmentAssignment.DepartmentID);
            ViewBag.EmployeeID = new SelectList(_employeeService.Get(x => x.ToList().OrderBy(e => e.FullName).ToList()), "ID", "FullName", employeeDepartmentAssignment.EmployeeID);
            return View(employeeDepartmentAssignment);
        }

        [OperationActionFilter(nameof(Operation.EmployeeDelete))]
        [OperationActionFilter(nameof(Operation.DepartmentDelete))]
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return StatusCode(StatusCodes.Status400BadRequest);
            }
            EmployeeDepartmentAssignment employeeDepartmentAssignment = _employeeDepartmentAssignmentService.GetById(id.Value);
            if (employeeDepartmentAssignment == null)
            {
                return StatusCode(StatusCodes.Status404NotFound);
            }
            return View(employeeDepartmentAssignment);
        }

        [OperationActionFilter(nameof(Operation.EmployeeDelete))]
        [OperationActionFilter(nameof(Operation.DepartmentDelete))]
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            EmployeeDepartmentAssignment employeeDepartmentAssignment = _employeeDepartmentAssignmentService.GetById(id);
            _employeeDepartmentAssignmentService.Delete(employeeDepartmentAssignment.ID);
            return RedirectToAction("Index");
        }
    }
}
