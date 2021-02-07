using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.IO;
//using PagedList.Mvc;
using System.ComponentModel.DataAnnotations;
using AutoMapper;
using BL.Validation;
using Core.BL;
using Core.BL.Interfaces;
using Core.Extensions;
using Core.Helpers;
using Core.Models;
using Core.Models.RBAC;
using Core.RecordVersionHistory;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using MainApp.App_Start;
using MainApp.Dto;
using MainApp.RBAC.Attributes;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using X.PagedList;


namespace MainApp.Controllers
{
    public enum ProjectViewType
    {
        [Display(Name = "Все проекты")] All,
        [Display(Name = "Мои проекты")] MyProjects
    }

    public class ProjectController : Controller
    {
        private readonly IProjectScheduleEntryService _projectScheduleEntryService;
        private readonly IProjectStatusRecordService _projectStatusRecordService;
        private readonly IProjectReportRecordService _projectReportRecordService;
        private readonly IExpensesRecordService _expensesRecordService;
        private readonly IProjectService _projectService;
        private readonly IProjectMembershipService _projectMembershipService;
        private readonly IProjectTypeService _projectTypeService;
        private readonly IEmployeeService _employeeService;
        private readonly IDepartmentService _departmentService;
        private readonly IOrganisationService _organisationService;
        private readonly IUserService _userService;
        private readonly IApplicationUserService _applicationUserService;
        private readonly IProjectExternalWorkspaceService _projectExternalWorkspaceService;
        private readonly IServiceService _serviceService;

        public ProjectController(IProjectScheduleEntryService projectScheduleEntryService,
            IProjectStatusRecordService projectStatusRecordService,
            IProjectReportRecordService projectReportRecordService, IExpensesRecordService expensesRecordService,
            IProjectService projectService,
            IProjectMembershipService projectMembershipService, IProjectTypeService projectTypeService,
            IEmployeeService employeeService, IDepartmentService departmentService,
            IOrganisationService organisationService, IUserService userService,
            IApplicationUserService applicationUserService, IProjectExternalWorkspaceService projectExternalWorkspaceService, IServiceService serviceService)
        {
            _projectScheduleEntryService = projectScheduleEntryService;
            _projectStatusRecordService = projectStatusRecordService;
            _projectReportRecordService = projectReportRecordService;
            _expensesRecordService = expensesRecordService;
            _projectService = projectService;
            _projectMembershipService = projectMembershipService;
            _projectTypeService = projectTypeService;
            _employeeService = employeeService;
            _departmentService = departmentService;
            _organisationService = organisationService;
            _userService = userService;
            _applicationUserService = applicationUserService;
            _projectExternalWorkspaceService = projectExternalWorkspaceService;
            _serviceService = serviceService;
        }

        [HttpGet]
        [AProjectDetailsView]
        public JsonResult GetProjectScheduleEntries(int projectid)
        {
            var sa = new JsonSerializerSettings();

            var projectScheduleEntries = _projectScheduleEntryService
                .Get(pList => pList
                    .Where(p => p.ProjectID == projectid)
                    .Include(p => p.ProjectScheduleEntryType)
                    .OrderBy(p => p.ProjectScheduleEntryType.WBSCode)
                    .ThenBy(p => p.DueDate)
                    .ToList()).Select(p => new { p.ID, p.ProjectScheduleEntryType?.WBSCode, p.Title, p.WorkResult, p.DueDate, p.DateCompleted, p.ExpectedDueDate, p.Comments, p.Amount, p.ContractNum, p.ContractStageNum });

            return Json(projectScheduleEntries);
        }

        public void FillProjectViewbag(Project project)
        {
            if (project.ReportRecords == null || project.ExpensesRecords == null)
            {
                project.ReportRecords = _projectReportRecordService.Get(x =>
                    x.Include(rr => rr.Employee).Where(rr => rr.ProjectID == project.ID).ToList()
                        .OrderBy(rr => rr.RecordSortKey).ToList());
                project.ExpensesRecords = _expensesRecordService.Get(x =>
                    x.Where(er => er.ProjectID == project.ID && er.RecordStatus == ExpensesRecordStatus.ActuallySpent)
                        .Include(er => er.CostSubItem).OrderBy(er => er.ExpensesDate).ToList());
            }

            ViewBag.EmployeePayrollBudget = project.EmployeePayrollBudget;
            ViewBag.EmployeePayrollTotalAmountActual = project.EmployeePayrollTotalAmountActual;

            ViewBag.CanCreateChild = (project.ParentProject == null && !project.ParentProjectID.HasValue);

            ViewBag.EquipmentCostsForResaleActual = Convert.ToDecimal(project.ExpensesRecords
                .Where(x => x.CostSubItem.IsProjectEquipmentCostsForResale == true).Sum(x => x.Amount));
            ViewBag.SubcontractorsAmountActual = Convert.ToDecimal(project.ExpensesRecords
                .Where(x => x.CostSubItem.IsProjectSubcontractorsCosts == true).Sum(x => x.Amount));
            ViewBag.EmployeeHoursActual =
                Convert.ToDecimal(project.ReportRecords.Where(x => x.EmployeeID == null).Sum(x => x.Hours));
            ViewBag.EmployeePayrollActual =
                Convert.ToDecimal(project.ReportRecords.Where(x => x.EmployeeID == null).Sum(x => x.EmployeePayroll));
            ViewBag.EmployeePerformanceBonusActual = Convert.ToDecimal(project.ExpensesRecords
                .Where(x => x.CostSubItem.IsProjectPerformanceBonusCosts == true).Sum(x => x.Amount));
            ViewBag.OtherCostsActual = Convert.ToDecimal(project.ExpensesRecords
                .Where(x => x.CostSubItem.IsProjectOtherCosts == true).Sum(x => x.Amount));
        }

        [OperationActionFilter(nameof(Operation.ProjectListView))]
        public ActionResult Index(
            string sortField,
            string sortOrder,
            string searchString,
            ProjectStatus? statusFilter,
            ProjectViewType? viewType,
            int? page)
        {
            ViewBag.CurrentFilter = searchString;

            if (statusFilter != null && statusFilter.HasValue)
            {
                ViewBag.CurrentStatusFilter = statusFilter;
            }
            else
            {
                ViewBag.CurrentStatusFilter = ProjectStatus.Active;

                statusFilter = ViewBag.CurrentStatusFilter;
            }

            if (viewType != null && viewType.HasValue)
            {
                ViewBag.CurrentViewType = viewType;
            }
            else
            {
                if (_applicationUserService.HasAccess(Operation.ProjectCreateUpdate) == true
                    || _applicationUserService.HasAccess(Operation.ProjectView) == true)
                {
                    ViewBag.CurrentViewType = ProjectViewType.All;
                }
                else if (_applicationUserService.HasAccess(Operation.ProjectMyProjectView) == true)
                {
                    ViewBag.CurrentViewType = ProjectViewType.MyProjects;
                }
                else
                {
                    ViewBag.CurrentViewType = ProjectViewType.All;
                }

                viewType = ViewBag.CurrentViewType;
            }

            if (!(_applicationUserService.HasAccess(Operation.ProjectCreateUpdate) == true
                  || _applicationUserService.HasAccess(Operation.ProjectView) == true
                  || _applicationUserService.HasAccess(Operation.ProjectMyDepartmentProjectView) == true)
                && _applicationUserService.HasAccess(Operation.ProjectMyProjectView))
            {
                ViewBag.CurrentViewType = ProjectViewType.MyProjects;
                viewType = ViewBag.CurrentViewType;
            }

            ViewBag.CurrentSortField = sortField;
            ViewBag.CurrentSortOrder = sortOrder;

            List<ProjectDto> projectList = GetProjectList(sortField, sortOrder, searchString, statusFilter, viewType);
            // projectList = OrderByShortNameThenByParent(projectList);
            ViewBag.SearchProjects = new SelectList(projectList.Select(p => p.Project).OrderBy(p => p.ShortName), "ID",
                "ShortName");

            int pageSize = 20;
            int pageNumber = (page ?? 1);
            return View(projectList.ToPagedList(pageNumber, pageSize));
        }

        private Dictionary<int, RelationDto<Project>> GetTreeParentChild(List<Project> filteredProjectList)
        {
            return GetTreeParentChild(filteredProjectList, null);
        }

        private Dictionary<int, RelationDto<Project>> GetTreeParentChild(List<Project> filteredProjectList,
            List<Project> originProjectList)
        {
            var projects = new List<Project>();
            var projectTree = new Dictionary<int, RelationDto<Project>>();
            // упорядочивание элементов по принципу
            // родитель
            // -> ребенок
            // родитель
            // -> ребенок
            foreach (var project in filteredProjectList)
            {
                if (!project.ParentProjectID.HasValue)
                {
                    if (!projectTree.ContainsKey(project.ID))
                        projectTree.Add(project.ID, new RelationDto<Project>(project, null));
                    // projectTree.Add(project.ID, new KeyValuePair<Project, List<Project>>(project, null));
                    else
                    {
                        projectTree[project.ID].Parent = project;
                        // var parentChilds = projectTree[project.ID];            
                        // projectTree[project.ID] = new KeyValuePair<Project, List<Project>>(project, parentChilds.Value);
                    }
                }
                else
                {
                    if (!projectTree.ContainsKey(project.ParentProjectID.Value))
                        projectTree.Add(project.ParentProjectID.Value,
                            new RelationDto<Project>(null, new List<Project>(new Project[] {project})));
                    // projectTree.Add(project.ParentProjectID.Value, new KeyValuePair<Project, List<Project>>(null, new List<Project>(new Project[] { project })));
                    else
                    {
                        var parentChilds = projectTree[project.ParentProjectID.Value].Childrens;
                        if (parentChilds == null)
                            projectTree[project.ParentProjectID.Value].Childrens =
                                new List<Project>(new Project[] {project});
                        else
                            parentChilds.Add(project);
                        /*;
                        var parentChilds = projectTree[project.ParentProjectID.Value];
                        if (parentChilds.Value == null)
                            projectTree[project.ParentProjectID.Value] = new KeyValuePair<Project, List<Project>>(parentChilds.Key, new List<Project>(new Project[] { project }));
                        else
                            parentChilds.Value.Add(project);
                        */
                    }
                }
            }

            if (originProjectList == null)
                return projectTree;

            foreach (var projectNode in projectTree)
            {
                if (projectNode.Value.Parent == null) // проверяем есть ли родитель
                {
                    var project = originProjectList.FirstOrDefault(p => p.ID == projectNode.Key);
                    projectTree[projectNode.Key].Parent = project;
                }
                else if (projectNode.Value.Childrens == null) // проверяем есть ли дети
                {
                    var childrenProjects = originProjectList.Where(p => p.ParentProjectID == projectNode.Key).ToList();
                    projectTree[projectNode.Key].Childrens = childrenProjects;
                    // projectTree[projectNode.Key] = new KeyValuePair<Project, List<Project>>(projectNode.Value.Key, childrenProjects);
                }
                else
                {
                    var childrenProjects = projectNode.Value.Childrens;
                    var appendChildrenProjects = originProjectList.Where(p =>
                        p.ParentProjectID == projectNode.Key && !childrenProjects.Any(cp => cp.ID == p.ID));
                    if (appendChildrenProjects.Count() > 0)
                        projectNode.Value.Childrens.AddRange(appendChildrenProjects);
                }
            }

            return projectTree;
            /*
            var prjctList = prjctDict.Select(p => p.Value).OrderBy(p => p.Key?.ShortName);
            // встроить фильтр по статусам
            foreach (var project in prjctList)
            {
                if (project.Key != null)
                    projects.Add(project.Key);
                if (project.Value != null)
                    projects.AddRange(project.Value);
            }
            return projects;
            */
        }

        private List<ProjectDto> GetSortedProjectList<T>(Dictionary<int, RelationDto<Project>> tree,
            Func<Project, T> condition, bool isAscending)
        {
            var flatProjectList = tree.Select(p => p.Value);
            flatProjectList = isAscending
                ? flatProjectList.OrderBy(node => condition(node.Parent))
                : flatProjectList.OrderByDescending(node => condition(node.Parent));
            var sortedProjectList = new List<ProjectDto>();
            foreach (var project in flatProjectList)
            {
                var projectDto = new ProjectDto()
                {
                    Project = project.Parent,
                    ParentStatus = ParentStatus.None
                };
                sortedProjectList.Add(projectDto);
                if (project.Childrens != null && project.Childrens.Count > 0)
                {
                    projectDto.ParentStatus = ParentStatus.Parent;
                    var sortedProjectChildrens = isAscending
                        ? project.Childrens.OrderBy(p => condition(p))
                        : project.Childrens.OrderByDescending(p => condition(p));
                    sortedProjectList.AddRange(sortedProjectChildrens.Select(p => new ProjectDto()
                        {Project = p, ParentStatus = ParentStatus.Children}));
                }
            }

            return sortedProjectList.ToList();
        }

        private List<ProjectDto> GetProjectList(
            string sortField,
            string sortOrder,
            string searchString,
            ProjectStatus? statusFilter,
            ProjectViewType? viewType)
        {
            var projectList = _projectService.Get(x =>
                x.Include(p => p.EmployeeCAM).Include(p => p.EmployeePM).Include(p => p.EmployeePA)
                    .Include(p => p.Department).Include(p => p.ProjectType).ToList());
            var filteredProjectList = projectList;

            if (!String.IsNullOrEmpty(searchString))
            {
                string[] searchTokens = searchString.Split(' ');

                List<string> searchTokensList = new List<string>();
                for (int i = 0; i < searchTokens.Length; i++)
                {
                    if (String.IsNullOrEmpty(searchTokens[i]) == false
                        && String.IsNullOrEmpty(searchTokens[i].Trim()) == false)
                    {
                        searchTokensList.Add(searchTokens[i].Trim().ToLower());
                    }
                }

                if (searchTokensList.Count > 1)
                {
                    filteredProjectList = filteredProjectList.Where(p => searchTokensList.All(stl => p.ShortName.ToLower().Contains(stl)
                                           || p.Title.ToLower().Contains(stl.ToLower())
                                           || (p.ProjectType?.ShortName?.ToLower().Contains(stl)).NullableBoolToBool()
                                           || (p.Department?.DisplayShortTitle?.ToLower().Contains(stl)).NullableBoolToBool()
                                           || (p.EmployeePM?.FirstName?.ToLower().Contains(stl)).NullableBoolToBool()
                                           || (p.EmployeePM?.LastName?.ToLower().Contains(stl)).NullableBoolToBool()
                                           || (p.EmployeePM?.MidName?.ToLower().Contains(stl)).NullableBoolToBool()
                                           || (p.EmployeeCAM?.FirstName?.ToLower().Contains(stl)).NullableBoolToBool()
                                           || (p.EmployeeCAM?.LastName?.ToLower().Contains(stl)).NullableBoolToBool()
                                           || (p.EmployeeCAM?.MidName?.ToLower().Contains(stl)).NullableBoolToBool()
                                           || (p.EmployeePA?.FirstName?.ToLower().Contains(stl)).NullableBoolToBool()
                                           || (p.EmployeePA?.LastName?.ToLower().Contains(stl)).NullableBoolToBool()
                                           || (p.EmployeePA?.MidName?.ToLower().Contains(stl)).NullableBoolToBool()
                                           || (p.ProductionDepartment?.DisplayShortTitle?.ToLower().Contains(stl)).NullableBoolToBool()
                                           )).ToList();
                }
                else
                {
                    string searchStringTrim = searchString.Trim().ToLower();
                    filteredProjectList = filteredProjectList.Where(p => p.ShortName.ToLower().Contains(searchStringTrim)
                                           || p.Title.ToLower().Contains(searchStringTrim)
                                           || (p.ProjectType?.ShortName?.ToLower().Contains(searchStringTrim)).NullableBoolToBool()
                                           || (p.Department?.DisplayShortTitle?.ToLower().Contains(searchStringTrim)).NullableBoolToBool()
                                           || (p.EmployeePM?.FirstName?.ToLower().Contains(searchStringTrim)).NullableBoolToBool()
                                           || (p.EmployeePM?.LastName?.ToLower().Contains(searchStringTrim)).NullableBoolToBool()
                                           || (p.EmployeePM?.MidName?.ToLower().Contains(searchStringTrim)).NullableBoolToBool()
                                           || (p.EmployeeCAM?.FirstName?.ToLower().Contains(searchStringTrim)).NullableBoolToBool()
                                           || (p.EmployeeCAM?.LastName?.ToLower().Contains(searchStringTrim)).NullableBoolToBool()
                                           || (p.EmployeeCAM?.MidName?.ToLower().Contains(searchStringTrim)).NullableBoolToBool()
                                           || (p.EmployeePA?.FirstName?.ToLower().Contains(searchStringTrim)).NullableBoolToBool()
                                           || (p.EmployeePA?.LastName?.ToLower().Contains(searchStringTrim)).NullableBoolToBool()
                                           || (p.EmployeePA?.MidName?.ToLower().Contains(searchStringTrim)).NullableBoolToBool()
                                           || (p.ProductionDepartment?.DisplayShortTitle?.ToLower().Contains(searchStringTrim)).NullableBoolToBool()
                                           ).ToList();
                }
            }

            if (statusFilter != ProjectStatus.All)
                filteredProjectList = filteredProjectList.Where(p => p.Status == statusFilter).ToList();

            int userEmployeeID = _applicationUserService.GetEmployeeID();

            if (viewType != ProjectViewType.All && viewType == ProjectViewType.MyProjects)
            {
                filteredProjectList = filteredProjectList.Where(p => p.EmployeePMID == userEmployeeID
                                                                     || p.EmployeeCAMID == userEmployeeID
                                                                     || p.EmployeePAID == userEmployeeID
                                                                     || (p.Department != null && p.Department.DepartmentPAID == userEmployeeID)
                                                                     || (p.ParentProject != null &&
                                                                         (p.ParentProject.EmployeePMID == userEmployeeID
                                                                          || p.ParentProject.EmployeeCAMID == userEmployeeID
                                                                          || p.ParentProject.EmployeePAID == userEmployeeID
                                                                          || (p.ParentProject.Department != null && p.ParentProject.Department.DepartmentPAID == userEmployeeID)))).ToList();
            }

            Dictionary<int, RelationDto<Project>> projectRelations = null;
            if (filteredProjectList.Count == projectList.Count)
                projectRelations = GetTreeParentChild(filteredProjectList.ToList());
            else
                projectRelations = GetTreeParentChild(filteredProjectList.ToList(), projectList.ToList());

            if (!string.IsNullOrEmpty(sortField))
            {
                if (sortField.Contains("_"))
                {
                    string[] fields = sortField.Split('_');
                    if (String.IsNullOrEmpty(sortOrder) || !sortOrder.Equals("desc"))
                        return GetSortedProjectList(projectRelations,
                            p => p.GetType().GetProperty(fields[0]).GetValue(p) != null
                                ? p.GetType().GetProperty(fields[0]).GetValue(p).GetType().GetProperty(fields[1])
                                    .GetValue(p.GetType().GetProperty(fields[0]).GetValue(p))
                                : "",
                            true);
                    else
                        return GetSortedProjectList(projectRelations,
                            p => p.GetType().GetProperty(fields[0]).GetValue(p) != null
                                ? p.GetType().GetProperty(fields[0]).GetValue(p).GetType().GetProperty(fields[1])
                                    .GetValue(p.GetType().GetProperty(fields[0]).GetValue(p))
                                : "",
                            true);
                }
                else
                {
                    if (String.IsNullOrEmpty(sortOrder) == true || sortOrder.Equals("desc") == false)
                        return GetSortedProjectList(projectRelations,
                            p => p.GetType().GetProperty(sortField).GetValue(p), true);
                    else
                        return GetSortedProjectList(projectRelations,
                            p => p.GetType().GetProperty(sortField).GetValue(p), false);
                }
            }
            else
                return GetSortedProjectList(projectRelations, p => p.ShortName, true);
        }

        [AProjectDetailsView]
        public ActionResult Details(int? id, int? version)
        {
            if (id == null)
                return StatusCode(StatusCodes.Status400BadRequest);

            Project project = _projectService.GetById((int) id);
            if (project == null)
                return StatusCode(StatusCodes.Status404NotFound);

            if (version != null && version.HasValue)
            {
                int projectID = id.Value;
                Project projectVersion = _projectService.Get(p => p.Where(x => x.ItemID == projectID
                                                                               && ((x.VersionNumber == version.Value)
                                                                                   || (version.Value == 0 &&
                                                                                       x.VersionNumber == null)))
                    .ToList(), GetEntityMode.VersionAndOther).FirstOrDefault();
                if (projectVersion == null)
                    return StatusCode(StatusCodes.Status404NotFound);

                /*projectVersion.ReportRecords = db.ProjectReportRecords.Include(rr => rr.Employee).Where(rr => rr.ProjectID == projectID).ToList().OrderBy(rr => rr.RecordSortKey);
                projectVersion.StatusRecords = db.ProjectStatusRecords.Where(sr => sr.ProjectID == projectID).OrderByDescending(sr => sr.Created);
                projectVersion.ChildProjects = db.Projects.Where(p => p.ParentProjectID == projectID).OrderBy(p => p.ShortName).ToList();
                projectVersion.ProjectTeam = db.ProjectMembers.Where(pm => pm.ProjectID.Value == projectID).OrderByDescending(pm => pm.Employee.LastName);*/
                projectVersion.Versions = new List<Project>().AsEnumerable();

                ViewBag.CanCreateChild = false;

                ViewBag.Title = projectVersion.ShortName;

                return View(projectVersion);
            }
            else
            {
                if (project.IsVersion)
                    return StatusCode(StatusCodes.Status403Forbidden);


                //project.ReportRecords = db.ProjectReportRecords.Include(rr => rr.Employee).Where(rr => rr.ProjectID == project.ID).ToList().OrderBy(rr => rr.RecordSortKey);
                project.ReportRecords = _projectReportRecordService.Get(x =>
                    x.Include(rr => rr.Employee).Where(rr => rr.ProjectID == project.ID).ToList()
                        .OrderBy(rr => rr.RecordSortKey).ToList());

                project.StatusRecords = _projectStatusRecordService.Get(x =>
                    x.Where(sr => sr.ProjectID == project.ID).OrderByDescending(sr => sr.ProjectStatusBeginDate).ToList());
                foreach (var projectStatusRecord in project.StatusRecords)
                {
                    projectStatusRecord.StatusPeriodName = projectStatusRecord.StatusPeriodName + " (" + projectStatusRecord.ProjectStatusBeginDate?.ToString("dd.MM") + " - " + projectStatusRecord.ProjectStatusEndDate?.ToString("dd.MM") + ")";
                }

                project.ChildProjects = _projectService.Get(x =>
                    x.Where(p => p.ParentProjectID == project.ID).OrderBy(p => p.ShortName).ToList());
                //project.ProjectTeam = db.ProjectMembers.Where(pm => pm.ProjectID.Value == project.ID /*&& pm.ProjectRole.RoleType != ProjectRoleType.CAM */ /*&& (pm.MembershipDateEnd == null || pm.MembershipDateEnd >= DateTime.Now)*/).OrderByDescending(pp => pp.Employee.LastName);
                project.ProjectTeam = _projectMembershipService.Get(x =>
                    x.Include(e=>e.Employee).Include(p=>p.ProjectRole).Where(
                            pm => pm.ProjectID.Value ==
                                  project.ID /*&& pm.ProjectRole.RoleType != ProjectRoleType.CAM */
                            /*&& (pm.MembershipDateEnd == null || pm.MembershipDateEnd >= DateTime.Now)*/)
                        .OrderByDescending(pp => pp.Employee.LastName).ToList());

                //project.ExpensesRecords = db.ExpensesRecords.Where(er => er.ProjectID == project.ID && er.RecordStatus == ExpensesRecordStatus.ActuallySpent).Include(er => er.CostSubItem).OrderBy(er => er.ExpensesDate).ToList();
                project.ExpensesRecords = _expensesRecordService.Get(x =>
                    x.Where(er => er.ProjectID == project.ID && er.RecordStatus == ExpensesRecordStatus.ActuallySpent)
                        .Include(er => er.CostSubItem).OrderBy(er => er.ExpensesDate).ToList());

                project.ProjectExternalWorkspace = _projectExternalWorkspaceService.Get(x => x.Where(t => t.WorkspaceType == ExternalWorkspaceType.JIRA && t.ProjectID == project.ID).ToList());


                project.Versions = _projectService.Get(x => x
                    .Where(p => /*p.IsVersion == true &&*/ p.ItemID == project.ID || p.ID == project.ID)
                    .OrderByDescending(p => p.VersionNumber).ToList(), GetEntityMode.VersionAndOther);

                int versionsCount = project.Versions.Count();
                for (int i = 0; i < versionsCount; i++)
                {

                    if (i == versionsCount - 1)
                        continue;

                    var changes = ChangedRecordsFiller.GetChangedData(project.Versions.ElementAt(i),
                        project.Versions.ElementAt(i + 1));
                    project.Versions.ElementAt(i).ChangedRecords = changes;
                }

                ViewBag.EmployeePayrollBudget = project.EmployeePayrollBudget;
                ViewBag.EmployeePayrollTotalAmountActual = project.EmployeePayrollTotalAmountActual;

                ViewBag.CanCreateChild = (project.ParentProject == null && !project.ParentProjectID.HasValue);

                ViewBag.EquipmentCostsForResaleActual = Convert.ToDecimal(project.ExpensesRecords
                    .Where(x => x.CostSubItem.IsProjectEquipmentCostsForResale == true).Sum(x => x.Amount));
                ViewBag.SubcontractorsAmountActual = Convert.ToDecimal(project.ExpensesRecords
                    .Where(x => x.CostSubItem.IsProjectSubcontractorsCosts == true).Sum(x => x.Amount));
                ViewBag.EmployeeHoursActual =
                    Convert.ToDecimal(project.ReportRecords.Where(x => x.EmployeeID == null).Sum(x => x.Hours));
                ViewBag.EmployeePayrollActual = Convert.ToDecimal(project.ReportRecords.Where(x => x.EmployeeID == null)
                    .Sum(x => x.EmployeePayroll));
                ViewBag.EmployeePerformanceBonusActual = Convert.ToDecimal(project.ExpensesRecords
                    .Where(x => x.CostSubItem.IsProjectPerformanceBonusCosts == true).Sum(x => x.Amount));
                ViewBag.OtherCostsActual = Convert.ToDecimal(project.ExpensesRecords
                    .Where(x => x.CostSubItem.IsProjectOtherCosts == true).Sum(x => x.Amount));

                ViewBag.Title = project.ShortName;

                return View(project);
            }

        }

        [AProjectDetailsView]
        public string GetStatusRecords(int? id)
        {
            List<ProjectStatusRecord> projectStatusRecords = _projectStatusRecordService
                .Get(prs => prs.Where(sr => sr.ProjectID == id).OrderByDescending(sr => sr.ProjectStatusBeginDate).ToList()).ToList();

            foreach (var projectStatusRecord in projectStatusRecords)
            {
                projectStatusRecord.StatusPeriodName = projectStatusRecord.StatusPeriodName + " (" + projectStatusRecord.ProjectStatusBeginDate?.ToString("dd.MM") + " - " + projectStatusRecord.ProjectStatusEndDate?.ToString("dd.MM") + ")";
            }
            var config = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile(new AutoMapperConfig.ProjectStatusRecordGetStatusRecordsProfile());
            }).CreateMapper();
            var projectStatusRecorLists =
                config.Map<List<ProjectStatusRecord>, List<ProjectStatusRecordDto>>(projectStatusRecords);
            return JsonConvert.SerializeObject(projectStatusRecorLists);
        }

        [HttpGet]
        [AProjectDetailsView]
        public ActionResult ProjectReportRecordsByEmployee(int? projectid, string reportPeriodName)
        {
            if (projectid == 0 && string.IsNullOrEmpty(reportPeriodName))
                return PartialView();

            ViewBag.SelectPartial = true;
            ViewBag.ReportPeriodName = reportPeriodName;
            return PartialView(_projectService.GetById((int) projectid));
        }

        [HttpGet]
        [AProjectDetailsView]
        public ActionResult ProjectExpensesRecords(int? projectid, string expensesMonth, string nameView)
        {
            if (projectid == 0 && string.IsNullOrEmpty(expensesMonth))
                return PartialView();

            ViewBag.SelectPartial = true;
            ViewBag.StartExpensesMonth = new DateTime(Convert.ToInt32(expensesMonth.Split('.')[1]),
                Convert.ToInt16(expensesMonth.Split('.')[0]), 1).ToShortDateString();
            ViewBag.NameView = nameView;
            return PartialView(_projectService.GetById((int) projectid));
        }

        [HttpGet]
        [AProjectDetailsView]
        public ActionResult ProjectScheduleEntryRecords(int? projectid)
        {
            return PartialView(_projectService.GetById((int) projectid));
        }

        [HttpPost]
        [OperationActionFilter(nameof(Operation.ProjectCreateUpdate))]
        public void UpdateProjectFinanceSummary(Project project, int? projectid)
        {
            var projectInDB = _projectService.GetById(project.ID);

            // currentProject
            projectInDB.ContractAmount = project.ContractAmount;
            projectInDB.ContractEquipmentResaleAmount = project.ContractEquipmentResaleAmount;
            projectInDB.EquipmentCostsForResale = project.EquipmentCostsForResale;
            projectInDB.SubcontractorsAmountBudget = project.SubcontractorsAmountBudget;
            projectInDB.SubcontractorsAmountBudgetPMP = project.SubcontractorsAmountBudgetPMP;
            projectInDB.EmployeeHoursBudget = project.EmployeeHoursBudget;
            projectInDB.EmployeeHoursBudgetPMP = project.EmployeeHoursBudgetPMP;
            projectInDB.EmployeePayrollBudget = project.EmployeePayrollBudget;
            projectInDB.EmployeePayrollBudgetPMP = project.EmployeePayrollBudgetPMP;
            projectInDB.OtherCostsBudget = project.OtherCostsBudget;
            projectInDB.OtherCostsBudgetPMP = project.OtherCostsBudgetPMP;
            projectInDB.CalcDocTemplateVersion = project.CalcDocTemplateVersion;

            projectInDB.CalcDocTemplateVersion = project.CalcDocTemplateVersion;
            projectInDB.CalcDocUploaded = project.CalcDocUploaded;
            projectInDB.CalcDocUploadedBy = project.CalcDocUploadedBy;
            projectInDB.CalcDocTemplateVersionPMP = project.CalcDocTemplateVersionPMP;
            projectInDB.CalcDocUploadedPMP = project.CalcDocUploadedPMP;
            projectInDB.CalcDocUploadedByPMP = project.CalcDocUploadedByPMP;

            _projectService.Update(projectInDB);
        }

        [HttpPost]
        [OperationActionFilter(nameof(Operation.ProjectCreateUpdate))]
        public string LoadProjectFinanceSummary(IFormFile projectFinanceSummaryUpload)
        {
            var projectFinanceSummarySheetDataTable = new DataTable();
            projectFinanceSummarySheetDataTable.Columns.Add("ColumnA", typeof(string)).Caption = "ColumnA";
            projectFinanceSummarySheetDataTable.Columns.Add("ColumnB", typeof(string)).Caption = "ColumnB";
            projectFinanceSummarySheetDataTable.Columns.Add("ColumnC", typeof(string)).Caption = "ColumnC";
            projectFinanceSummarySheetDataTable.Columns.Add("ColumnD", typeof(string)).Caption = "ColumnD";
            projectFinanceSummarySheetDataTable.Columns.Add("ColumnE", typeof(string)).Caption = "ColumnE";
            projectFinanceSummarySheetDataTable.Columns.Add("ColumnF", typeof(string)).Caption = "ColumnF";

            projectFinanceSummarySheetDataTable = ExcelHelper.ExportColumnsAndDataBySheetName(projectFinanceSummarySheetDataTable, projectFinanceSummaryUpload.OpenReadStream(), "");

            //Сумма по договору (поступление за работы)
            var contractAmount = projectFinanceSummarySheetDataTable.Rows[0].Field<string>("ColumnB").ConvertToDecimal();
            //Сумма поступления на оборудование(для перепродажи)
            var contractEquipmentResaleAmount = projectFinanceSummarySheetDataTable.Rows[1].Field<string>("ColumnB").ConvertToDecimal();
            //затраты на оборудование для перепродажи
            var equipmentCostsForResale = projectFinanceSummarySheetDataTable.Rows[2].Field<string>("ColumnB").ConvertToDecimal();
            //затраты на субподрядчиков
            var subcontractorsAmountBudget = projectFinanceSummarySheetDataTable.Rows[3].Field<string>("ColumnB").ConvertToDecimal();
            //стоимость работ (Ч-Ч)
            var employeeHoursBudget = projectFinanceSummarySheetDataTable.Rows[4].Field<string>("ColumnB").ConvertToDecimal();
            //стоимость работы (ФОТ)
            var employeePayrollBudget = projectFinanceSummarySheetDataTable.Rows[5].Field<string>("ColumnB").ConvertToDecimal();
            //прочие затраты проекта
            var otherCostsBudget = projectFinanceSummarySheetDataTable.Rows[6].Field<string>("ColumnB").ConvertToDecimal();
            //версия шаблона
            var calcDocTemplateVersion = "-";
            if (projectFinanceSummarySheetDataTable.Rows.Count >= 13)
            {
                calcDocTemplateVersion = projectFinanceSummarySheetDataTable.Rows[12].Field<string>("ColumnB");
            }

            //если текущему пользователю сопоставлен сотрудник
            var currentUserEmployee = _userService.GetEmployeeForCurrentUser();
            var calcDocUploaded = currentUserEmployee != null ? currentUserEmployee.FullName : User.Identity.Name;

            var dataObject = new
            {
                ContractAmount = contractAmount,
                ContractEquipmentResaleAmount = contractEquipmentResaleAmount,
                EquipmentCostsForResale = equipmentCostsForResale,
                SubcontractorsAmountBudget = subcontractorsAmountBudget,
                EmployeeHoursBudget = employeeHoursBudget,
                EmployeePayrollBudget = employeePayrollBudget,
                OtherCostsBudget = otherCostsBudget,
                CalcDocTemplateVersion = calcDocTemplateVersion,
                CalcDocTemplateVersionPMP = calcDocTemplateVersion,
                CalcDocUploaded = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss"),
                CalcDocUploadedPMP = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss"),
                CalcDocUploadedBy = calcDocUploaded,
                CalcDocUploadedByPMP = calcDocUploaded
            };
            return JsonConvert.SerializeObject(dataObject);
        }

        [HttpGet]
        [AProjectDetailsView]
        public ActionResult ProjectFinanceSummary(Project project, int? projectid)
        {
            project = projectid.HasValue ? _projectService.GetById(projectid.Value) : project;
            ViewBag.SelectPartial = projectid.HasValue;
            FillProjectViewbag(project);
            return PartialView(project);
        }


        [AProjectDetailsView]
        public string GetReportRecords(int? id)
        {
            var projectReportRecordList = _projectReportRecordService.Get(x => x.Include(rr => rr.Employee)
                .Where(rr => (rr.EmployeeID == null && rr.ProjectID == id)).ToList().OrderBy(rr => rr.RecordSortKey).ToList());
            var expensesRecordList = _expensesRecordService.Get(ex => ex.Include(x => x.CostSubItem)
                .Where(x => x.ProjectID == id && x.RecordStatus == ExpensesRecordStatus.ActuallySpent).ToList()
                .OrderBy(x => x.ExpensesDate).ToList());

            var config = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile(new AutoMapperConfig.ProjectReportRecordGetReportRecordsProfile());
            }).CreateMapper();
            var projectReportRecordDtoList = config.Map<IList<ProjectReportRecord>, IList<ProjectReportRecordDto>>(projectReportRecordList);

            List<ProjectReportRecordDto> recordList = new List<ProjectReportRecordDto>();

            foreach (var record in projectReportRecordDtoList)
            {
                try
                {
                    if (record.ReportPeriodName != null && record.ReportPeriodName.Contains(".") == true)
                    {
                        int recordPeriodYear = Convert.ToInt32(record.ReportPeriodName.Split('.')[1]);
                        int recordPeriodMonth = Convert.ToInt32(record.ReportPeriodName.Split('.')[0]);

                        record.EmployeePerformanceBonus = Convert.ToDecimal(expensesRecordList
                            .Where(er => er.ExpensesDate != null
                                         && er.ExpensesDate.Year == recordPeriodYear
                                         && er.ExpensesDate.Month == recordPeriodMonth
                                         && er.CostSubItem.IsProjectPerformanceBonusCosts).Sum(er => er.Amount));
                        record.OtherCosts = Convert.ToDecimal(expensesRecordList.Where(er => er.ExpensesDate != null
                            && er.ExpensesDate.Year == recordPeriodYear
                            && er.ExpensesDate.Month == recordPeriodMonth
                            && !er.CostSubItem.IsProjectPerformanceBonusCosts).Sum(er => er.Amount));
                        record.TotalCosts = Convert.ToDecimal(record.EmployeePayroll) + Convert.ToDecimal(record.EmployeePerformanceBonus) + Convert.ToDecimal(record.OtherCosts);
                    }

                    recordList.Add(record);
                }
                catch
                {
                }

            }

            return JsonConvert.SerializeObject(recordList);
        }

        [AProjectDetailsView]
        public string GetExpensesRecords(int? id, string nameView, string startExpensesMonth)
        {
            var dateTimeStartExpensesMonth = string.IsNullOrEmpty(startExpensesMonth)
                ? new DateTime()
                : DateTime.Parse(startExpensesMonth);
            var expensesRecords = new List<ExpensesRecord>();

            if (dateTimeStartExpensesMonth == new DateTime())
            {
                expensesRecords = _expensesRecordService.Get(r => r.Include(x => x.CostSubItem)
                    .Where(x => x.ProjectID == id && x.RecordStatus == ExpensesRecordStatus.ActuallySpent).ToList()
                    .OrderBy(x => x.CostSubItem.FullName).ThenBy(x => x.ExpensesDate).ToList()).ToList();
            }
            else
            {
                var lastDayInExpensesMonth = dateTimeStartExpensesMonth.LastDayOfMonth();
                if (nameView == "OtherCosts")
                    expensesRecords = _expensesRecordService.Get(r => r.Include(x => x.CostSubItem).Where(x =>
                            x.ProjectID == id && x.RecordStatus == ExpensesRecordStatus.ActuallySpent &&
                            x.ExpensesDate >= dateTimeStartExpensesMonth && x.ExpensesDate <= lastDayInExpensesMonth &&
                            x.CostSubItem.IsProjectPerformanceBonusCosts == false)
                        .ToList().OrderBy(x => x.CostSubItem.FullName).ThenBy(x => x.ExpensesDate).ToList()).ToList();
                else if (nameView == "EmployeePerformanceBonus")
                    expensesRecords = _expensesRecordService.Get(r => r.Include(x => x.CostSubItem)
                        .Where(x => x.ProjectID == id && x.RecordStatus == ExpensesRecordStatus.ActuallySpent &&
                                    x.ExpensesDate >= dateTimeStartExpensesMonth &&
                                    x.ExpensesDate <= lastDayInExpensesMonth &&
                                    x.CostSubItem.IsProjectPerformanceBonusCosts == true)
                        .ToList().OrderBy(x => x.CostSubItem.FullName).ThenBy(x => x.ExpensesDate).ToList()).ToList();
            }

            var config = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile(new AutoMapperConfig.ExpensesRecordGetExpensesRecordsProfile());
            }).CreateMapper();
            var expensesRecordList = config.Map<List<ExpensesRecord>, List<ExpensesRecordDto>>(expensesRecords);

            return JsonConvert.SerializeObject(expensesRecordList);
        }


        [AProjectDetailsView]
        public string GetReportRecordsByEmployee(int? id, string reportPeriodName)
        {
            var projectReportRecords = string.IsNullOrEmpty(reportPeriodName)
                ? _projectReportRecordService.Get(r => r.Include(rr => rr.Employee)
                    .Where(rr => (rr.EmployeeID != null && rr.ProjectID == id)).ToList()
                    .OrderBy(rr => rr.RecordSortKey + rr.Employee.FullName).ToList())
                : _projectReportRecordService.Get(r => r.Include(rr => rr.Employee)
                    .Where(rr =>
                        (rr.EmployeeID != null && rr.ProjectID == id && rr.ReportPeriodName == reportPeriodName))
                    .ToList().OrderBy(rr => rr.RecordSortKey + rr.Employee.FullName).ToList());

            var config = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile(new AutoMapperConfig.ProjectReportRecordGetReportRecordsByEmployeeProfile());
            }).CreateMapper();
            var projectReportRecordList =
                config.Map<IList<ProjectReportRecord>, List<ProjectReportRecordDto>>(projectReportRecords);

            return JsonConvert.SerializeObject(projectReportRecordList);
        }

        [OperationActionFilter(nameof(Operation.ProjectCreateUpdate))]
        [HttpGet]
        public ActionResult Create(int? parent)
        {
            Project project = null;
            if (parent.HasValue)
            {
                var parentProject = _projectService.GetById(parent.Value);
                if (parentProject == null)
                    return StatusCode(StatusCodes.Status400BadRequest);
                project = new Project
                {
                    ParentProjectID = parentProject.ID,
                    CustomerTitle = parentProject.CustomerTitle,
                    OrganisationID = parentProject.OrganisationID,
                    DepartmentID = parentProject.DepartmentID,
                    ProductionDepartmentID = parentProject.ProductionDepartmentID,
                    BeginDate = parentProject.BeginDate,
                    EndDate = parentProject.EndDate
                };
            }

            ViewBag.IgnoreOptionalRequiredFields = false;
            return RenderEdiatableView(project);
        }

        [OperationActionFilter(nameof(Operation.ProjectCreateUpdate))]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(string ignoreOptionalRequiredFields,
            Project project)
        {
            if (!string.IsNullOrEmpty(project.ShortName))
                project.ShortName = project.ShortName.Trim();

            var validator = new ProjectValidator(project, new ModelStateValidationRecipient(ModelState),
                _projectService.Get(r => r.ToList()).AsQueryable());
            validator.Validate();
            if (ignoreOptionalRequiredFields == "false")
            {
                if (project.ProjectTypeID == null)
                    ModelState.AddModelError("ProjectTypeID",
                        "Требуется указать тип проекта для включения проекта в систему учета");

                if (project.DepartmentID == null)
                    ModelState.AddModelError("DepartmentID",
                        "Требуется указать исполнителя для включения проекта в систему учета.");
                if (project.EmployeePMID == null)
                    ModelState.AddModelError("EmployeePMID",
                        "Требуется указать руководителя проекта для включения проекта в систему учета.");
            }

            if (ModelState.IsValid)
            {
                //if (project.ProjectType?.TSApproveMode == ProjectTypeTSApproveMode.Default || project.ProjectType?.TSApproveMode == ProjectTypeTSApproveMode.PM || project.ProjectType == null)
                //    project.ApproveHoursEmployee = project.EmployeePM;
                //else
                //    project.ApproveHoursEmployee = project.EmployeeCAM;

                _projectService.Add(project);
                return RedirectToAction(nameof(Details), new {id = project.ID});
            }

            ViewBag.IgnoreOptionalRequiredFields = false;
            return RenderEdiatableView(project);
        }

        [OperationActionFilter(nameof(Operation.ProjectCreateUpdate))]
        public ActionResult Edit(int? id)
        {

            if (id == null)
                return StatusCode(StatusCodes.Status400BadRequest);

            Project project = _projectService.GetById(id.Value);
            if (project == null)
                return StatusCode(StatusCodes.Status404NotFound);

            if (project.IsVersion)
                return StatusCode(StatusCodes.Status403Forbidden);


            return RenderEdiatableView(project);
        }

        [OperationActionFilter(nameof(Operation.ProjectCreateUpdate))]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(
            Project project)
        {
            if (String.IsNullOrEmpty(project.ShortName) == false)
                project.ShortName = project.ShortName.Trim();

            _projectService.Validate(project, new ModelStateValidationRecipient(ModelState));
            if (ModelState.IsValid)
            {

                //if (project.ProjectTypeID == null)
                //{
                //    project.ApproveHoursEmployee = db.Employees.Find(project.EmployeePMID);
                //}
                //else
                //{
                //    var projectType = db.ProjectTypes.Find(project.ProjectTypeID);
                //    if (projectType.TSApproveMode == ProjectTypeTSApproveMode.Default || projectType.TSApproveMode == ProjectTypeTSApproveMode.PM)
                //    {
                //        project.ApproveHoursEmployee = db.Employees.Find(project.EmployeePMID);
                //        project.ApproveHoursEmployeeID = project.EmployeePMID;
                //    }
                //    else
                //    {
                //        project.ApproveHoursEmployee = db.Employees.Find(project.EmployeeCAMID);
                //        project.ApproveHoursEmployeeID = project.EmployeeCAMID;
                //    }
                //}

                _projectService.Update(project);
                return RedirectToAction(nameof(Details), new {id = project.ID});
            }

            return RenderEdiatableView(project);
        }

        private ActionResult RenderEdiatableView(Project project)
        {
            ViewBag.ProjectTypeID =
                new SelectList(_projectTypeService.Get(x => x.ToList().OrderBy(pt => pt.ShortName).ToList()), "ID",
                    "FullName", project?.ProjectTypeID);
            ViewBag.EmployeeCAMID =
                new SelectList(_employeeService.Get(x => x.ToList().OrderBy(e => e.FullName).ToList()), "ID",
                    "FullName", project?.EmployeeCAMID);
            ViewBag.EmployeePMID =
                new SelectList(_employeeService.Get(x => x.ToList().OrderBy(e => e.FullName).ToList()), "ID",
                    "FullName", project?.EmployeePMID);
            ViewBag.EmployeePAID =
                new SelectList(_employeeService.Get(x => x.ToList().OrderBy(e => e.FullName).ToList()), "ID",
                    "FullName", project?.EmployeePAID);
            ViewBag.OrganisationID =
                new SelectList(_organisationService.Get(x => x.ToList().OrderBy(o => o.ShortName).ToList()), "ID",
                    "FullName", project?.OrganisationID);
            ViewBag.DepartmentID =
                new SelectList(
                    _departmentService.Get(r => r.Where(d => d.IsFinancialCentre).OrderBy(d => d.ShortName).ToList()),
                    "ID", "FullName", project?.DepartmentID);
            ViewBag.ProductionDepartmentID =
                new SelectList(
                    _departmentService.Get(r => r.Where(d => d.IsFinancialCentre).OrderBy(d => d.ShortName).ToList()),
                    "ID", "FullName", project?.ProductionDepartmentID);
            ViewBag.ApproveHoursEmployeeID =
                new SelectList(_employeeService.Get(x => x.ToList().OrderBy(e => e.FullName).ToList()), "ID",
                    "FullName", project?.ApproveHoursEmployeeID);

            IQueryable<Project> otherProjects = _projectService.Get(x => x.ToList()).AsQueryable();
            if (project != null)
                otherProjects = otherProjects.Where(p => p.ID != project.ID);
            otherProjects = otherProjects.Where(p => !p.ParentProjectID.HasValue).OrderBy(p => p.ShortName);
            ViewBag.OtherProjects = new SelectList(otherProjects, "ID", "ShortName", project?.ParentProjectID);

            if (project == null)
                return View();
            else
                return View(project);
        }

        [OperationActionFilter(nameof(Operation.ProjectDelete))]
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return StatusCode(StatusCodes.Status400BadRequest);
            }

            Project project = _projectService.GetById(id.Value);
            if (project == null)
            {
                return StatusCode(StatusCodes.Status404NotFound);
            }

            return View(project);
        }

        [OperationActionFilter(nameof(Operation.ProjectDelete))]
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            Project project = _projectService.GetById(id);
            var user = _userService.GetUserDataForVersion();
            var recycleBinInDBRelation = _serviceService.HasRecycleBinInDBRelation(project);
            if (recycleBinInDBRelation.hasRelated == false)
            {
                var recycleToRecycleBin = _projectService.RecycleToRecycleBin(project.ID, user.Item1, user.Item2);
                if (!recycleToRecycleBin.toRecycleBin)
                {
                    ViewBag.RecycleBinError =
                        "Невозможно удалить, так как на удаляемый элемент ссылаются другие элементы в системе." +
                        "Сначала необходимо удалить элементы, которые ссылаются на данный элемент. " +
                        recycleToRecycleBin.relatedClassId;
                    return View(project);
                }
            }
            else
            {
                ViewBag.RecycleBinError =
                    "Невозможно удалить, так как на удаляемый элемент ссылаются другие элементы в системе." +
                    $"Сначала необходимо удалить элементы, которые ссылаются на данный элемент. {recycleBinInDBRelation.relatedInDBClassId}";
                return View(project);
            }

            return RedirectToAction("Index");
        }

        private ProjectStatusRecord GetRecentStatusWithFilledProperties(int projectId)
        {
            var statusRecord = _projectStatusRecordService
                .Get(x => x.Where(ps => ps.ProjectID == projectId).OrderByDescending(ps => ps.Created).ToList())
                .FirstOrDefault();
            if (statusRecord == null)
                statusRecord = new ProjectStatusRecord();
            if (!statusRecord.ContractReceivedMoneyAmountActual.HasValue)
                statusRecord.ContractReceivedMoneyAmountActual = 0M;
            if (!statusRecord.PaidToSubcontractorsAmountActual.HasValue)
                statusRecord.PaidToSubcontractorsAmountActual = 0M;
            if (!statusRecord.EmployeePayrollAmountActual.HasValue)
                statusRecord.EmployeePayrollAmountActual = 0M;
            if (!statusRecord.OtherCostsAmountActual.HasValue)
                statusRecord.OtherCostsAmountActual = 0M;
            return statusRecord;
        }

        private void AddChildProjectsReportRow(DataTable dataTable, string projectName,
            ProjectReportRecord reportRecord,
            ProjectStatusRecord statusRecord)
        {
            AddChildProjectsReportRow(dataTable, projectName, reportRecord, statusRecord, -1);
        }

        private DataRow AddChildProjectsReportRow(DataTable dataTable, string projectShortName,
            ProjectReportRecord reportRecord,
            ProjectStatusRecord statusRecord, int index)
        {
            var row = dataTable.NewRow();
            row[nameof(Project.ShortName)] = projectShortName;
            row[nameof(ProjectReportRecord.Hours)] = reportRecord.Hours;
            row[nameof(ProjectReportRecord.EmployeePayroll)] = reportRecord.EmployeePayroll;
            row[nameof(ProjectReportRecord.EmployeePerformanceBonus)] = reportRecord.EmployeePerformanceBonus;
            row[nameof(ProjectReportRecord.OtherCosts)] = reportRecord.OtherCosts;
            row[nameof(ProjectReportRecord.TotalCosts)] = reportRecord.TotalCosts;
            row[nameof(ProjectStatusRecord.StatusText)] = statusRecord.StatusText;
            if (index < 0)
                dataTable.Rows.Add(row);
            else
                dataTable.Rows.InsertAt(row, index);
            return row;
        }

        [AProjectDetailsView]
        [HttpGet]
        public ActionResult ExportChildProjectsToExcel(int id)
        {
            Project project = _projectService.GetById(id);
            if (project == null)
                return StatusCode(StatusCodes.Status404NotFound);
            project.ChildProjects = _projectService.Get(x => x.Where(p => p.ParentProjectID == project.ID).ToList());
            if (project.ChildProjects.Count() == 0)
                return StatusCode(StatusCodes.Status400BadRequest);

            DataTable dataTable = new DataTable();
            dataTable.Columns.Add(nameof(Project.ShortName), typeof(string)).Caption = ExpressionExtension.GetPropertyName((Project x) => x.ShortName);
            dataTable.Columns[nameof(Project.ShortName)].ExtendedProperties["Width"] = (double)30;
            dataTable.Columns.Add(nameof(ProjectReportRecord.Hours), typeof(decimal)).Caption = ExpressionExtension.GetPropertyName((ProjectReportRecord x) => x.Hours);
            dataTable.Columns[nameof(ProjectReportRecord.Hours)].ExtendedProperties["Width"] = (double)15;
            dataTable.Columns.Add(nameof(ProjectReportRecord.EmployeePayroll), typeof(decimal)).Caption = ExpressionExtension.GetPropertyName((ProjectReportRecord x) => x.EmployeePayroll);
            dataTable.Columns[nameof(ProjectReportRecord.EmployeePayroll)].ExtendedProperties["Width"] = (double)15;
            dataTable.Columns.Add(nameof(ProjectReportRecord.EmployeePerformanceBonus), typeof(decimal)).Caption = ExpressionExtension.GetPropertyName((ProjectReportRecord x) => x.EmployeePerformanceBonus);
            dataTable.Columns[nameof(ProjectReportRecord.EmployeePerformanceBonus)].ExtendedProperties["Width"] = (double)15;
            dataTable.Columns.Add(nameof(ProjectReportRecord.OtherCosts), typeof(double)).Caption = ExpressionExtension.GetPropertyName((ProjectReportRecord x) => x.OtherCosts);
            dataTable.Columns[nameof(ProjectReportRecord.OtherCosts)].ExtendedProperties["Width"] = (double)15;
            dataTable.Columns.Add(nameof(ProjectReportRecord.TotalCosts), typeof(double)).Caption = ExpressionExtension.GetPropertyName((ProjectReportRecord x) => x.TotalCosts);
            dataTable.Columns[nameof(ProjectReportRecord.TotalCosts)].ExtendedProperties["Width"] = (double)15;
            dataTable.Columns.Add(nameof(ProjectStatusRecord.StatusText), typeof(string)).Caption = ExpressionExtension.GetPropertyName((ProjectStatusRecord x) => x.StatusText);
            dataTable.Columns[nameof(ProjectStatusRecord.StatusText)].ExtendedProperties["Width"] = (double)35;
            dataTable.Columns.Add("_ISGROUPROW_", typeof(bool)).Caption = "_ISGROUPROW_";

            var totalProjectStatusRecord = new ProjectStatusRecord
            {
                ContractReceivedMoneyAmountActual = 0M,
                PaidToSubcontractorsAmountActual = 0M,
                EmployeePayrollAmountActual = 0M,
                OtherCostsAmountActual = 0M
            };

            var totalProjectReportRecord = new ProjectReportRecord()
            {
                Hours = 0,
                EmployeePayroll = 0,
                EmployeePerformanceBonus = 0,
                OtherCosts = 0,
                TotalCosts = 0
            };

            foreach (var childProject in project.ChildProjects.OrderBy(p => p.ShortName))
            {
                var childProjectRecentStatusRecord = GetRecentStatusWithFilledProperties(childProject.ID);

                totalProjectStatusRecord.ContractReceivedMoneyAmountActual += childProjectRecentStatusRecord.ContractReceivedMoneyAmountActual;
                totalProjectStatusRecord.PaidToSubcontractorsAmountActual += childProjectRecentStatusRecord.PaidToSubcontractorsAmountActual;
                totalProjectStatusRecord.EmployeePayrollAmountActual += childProjectRecentStatusRecord.EmployeePayrollAmountActual;
                totalProjectStatusRecord.OtherCostsAmountActual += childProjectRecentStatusRecord.OtherCostsAmountActual;

                var childPojectReportRecordList = _projectReportRecordService.Get(x => x.Include(rr => rr.Employee)
                    .Where(rr => (rr.EmployeeID == null && rr.ProjectID == childProject.ID)).ToList().OrderBy(rr => rr.RecordSortKey).ToList());
                var childProjectExpensesRecordList = _expensesRecordService.Get(ex => ex.Include(x => x.CostSubItem)
                    .Where(x => x.ProjectID == childProject.ID && x.RecordStatus == ExpensesRecordStatus.ActuallySpent).ToList()
                    .OrderBy(x => x.ExpensesDate).ToList());

                var childProjectReportRecord = new ProjectReportRecord
                {
                    Hours = childPojectReportRecordList.Sum(prr => prr.Hours),
                    EmployeePayroll = childPojectReportRecordList.Sum(prr => prr.EmployeePayroll),
                    EmployeePerformanceBonus = Convert.ToDecimal(childProjectExpensesRecordList.Where(er => er.CostSubItem.IsProjectPerformanceBonusCosts).Sum(er => er.Amount)),
                    OtherCosts = Convert.ToDecimal(childProjectExpensesRecordList.Where(er => !er.CostSubItem.IsProjectPerformanceBonusCosts).Sum(er => er.Amount))
                };

                childProjectReportRecord.TotalCosts = Convert.ToDecimal(childProjectReportRecord.EmployeePayroll)
                    + Convert.ToDecimal(childProjectReportRecord.EmployeePerformanceBonus) + Convert.ToDecimal(childProjectReportRecord.OtherCosts);

                totalProjectReportRecord.Hours += childProjectReportRecord.Hours;
                totalProjectReportRecord.EmployeePayroll += childProjectReportRecord.EmployeePayroll;
                totalProjectReportRecord.EmployeePerformanceBonus += childProjectReportRecord.EmployeePerformanceBonus;
                totalProjectReportRecord.OtherCosts += childProjectReportRecord.OtherCosts;
                totalProjectReportRecord.TotalCosts += childProjectReportRecord.TotalCosts;

                AddChildProjectsReportRow(dataTable, childProject.ShortName, childProjectReportRecord, childProjectRecentStatusRecord);
            }
            DataRow groupRow = AddChildProjectsReportRow(dataTable, "Итого по дочерним", totalProjectReportRecord, totalProjectStatusRecord, 0);
            groupRow["_ISGROUPROW_"] = true;

            var recentProjectStatusRecord = GetRecentStatusWithFilledProperties(project.ID);

            totalProjectStatusRecord.ContractReceivedMoneyAmountActual += recentProjectStatusRecord.ContractReceivedMoneyAmountActual;
            totalProjectStatusRecord.PaidToSubcontractorsAmountActual += recentProjectStatusRecord.PaidToSubcontractorsAmountActual;
            totalProjectStatusRecord.EmployeePayrollAmountActual += recentProjectStatusRecord.EmployeePayrollAmountActual;
            totalProjectStatusRecord.OtherCostsAmountActual += recentProjectStatusRecord.OtherCostsAmountActual;

            var projectReportRecordList = _projectReportRecordService.Get(x => x.Include(rr => rr.Employee)
                    .Where(rr => (rr.EmployeeID == null && rr.ProjectID == project.ID)).ToList().OrderBy(rr => rr.RecordSortKey).ToList());
            var expensesRecordList = _expensesRecordService.Get(ex => ex.Include(x => x.CostSubItem)
                .Where(x => x.ProjectID == project.ID && x.RecordStatus == ExpensesRecordStatus.ActuallySpent).ToList()
                .OrderBy(x => x.ExpensesDate).ToList());

            var projectReportRecord = new ProjectReportRecord
            {
                Hours = projectReportRecordList.Sum(prr => prr.Hours),
                EmployeePayroll = projectReportRecordList.Sum(prr => prr.EmployeePayroll),
                EmployeePerformanceBonus = Convert.ToDecimal(expensesRecordList.Where(er => er.CostSubItem.IsProjectPerformanceBonusCosts).Sum(er => er.Amount)),
                OtherCosts = Convert.ToDecimal(expensesRecordList.Where(er => !er.CostSubItem.IsProjectPerformanceBonusCosts).Sum(er => er.Amount))
            };

            projectReportRecord.TotalCosts = Convert.ToDecimal(projectReportRecord.EmployeePayroll)
                + Convert.ToDecimal(projectReportRecord.EmployeePerformanceBonus) + Convert.ToDecimal(projectReportRecord.OtherCosts);

            totalProjectReportRecord.Hours += projectReportRecord.Hours;
            totalProjectReportRecord.EmployeePayroll += projectReportRecord.EmployeePayroll;
            totalProjectReportRecord.EmployeePerformanceBonus += projectReportRecord.EmployeePerformanceBonus;
            totalProjectReportRecord.OtherCosts += projectReportRecord.OtherCosts;
            totalProjectReportRecord.TotalCosts += projectReportRecord.TotalCosts;

            AddChildProjectsReportRow(dataTable, project.ShortName, projectReportRecord, recentProjectStatusRecord, 0);
            groupRow = AddChildProjectsReportRow(dataTable, "Итого по всем", totalProjectReportRecord, totalProjectStatusRecord, 0);
            groupRow["_ISGROUPROW_"] = true;

            byte[] fileBytes = null;
            using (MemoryStream stream = new MemoryStream())
            {
                using (SpreadsheetDocument doc = SpreadsheetDocument.Create(stream, SpreadsheetDocumentType.Workbook))
                {
                    WorkbookPart workbookPart = ExcelHelper.CreateWorkbookPart(doc, "Отчет");

                    WorksheetPart rId1 = ExcelHelper.CreateWorksheetPartAndImportDataTable(workbookPart, "rId1", 1, 1, (uint)dataTable.Columns.Count,
                        $"Дочерние проекты: {project.ShortName}", dataTable, 3, 1);

                    doc.WorkbookPart.Workbook.Save();
                }

                stream.Position = 0;
                using (BinaryReader reader = new BinaryReader(stream))
                {
                    fileBytes = reader.ReadBytes((int)stream.Length);
                }
            }

            return File(fileBytes, ExcelHelper.ExcelContentType, $"ChildProjects{DateTime.Now.ToString("ddMMyyHHmmss")}.xlsx");
        }

        [AProjectDetailsView]
        [HttpGet]
        public FileContentResult ExportStatusRecordsToExcel(int id)
        {
            byte[] binData = null;

            Project project = _projectService.GetById(id);
            project.StatusRecords = _projectStatusRecordService.Get(x => x.Where(sr => sr.ProjectID == project.ID).OrderByDescending(sr => sr.ProjectStatusBeginDate).ToList());

            DataTable dataTable = new DataTable();
            dataTable.Columns.Add(nameof(ProjectStatusRecord.StatusPeriodName), typeof(string)).Caption = ExpressionExtension.GetPropertyName((ProjectStatusRecord x) => x.StatusPeriodName);
            dataTable.Columns[nameof(ProjectStatusRecord.StatusPeriodName)].ExtendedProperties["Width"] = (double)30;
            dataTable.Columns.Add(nameof(ProjectStatusRecord.RiskIndicatorFlag), typeof(string)).Caption = "Р";
            dataTable.Columns[nameof(ProjectStatusRecord.RiskIndicatorFlag)].ExtendedProperties["Width"] = (double)4;
            dataTable.Columns.Add(nameof(ProjectStatusRecord.StatusText), typeof(ExcelCell)).Caption = ExpressionExtension.GetPropertyName((ProjectStatusRecord x) => x.StatusInfoText); ;
            dataTable.Columns[nameof(ProjectStatusRecord.StatusText)].ExtendedProperties["Width"] = (double)60;
            dataTable.Columns.Add(nameof(ProjectStatusRecord.SupervisorComments), typeof(string)).Caption = ExpressionExtension.GetPropertyName((ProjectStatusRecord x) => x.SupervisorComments);
            dataTable.Columns[nameof(ProjectStatusRecord.SupervisorComments)].ExtendedProperties["Width"] = (double)50;
            dataTable.Columns.Add(nameof(ProjectStatusRecord.Created), typeof(string)).Caption = ExpressionExtension.GetPropertyName((ProjectStatusRecord x) => x.Created);
            dataTable.Columns[nameof(ProjectStatusRecord.Created)].ExtendedProperties["Width"] = (double)19;
            dataTable.Columns.Add(nameof(ProjectStatusRecord.Author), typeof(string)).Caption = ExpressionExtension.GetPropertyName((ProjectStatusRecord x) => x.Author);
            dataTable.Columns[nameof(ProjectStatusRecord.Author)].ExtendedProperties["Width"] = (double)30;
            //dataTable.Columns.Add("_ISGROUPROW_", typeof(bool)).Caption = "_ISGROUPROW_";

            foreach (var record in project.StatusRecords)
            {

                ExcelCell projectProgressCell = new ExcelCell()
                {
                    Value = record.StatusInfoText,
                    Style = new ExcelCellStyle() { DataFormat = ExcelCellStyle.CellValueDataFormat.ForcedText }
                };

                dataTable.Rows.Add(
                    record.StatusPeriodName + " (" + record.ProjectStatusBeginDate?.ToString("dd.MM") + " - " + record.ProjectStatusEndDate?.ToString("dd.MM") + ")",
                    record.RiskIndicatorFlag.GetAttributeOfType<DisplayAttribute>().Name[0],
                    projectProgressCell,
                    record.SupervisorComments,
                    record.Created,
                    record.Author);
            }

            using (MemoryStream stream = new MemoryStream())
            {
                using (SpreadsheetDocument doc = SpreadsheetDocument.Create(stream, SpreadsheetDocumentType.Workbook))
                {
                    WorkbookPart workbookPart = ExcelHelper.CreateWorkbookPart(doc, "Отчет");

                    WorksheetPart rId1 = ExcelHelper.CreateWorksheetPartAndImportDataTable(workbookPart, "rId1", 1, 1, (uint)dataTable.Columns.Count,
                        "Отчеты по статусу проекта: " + project.ShortName,
                        dataTable, 3, 1);

                    doc.WorkbookPart.Workbook.Save();
                }

                stream.Position = 0;
                BinaryReader b = new BinaryReader(stream);
                binData = b.ReadBytes((int)stream.Length);
            }

            return File(binData, ExcelHelper.ExcelContentType, "ProjectStatusReport" + DateTime.Now.ToString("ddMMyyHHmmss") + ".xlsx");
        }

        [AProjectDetailsView]
        [HttpGet]
        public FileContentResult ExportExpensesRecordsToExcel(int id, string startExpensesMonth)
        {
            byte[] binData = null;

            var startExpensesRecord = DateTime.MinValue;
            var endExpensesRecord = DateTime.MaxValue;
            if (startExpensesMonth != null)
            {
                try
                {
                    startExpensesRecord = Convert.ToDateTime(startExpensesMonth);
                    endExpensesRecord = startExpensesRecord.LastDayOfMonth();
                }
                catch (Exception)
                {
                    startExpensesRecord = DateTime.MinValue;
                }
            }

            Project project = _projectService.GetById(id);
            var reportRecords = (startExpensesRecord == DateTime.MinValue)
                ? _expensesRecordService.Get(r => r.Include(x => x.CostSubItem)
                    .Where(x => x.ProjectID == id && x.RecordStatus == ExpensesRecordStatus.ActuallySpent).ToList()
                    .OrderBy(x => x.ExpensesDate).ToList())
                : _expensesRecordService.Get(r => r.Include(x => x.CostSubItem)
                    .Where(x => x.ProjectID == id && x.RecordStatus == ExpensesRecordStatus.ActuallySpent &&
                                x.ExpensesDate >= startExpensesRecord && x.ExpensesDate <= endExpensesRecord).ToList()
                    .OrderBy(x => x.ExpensesDate).ToList());

            var dataTable = new DataTable();

            dataTable.Columns.Add("ExpensesDate", typeof(DateTime)).Caption = "Дата расхода";
            dataTable.Columns["ExpensesDate"].ExtendedProperties["Width"] = (double) 16;
            dataTable.Columns.Add("CostSubItem", typeof(string)).Caption = "Подстатья затрат";
            dataTable.Columns["CostSubItem"].ExtendedProperties["Width"] = (double) 50;
            dataTable.Columns.Add("ExpensesRecordName", typeof(string)).Caption = "Наименование затрат";
            dataTable.Columns["ExpensesRecordName"].ExtendedProperties["Width"] = (double) 90;
            dataTable.Columns.Add("Amount", typeof(double)).Caption = "Сумма расхода";
            dataTable.Columns["Amount"].ExtendedProperties["Width"] = (double) 20;
            dataTable.Columns.Add("BitrixURegNum", typeof(string)).Caption = "Заявка Б24";
            dataTable.Columns["BitrixURegNum"].ExtendedProperties["Width"] = (double) 20;

            foreach (var record in reportRecords)
            {
                dataTable.Rows.Add(record.ExpensesDate,
                    record.CostSubItem.FullName,
                    record.ExpensesRecordName,
                    record.Amount,
                    record.BitrixURegNum
                );
            }

            using (var stream = new MemoryStream())
            {
                using (var doc = SpreadsheetDocument.Create(stream, SpreadsheetDocumentType.Workbook))
                {
                    WorkbookPart workbookPart = ExcelHelper.CreateWorkbookPart(doc, "Отчет");

                    WorksheetPart rId1 = ExcelHelper.CreateWorksheetPartAndImportDataTable(workbookPart, "rId1", 1, 1,
                        (uint) dataTable.Columns.Count,
                        "Отчет о расходах проекта: " + project.ShortName,
                        dataTable, 3, 1);

                    doc.WorkbookPart.Workbook.Save();
                }

                stream.Position = 0;
                var b = new BinaryReader(stream);
                binData = b.ReadBytes((int) stream.Length);
            }

            return File(binData, ExcelHelper.ExcelContentType,
                "Project" + id.ToString() + "ExpensesRecords" + DateTime.Now.ToString("ddMMyyHHmmss") + ".xlsx");

        }

        [AProjectDetailsView]
        [HttpGet]
        public FileContentResult ExportReportRecordsToExcel(int id)
        {
            byte[] binData = null;

            Project project = _projectService.GetById(id);
            var projectReportRecordList = _projectReportRecordService.Get(x => x.Where(rr => rr.EmployeeID == null && rr.ProjectID == project.ID).ToList().OrderBy(rr => rr.RecordSortKey).ToList());
            var expensesRecordList = _expensesRecordService.Get(r =>
                r.Include(x => x.CostSubItem)
                    .Where(x => x.ProjectID == id && x.RecordStatus == ExpensesRecordStatus.ActuallySpent).ToList()
                    .OrderBy(x => x.ExpensesDate).ToList());

            DataTable dataTable = new DataTable();

            dataTable.Columns.Add(nameof(ProjectReportRecord.ReportPeriodName), typeof(string)).Caption = ExpressionExtension.GetPropertyName((ProjectReportRecord x) => x.ReportPeriodName);
            dataTable.Columns[nameof(ProjectReportRecord.ReportPeriodName)].ExtendedProperties["Width"] = (double)19;
            dataTable.Columns.Add(nameof(ProjectReportRecord.EmployeeCount), typeof(int)).Caption = ExpressionExtension.GetPropertyName((ProjectReportRecord x) => x.EmployeeCount);
            dataTable.Columns[nameof(ProjectReportRecord.EmployeeCount)].ExtendedProperties["Width"] = (double)15;
            dataTable.Columns.Add(nameof(ProjectReportRecord.Hours), typeof(double)).Caption = ExpressionExtension.GetPropertyName((ProjectReportRecord x) => x.Hours);
            dataTable.Columns[nameof(ProjectReportRecord.Hours)].ExtendedProperties["Width"] = (double)15;
            dataTable.Columns.Add(nameof(ProjectReportRecord.EmployeePayroll), typeof(double)).Caption = ExpressionExtension.GetPropertyName((ProjectReportRecord x) => x.EmployeePayroll);
            dataTable.Columns[nameof(ProjectReportRecord.EmployeePayroll)].ExtendedProperties["Width"] = (double)15;
            dataTable.Columns.Add(nameof(ProjectReportRecord.EmployeePerformanceBonus), typeof(double)).Caption = ExpressionExtension.GetPropertyName((ProjectReportRecord x) => x.EmployeePerformanceBonus);
            dataTable.Columns[nameof(ProjectReportRecord.EmployeePerformanceBonus)].ExtendedProperties["Width"] = (double)15;
            dataTable.Columns.Add(nameof(ProjectReportRecord.OtherCosts), typeof(double)).Caption = ExpressionExtension.GetPropertyName((ProjectReportRecord x) => x.OtherCosts);
            dataTable.Columns[nameof(ProjectReportRecord.OtherCosts)].ExtendedProperties["Width"] = (double)15;
            dataTable.Columns.Add(nameof(ProjectReportRecord.TotalCosts), typeof(double)).Caption = ExpressionExtension.GetPropertyName((ProjectReportRecord x) => x.TotalCosts);
            dataTable.Columns[nameof(ProjectReportRecord.TotalCosts)].ExtendedProperties["Width"] = (double)15;
            dataTable.Columns.Add(nameof(ProjectReportRecord.CalcDate), typeof(DateTime)).Caption = ExpressionExtension.GetPropertyName((ProjectReportRecord x) => x.CalcDate);
            dataTable.Columns[nameof(ProjectReportRecord.CalcDate)].ExtendedProperties["Width"] = (double)19;
            dataTable.Columns.Add(nameof(ProjectReportRecord.Comments), typeof(string)).Caption = ExpressionExtension.GetPropertyName((ProjectReportRecord x) => x.Comments);
            dataTable.Columns[nameof(ProjectReportRecord.Comments)].ExtendedProperties["Width"] = (double)60;
            dataTable.Columns.Add("_ISGROUPROW_", typeof(bool)).Caption = "_ISGROUPROW_";

            decimal sumHours = 0;
            decimal sumEmployeePayroll = 0;
            decimal sumEmployeePerformanceBonus = 0;
            decimal sumOtherCosts = 0;
            decimal sumTotalCosts = 0;

            foreach (var record in projectReportRecordList)
            {

                try
                {
                    if (record.ReportPeriodName != null && record.ReportPeriodName.Contains(".") == true)
                    {
                        int recordPeriodYear = Convert.ToInt32(record.ReportPeriodName.Split('.')[1]);
                        int recordPeriodMonth = Convert.ToInt32(record.ReportPeriodName.Split('.')[0]);

                        record.EmployeeCount = record.EmployeeCount != null ? record.EmployeeCount.Value : 0;
                        record.Hours = record.Hours != null ? record.Hours.Value : 0;
                        record.EmployeePayroll = record.EmployeePayroll != null ? record.EmployeePayroll.Value : 0;
                        record.EmployeePerformanceBonus = Convert.ToDecimal(expensesRecordList
                            .Where(er => er.ExpensesDate != null
                                         && er.ExpensesDate.Year == recordPeriodYear
                                         && er.ExpensesDate.Month == recordPeriodMonth
                                         && er.CostSubItem.IsProjectPerformanceBonusCosts).Sum(er => er.Amount));
                        record.OtherCosts = Convert.ToDecimal(expensesRecordList
                            .Where(er => er.ExpensesDate != null
                                         && er.ExpensesDate.Year == recordPeriodYear
                                         && er.ExpensesDate.Month == recordPeriodMonth
                                         && !er.CostSubItem.IsProjectPerformanceBonusCosts).Sum(er => er.Amount));
                        record.TotalCosts = Convert.ToDecimal(record.EmployeePayroll) + Convert.ToDecimal(record.EmployeePerformanceBonus) + Convert.ToDecimal(record.OtherCosts);
                    }
                }
                catch
                {

                }

                if (record.EmployeeID == null)
                {
                    dataTable.Rows.Add(record.ReportPeriodName,
                        record.EmployeeCount,
                        record.Hours,
                        record.EmployeePayroll,
                        record.EmployeePerformanceBonus,
                        record.OtherCosts,
                        record.TotalCosts,
                        record.CalcDate,
                        record.Comments,
                        false);

                    sumHours += Convert.ToDecimal(record.Hours);
                    sumEmployeePayroll += Convert.ToDecimal(record.EmployeePayroll);
                    sumEmployeePerformanceBonus += Convert.ToDecimal(record.EmployeePerformanceBonus);
                    sumOtherCosts += Convert.ToDecimal(record.OtherCosts);
                    sumTotalCosts += Convert.ToDecimal(record.TotalCosts);
                }
            }

            dataTable.Rows.Add("Итого:",
                        null,
                        sumHours,
                        sumEmployeePayroll,
                        sumEmployeePerformanceBonus,
                        sumOtherCosts,
                        sumTotalCosts,
                        null,
                        "",
                        true);

            using (var stream = new MemoryStream())
            {
                using (var doc = SpreadsheetDocument.Create(stream, SpreadsheetDocumentType.Workbook))
                {
                    WorkbookPart workbookPart = ExcelHelper.CreateWorkbookPart(doc, "Отчет");

                    WorksheetPart rId1 = ExcelHelper.CreateWorksheetPartAndImportDataTable(workbookPart, "rId1", 1, 1, (uint)dataTable.Columns.Count,
                        "Отчет за периоды по проекту: " + project.ShortName,
                        dataTable, 3, 1);

                    doc.WorkbookPart.Workbook.Save();
                }

                stream.Position = 0;
                var b = new BinaryReader(stream);
                binData = b.ReadBytes((int)stream.Length);
            }

            return File(binData, ExcelHelper.ExcelContentType, project.ShortName + "_Report" + DateTime.Now.ToString("ddMMyyHHmmss") + ".xlsx");
        }

        [AProjectDetailsView]
        [HttpGet]
        public FileContentResult ExportReportRecordsByEmployeeToExcel(int id, string reportPeriodName)
        {
            byte[] binData = null;

            Project project = _projectService.GetById(id);
            project.ReportRecords = string.IsNullOrEmpty(reportPeriodName) ?
                _projectReportRecordService.Get(x => x.Include(rr => rr.Employee).Where(rr => rr.ProjectID == project.ID).ToList().OrderBy(rr => rr.RecordSortKey).ToList()) :
                _projectReportRecordService.Get(x => x.Include(rr => rr.Employee).Where(rr => rr.ProjectID == project.ID && rr.ReportPeriodName == reportPeriodName).ToList().OrderBy(rr => rr.RecordSortKey).ToList());

            var dataTable = new DataTable();

            dataTable.Columns.Add("ReportPeriodName", typeof(string)).Caption = ExpressionExtension.GetPropertyName((ProjectReportRecord x) => x.ReportPeriodName);
            dataTable.Columns["ReportPeriodName"].ExtendedProperties["Width"] = (double)19;
            dataTable.Columns.Add("CalcDate", typeof(string)).Caption = ExpressionExtension.GetPropertyName((ProjectReportRecord x) => x.CalcDate);
            dataTable.Columns["CalcDate"].ExtendedProperties["Width"] = (double)19;
            dataTable.Columns.Add("EmployeeTitle", typeof(string)).Caption = ExpressionExtension.GetPropertyName((Employee x) => x.FullName);
            dataTable.Columns["EmployeeTitle"].ExtendedProperties["Width"] = (double)41;
            dataTable.Columns.Add("EmployeePositionTitle", typeof(string)).Caption = ExpressionExtension.GetPropertyName((Employee x) => x.EmployeePositionTitle);
            dataTable.Columns["EmployeePositionTitle"].ExtendedProperties["Width"] = (double)41;
            dataTable.Columns.Add("Hours", typeof(double)).Caption = ExpressionExtension.GetPropertyName((ProjectReportRecord x) => x.Hours);
            dataTable.Columns["Hours"].ExtendedProperties["Width"] = (double)19;
            dataTable.Columns.Add("EmployeeCount", typeof(int)).Caption = ExpressionExtension.GetPropertyName((ProjectReportRecord x) => x.EmployeeCount);
            dataTable.Columns["EmployeeCount"].ExtendedProperties["Width"] = (double)19;
            dataTable.Columns.Add("Comments", typeof(string)).Caption = ExpressionExtension.GetPropertyName((ProjectReportRecord x) => x.Comments);
            dataTable.Columns["Comments"].ExtendedProperties["Width"] = (double)60;
            dataTable.Columns.Add("_ISGROUPROW_", typeof(bool)).Caption = "_ISGROUPROW_";

            foreach (var group in project.ReportRecords.GroupBy(x => x.ReportPeriodName))
            {
                dataTable.Rows.Add(group.Key, group.Where(x => x.EmployeeID == null).FirstOrDefault().CalcDate, "", "",
                       group.Where(x => x.EmployeeID == null).FirstOrDefault().Hours,
                       @group.Where(x => x.EmployeeID == null).FirstOrDefault().EmployeeCount,
                       group.Where(x => x.EmployeeID == null).FirstOrDefault().Comments,
                       true);

                foreach (var record in group.Where(x => x.EmployeeID != null).OrderBy(x => x.Employee.LastName))
                {
                    dataTable.Rows.Add("",
                         "",
                         record.Employee.LastName + " " + record.Employee.FirstName + " " + record.Employee.MidName,
                         record.Employee.EmployeePositionTitle,
                         record.Hours);
                }
            }

            using (MemoryStream stream = new MemoryStream())
            {
                using (SpreadsheetDocument doc = SpreadsheetDocument.Create(stream, SpreadsheetDocumentType.Workbook))
                {
                    WorkbookPart workbookPart = ExcelHelper.CreateWorkbookPart(doc, "Трудозатраты");

                    WorksheetPart rId1 = ExcelHelper.CreateWorksheetPartAndImportDataTable(workbookPart, "rId1", 1, 1, (uint)dataTable.Columns.Count,
                        "Трудозатраты за периоды по сотрудникам по проекту: " + project.ShortName,
                        dataTable, 3, 1);

                    doc.WorkbookPart.Workbook.Save();
                }

                stream.Position = 0;
                BinaryReader b = new BinaryReader(stream);
                binData = b.ReadBytes((int)stream.Length);
            }

            return File(binData, ExcelHelper.ExcelContentType, "ProjectReportByEmployee" + DateTime.Now.ToString("ddMMyyHHmmss") + ".xlsx");
        }

        [OperationActionFilter(nameof(Operation.ProjectView))]
        [HttpGet]
        public FileContentResult ExportListToExcel(string searchString,
            string sortField,
            string sortOrder,
            ProjectStatus? statusFilter,
            ProjectViewType? viewType)
        {
            byte[] binData = null;

            List<Project> projectList = GetProjectList(sortField, sortOrder, searchString, statusFilter, viewType)
                .Select(p => p.Project).ToList();
            IList<ProjectMember> projectMembers = _projectMembershipService.Get(members => members
                .Where(m => m.ProjectRole.RoleType == ProjectRoleType.Analyst ||
                            m.ProjectRole.RoleType == ProjectRoleType.TPM)
                .Where(m => (!m.MembershipDateBegin.HasValue || m.MembershipDateBegin <= DateTime.Today) &&
                            (!m.MembershipDateEnd.HasValue || m.MembershipDateEnd >= DateTime.Today))
                .ToList());


            DataTable dataTable = new DataTable();

            dataTable.Columns.Add("RowNum", typeof(string)).Caption = "№";
            dataTable.Columns["RowNum"].ExtendedProperties["Width"] = (double) 6;
            dataTable.Columns.Add("ShortName", typeof(string)).Caption = "Код";
            dataTable.Columns["ShortName"].ExtendedProperties["Width"] = (double) 25;
            dataTable.Columns.Add("Title", typeof(string)).Caption = "Полное наименование";
            dataTable.Columns["Title"].ExtendedProperties["Width"] = (double) 65;


            dataTable.Columns.Add("ProjectType", typeof(string)).Caption = "Тип проекта";
            dataTable.Columns["ProjectType"].ExtendedProperties["Width"] = (double) 14;
            dataTable.Columns.Add("Comments", typeof(string)).Caption = "Примечание к проекту";
            dataTable.Columns["Comments"].ExtendedProperties["Width"] = (double) 65;

            dataTable.Columns.Add("BeginDate", typeof(string)).Caption = "Открыт";
            dataTable.Columns["BeginDate"].ExtendedProperties["Width"] = (double) 12;
            dataTable.Columns.Add("EndDate", typeof(string)).Caption = "Закрыт";
            dataTable.Columns["EndDate"].ExtendedProperties["Width"] = (double) 12;

            dataTable.Columns.Add("CustomerTitle", typeof(string)).Caption = "Заказчик";
            dataTable.Columns["CustomerTitle"].ExtendedProperties["Width"] = (double) 20;
            dataTable.Columns.Add("Department", typeof(string)).Caption = "ЦФО";
            dataTable.Columns["Department"].ExtendedProperties["Width"] = (double) 16;
            dataTable.Columns.Add("ProductionDepartment", typeof(string)).Caption = "Исполнитель";
            dataTable.Columns["ProductionDepartment"].ExtendedProperties["Width"] = (double) 16;

            dataTable.Columns.Add("EmployeeCAM", typeof(string)).Caption = "КАМ";
            dataTable.Columns["EmployeeCAM"].ExtendedProperties["Width"] = (double) 35;
            dataTable.Columns.Add("EmployeePM", typeof(string)).Caption = "РП";
            dataTable.Columns["EmployeePM"].ExtendedProperties["Width"] = (double) 35;
            dataTable.Columns.Add("EmployeePA", typeof(string)).Caption = "Администратор";
            dataTable.Columns["EmployeePA"].ExtendedProperties["Width"] = (double) 35;

            dataTable.Columns.Add("Analyst", typeof(string)).Caption = "Аналитик";
            dataTable.Columns["Analyst"].ExtendedProperties["Width"] = (double) 35;
            dataTable.Columns.Add("TPM", typeof(string)).Caption = "ГИП";
            dataTable.Columns["TPM"].ExtendedProperties["Width"] = (double) 35;

            dataTable.Columns.Add("Status", typeof(string)).Caption = "Статус";
            dataTable.Columns["Status"].ExtendedProperties["Width"] = (double) 16;

            int rowNum = 1;
            foreach (var item in projectList)
            {
                String beginDate = "";
                if (item.BeginDate != null && item.BeginDate.HasValue)
                {
                    beginDate = item.BeginDate.Value.ToShortDateString();
                }

                String endDate = "";
                if (item.EndDate != null && item.EndDate.HasValue)
                {
                    endDate = item.EndDate.Value.ToShortDateString();
                }

                string statusDisplayName = ((DisplayAttribute) (item.Status.GetType().GetMember(item.Status.ToString())
                    .First().GetCustomAttributes(true)[0])).Name;

                var membersForCurrentProject = projectMembers
                    .Where(m => m.ProjectID == item.ID && m.Employee != null && m.Employee.FullName != null).ToList();

                var analystFullNameList = membersForCurrentProject
                    .Where(m => m.ProjectRole.RoleType == ProjectRoleType.Analyst).OrderBy(m => m.Employee.FullName)
                    .Select(m => m.Employee.FullName);
                var tpmFullNameList = membersForCurrentProject.Where(m => m.ProjectRole.RoleType == ProjectRoleType.TPM)
                    .OrderBy(m => m.Employee.FullName).Select(m => m.Employee.FullName);

                dataTable.Rows.Add(rowNum.ToString(),
                    ((item.ShortName != null) ? item.ShortName : ""),
                    ((item.Title != null) ? item.Title : ""),
                    ((item.ProjectType != null) ? item.ProjectType.Title : ""),
                    ((item.Comments != null) ? item.Comments : ""),
                    beginDate,
                    endDate,
                    ((item.CustomerTitle != null) ? item.CustomerTitle : ""),
                    ((item.Department != null) ? item.Department.DisplayShortTitle : ""),
                    ((item.ProductionDepartment != null) ? item.ProductionDepartment.DisplayShortTitle : ""),
                    ((item.EmployeeCAM != null) ? item.EmployeeCAM.FullName : ""),
                    ((item.EmployeePM != null) ? item.EmployeePM.FullName : ""),
                    ((item.EmployeePA != null) ? item.EmployeePA.FullName : ""),
                    ((analystFullNameList != null) ? string.Join(", ", analystFullNameList) : ""),
                    ((tpmFullNameList != null) ? string.Join(", ", tpmFullNameList) : ""),
                    statusDisplayName);

                rowNum++;
            }



            using (var stream = new MemoryStream())
            {
                using (var doc = SpreadsheetDocument.Create(stream, SpreadsheetDocumentType.Workbook))
                {
                    WorkbookPart workbookPart = ExcelHelper.CreateWorkbookPart(doc, "Проекты");

                    WorksheetPart rId1 = ExcelHelper.CreateWorksheetPartAndImportDataTable(workbookPart, "rId1", 1, 1,
                        (uint) dataTable.Columns.Count,
                        "Список проектов", dataTable, 3, 1);

                    doc.WorkbookPart.Workbook.Save();
                }

                stream.Position = 0;
                var b = new BinaryReader(stream);
                binData = b.ReadBytes((int) stream.Length);
            }

            return File(binData, ExcelHelper.ExcelContentType,
                "ProjectList" + DateTime.Now.ToString("ddMMyyHHmmss") + ".xlsx");
        }


        [OperationActionFilter(nameof(Operation.ProjectView))]
        [HttpGet]
        public ActionResult ViewVersion(int? id)
        {
            Project project = null;

            if (id == null)
            {
                return StatusCode(StatusCodes.Status400BadRequest);
            }

            project = _projectService.Get(x => x.Where(p => p.ID == id).ToList(), GetEntityMode.VersionAndOther)
                .FirstOrDefault();

            if (project == null)
            {
                return StatusCode(StatusCodes.Status404NotFound);
            }

            if (!project.IsVersion)
                return StatusCode(StatusCodes.Status403Forbidden);

            // ReSharper disable once Mvc.ViewNotResolved
            return View(project);

        }
    }
}
