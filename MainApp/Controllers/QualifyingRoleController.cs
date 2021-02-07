using System;
using System.Linq;
using Core.BL.Interfaces;
using Core.Models;
using Core.Models.RBAC;
using MainApp.RBAC.Attributes;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;


namespace MainApp.Controllers
{
    public class QualifyingRoleController : Controller
    {
        private readonly IQualifyingRoleService _qualifyingRoleRate;

        public QualifyingRoleController(IQualifyingRoleService qualifyingRoleRate)
        {
            _qualifyingRoleRate = qualifyingRoleRate;
        }

        [OperationActionFilter(nameof(Operation.QualifyingRoleView))]
        public ActionResult Index()
        {
            return View(_qualifyingRoleRate.Get(x => x.ToList().OrderBy(qr => qr.ShortName).ToList()));
        }

        [OperationActionFilter(nameof(Operation.QualifyingRoleView))]
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return StatusCode(StatusCodes.Status400BadRequest);
            }
            QualifyingRole qualifyingRole = _qualifyingRoleRate.GetById(id.Value);
            if (qualifyingRole == null)
            {
                return StatusCode(StatusCodes.Status404NotFound);
            }
            return View(qualifyingRole);
        }

        [OperationActionFilter(nameof(Operation.QualifyingRoleCreateUpdate))]
        public ActionResult Create()
        {
            return View();
        }


        [OperationActionFilter(nameof(Operation.QualifyingRoleCreateUpdate))]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(QualifyingRole qualifyingRole)
        {
            if (ModelState.IsValid)
            {
                _qualifyingRoleRate.Add(qualifyingRole);
                return RedirectToAction("Index");
            }

            return View(qualifyingRole);
        }

        [OperationActionFilter(nameof(Operation.QualifyingRoleCreateUpdate))]
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return StatusCode(StatusCodes.Status400BadRequest);
            }
            QualifyingRole qualifyingRole = _qualifyingRoleRate.GetById(id.Value);
            if (qualifyingRole == null)
            {
                return StatusCode(StatusCodes.Status404NotFound);
            }
            return View(qualifyingRole);
        }

        [OperationActionFilter(nameof(Operation.QualifyingRoleCreateUpdate))]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(QualifyingRole qualifyingRole)
        {
            if (ModelState.IsValid)
            {
                _qualifyingRoleRate.Update(qualifyingRole);
                return RedirectToAction("Index");
            }
            return View(qualifyingRole);
        }

        [OperationActionFilter(nameof(Operation.QualifyingRoleDelete))]
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return StatusCode(StatusCodes.Status400BadRequest);
            }
            QualifyingRole qualifyingRole = _qualifyingRoleRate.GetById(id.Value);
            if (qualifyingRole == null)
            {
                return StatusCode(StatusCodes.Status404NotFound);
            }
            return View(qualifyingRole);
        }

        [OperationActionFilter(nameof(Operation.QualifyingRoleDelete))]
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            QualifyingRole qualifyingRole = _qualifyingRoleRate.GetById(id);
            _qualifyingRoleRate.Delete(qualifyingRole.ID);
            return RedirectToAction("Index");
        }
    }
}
