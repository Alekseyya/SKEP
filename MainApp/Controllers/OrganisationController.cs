using System;
using System.Linq;
using System.Net;
using Core.BL.Interfaces;
using Core.Models;
using Core.Models.RBAC;
using MainApp.RBAC.Attributes;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;


namespace MainApp.Controllers
{
    public class OrganisationController : Controller
    {
        private readonly IOrganisationService _organisationService;

        public OrganisationController(IOrganisationService organisationService)
        {
            _organisationService = organisationService;
        }

        [OperationActionFilter(nameof(Operation.OrganizationView))]
        public ActionResult Index()
        {
            return View(_organisationService.Get(x => x.ToList()));
        }

        [OperationActionFilter(nameof(Operation.OrganizationView))]
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return StatusCode(StatusCodes.Status400BadRequest);
            }

            Organisation organisation = _organisationService.GetById(id.Value);
            if (organisation == null)
            {
                return StatusCode(StatusCodes.Status404NotFound);
                
            }
            return View(organisation);
        }

        [OperationActionFilter(nameof(Operation.OrganizationCreateUpdate))]
        public ActionResult Create()
        {
            return View();
        }

        [OperationActionFilter(nameof(Operation.OrganizationCreateUpdate))]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(Organisation organisation)
        {
            if (ModelState.IsValid)
            {
                _organisationService.Add(organisation);
                return RedirectToAction("Index");
            }

            return View(organisation);
        }

        [OperationActionFilter(nameof(Operation.OrganizationCreateUpdate))]
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return StatusCode(StatusCodes.Status400BadRequest);
            }
            Organisation organisation = _organisationService.GetById(id.Value);
            if (organisation == null)
            {
                return StatusCode(StatusCodes.Status404NotFound);
            }
            return View(organisation);
        }

        [OperationActionFilter(nameof(Operation.OrganizationCreateUpdate))]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(Organisation organisation)
        {
            if (ModelState.IsValid)
            {
                _organisationService.Update(organisation);
                return RedirectToAction("Index");
            }
            return View(organisation);
        }

        [OperationActionFilter(nameof(Operation.OrganizationDelete))]
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return StatusCode(StatusCodes.Status400BadRequest);
            }
            Organisation organisation = _organisationService.GetById(id.Value);
            if (organisation == null)
            {
                return StatusCode(StatusCodes.Status404NotFound);
            }
            return View(organisation);
        }

        [OperationActionFilter(nameof(Operation.OrganizationDelete))]
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            Organisation organisation = _organisationService.GetById(id);
            _organisationService.Delete(organisation.ID);
            return RedirectToAction("Index");
        }
    }
}
