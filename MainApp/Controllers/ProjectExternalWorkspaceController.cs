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
    public class ProjectExternalWorkspaceController : Controller
    {
        private readonly IProjectExternalWorkspaceService _projectExternalWorkspaceService;
        private readonly IProjectService _projectService;
        private readonly IJiraService _jiraService;

        public ProjectExternalWorkspaceController(IProjectExternalWorkspaceService projectExternalWorkspaceService, IProjectService projectService, IJiraService jiraService)
        {
            _projectExternalWorkspaceService = projectExternalWorkspaceService;
            _projectService = projectService;
            _jiraService = jiraService;
        }

        [OperationActionFilter(nameof(Operation.ProjectExternalWorkspaceView))]
        public ActionResult Index()
        {
            var projectExternalWorkspaces = _projectExternalWorkspaceService.Get(x => x.Include(t => t.Project).ToList());
            return View(projectExternalWorkspaces.ToList());
        }

        [HttpGet]
        [AProjectExternalWorkspaceDetailsView]
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return StatusCode(StatusCodes.Status400BadRequest);
            }
            var projectExternalWorkspace = _projectExternalWorkspaceService.GetById((int)id);
            if (projectExternalWorkspace == null)
            {
                return StatusCode(StatusCodes.Status404NotFound);
            }
            return View(projectExternalWorkspace);
        }

        [HttpGet]
        [AProjectExternalWorkspaceCreateUpdate]
        public ActionResult Create(int? projectId)
        {
            var projectExternalWorkspace = new ProjectExternalWorkspace();
            if (projectId.HasValue)
                projectExternalWorkspace.ProjectID = projectId.Value;
            ViewBag.ProjectID = new SelectList(_projectService.Get(x => x.ToList().OrderBy(p => p.ShortName).ToList()), "ID", "ShortName");
            return View(projectExternalWorkspace);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [OperationActionFilter(nameof(Operation.ProjectListView))]
        public ActionResult Create(ProjectExternalWorkspace projectExternalWorkspace)
        {
            if (!string.IsNullOrEmpty(projectExternalWorkspace.ExternalWorkspaceProjectShortName) && _jiraService.IsExternalWorkspaceProjectShortName(projectExternalWorkspace.ExternalWorkspaceProjectShortName) == false)
                ModelState.AddModelError("ExternalWorkspaceProjectShortName", "Код проекта не найден в системе Jira.");
            foreach (var project in _projectService.Get(x => x.Include(p => p.ProjectExternalWorkspace).Where(r => r.ProjectExternalWorkspace.Where(t => t.WorkspaceType == ExternalWorkspaceType.JIRA).Any(t => t.ExternalWorkspaceProjectShortName == projectExternalWorkspace.ExternalWorkspaceProjectShortName)).ToList()))
            {
                foreach (var projectExternalWorkspaceTMP in project.ProjectExternalWorkspace.Where(x => x.ExternalWorkspaceProjectShortName == projectExternalWorkspace.ExternalWorkspaceProjectShortName))
                {
                    if (projectExternalWorkspace.ExternalWorkspaceDateBegin >= projectExternalWorkspaceTMP.ExternalWorkspaceDateBegin && projectExternalWorkspace.ExternalWorkspaceDateBegin <= projectExternalWorkspaceTMP.ExternalWorkspaceDateEnd)
                        ModelState.AddModelError("ExternalWorkspaceDateBegin", "Период действия пересекается с проектом:" + projectExternalWorkspaceTMP.Project.ShortName);
                    if (projectExternalWorkspace.ExternalWorkspaceDateEnd >= projectExternalWorkspaceTMP.ExternalWorkspaceDateBegin && projectExternalWorkspace.ExternalWorkspaceDateEnd <= projectExternalWorkspaceTMP.ExternalWorkspaceDateEnd)
                        ModelState.AddModelError("ExternalWorkspaceDateEnd", "Период действия пересекается с проектом:" + projectExternalWorkspaceTMP.Project.ShortName);
                }
            }

            if (ModelState.IsValid)
            {
                projectExternalWorkspace.WorkspaceType = ExternalWorkspaceType.JIRA;
                _projectExternalWorkspaceService.Add(projectExternalWorkspace);
                string returnUrl = Url.Action("Details", "Project", new { id = projectExternalWorkspace.ProjectID + "#projectexternalworkspaces" }).Replace("%23", "#");
                return new RedirectResult(returnUrl);
            }
            ViewBag.ProjectID = new SelectList(_projectService.Get(x => x.ToList().OrderBy(p => p.ShortName).ToList()), "ID", "ShortName", projectExternalWorkspace.ProjectID);
            return View(projectExternalWorkspace);
        }


        [AProjectExternalWorkspaceCreateUpdate]
        public ActionResult Edit(int? id)
        {
            if (id.HasValue == false)
                return StatusCode(StatusCodes.Status400BadRequest);

            var projectExternalWorkspace = _projectExternalWorkspaceService.GetById(id.Value);
            if (projectExternalWorkspace == null)
                return StatusCode(StatusCodes.Status404NotFound);

            ViewBag.ProjectID = new SelectList(_projectService.Get(x => x.ToList().OrderBy(p => p.ShortName).ToList()), "ID", "ShortName", projectExternalWorkspace.ProjectID);
            return View(projectExternalWorkspace);
        }

        [HttpPost]
        [OperationActionFilter(nameof(Operation.ProjectListView))]
        public ActionResult Edit(ProjectExternalWorkspace projectExternalWorkspace)
        {
            if (!string.IsNullOrEmpty(projectExternalWorkspace.ExternalWorkspaceProjectShortName) && _jiraService.IsExternalWorkspaceProjectShortName(projectExternalWorkspace.ExternalWorkspaceProjectShortName) == false)
                ModelState.AddModelError("ExternalWorkspaceProjectShortName", "Код проекта не найден в системе Jira.");
            foreach (var project in _projectService.Get(x => x.Include(p => p.ProjectExternalWorkspace).Where(r => r.ProjectExternalWorkspace.Where(t => t.WorkspaceType == ExternalWorkspaceType.JIRA).Any(t => t.ExternalWorkspaceProjectShortName == projectExternalWorkspace.ExternalWorkspaceProjectShortName)).ToList()))
            {
                foreach (var projectExternalWorkspaceTMP in project.ProjectExternalWorkspace.Where(x => x.ExternalWorkspaceProjectShortName == projectExternalWorkspace.ExternalWorkspaceProjectShortName))
                {
                    if (projectExternalWorkspaceTMP.ID != projectExternalWorkspace.ID)
                    {
                        if (projectExternalWorkspace.ExternalWorkspaceDateBegin >= projectExternalWorkspaceTMP.ExternalWorkspaceDateBegin && projectExternalWorkspace.ExternalWorkspaceDateBegin <= projectExternalWorkspaceTMP.ExternalWorkspaceDateEnd)
                            ModelState.AddModelError("ExternalWorkspaceDateBegin", "Период действия пересекается с проектом:" + projectExternalWorkspaceTMP.Project.ShortName);
                        if (projectExternalWorkspace.ExternalWorkspaceDateEnd >= projectExternalWorkspaceTMP.ExternalWorkspaceDateBegin && projectExternalWorkspace.ExternalWorkspaceDateEnd <= projectExternalWorkspaceTMP.ExternalWorkspaceDateEnd)
                            ModelState.AddModelError("ExternalWorkspaceDateEnd", "Период действия пересекается с проектом:" + projectExternalWorkspaceTMP.Project.ShortName);
                    }
                }
            }

            if (ModelState.IsValid)
            {
                projectExternalWorkspace.WorkspaceType = ExternalWorkspaceType.JIRA;
                _projectExternalWorkspaceService.Update(projectExternalWorkspace);
                string returnUrl = Url.Action("Details", "Project", new { id = projectExternalWorkspace.ProjectID + "#projectexternalworkspaces" }).Replace("%23", "#");
                return new RedirectResult(returnUrl);
            }
            ViewBag.ProjectID = new SelectList(_projectService.Get(x => x.ToList().OrderBy(p => p.ShortName).ToList()), "ID", "ShortName", projectExternalWorkspace.ProjectID);
            return View(projectExternalWorkspace);
        }

        [OperationActionFilter(nameof(Operation.ProjectExternalWorkspaceDelete))]
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return StatusCode(StatusCodes.Status400BadRequest);
            }

            var projectExternalWorkspace = _projectExternalWorkspaceService.GetById(id.Value);
            if (projectExternalWorkspace == null)
            {
                return StatusCode(StatusCodes.Status404NotFound);
            }
            return View(projectExternalWorkspace);
        }

        [OperationActionFilter(nameof(Operation.ProjectExternalWorkspaceDelete))]
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            var projectExternalWorkspace = _projectExternalWorkspaceService.GetById(id);
            int projectID = projectExternalWorkspace.ProjectID.Value;
            _projectExternalWorkspaceService.Delete(projectExternalWorkspace.ID);
            string returnUrl = Url.Action("Details", "Project", new { id = projectID + "#projectexternalworkspaces" }).Replace("%23", "#");
            return new RedirectResult(returnUrl);

        }
    }
}
