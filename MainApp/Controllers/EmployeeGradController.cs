using System.Linq;
using Core.BL.Interfaces;
using Core.Models;
using Core.Models.RBAC;
using MainApp.RBAC.Attributes;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;






namespace MainApp.Controllers
{
    public class EmployeeGradController : Controller
    {
        private readonly IEmployeeGradService _employeeGradService;

        public EmployeeGradController(IEmployeeGradService employeeGradService)
        {
            _employeeGradService = employeeGradService;
        }

        [OperationActionFilter(nameof(Operation.GradView))]
        public ActionResult Index()
        {
            return View(_employeeGradService.Get(x => x.ToList().OrderBy(eg => eg.FullName).ToList()));
        }

        [OperationActionFilter(nameof(Operation.GradView))]
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return StatusCode(StatusCodes.Status400BadRequest);
            }

            EmployeeGrad employeeGrad = _employeeGradService.GetById(id.Value);
            if (employeeGrad == null)
            {
                return StatusCode(StatusCodes.Status404NotFound);
            }
            return View(employeeGrad);
        }

        [OperationActionFilter(nameof(Operation.GradCreateUpdate))]
        public ActionResult Create()
        {
            return View();
        }

        [OperationActionFilter(nameof(Operation.GradCreateUpdate))]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(EmployeeGrad employeeGrad)
        {
            if (ModelState.IsValid)
            {
                _employeeGradService.Add(employeeGrad);
                return RedirectToAction("Index");
            }

            return View(employeeGrad);
        }

        [OperationActionFilter(nameof(Operation.GradCreateUpdate))]
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return StatusCode(StatusCodes.Status400BadRequest);
            }
            EmployeeGrad employeeGrad = _employeeGradService.GetById(id.Value);
            if (employeeGrad == null)
            {
                return StatusCode(StatusCodes.Status404NotFound);
            }
            return View(employeeGrad);
        }

        [OperationActionFilter(nameof(Operation.GradCreateUpdate))]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(EmployeeGrad employeeGrad)
        {
            if (ModelState.IsValid)
            {
                _employeeGradService.Update(employeeGrad);
                return RedirectToAction("Index");
            }
            return View(employeeGrad);
        }

        [OperationActionFilter(nameof(Operation.GradDelete))]
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return StatusCode(StatusCodes.Status400BadRequest);
            }
            EmployeeGrad employeeGrad = _employeeGradService.GetById(id.Value);
            if (employeeGrad == null)
            {
                return StatusCode(StatusCodes.Status404NotFound);
            }
            return View(employeeGrad);
        }

        [OperationActionFilter(nameof(Operation.GradDelete))]
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            EmployeeGrad employeeGrad = _employeeGradService.GetById(id);
            _employeeGradService.Delete(employeeGrad.ID);
            return RedirectToAction("Index");
        }
    }
}
