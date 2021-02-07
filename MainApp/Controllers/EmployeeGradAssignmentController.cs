using System.Linq;
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
    public class EmployeeGradAssignmentController : Controller
    {
        private readonly IEmployeeGradAssignmentService _employeeGradAssignmentService;
        private readonly IEmployeeService _employeeService;
        private readonly IEmployeeGradService _employeeGradService;

        public EmployeeGradAssignmentController(IEmployeeGradAssignmentService employeeGradAssignmentService,
            IEmployeeService employeeService, IEmployeeGradService employeeGradService)
        {
            _employeeGradAssignmentService = employeeGradAssignmentService;
            _employeeService = employeeService;
            _employeeGradService = employeeGradService;
        }

        [OperationActionFilter(nameof(Operation.GradView))]
        [OperationActionFilter(nameof(Operation.EmployeeView))]
        public ActionResult Index()
        {
            var employeeGradAssignments = _employeeGradAssignmentService.Get(x =>
                x.Include(e => e.Employee).Include(e => e.EmployeeGrad).ToList().OrderByDescending(e => e.BeginDate)
                    .ToList());
            return View(employeeGradAssignments);
        }

        [OperationActionFilter(nameof(Operation.GradView))]
        [OperationActionFilter(nameof(Operation.EmployeeView))]
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return StatusCode(StatusCodes.Status400BadRequest);
            }
            EmployeeGradAssignment employeeGradAssignment = _employeeGradAssignmentService.GetById(id.Value);
            if (employeeGradAssignment == null)
            {
                return StatusCode(StatusCodes.Status404NotFound);
            }
            return View(employeeGradAssignment);
        }

        [OperationActionFilter(nameof(Operation.GradCreateUpdate))]
        [OperationActionFilter(nameof(Operation.EmployeeCreateUpdate))]
        public ActionResult Create()
        {
            ViewBag.EmployeeID = new SelectList(_employeeService.Get(x => x.ToList().OrderBy(e => e.FullName).ToList()), "ID", "FullName");
            ViewBag.EmployeeGradID = new SelectList(_employeeGradService.Get(x => x.OrderBy(e => e.ShortName).ToList()), "ID", "FullName");
            return View();
        }

        [OperationActionFilter(nameof(Operation.GradCreateUpdate))]
        [OperationActionFilter(nameof(Operation.EmployeeCreateUpdate))]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(EmployeeGradAssignment employeeGradAssignment)
        {
            if (ModelState.IsValid)
            {
                _employeeGradAssignmentService.Add(employeeGradAssignment);
                return RedirectToAction("Index");
            }

            ViewBag.EmployeeID = new SelectList(_employeeService.Get(x => x.ToList().OrderBy(e => e.FullName).ToList()), "ID", "FullName", employeeGradAssignment.EmployeeID);
            ViewBag.EmployeeGradID = new SelectList(_employeeGradService.Get(x => x.OrderBy(e => e.ShortName).ToList()), "ID", "FullName", employeeGradAssignment.EmployeeGradID);
            return View(employeeGradAssignment);
        }

        [OperationActionFilter(nameof(Operation.GradCreateUpdate))]
        [OperationActionFilter(nameof(Operation.EmployeeCreateUpdate))]
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return StatusCode(StatusCodes.Status400BadRequest);
            }
            EmployeeGradAssignment employeeGradAssignment = _employeeGradAssignmentService.GetById(id.Value);
            if (employeeGradAssignment == null)
            {
                return StatusCode(StatusCodes.Status404NotFound);
            }
            ViewBag.EmployeeID = new SelectList(_employeeService.Get(x => x.ToList().OrderBy(e => e.FullName).ToList()), "ID", "FullName", employeeGradAssignment.EmployeeID);
            ViewBag.EmployeeGradID = new SelectList(_employeeGradService.Get(x => x.OrderBy(e => e.ShortName).ToList()), "ID", "FullName", employeeGradAssignment.EmployeeGradID);
            return View(employeeGradAssignment);
        }

        [OperationActionFilter(nameof(Operation.GradCreateUpdate))]
        [OperationActionFilter(nameof(Operation.EmployeeCreateUpdate))]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(EmployeeGradAssignment employeeGradAssignment)
        {
            if (ModelState.IsValid)
            {
                _employeeGradAssignmentService.Update(employeeGradAssignment);
                return RedirectToAction("Index");
            }
            ViewBag.EmployeeID = new SelectList(_employeeService.Get(x => x.ToList().OrderBy(e => e.FullName).ToList()), "ID", "FullName", employeeGradAssignment.EmployeeID);
            ViewBag.EmployeeGradID = new SelectList(_employeeGradService.Get(x => x.OrderBy(e => e.ShortName).ToList()), "ID", "FullName", employeeGradAssignment.EmployeeGradID);
            return View(employeeGradAssignment);
        }

        [OperationActionFilter(nameof(Operation.GradDelete))]
        [OperationActionFilter(nameof(Operation.EmployeeDelete))]
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return StatusCode(StatusCodes.Status400BadRequest);
            }
            EmployeeGradAssignment employeeGradAssignment = _employeeGradAssignmentService.GetById(id.Value);
            if (employeeGradAssignment == null)
            {
                return StatusCode(StatusCodes.Status404NotFound);
            }
            return View(employeeGradAssignment);
        }

        [OperationActionFilter(nameof(Operation.GradDelete))]
        [OperationActionFilter(nameof(Operation.EmployeeDelete))]
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            EmployeeGradAssignment employeeGradAssignment = _employeeGradAssignmentService.GetById(id);
            _employeeGradAssignmentService.Delete(employeeGradAssignment.ID);
            return RedirectToAction("Index");
        }
    }
}
