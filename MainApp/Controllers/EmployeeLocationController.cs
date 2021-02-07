using System.Linq;
using Core.BL.Interfaces;
using Core.Models;
using Core.Models.RBAC;
using MainApp.RBAC.Attributes;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;






namespace MainApp.Controllers
{
    public class EmployeeLocationController : Controller
    {
        private readonly IEmployeeLocationService _employeeLocationService;

        public EmployeeLocationController(IEmployeeLocationService employeeLocationService)
        {
            _employeeLocationService = employeeLocationService;
        }

        [OperationActionFilter(nameof(Operation.EmployeeLocationView))]
        public ActionResult Index()
        {
            return View(_employeeLocationService.Get(x => x.ToList()));
        }

        [OperationActionFilter(nameof(Operation.EmployeeLocationView))]
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return StatusCode(StatusCodes.Status400BadRequest);
            }

            EmployeeLocation employeeLocation = _employeeLocationService.GetById(id.Value);
            if (employeeLocation == null)
            {
                return StatusCode(StatusCodes.Status404NotFound);
            }
            return View(employeeLocation);
        }

        [OperationActionFilter(nameof(Operation.EmployeeLocationCreateUpdate))]
        public ActionResult Create()
        {
            return View();
        }

        [OperationActionFilter(nameof(Operation.EmployeeLocationCreateUpdate))]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(EmployeeLocation employeeLocation)
        {
            if (ModelState.IsValid)
            {
                _employeeLocationService.Add(employeeLocation);
                return RedirectToAction("Index");
            }

            return View(employeeLocation);
        }

        [OperationActionFilter(nameof(Operation.EmployeeLocationCreateUpdate))]
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return StatusCode(StatusCodes.Status400BadRequest);
            }
            EmployeeLocation employeeLocation = _employeeLocationService.GetById(id.Value);
            if (employeeLocation == null)
            {
                return StatusCode(StatusCodes.Status404NotFound);
            }
            return View(employeeLocation);
        }

        [OperationActionFilter(nameof(Operation.EmployeeLocationCreateUpdate))]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(EmployeeLocation employeeLocation)
        {
            if (ModelState.IsValid)
            {
                _employeeLocationService.Update(employeeLocation);
                return RedirectToAction("Index");
            }
            return View(employeeLocation);
        }

        [OperationActionFilter(nameof(Operation.EmployeeLocationDelete))]
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return StatusCode(StatusCodes.Status400BadRequest);
            }
            EmployeeLocation employeeLocation = _employeeLocationService.GetById(id.Value);
            if (employeeLocation == null)
            {
                return StatusCode(StatusCodes.Status404NotFound);
            }
            return View(employeeLocation);
        }

        [OperationActionFilter(nameof(Operation.EmployeeLocationDelete))]
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            EmployeeLocation employeeLocation = _employeeLocationService.GetById(id);
            _employeeLocationService.Delete(employeeLocation.ID);
            return RedirectToAction("Index");
        }
    }
}
