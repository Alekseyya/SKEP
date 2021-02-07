using System.Linq;
using Core.BL.Interfaces;
using Core.Models;
using Core.Models.RBAC;
using MainApp.RBAC.Attributes;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;






namespace MainApp.Controllers
{
    public class ProjectTypeController : Controller
    {
        private readonly IProjectTypeService _projectTypeService;
        private readonly ICostSubItemService _costSubItemService;

        public ProjectTypeController(IProjectTypeService projectTypeService, ICostSubItemService costSubItemService)
        {
            _projectTypeService = projectTypeService;
            _costSubItemService = costSubItemService;
        }

        [OperationActionFilter(nameof(Operation.ProjectTypeView))]
        public ActionResult Index()
        {
            return View(_projectTypeService.Get(x => x.ToList().OrderBy(pt => pt.ShortName).ToList()));
        }

        [OperationActionFilter(nameof(Operation.ProjectTypeView))]
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return StatusCode(StatusCodes.Status400BadRequest);
            }
            ProjectType projectType = _projectTypeService.GetById((int)id);
            if (projectType == null)
            {
                return StatusCode(StatusCodes.Status404NotFound);
            }
            return View(projectType);
        }

        private void SetViewBag(ProjectType projectType)
        {
            ViewBag.BusinessTripCostSubItemID = new SelectList(_costSubItemService.Get(x => x.Where(csi => csi.IsProjectBusinessTripCosts == true).OrderBy(csi => csi.ShortName).ToList()), "ID", "FullName", projectType?.BusinessTripCostSubItemID);
        }

        [OperationActionFilter(nameof(Operation.ProjectTypeCreateUpdate))]
        public ActionResult Create()
        {
            SetViewBag(null);
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [OperationActionFilter(nameof(Operation.ProjectTypeCreateUpdate))]
        public ActionResult Create(ProjectType projectType)
        {
            if (ModelState.IsValid)
            {
                _projectTypeService.Add(projectType);
                return RedirectToAction("Index");
            }

            SetViewBag(projectType);
            return View(projectType);
        }

        [OperationActionFilter(nameof(Operation.ProjectTypeCreateUpdate))]
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return StatusCode(StatusCodes.Status400BadRequest);
            }
            ProjectType projectType = _projectTypeService.GetById((int)id);
            if (projectType == null)
            {
                return StatusCode(StatusCodes.Status404NotFound);
            }

            SetViewBag(projectType);
            return View(projectType);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [OperationActionFilter(nameof(Operation.ProjectTypeCreateUpdate))]
        public ActionResult Edit(ProjectType projectType)
        {
            if (ModelState.IsValid)
            {
                _projectTypeService.Update(projectType);
                return RedirectToAction("Index");
            }

            SetViewBag(projectType);
            return View(projectType);
        }

        [OperationActionFilter(nameof(Operation.ProjectTypeDelete))]
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return StatusCode(StatusCodes.Status400BadRequest);
            }
            ProjectType projectType = _projectTypeService.GetById((int)id);
            if (projectType == null)
            {
                return StatusCode(StatusCodes.Status404NotFound);
            }
            return View(projectType);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [OperationActionFilter(nameof(Operation.ProjectTypeDelete))]
        public ActionResult DeleteConfirmed(int id)
        {
            ProjectType projectType = _projectTypeService.GetById((int)id);
            _projectTypeService.Delete(id);
            return RedirectToAction("Index");
        }
    }
}
