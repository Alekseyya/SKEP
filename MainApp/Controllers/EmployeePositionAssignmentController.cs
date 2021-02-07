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
    public class EmployeePositionAssignmentController : Controller
    {
        private readonly IEmployeePositionAssignmentService _employeePositionAssignmentService;
        private readonly IEmployeeService _employeeService;
        private readonly IEmployeePositionService _employeePositionService;

        public EmployeePositionAssignmentController(IEmployeePositionAssignmentService employeePositionAssignmentService,
            IEmployeeService employeeService,
            IEmployeePositionService employeePositionService)
        {
            _employeePositionAssignmentService = employeePositionAssignmentService;
            _employeeService = employeeService;
            _employeePositionService = employeePositionService;
        }

        [OperationActionFilter(nameof(Operation.PositionView))]
        [OperationActionFilter(nameof(Operation.EmployeeView))]
        public ActionResult Index()
        {
            var employeePositionAssignments = _employeePositionAssignmentService.Get(x => x.Include(e => e.Employee).Include(e => e.EmployeePosition).ToList().OrderByDescending(e => e.BeginDate).ToList());
            return View(employeePositionAssignments.ToList());
        }

        [OperationActionFilter(nameof(Operation.PositionView))]
        [OperationActionFilter(nameof(Operation.EmployeeView))]
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return StatusCode(StatusCodes.Status400BadRequest);
            }

            EmployeePositionAssignment employeePositionAssignment = _employeePositionAssignmentService.GetById(id.Value);
            if (employeePositionAssignment == null)
            {
                return StatusCode(StatusCodes.Status404NotFound);
            }
            return View(employeePositionAssignment);
        }

        [OperationActionFilter(nameof(Operation.PositionCreateUpdate))]
        [OperationActionFilter(nameof(Operation.EmployeeCreateUpdate))]
        public ActionResult Create()
        {
            ViewBag.EmployeeID = new SelectList(_employeeService.Get(x => x.ToList().OrderBy(e => e.FullName).ToList()), "ID", "FullName");
            ViewBag.EmployeePositionID = new SelectList(_employeePositionService.Get(x => x.ToList().OrderBy(ep => ep.ShortName).ToList()), "ID", "FullName");
            return View();
        }

        [OperationActionFilter(nameof(Operation.PositionCreateUpdate))]
        [OperationActionFilter(nameof(Operation.EmployeeCreateUpdate))]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(EmployeePositionAssignment employeePositionAssignment)
        {
            if (ModelState.IsValid)
            {
                _employeePositionAssignmentService.Add(employeePositionAssignment);
                return RedirectToAction("Index");
            }

            ViewBag.EmployeeID = new SelectList(_employeeService.Get(x => x.ToList().OrderBy(e => e.FullName).ToList()), "ID", "FullName", employeePositionAssignment.EmployeeID);
            ViewBag.EmployeePositionID = new SelectList(_employeePositionService.Get(x => x.ToList().OrderBy(ep => ep.ShortName).ToList()), "ID", "FullName", employeePositionAssignment.EmployeePositionID);
            return View(employeePositionAssignment);
        }

        [OperationActionFilter(nameof(Operation.PositionCreateUpdate))]
        [OperationActionFilter(nameof(Operation.EmployeeCreateUpdate))]
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return StatusCode(StatusCodes.Status400BadRequest);
            }
            EmployeePositionAssignment employeePositionAssignment = _employeePositionAssignmentService.GetById(id.Value);
            if (employeePositionAssignment == null)
            {
                return StatusCode(StatusCodes.Status404NotFound);
            }
            ViewBag.EmployeeID = new SelectList(_employeeService.Get(x => x.ToList().OrderBy(e => e.FullName).ToList()), "ID", "FullName", employeePositionAssignment.EmployeeID);
            ViewBag.EmployeePositionID = new SelectList(_employeePositionService.Get(x => x.ToList().OrderBy(ep => ep.ShortName).ToList()), "ID", "FullName", employeePositionAssignment.EmployeePositionID);
            return View(employeePositionAssignment);
        }

        [OperationActionFilter(nameof(Operation.PositionCreateUpdate))]
        [OperationActionFilter(nameof(Operation.EmployeeCreateUpdate))]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(EmployeePositionAssignment employeePositionAssignment)
        {
            if (ModelState.IsValid)
            {
                _employeePositionAssignmentService.Update(employeePositionAssignment);
                return RedirectToAction("Index");
            }
            ViewBag.EmployeeID = new SelectList(_employeeService.Get(x => x.ToList().OrderBy(e => e.FullName).ToList()), "ID", "FullName", employeePositionAssignment.EmployeeID);
            ViewBag.EmployeePositionID = new SelectList(_employeePositionService.Get(x => x.ToList().OrderBy(ep => ep.ShortName).ToList()), "ID", "FullName", employeePositionAssignment.EmployeePositionID);
            return View(employeePositionAssignment);
        }

        [OperationActionFilter(nameof(Operation.PositionDelete))]
        [OperationActionFilter(nameof(Operation.EmployeeDelete))]
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return StatusCode(StatusCodes.Status400BadRequest);
            }
            EmployeePositionAssignment employeePositionAssignment = _employeePositionAssignmentService.GetById(id.Value);
            if (employeePositionAssignment == null)
            {
                return StatusCode(StatusCodes.Status404NotFound);
            }
            return View(employeePositionAssignment);
        }

        [OperationActionFilter(nameof(Operation.PositionDelete))]
        [OperationActionFilter(nameof(Operation.EmployeeDelete))]
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            EmployeePositionAssignment employeePositionAssignment = _employeePositionAssignmentService.GetById(id);
            _employeePositionAssignmentService.Delete(employeePositionAssignment.ID);
            return RedirectToAction("Index");
        }
    }
}
