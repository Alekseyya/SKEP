using System;
using System.Collections.Generic;
using System.Net;

using System.Collections;
using System.Linq;
using BL.Validation;
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
    public class ProjectMemberController : Controller
    {
        private readonly IProjectService _projectService;
        private readonly IDepartmentService _departmentService;
        private readonly IEmployeeService _employeeService;
        private readonly IUserService _userService;
        private readonly IProjectMembershipService _projectMembershipService;
        private readonly IProjectRoleService _projectRoleService;

        public ProjectMemberController(IProjectService projectService,
            IDepartmentService departmentService,
            IEmployeeService employeeService,
            IUserService userService,
            IProjectMembershipService projectMembershipService, IProjectRoleService projectRoleService)
        {
            _projectService = projectService;
            _departmentService = departmentService;
            _employeeService = employeeService;
            _userService = userService;
            _projectMembershipService = projectMembershipService;
            _projectRoleService = projectRoleService;
        }

        [OperationActionFilter(nameof(Operation.ProjectMemberView))]
        public ActionResult Index()
        {
            var projectMembers = _projectMembershipService.Get(x => x.Include(p => p.Employee).Include(p=>p.Project).Include(p => p.ProjectRole).ToList());
            return View(projectMembers.ToList());
        }

        [OperationActionFilter(nameof(Operation.ProjectMemberView))]
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return StatusCode(StatusCodes.Status400BadRequest);
            }
            //ProjectMember projectMember = _projectMembershipService.GetById((int)id);
            ProjectMember projectMember = _projectMembershipService.GetById((int)id,x=>x.ProjectRole);
            if (projectMember == null)
            {
                return StatusCode(StatusCodes.Status404NotFound);
            }
            return View(projectMember);
        }

        [OperationActionFilter(nameof(Operation.ProjectMemberCreateUpdate))]
        public ActionResult Create(int? projectid)
        {
            var projectMember = new ProjectMember();
            if (projectid.HasValue)
                projectMember.ProjectID = projectid.Value;

            ViewBag.ProjectID = new SelectList(_projectService.Get(x => x.ToList().OrderBy(p => p.ShortName).ToList()), "ID", "ShortName");
            ViewBag.EmployeeID = new SelectList(_employeeService.Get(x => x.Where(e => e.DismissalDate == null || e.DismissalDate > DateTime.Today).ToList().OrderBy(e => e.FullName).ToList()), "ID", "FullName");
            ViewBag.ProjectRoleID = new SelectList(_projectRoleService.Get(x => x.ToList().OrderBy(pr => (pr.ShortName + pr.Title)).ToList()), "ID", "FullName");
            return View(projectMember);
        }

        [OperationActionFilter(nameof(Operation.ProjectMemberCreateUpdate))]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(ProjectMember projectMember)
        {
            ValidateProjectMember(projectMember);

            if (ModelState.IsValid)
            {
                Project project = _projectService.GetById(projectMember.ProjectID.Value);
                _projectMembershipService.Add(projectMember);

                if (projectMember.ProjectRoleID.HasValue == true)
                {
                    ProjectRole projectRole = _projectRoleService.GetById(projectMember.ProjectRoleID.Value);

                    if (projectRole.RoleType == ProjectRoleType.PM)
                    {
                        if (project.EmployeePM != null
                            && project.EmployeePMID != projectMember.EmployeeID)
                        {
                            ProjectMember oldProjectMemberPM = _projectMembershipService.Get(x =>
                                x.Where(pm =>
                                    pm.ProjectID == projectMember.ProjectID &&
                                    pm.ProjectRole.RoleType == ProjectRoleType.PM).ToList()).FirstOrDefault();
                            if (oldProjectMemberPM != null)
                            {
                                if (projectMember.MembershipDateBegin.HasValue == false)
                                {
                                    projectMember.MembershipDateBegin = DateTime.Today;
                                }

                                oldProjectMemberPM.MembershipDateEnd = projectMember.MembershipDateBegin.Value.AddDays(-1);
                                _projectMembershipService.Update(oldProjectMemberPM);

                                project.EmployeePMID = projectMember.EmployeeID;
                                _projectService.Update(project);
                            }
                        }
                        else if (project.EmployeePM == null)
                        {
                            project.EmployeePMID = projectMember.EmployeeID;
                            _projectService.Update(project);
                        }
                    }
                    else if (projectRole.RoleType == ProjectRoleType.CAM)
                    {
                        if (project.EmployeeCAM != null
                            && project.EmployeeCAMID != projectMember.EmployeeID)
                        {
                            ProjectMember oldProjectMemberCAM = _projectMembershipService.Get(x =>
                                x.Where(pm =>
                                    pm.ProjectID == projectMember.ProjectID &&
                                    pm.ProjectRole.RoleType == ProjectRoleType.CAM).ToList()).FirstOrDefault();
                            if (oldProjectMemberCAM != null)
                            {
                                if (projectMember.MembershipDateBegin.HasValue == false)
                                {
                                    projectMember.MembershipDateBegin = DateTime.Today;
                                }

                                oldProjectMemberCAM.MembershipDateEnd = projectMember.MembershipDateBegin.Value.AddDays(-1);
                                _projectMembershipService.Update(oldProjectMemberCAM);

                                project.EmployeeCAMID = projectMember.EmployeeID;
                                _projectService.Update(project);
                            }
                        }
                        else if (project.EmployeeCAM == null)
                        {
                            project.EmployeeCAMID = projectMember.EmployeeID;
                            _projectService.Update(project);
                        }
                    }
                }

                string returnUrl = Url.Action("Details", "Project", new { id = projectMember.ProjectID + "#projectmembers" }).Replace("%23", "#");
                return new RedirectResult(returnUrl);
            }

            ViewBag.ProjectID = new SelectList(_projectService.Get(x => x.ToList().OrderBy(p => p.ShortName).ToList()), "ID", "ShortName", projectMember.ProjectID);
            ViewBag.EmployeeID = new SelectList(_employeeService.Get(x => x.Where(e => e.DismissalDate == null || e.DismissalDate > DateTime.Today).ToList().OrderBy(e => e.FullName).ToList()), "ID", "FullName", projectMember.EmployeeID);
            ViewBag.ProjectRoleID = new SelectList(_projectRoleService.Get(x => x.ToList().OrderBy(pr => (pr.ShortName + pr.Title)).ToList()), "ID", "FullName", projectMember.ProjectRoleID);
            return View(projectMember);
        }

        private void ValidateProjectMember(ProjectMember member)
        {
            var validator = new ProjectMemberValidator(member, new ModelStateValidationRecipient(ModelState), _projectService.Get(x => x.ToList()).AsQueryable());
            validator.Validate();
        }

        [OperationActionFilter(nameof(Operation.ProjectMemberCreateUpdate))]
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return StatusCode(StatusCodes.Status400BadRequest);
            }

            ProjectMember projectMember = _projectMembershipService.GetById(id.Value);
            if (projectMember == null)
            {
                return StatusCode(StatusCodes.Status404NotFound);
            }
            ViewBag.ProjectID = new SelectList(_projectService.Get(x => x.ToList().OrderBy(p => p.ShortName).ToList()), "ID", "ShortName", projectMember.ProjectID);
            ViewBag.EmployeeID = new SelectList(_employeeService.Get(x => x.Where(e => e.DismissalDate == null || e.DismissalDate > DateTime.Today).ToList().OrderBy(e => e.FullName).ToList()), "ID", "FullName", projectMember.EmployeeID);
            ViewBag.ProjectRoleID = new SelectList(_projectRoleService.Get(x => x.ToList().OrderBy(pr => (pr.ShortName + pr.Title)).ToList()), "ID", "FullName", projectMember.ProjectRoleID);
            return View(projectMember);
        }

        [OperationActionFilter(nameof(Operation.ProjectMemberCreateUpdate))]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(ProjectMember projectMember)
        {
            ValidateProjectMember(projectMember);
            if (ModelState.IsValid)
            {
                _projectMembershipService.Update(projectMember);
                string returnUrl = Url.Action("Details", "Project", new { id = projectMember.ProjectID + "#projectmembers" }).Replace("%23", "#");
                return new RedirectResult(returnUrl);
            }
            ViewBag.ProjectID = new SelectList(_projectService.Get(x => x.ToList().OrderBy(p => p.ShortName).ToList()), "ID", "ShortName", projectMember.ProjectID);
            ViewBag.EmployeeID = new SelectList(_employeeService.Get(x => x.Where(e => e.DismissalDate == null || e.DismissalDate > DateTime.Today).ToList().OrderBy(e => e.FullName).ToList()), "ID", "FullName", projectMember.EmployeeID);
            ViewBag.ProjectRoleID = new SelectList(_projectRoleService.Get(x => x.ToList().OrderBy(pr => (pr.ShortName + pr.Title)).ToList()), "ID", "FullName", projectMember.ProjectRoleID);

            return View(projectMember);
        }

        [OperationActionFilter(nameof(Operation.ProjectMemberDelete))]
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return StatusCode(StatusCodes.Status400BadRequest);
            }
            ProjectMember projectMember = _projectMembershipService.GetById((int)id);
            if (projectMember == null)
            {
                return StatusCode(StatusCodes.Status404NotFound);
            }
            return View(projectMember);
        }

        [OperationActionFilter(nameof(Operation.ProjectMemberDelete))]
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            ProjectMember projectMember = _projectMembershipService.GetById(id);
            int projectID = projectMember.ProjectID.Value;
            _projectMembershipService.Delete(id);
            string returnUrl = Url.Action("Details", "Project", new { id = projectID + "#projectmembers" }).Replace("%23", "#");
            return new RedirectResult(returnUrl);
        }

        [HttpGet]
        public ActionResult PercentageAllocationByProject(int? projectId)
        {
            IList<Project> projects;
            if (projectId.HasValue)
            {
                var project = _projectService.GetById(projectId.Value);
                if (project == null)
                    return StatusCode(StatusCodes.Status404NotFound);

                projects = new List<Project> { project };
                ViewBag.project = project.ID;
            }
            else
            {
                var currentEmployee = _userService.GetEmployeeForCurrentUser();
                if (currentEmployee == null)
                    projects = new List<Project>();
                else
                    projects = _projectService.GetAll(null, null, null, ProjectStatus.Active, currentEmployee.ID);
                ViewBag.project = "";
            }
            ViewBag.Projects = new SelectList(projects, "ID", "ShortName", null);
            ViewBag.employee = "";
            return View();
        }

        [HttpGet]
        public ActionResult PercentageAllocationSummary()
        {

            var currentEmployee = _userService.GetEmployeeForCurrentUser();
            if (currentEmployee == null)
                return StatusCode(StatusCodes.Status400BadRequest, Json("Текущий пользователь не является руководителем подразделения"));

            var department = _departmentService.GetDepartmentForManager(currentEmployee.ID);

            if (department == null)
                return StatusCode(StatusCodes.Status400BadRequest, Json("Текущий пользователь не является руководителем подразделения"));

            var childDepartments = _departmentService.GetChildDepartments(department.ID, false).OrderBy(d => d.FullName);
            var employees = _employeeService.GetEmployeesInDepartment(department.ID, true).OrderBy(e => e.FullName).ThenBy(e => e.ID);
            ViewBag.childDepartments = new SelectList(childDepartments, "ID", "FullName", null);
            ViewBag.employees = new SelectList(employees, "ID", "FullName", null);

            return View(department);
        }

        [HttpGet]
        public ActionResult PercentageAllocationForPM(int? managerId)
        {
            var currentEmployee = _userService.GetEmployeeForCurrentUser();
            Employee projectManager;
            if (managerId.HasValue)
            {
                projectManager = _employeeService.GetById(managerId.Value);
                if (projectManager == null)
                    return StatusCode(StatusCodes.Status404NotFound, Json("Сотрудник не найден"));
                // TODO: Следует ли проверять на то, не уволился ли сотрудник?
            }
            else if (currentEmployee == null) // ID РП явно не указан и текущий пользователь не является сотрудником
                return StatusCode(StatusCodes.Status400BadRequest, Json("Текущий пользователь не является сотрудником"));
            else
                projectManager = currentEmployee;

            ViewBag.CurrentEmployeeId = currentEmployee?.ID;
            return View(projectManager);
        }

        protected override void Dispose(bool disposing)
        {
            /*if (disposing)
            {
                db.Dispose();
            }*/
            base.Dispose(disposing);
        }

        [OperationActionFilter(nameof(Operation.ProjectMemberMyPeopleView))]
        public ActionResult MyPeople(int? projectPMID, int? projectID, string reportDate, bool? showOtherProjects)
        {
            ViewBag.ProjectPMID = _employeeService.Get(x => x.ToList().OrderBy(e => e.FullName).ToList());
            ViewBag.ProjectID = _projectService.Get(x => x.ToList().OrderBy(e => e.ShortName).ToList());

            var projectMemberList = _projectMembershipService.Get(x => x.Include(pm => pm.Project).Include(pm => pm.Employee).Include(pm => pm.ProjectRole).ToList());

            if (projectPMID != null && projectPMID.HasValue == true)
            {
                ViewBag.CurrentProjectPMID = projectPMID.Value;

                var projectPMProjectMemberList = projectMemberList.Where(pm => pm.Project.EmployeePMID == projectPMID).ToList();
                ArrayList projectPMEmployeeIDArray = new ArrayList();
                foreach (ProjectMember projectMember in projectPMProjectMemberList)
                {
                    projectPMEmployeeIDArray.Add(projectMember.EmployeeID);
                }

                projectMemberList = projectMemberList.Where(pm => projectPMEmployeeIDArray.Contains(pm.EmployeeID)).ToList();

            }
            if (projectID != null && projectID.HasValue)
            {
                ViewBag.CurrentProjectID = projectID.Value;
            }
            if (showOtherProjects != null && showOtherProjects.HasValue)
                ViewBag.ShowOtherProjects = showOtherProjects.Value;


            DateTime rDate = DateTime.MinValue;

            if (String.IsNullOrEmpty(reportDate) == false)
            {
                try
                {
                    rDate = Convert.ToDateTime(reportDate);
                }
                catch (Exception)
                {
                    rDate = DateTime.MinValue;
                }
            }

            if (rDate == DateTime.MinValue)
            {
                rDate = DateTime.Today;
            }

            projectMemberList = projectMemberList.Where(pm => (pm.MembershipDateBegin == null || pm.MembershipDateBegin <= rDate)
                && (pm.MembershipDateEnd == null || pm.MembershipDateEnd >= rDate)
                && pm.Employee != null
                && pm.Project != null && pm.Project.Status != ProjectStatus.Closed
                && pm.ProjectRole != null).ToList();

            return View(projectMemberList);
        }
    }
}
