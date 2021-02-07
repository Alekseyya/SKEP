using System.Linq;
using Core.BL.Interfaces;
using Core.Models;
using Core.Models.RBAC;
using MainApp.RBAC.Attributes;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;






namespace MainApp.Controllers
{
    public class EmployeePositionController : Controller
    {
        private readonly IEmployeePositionService _employeePositionService;

        public EmployeePositionController(IEmployeePositionService employeePositionService)
        {
            _employeePositionService = employeePositionService;
        }

        [OperationActionFilter(nameof(Operation.PositionView))]
        public ActionResult Index()
        {
            return View(_employeePositionService.Get(x => x.ToList()).OrderBy(ep => ep.FullName).ToList());
        }

        [OperationActionFilter(nameof(Operation.PositionView))]
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return StatusCode(StatusCodes.Status400BadRequest);
            }

            EmployeePosition employeePosition = _employeePositionService.GetById(id.Value);
            if (employeePosition == null)
            {
                return StatusCode(StatusCodes.Status404NotFound);
            }
            return View(employeePosition);
        }

        [OperationActionFilter(nameof(Operation.PositionCreateUpdate))]
        public ActionResult Create()
        {
            return View();
        }

        [OperationActionFilter(nameof(Operation.PositionCreateUpdate))]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(EmployeePosition employeePosition)
        {
            if (ModelState.IsValid)
            {
                _employeePositionService.Add(employeePosition);
                return RedirectToAction("Index");
            }

            return View(employeePosition);
        }

        [OperationActionFilter(nameof(Operation.PositionCreateUpdate))]
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return StatusCode(StatusCodes.Status400BadRequest);
            }
            EmployeePosition employeePosition = _employeePositionService.GetById(id.Value);
            if (employeePosition == null)
            {
                return StatusCode(StatusCodes.Status404NotFound);
            }
            return View(employeePosition);
        }

        [OperationActionFilter(nameof(Operation.PositionCreateUpdate))]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(EmployeePosition employeePosition)
        {
            if (ModelState.IsValid)
            {
                _employeePositionService.Update(employeePosition);
                return RedirectToAction("Index");
            }
            return View(employeePosition);
        }

        [OperationActionFilter(nameof(Operation.PositionDelete))]
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return StatusCode(StatusCodes.Status400BadRequest);
            }
            EmployeePosition employeePosition = _employeePositionService.GetById(id.Value);
            if (employeePosition == null)
            {
                return StatusCode(StatusCodes.Status404NotFound);
            }
            return View(employeePosition);
        }

        [OperationActionFilter(nameof(Operation.PositionDelete))]
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            EmployeePosition employeePosition = _employeePositionService.GetById(id);
            _employeePositionService.Delete(employeePosition.ID);
            return RedirectToAction("Index");
        }
    }
}
