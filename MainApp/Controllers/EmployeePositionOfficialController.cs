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
    public class EmployeePositionOfficialController : Controller
    {
        private readonly IEmployeePositionOfficialService _employeePositionOfficialService;
        private readonly IOrganisationService _organisationService;

        public EmployeePositionOfficialController(IEmployeePositionOfficialService employeePositionOfficialService, IOrganisationService organisationService)
        {
            _employeePositionOfficialService = employeePositionOfficialService;
            _organisationService = organisationService;
        }

        [OperationActionFilter(nameof(Operation.PositionView))]
        public ActionResult Index()
        {
            var employeePositionOfficials = _employeePositionOfficialService.Get(x => x.Include(e => e.Organisation).ToList());
            return View(employeePositionOfficials.ToList());
        }

        [OperationActionFilter(nameof(Operation.PositionView))]
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return StatusCode(StatusCodes.Status400BadRequest);
            }

            EmployeePositionOfficial employeePositionOfficial = _employeePositionOfficialService.GetById(id.Value);
            if (employeePositionOfficial == null)
            {
                return StatusCode(StatusCodes.Status404NotFound);
            }
            return View(employeePositionOfficial);
        }

        [OperationActionFilter(nameof(Operation.PositionCreateUpdate))]
        public ActionResult Create()
        {
            ViewBag.OrganisationID = new SelectList(_organisationService.Get(x => x.ToList().OrderBy(o => o.ShortName).ToList()), "ID", "FullName");
            return View();
        }

        [OperationActionFilter(nameof(Operation.PositionCreateUpdate))]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(EmployeePositionOfficial employeePositionOfficial)
        {
            if (ModelState.IsValid)
            {
                _employeePositionOfficialService.Add(employeePositionOfficial);
                return RedirectToAction("Index");
            }

            ViewBag.OrganisationID = new SelectList(_organisationService.Get(x => x.ToList().OrderBy(o => o.ShortName).ToList()), "ID", "FullName", employeePositionOfficial.OrganisationID);
            return View(employeePositionOfficial);
        }

        [OperationActionFilter(nameof(Operation.PositionCreateUpdate))]
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return StatusCode(StatusCodes.Status400BadRequest);
            }
            EmployeePositionOfficial employeePositionOfficial = _employeePositionOfficialService.GetById(id.Value);
            if (employeePositionOfficial == null)
            {
                return StatusCode(StatusCodes.Status404NotFound);
            }
            ViewBag.OrganisationID = new SelectList(_organisationService.Get(x => x.ToList().OrderBy(o => o.ShortName).ToList()), "ID", "FullName", employeePositionOfficial.OrganisationID);
            return View(employeePositionOfficial);
        }

        [OperationActionFilter(nameof(Operation.PositionCreateUpdate))]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(EmployeePositionOfficial employeePositionOfficial)
        {
            if (ModelState.IsValid)
            {
                _employeePositionOfficialService.Update(employeePositionOfficial);
                return RedirectToAction("Index");
            }
            ViewBag.OrganisationID = new SelectList(_organisationService.Get(x => x.ToList().OrderBy(o => o.ShortName).ToList()), "ID", "FullName", employeePositionOfficial.OrganisationID);
            return View(employeePositionOfficial);
        }
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return StatusCode(StatusCodes.Status400BadRequest);
            }
            EmployeePositionOfficial employeePositionOfficial = _employeePositionOfficialService.GetById(id.Value);
            if (employeePositionOfficial == null)
            {
                return StatusCode(StatusCodes.Status404NotFound);
            }
            return View(employeePositionOfficial);
        }

        [OperationActionFilter(nameof(Operation.PositionDelete))]
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            EmployeePositionOfficial employeePositionOfficial = _employeePositionOfficialService.GetById(id);
            _employeePositionOfficialService.Delete(employeePositionOfficial.ID);
            return RedirectToAction("Index");
        }
    }
}
