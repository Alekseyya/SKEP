using System.Linq;
using Core.BL.Interfaces;
using Core.Models;
using Core.Models.RBAC;
using MainApp.RBAC.Attributes;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;






namespace MainApp.Controllers
{
    public class ProjectRoleController : Controller
    {
        private readonly IProjectRoleService _projectRoleService;

        public ProjectRoleController(IProjectRoleService projectRoleService)
        {
            _projectRoleService = projectRoleService;
        }

        [OperationActionFilter(nameof(Operation.ProjectRoleView))]
        public ActionResult Index()
        {
            return View(_projectRoleService.Get(x => x.ToList()));
        }

        [OperationActionFilter(nameof(Operation.ProjectRoleView))]
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return StatusCode(StatusCodes.Status400BadRequest);
            }
            ProjectRole projectRole = _projectRoleService.GetById((int)id);
            if (projectRole == null)
            {
                return StatusCode(StatusCodes.Status404NotFound);
            }
            return View(projectRole);
        }

        [OperationActionFilter(nameof(Operation.ProjectRoleCreateUpdate))]
        public ActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [OperationActionFilter(nameof(Operation.ProjectRoleCreateUpdate))]
        public ActionResult Create(ProjectRole projectRole)
        {
            if (ModelState.IsValid)
            {
                _projectRoleService.Add(projectRole);
                return RedirectToAction("Index");
            }

            return View(projectRole);
        }

        [OperationActionFilter(nameof(Operation.ProjectRoleCreateUpdate))]
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return StatusCode(StatusCodes.Status400BadRequest);
            }
            ProjectRole projectRole = _projectRoleService.GetById((int)id);
            if (projectRole == null)
            {
                return StatusCode(StatusCodes.Status404NotFound);
            }
            return View(projectRole);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [OperationActionFilter(nameof(Operation.ProjectRoleCreateUpdate))]
        public ActionResult Edit(ProjectRole projectRole)
        {
            if (ModelState.IsValid)
            {
                _projectRoleService.Update(projectRole);
                return RedirectToAction("Index");
            }
            return View(projectRole);
        }

        [OperationActionFilter(nameof(Operation.ProjectRoleDelete))]
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return StatusCode(StatusCodes.Status400BadRequest);
            }
            ProjectRole projectRole = _projectRoleService.GetById((int)id);
            if (projectRole == null)
            {
                return StatusCode(StatusCodes.Status404NotFound);
            }
            return View(projectRole);
        }

        [OperationActionFilter(nameof(Operation.ProjectRoleDelete))]
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            ProjectRole projectRole = _projectRoleService.GetById((int)id);
            _projectRoleService.Delete(projectRole.ID);
            return RedirectToAction("Index");
        }
    }
}
