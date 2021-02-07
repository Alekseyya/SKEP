using System;
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
    public class QualifyingRoleRateController : Controller
    {
        private readonly IQualifyingRoleRateService _qualifyingRoleRateService;
        private readonly IDepartmentService _departmentService;
        private readonly IQualifyingRoleService _qualifyingRoleService;
        public QualifyingRoleRateController(IQualifyingRoleRateService qualifyingRoleRateService,
            IDepartmentService departmentService, IQualifyingRoleService qualifyingRoleService)
        {
            _qualifyingRoleRateService = qualifyingRoleRateService;
            _departmentService = departmentService;
            _qualifyingRoleService = qualifyingRoleService;
        }


        [OperationActionFilter(nameof(Operation.QualifyingRoleRateView))]
        public ActionResult Index()
        {
            var qualifyingRoleRates = _qualifyingRoleRateService.Get(x => x.Include(q => q.Department).Include(q => q.QualifyingRole).ToList());
            return View(qualifyingRoleRates.ToList());
        }

        [OperationActionFilter(nameof(Operation.QualifyingRoleRateView))]
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return StatusCode(StatusCodes.Status400BadRequest);
            }
            QualifyingRoleRate qualifyingRoleRate = _qualifyingRoleRateService.GetById((int)id);
            if (qualifyingRoleRate == null)
            {
                return StatusCode(StatusCodes.Status404NotFound);
            }
            return View(qualifyingRoleRate);
        }

        [OperationActionFilter(nameof(Operation.QualifyingRoleRateCreateUpdate))]
        public ActionResult Create()
        {
            ViewBag.DepartmentID = new SelectList(_departmentService.Get(x => x.ToList()), "ID", "ShortName");
            ViewBag.QualifyingRoleID = new SelectList(_qualifyingRoleService.Get(x => x.ToList()), "ID", "ShortName");
            return View();
        }

        [OperationActionFilter(nameof(Operation.QualifyingRoleRateCreateUpdate))]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(QualifyingRoleRate qualifyingRoleRate)
        {
            if (ModelState.IsValid)
            {
                _qualifyingRoleRateService.Add(qualifyingRoleRate);
                return RedirectToAction("Index");
            }

            ViewBag.DepartmentID = new SelectList(_departmentService.Get(x => x.ToList()), "ID", "ShortName");
            ViewBag.QualifyingRoleID = new SelectList(_qualifyingRoleService.Get(x => x.ToList()), "ID", "ShortName");
            return View(qualifyingRoleRate);
        }

        [OperationActionFilter(nameof(Operation.QualifyingRoleRateCreateUpdate))]
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return StatusCode(StatusCodes.Status400BadRequest);
            }
            QualifyingRoleRate qualifyingRoleRate = _qualifyingRoleRateService.GetById((int)id);
            if (qualifyingRoleRate == null)
            {
                return StatusCode(StatusCodes.Status404NotFound);
            }
            ViewBag.DepartmentID = new SelectList(_departmentService.Get(x => x.ToList()), "ID", "ShortName");
            ViewBag.QualifyingRoleID = new SelectList(_qualifyingRoleService.Get(x => x.ToList()), "ID", "ShortName");
            return View(qualifyingRoleRate);
        }

        [OperationActionFilter(nameof(Operation.QualifyingRoleRateCreateUpdate))]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(QualifyingRoleRate qualifyingRoleRate)
        {
            if (ModelState.IsValid)
            {
                _qualifyingRoleRateService.Update(qualifyingRoleRate);
                return RedirectToAction("Index");
            }
            ViewBag.DepartmentID = new SelectList(_departmentService.Get(x => x.ToList()), "ID", "ShortName");
            ViewBag.QualifyingRoleID = new SelectList(_qualifyingRoleService.Get(x => x.ToList()), "ID", "ShortName");
            return View(qualifyingRoleRate);
        }

        [OperationActionFilter(nameof(Operation.QualifyingRoleRateDelete))]
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return StatusCode(StatusCodes.Status400BadRequest);
            }
            QualifyingRoleRate qualifyingRoleRate = _qualifyingRoleRateService.GetById((int)id);
            if (qualifyingRoleRate == null)
            {
                return StatusCode(StatusCodes.Status404NotFound);
            }
            return View(qualifyingRoleRate);
        }

        [OperationActionFilter(nameof(Operation.QualifyingRoleRateDelete))]
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            QualifyingRoleRate qualifyingRoleRate = _qualifyingRoleRateService.GetById((int)id);
            _qualifyingRoleRateService.Delete(qualifyingRoleRate.ID);
            return RedirectToAction("Index");
        }
    }
}
