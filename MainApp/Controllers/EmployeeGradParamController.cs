using System.Linq;
using System.Net;
using Core.BL.Interfaces;
using Core.Models;
using Core.Models.RBAC;
using MainApp.RBAC.Attributes;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;





namespace MainApp.Controllers
{
    public class EmployeeGradParamController : Controller
    {
        private readonly IEmployeeGradParamService _employeeGradParamService;
        private readonly IEmployeeGradService _employeeGradService;
        public EmployeeGradParamController(IEmployeeGradParamService employeeGradParamService,
            IEmployeeGradService employeeGradService)
        {
            _employeeGradParamService = employeeGradParamService;
            _employeeGradService = employeeGradService;
        }

        [OperationActionFilter(nameof(Operation.FinDataView))]
        public ActionResult Index()
        {
            var gradParams = _employeeGradParamService.Get(egps => egps.ToList()).OrderBy(x => x.BeginDate).ThenBy(x => x.EmployeeGrad.ShortName);
            return View(gradParams);
        }

        [OperationActionFilter(nameof(Operation.FinDataView))]
        public ActionResult Details(int? id)
        {
            if (id == null)
                return StatusCode(StatusCodes.Status400BadRequest);
            EmployeeGradParam gradParam = _employeeGradParamService.GetById(id.Value);
            if (gradParam == null)
                return StatusCode(StatusCodes.Status404NotFound);;
            return View(gradParam);
        }

        [HttpGet]
        [OperationActionFilter(nameof(Operation.FinDataCreateUpdate))]
        public ActionResult Create()
        {
            ViewBag.EmployeeGrad = new SelectList(_employeeGradService.Get(x => x.ToList()).OrderBy(x => x.ShortName), "ID", "FullName");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [OperationActionFilter(nameof(Operation.FinDataCreateUpdate))]
        public ActionResult Create(EmployeeGradParam employeeGradParam)
        {
            if (ModelState.IsValid)
            {
                var gradParams = _employeeGradParamService.Get(egps => egps
                    .Where(egp => egp.BeginDate == employeeGradParam.BeginDate
                    && egp.EmployeeGradID == employeeGradParam.EmployeeGradID
                    && egp.RoleType == employeeGradParam.RoleType
                    ).ToList());
                if (gradParams.Count > 0)
                    ModelState.AddModelError("EmployeeGradID", "Для связки 'Дата начала действия-Грейд-Тип роли' уже существует запись, измените один или несколько параметров.");
                else if (employeeGradParam.EmployeeYearPayrollRatio < 1)
                    ModelState.AddModelError("EmployeeYearPayrollRatio", "% выплат от годовой зп не может быть меньше 1");
            }

            if (ModelState.IsValid)
            {
                _employeeGradParamService.Add(employeeGradParam);

                return RedirectToAction("Index");
            }
            ViewBag.EmployeeGrad = new SelectList(_employeeGradService.Get(x => x.ToList()).OrderBy(x => x.ShortName), "ID", "FullName");
            return View(employeeGradParam);
        }

        [HttpGet]
        [OperationActionFilter(nameof(Operation.FinDataCreateUpdate))]
        public ActionResult Edit(int? id)
        {
            if (id == null)
                return StatusCode(StatusCodes.Status400BadRequest);
            EmployeeGradParam gradParam = _employeeGradParamService.GetById(id.Value);
            if (gradParam == null)
                return StatusCode(StatusCodes.Status404NotFound);;
            ViewBag.EmployeeGrad = new SelectList(_employeeGradService.Get(x => x.ToList()).OrderBy(x => x.ShortName), "ID", "FullName", gradParam.EmployeeGradID);
            return View(gradParam);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [OperationActionFilter(nameof(Operation.FinDataCreateUpdate))]
        public ActionResult Edit(EmployeeGradParam employeeGradParam)
        {
            if (ModelState.IsValid)
            {
                var gradParams = _employeeGradParamService.Get(egps => egps
                    .Where(egp => egp.BeginDate == employeeGradParam.BeginDate
                    && egp.EmployeeGradID == employeeGradParam.EmployeeGradID
                    && egp.RoleType == employeeGradParam.RoleType
                    && egp.ID != employeeGradParam.ID
                    ).ToList());
                if (gradParams.Count > 0)
                    ModelState.AddModelError("EmployeeGradID", "Для связки 'Дата начала действия-Грейд-Тип роли' уже существует запись, измените один или несколько параметров.");
                else if (employeeGradParam.EmployeeYearPayrollRatio < 1)
                    ModelState.AddModelError("EmployeeYearPayrollRatio", "% выплат от годовой зп не может быть меньше 1");
            }

            if (ModelState.IsValid)
            {
                _employeeGradParamService.Update(employeeGradParam);

                return RedirectToAction("Index");
            }
            ViewBag.EmployeeGrad = new SelectList(_employeeGradService.Get(x => x.ToList()).OrderBy(x => x.ShortName), "ID", "FullName", employeeGradParam.EmployeeGradID);
            return View(employeeGradParam);
        }

        [HttpGet]
        [OperationActionFilter(nameof(Operation.FinDataDelete))]
        public ActionResult Delete(int? id)
        {
            if (id == null)
                return StatusCode(StatusCodes.Status400BadRequest);
            EmployeeGradParam gradParam = _employeeGradParamService.GetById(id.Value);
            if (gradParam == null)
                return StatusCode(StatusCodes.Status404NotFound);;
            return View(gradParam);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [OperationActionFilter(nameof(Operation.FinDataDelete))]
        public ActionResult DeleteConfirmed(int id)
        {
            EmployeeGradParam gradParam = _employeeGradParamService.GetById(id);
            int gradParamID = gradParam.ID;
            if (gradParam != null)
            {
                _employeeGradParamService.Delete(gradParam.ID);
            }
            // string returnUrl = Url.Action("Index");
            return RedirectToAction("Index");
            // return new RedirectResult(returnUrl);
        }
    }
}
