using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using AutoMapper;
using Core;
using Core.BL.Interfaces;
using Core.Config;
using Core.Extensions;
using Core.Helpers;
using Core.JIRA;
using Core.Models;
using Core.Models.RBAC;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using MainApp.App_Start;
using MainApp.Dto;
using MainApp.RBAC.Attributes;
using MainApp.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using X.PagedList;

namespace MainApp.Common
{
    public abstract class BaseTimesheetController : Controller
    {
        protected readonly IEmployeeService _employeeService;
        protected readonly ITSHoursRecordService _tsHoursRecordService;
        protected readonly IProjectService _projectService;
        protected readonly IProjectMembershipService _projectMembershipService;
        protected readonly IUserService _userService;
        protected readonly ITSAutoHoursRecordService _tsAutoHoursRecordService;
        protected readonly IVacationRecordService _vacationRecordService;
        private readonly IReportingPeriodService _reportingPeriodService;
        private readonly IDepartmentService _departmentService;
        private readonly IProductionCalendarService _productionCalendarService;
        private readonly IEmployeeCategoryService _employeeCategoryService;
        private readonly IJiraService _jiraService;
        private readonly IApplicationUserService _applicationUserService;
        private JiraConfig _jiraConfig;


        protected BaseTimesheetController(IEmployeeService employeeService,
                                       ITSHoursRecordService tsHoursRecordService,
                                       IProjectService projectService,
                                       IProjectMembershipService projectMembershipService,
                                       IUserService userService,
                                       ITSAutoHoursRecordService tsAutoHoursRecordService,
                                       IVacationRecordService vacationRecordService,
                                       IReportingPeriodService reportingPeriodService,
                                       IDepartmentService departmentService,
                                       IProductionCalendarService productionCalendarService,
                                       IEmployeeCategoryService employeeCategoryService,
                                       IJiraService jiraService, IOptions<JiraConfig> jiraOptions, IApplicationUserService applicationUserService)
        {
            if (employeeService == null)
                throw new ArgumentException(nameof(employeeService));
            if (tsHoursRecordService == null)
                throw new ArgumentException(nameof(tsHoursRecordService));
            if (projectService == null)
                throw new ArgumentException(nameof(projectService));
            if (projectMembershipService == null)
                throw new ArgumentException(nameof(projectMembershipService));
            if (userService == null)
                throw new ArgumentException(nameof(userService));
            if (tsAutoHoursRecordService == null)
                throw new ArgumentException(nameof(tsAutoHoursRecordService));
            if (vacationRecordService == null)
                throw new ArgumentException(nameof(vacationRecordService));
            if (reportingPeriodService == null)
                throw new ArgumentException(nameof(reportingPeriodService));


            _employeeService = employeeService;
            _tsHoursRecordService = tsHoursRecordService;
            _projectService = projectService;
            _projectMembershipService = projectMembershipService;
            _userService = userService;
            _tsAutoHoursRecordService = tsAutoHoursRecordService;
            _vacationRecordService = vacationRecordService;
            _reportingPeriodService = reportingPeriodService;
            _departmentService = departmentService;
            _productionCalendarService = productionCalendarService;
            _employeeCategoryService = employeeCategoryService;
            _jiraService = jiraService;
            _applicationUserService = applicationUserService;
            _jiraConfig = jiraOptions.Value;
        }


        #region Мои трудозатраты
        [ReturnUrlActionFilter]
        [OperationActionFilter(nameof(Operation.TSHoursRecordView))]
        public ActionResult Index(string searchString, int? employeeId, int? week, string weekStartDate,
                                                              TSRecordStatus? tsRecordStatus, string dateStart, string dateEnd, int? projectId, int? page = 1)
        {
            ViewBag.CurrentSearchString = searchString;
            ViewBag.CurrentEmployeeId = employeeId.HasValue && employeeId.Value != 0 ? employeeId.Value.ToString() : "";
            ViewBag.CurrentWeek = week;
            ViewBag.CurrentWeekStartDate = weekStartDate;
            ViewBag.CurrentTSRecordStatus = tsRecordStatus;
            ViewBag.CurrentDateStart = dateStart;
            ViewBag.CurrentDateEnd = dateEnd;
            ViewBag.CurrentProjectId = projectId.HasValue && projectId.Value != 0 ? projectId.Value.ToString() : "";
            //DateTime.TryParse(dateStart, out DateTime dateTimeStart);
            //DateTime.TryParse(dateEnd, out DateTime dateTimeEnd);

            var dateTuple = SwitchingDate(week, weekStartDate, dateStart, dateEnd);

            var tsHoursRecordStatus = TSRecordStatus.All;
            if (tsRecordStatus != null)
                tsHoursRecordStatus = (TSRecordStatus)tsRecordStatus;

            var tsHoursRecords = new List<TSHoursRecord>();
            var recordStatusList = new List<TSRecordStatus>
            {
                TSRecordStatus.All, TSRecordStatus.Editing, TSRecordStatus.Approving, TSRecordStatus.PMApproved,
                TSRecordStatus.HDApproved, TSRecordStatus.Declined, TSRecordStatus.DeclinedEditing,
                TSRecordStatus.Archived
            };


            //Указание количества записей на странице
            int pageSize = 30;
            int pageNumber = page ?? 1;

            //Когда в строку поиска ничего не вписано
            if (string.IsNullOrEmpty(searchString))
            {
                var recordStatus = TSRecordStatus.All;
                if (tsRecordStatus != null)
                    recordStatus = (TSRecordStatus)tsRecordStatus;

                tsHoursRecords = _tsHoursRecordService.GetEmployeeTSHoursRecords(employeeId ?? 0, dateTuple.Item1,
                    dateTuple.Item2, projectId ?? 0, recordStatus, null).OrderBy(x => x.RecordDate).ThenBy(x => x.Project.ShortName).ToList();


                ViewBag.ArrayStatus = GetMyHoursRecordStatus(recordStatusList, null);
                ViewBag.EmployeesId = _employeeService.GetAllEmployees().OrderBy(empl => empl.LastName).ToList();
                ViewBag.Search = new SelectList(_projectService.GetAll("", "", "", ProjectStatus.All, null), "ID", "ShortName");
                if (employeeId.HasValue)
                    SetProjectListEmployee(employeeId.Value);

                ViewBag.Week = week == null ? 0 : (int)week;
                return View(tsHoursRecords.ToPagedList(pageNumber, pageSize));
            }

            if (employeeId.HasValue)
            {
                SetProjectListEmployee(employeeId.Value);
                if (projectId.HasValue)
                {
                    if (dateTuple.Item1 == new DateTime() && dateTuple.Item2 == new DateTime() && tsRecordStatus == tsHoursRecordStatus)
                        tsHoursRecords = _tsHoursRecordService.FindHoursRecords(searchString).ToList().Where(x => x.EmployeeID == employeeId && x.ProjectID == projectId.Value).OrderBy(x => x.RecordDate).ThenBy(x => x.Project.ShortName).ToList();
                    if (dateTuple.Item1 != new DateTime() && dateTuple.Item2 == new DateTime() && tsRecordStatus == tsHoursRecordStatus)
                        tsHoursRecords = _tsHoursRecordService.FindHoursRecords(searchString).ToList().Where(x => x.EmployeeID == employeeId && x.ProjectID == projectId.Value && x.RecordDate >= dateTuple.Item1).OrderBy(x => x.RecordDate).ThenBy(x => x.Project.ShortName).ToList();
                    if (dateTuple.Item1 == new DateTime() && dateTuple.Item2 != new DateTime() && tsRecordStatus == tsHoursRecordStatus)
                        tsHoursRecords = _tsHoursRecordService.FindHoursRecords(searchString).ToList().Where(x => x.EmployeeID == employeeId && x.ProjectID == projectId.Value && x.RecordDate <= dateTuple.Item2).OrderBy(x => x.RecordDate).ThenBy(x => x.Project.ShortName).ToList();
                    if (dateTuple.Item1 != new DateTime() && dateTuple.Item2 != new DateTime() && tsRecordStatus == tsHoursRecordStatus)
                        tsHoursRecords = _tsHoursRecordService.FindHoursRecords(searchString).ToList().Where(x => x.EmployeeID == employeeId && x.ProjectID == projectId.Value && x.RecordDate >= dateTuple.Item1 && x.RecordDate <= dateTuple.Item2).OrderBy(x => x.RecordDate).ThenBy(x => x.Project.ShortName).ToList();
                }
                else
                {
                    if (dateTuple.Item1 == new DateTime() && dateTuple.Item2 == new DateTime() && tsRecordStatus == tsHoursRecordStatus)
                        tsHoursRecords = _tsHoursRecordService.FindHoursRecords(searchString).ToList().Where(x => x.EmployeeID == employeeId).OrderBy(x => x.RecordDate).ThenBy(x => x.Project.ShortName).ToList();
                    if (dateTuple.Item1 != new DateTime() && dateTuple.Item2 == new DateTime() && tsRecordStatus == tsHoursRecordStatus)
                        tsHoursRecords = _tsHoursRecordService.FindHoursRecords(searchString).ToList().Where(x => x.EmployeeID == employeeId && x.RecordDate >= dateTuple.Item1).OrderBy(x => x.RecordDate).ThenBy(x => x.Project.ShortName).ToList();
                    if (dateTuple.Item1 == new DateTime() && dateTuple.Item2 != new DateTime() && tsRecordStatus == tsHoursRecordStatus)
                        tsHoursRecords = _tsHoursRecordService.FindHoursRecords(searchString).ToList().Where(x => x.EmployeeID == employeeId && x.RecordDate <= dateTuple.Item2).OrderBy(x => x.RecordDate).ThenBy(x => x.Project.ShortName).ToList();
                    if (dateTuple.Item1 != new DateTime() && dateTuple.Item2 != new DateTime() && tsRecordStatus == tsHoursRecordStatus)
                        tsHoursRecords = _tsHoursRecordService.FindHoursRecords(searchString).ToList().Where(x => x.EmployeeID == employeeId && x.RecordDate >= dateTuple.Item1 && x.RecordDate <= dateTuple.Item2).OrderBy(x => x.RecordDate).ThenBy(x => x.Project.ShortName).ToList();
                }
            }
            else
            {
                ViewBag.ProjectsFromDB = _projectService.Get(x => x.ToList());
                if (projectId.HasValue)
                {
                    if (dateTuple.Item1 == new DateTime() && dateTuple.Item2 == new DateTime() && tsRecordStatus == tsHoursRecordStatus)
                        tsHoursRecords = _tsHoursRecordService.FindHoursRecords(searchString).ToList().Where(x => x.ProjectID == projectId.Value).OrderBy(x => x.RecordDate).ThenBy(x => x.Project.ShortName).ToList();
                    if (dateTuple.Item1 != new DateTime() && dateTuple.Item2 == new DateTime() && tsRecordStatus == tsHoursRecordStatus)
                        tsHoursRecords = _tsHoursRecordService.FindHoursRecords(searchString).ToList().Where(x => x.ProjectID == projectId.Value && x.RecordDate >= dateTuple.Item1).OrderBy(x => x.RecordDate).ThenBy(x => x.Project.ShortName).ToList();
                    if (dateTuple.Item1 == new DateTime() && dateTuple.Item2 != new DateTime() && tsRecordStatus == tsHoursRecordStatus)
                        tsHoursRecords = _tsHoursRecordService.FindHoursRecords(searchString).ToList().Where(x => x.ProjectID == projectId.Value && x.RecordDate <= dateTuple.Item2).OrderBy(x => x.RecordDate).ThenBy(x => x.Project.ShortName).ToList();
                    if (dateTuple.Item1 != new DateTime() && dateTuple.Item2 != new DateTime() && tsRecordStatus == tsHoursRecordStatus)
                        tsHoursRecords = _tsHoursRecordService.FindHoursRecords(searchString).ToList().Where(x => x.ProjectID == projectId.Value && x.RecordDate >= dateTuple.Item1 && x.RecordDate <= dateTuple.Item2).OrderBy(x => x.RecordDate).ThenBy(x => x.Project.ShortName).ToList();
                }
                else
                {
                    if (dateTuple.Item1 == new DateTime() && dateTuple.Item2 == new DateTime() && tsRecordStatus == tsHoursRecordStatus)
                        tsHoursRecords = _tsHoursRecordService.FindHoursRecords(searchString).ToList().OrderBy(x => x.RecordDate).ThenBy(x => x.Project.ShortName).ToList();
                    if (dateTuple.Item1 != new DateTime() && dateTuple.Item2 == new DateTime() && tsRecordStatus == tsHoursRecordStatus)
                        tsHoursRecords = _tsHoursRecordService.FindHoursRecords(searchString).ToList().Where(x => x.RecordDate >= dateTuple.Item1).OrderBy(x => x.RecordDate).ThenBy(x => x.Project.ShortName).ToList();
                    if (dateTuple.Item1 == new DateTime() && dateTuple.Item2 != new DateTime() && tsRecordStatus == tsHoursRecordStatus)
                        tsHoursRecords = _tsHoursRecordService.FindHoursRecords(searchString).ToList().Where(x => x.RecordDate <= dateTuple.Item2).OrderBy(x => x.RecordDate).ThenBy(x => x.Project.ShortName).ToList();
                    if (dateTuple.Item1 != new DateTime() && dateTuple.Item2 != new DateTime() && tsRecordStatus == tsHoursRecordStatus)
                        tsHoursRecords = _tsHoursRecordService.FindHoursRecords(searchString).ToList().Where(x => x.RecordDate >= dateTuple.Item1 && x.RecordDate <= dateTuple.Item2).OrderBy(x => x.RecordDate).ThenBy(x => x.Project.ShortName).ToList();
                }
            }


            ViewBag.ArrayStatus = GetMyHoursRecordStatus(recordStatusList, null);
            ViewBag.EmployeesId = _employeeService.GetAllEmployees().OrderBy(empl => empl.LastName).ToList();
            ViewBag.Search = new SelectList(_projectService.GetAll("", "", "", ProjectStatus.All, null), "ID", "ShortName");
            SwitchingDate(week, weekStartDate, dateStart, dateEnd);
            ViewBag.Week = week == null ? 0 : (int)week;

            return View(tsHoursRecords.ToPagedList(pageNumber, pageSize));
        }
        [HttpGet]
        [OperationActionFilter(nameof(Operation.TSHoursRecordCreateUpdate))]
        public virtual ActionResult Create(int? employeeId)
        {
            ViewBag.EmployeeID = new SelectList(_employeeService.GetAllEmployees(), "ID", "FullName", employeeId);
            ViewBag.ProjectID = new SelectList(_projectService.GetAll("", "", "", ProjectStatus.All, null), "ID", "ShortName");
            ViewBag.ParentTSAutoHoursRecordID = new SelectList(_tsAutoHoursRecordService.GetAll(), "ID", "FullName");
            ViewBag.ParentVacationRecordID = new SelectList(_vacationRecordService.GetAll(), "ID", "FullName");
            ViewBag.ArrayStatus = new SelectList(TSRecordStatus.All.GetCollectionList<TSRecordStatus>(x => x != TSRecordStatus.All)
                .Select(x =>
                {
                    return new SelectListItem
                    {
                        Text = x.GetAttributeOfType<DisplayAttribute>().Name,
                        Value = (Convert.ToInt32(x)).ToString()
                    };
                }), "Value", "Text");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [OperationActionFilter(nameof(Operation.TSHoursRecordCreateUpdate))]
        public ActionResult Create(TSHoursRecord tSHoursRecord)
        {
            var currUser = _userService.GetUserDataForVersion();
            if (tSHoursRecord.RecordDate == null)
                ModelState.AddModelError("RecordDate", "Требудется указать отчетную дату.");
            if (tSHoursRecord.Hours == null)
                ModelState.AddModelError("Hours", "Требудется указать часы.");
            if (tSHoursRecord.Description == null)
                ModelState.AddModelError("Descriptions", "Требуется указать состав работ.");

            tSHoursRecord.RecordSource = TSRecordSource.UserInput;

            //есть ли у человека проекты
            if (ModelState.IsValid == true)
            {
                if (tSHoursRecord.EmployeeID != null && tSHoursRecord.EmployeeID.HasValue == true
                    && tSHoursRecord.RecordDate != null && tSHoursRecord.RecordDate.HasValue == true)
                {
                    var employeeProjectList = _projectMembershipService.GetProjectsForEmployeeInTSMyHours(tSHoursRecord.EmployeeID.Value, new DateTimeRange((DateTime)tSHoursRecord.RecordDate, (DateTime)tSHoursRecord.RecordDate));

                    if (employeeProjectList != null && employeeProjectList.Count() != 0)
                    {
                        //проверка состоит ли человек в данном проекте
                        if (employeeProjectList.Count(x => x.ID == tSHoursRecord.ProjectID) != 0)
                        {
                            _tsHoursRecordService.Add(tSHoursRecord, currUser.Item1, currUser.Item2);

                            DateTime currentWeekStartDate = DateTime.Now.StartOfWeek();
                            DateTime currentWeekEndDate = currentWeekStartDate.EndOfWeek();

                            DateTime dateStart = (tSHoursRecord.RecordDate.Value < currentWeekStartDate) ? tSHoursRecord.RecordDate.Value : currentWeekStartDate;
                            DateTime dateEnd = (tSHoursRecord.RecordDate.Value > currentWeekEndDate) ? tSHoursRecord.RecordDate.Value : currentWeekEndDate;
                            return RedirectToAction("Index", new { employeeId = tSHoursRecord.EmployeeID, dateStart = dateStart.ToShortDateString(), dateEnd = dateEnd.ToShortDateString() });
                        }
                        else
                            ModelState.AddModelError("EmployeeID", "Данный сотрудник не состоит в проекте, либо неправильно указана отчетная дата. Возможно, отчетный период на этот месяц закрыт.");
                    }
                    else
                        ModelState.AddModelError("ProjectID", "Данный сотрудник не состоит ни в одной РГ проекта");
                }
            }

            ViewBag.EmployeeID = new SelectList(_employeeService.GetAllEmployees(), "ID", "FullName", tSHoursRecord.EmployeeID);
            ViewBag.ProjectID = new SelectList(_projectService.GetAll("", "", "", ProjectStatus.All, null), "ID", "ShortName", tSHoursRecord.ProjectID);
            ViewBag.ParentTSAutoHoursRecordID = new SelectList(_tsAutoHoursRecordService.GetAll(), "ID", "FullName");
            ViewBag.ParentVacationRecordID = new SelectList(_vacationRecordService.GetAll(), "ID", "FullName");
            ViewBag.ArrayStatus = new SelectList(TSRecordStatus.All.GetCollectionList<TSRecordStatus>(x => x != TSRecordStatus.All)
                .Select(x =>
                {
                    return new SelectListItem
                    {
                        Text = x.GetAttributeOfType<DisplayAttribute>().Name,
                        Value = (Convert.ToInt32(x)).ToString()
                    };
                }), "Value", "Text");
            return View(tSHoursRecord);
        }

        [HttpGet]
        [OperationActionFilter(nameof(Operation.TSHoursRecordView))]
        public ActionResult Details(int? id, int? version)
        {
            if (id == null)
            {
                return StatusCode(StatusCodes.Status400BadRequest);
            }

            TSHoursRecord tSHoursRecord = _tsHoursRecordService.GetById((int)id);
            if (tSHoursRecord == null)
            {
                return StatusCode(StatusCodes.Status404NotFound);
            }

            if (version != null && version.HasValue)
            {
                var recordVersion = _tsHoursRecordService.GetVersion(id.Value, version.Value);
                if (recordVersion == null)
                    return StatusCode(StatusCodes.Status404NotFound);
                recordVersion.Versions = new List<TSHoursRecord>().AsEnumerable();
                return View(recordVersion);
            }

            tSHoursRecord = _tsHoursRecordService.GetById(id.Value);


            return View(tSHoursRecord);
        }


        [HttpGet]
        [OperationActionFilter(nameof(Operation.TSHoursRecordCreateUpdate))]
        public ActionResult Edit(int? id)
        {
            if (id == null)
                return StatusCode(StatusCodes.Status400BadRequest);

            TSHoursRecord tSHoursRecord = _tsHoursRecordService.GetById((int)id);
            if (tSHoursRecord == null)
                return StatusCode(StatusCodes.Status404NotFound);

            if (tSHoursRecord.IsVersion)
                 return StatusCode(StatusCodes.Status403Forbidden);

            ViewBag.EmployeeID = new SelectList(_employeeService.GetAllEmployees(), "ID", "FullName", tSHoursRecord.EmployeeID);
            ViewBag.ProjectID = new SelectList(_projectService.GetAll("", "", "", ProjectStatus.All, null), "ID", "ShortName", tSHoursRecord.ProjectID);
            ViewBag.ParentTSAutoHoursRecordID = new SelectList(_tsAutoHoursRecordService.GetAll(), "ID", "FullName");
            ViewBag.ParentVacationRecordID = new SelectList(_vacationRecordService.GetAll(), "ID", "FullName");
            ViewBag.ArrayStatus = new SelectList(TSRecordStatus.All.GetCollectionList<TSRecordStatus>(x => x != TSRecordStatus.All)
                .Select(x =>
                {
                    return new SelectListItem
                    {
                        Text = x.GetAttributeOfType<DisplayAttribute>().Name,
                        Value = (Convert.ToInt32(x)).ToString()
                    };
                }), "Value", "Text");

            return View(tSHoursRecord);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [OperationActionFilter(nameof(Operation.TSHoursRecordCreateUpdate))]
        [ValidateAjax]
        public ActionResult Edit(TSHoursRecord tSHoursRecord)
        {
            var currUser = _userService.GetUserDataForVersion();

            if (tSHoursRecord.RecordDate == null)
                ModelState.AddModelError("RecordDate", "Требудется указать отчетную дату.");
            if (tSHoursRecord.Hours == null)
                ModelState.AddModelError("Hours", "Требудется указать часы.");
            if (tSHoursRecord.Description == null)
                ModelState.AddModelError("Descriptions", "Требуется указать состав работ.");

            //есть ли у человека проекты
            if (tSHoursRecord.EmployeeID != null && tSHoursRecord.EmployeeID.HasValue == true
                && tSHoursRecord.RecordDate != null && tSHoursRecord.RecordDate.HasValue == true)
            {
                var employeeProjectList = _projectMembershipService.GetProjectsForEmployeeInTSMyHours(tSHoursRecord.EmployeeID.Value, new DateTimeRange((DateTime)tSHoursRecord.RecordDate, (DateTime)tSHoursRecord.RecordDate));

                if (employeeProjectList != null && employeeProjectList.Count() != 0)
                {
                    //проверка состоит ли человек в данном проекте
                    if (employeeProjectList.Count(x => x.ID == tSHoursRecord.ProjectID) != 0)
                    {
                        _tsHoursRecordService.Update(tSHoursRecord, currUser.Item1, currUser.Item2);
                    }
                    else
                        ModelState.AddModelError("EmployeeID", "Данный сотрудник не состоит в проекте, либо неправильно указана отчетная дата. Возможно, отчетный период на этот месяц закрыт.");
                }
                else
                    ModelState.AddModelError("ProjectID", "Данный сотрудник не состоит ни в одной РГ проекта");
            }

            if (ModelState.IsValid && currUser.Item1 != "")
                return Json(new { success = true });
            return Json(new { success = false, errors = ModelState.Values.SelectMany(x => x.Errors).Select(x => x.ErrorMessage).ToList() });
        }

        [HttpGet]
        [OperationActionFilter(nameof(Operation.TSHoursRecordDelete))]
        public ActionResult Delete(int? id)
        {
            if (id == null)
                return StatusCode(StatusCodes.Status400BadRequest);

            var tsHoursRecord = _tsHoursRecordService.GetById(id.Value);
            if (tsHoursRecord == null)
                return StatusCode(StatusCodes.Status404NotFound);

            return View(tsHoursRecord);
        }

        [HttpPost, ActionName("Delete")]
        [OperationActionFilter(nameof(Operation.TSHoursRecordDelete))]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            var user = _userService.GetUserDataForVersion();
            _tsHoursRecordService.RecycleToRecycleBin(id, user.Item1, user.Item2);
            return RedirectToAction("Index");
        }

        [NonAction]
        public SelectList GetMyHoursRecordStatus(List<TSRecordStatus> recordsStatus, TSRecordStatus? selected)
        {

            var arrayStatus = new SelectList(Enum.GetValues(typeof(TSRecordStatus)).Cast<TSRecordStatus>()
                .Where(n => recordsStatus.Contains(n))
                .Select(x =>
                {
                    return new SelectListItem
                    {
                        Text = x == TSRecordStatus.All
                            ? "Все записи"
                            : x.GetAttributeOfType<DisplayAttribute>().Name,
                        Value = (Convert.ToInt32(x)).ToString(),
                        Selected = selected != null && (x == (TSRecordStatus)selected ? true : false)
                    };
                }), "Value", "Text");

            return arrayStatus;
        }

        [HttpPost]
        [OperationActionFilter(nameof(Operation.TSHoursRecordView))]
        public void SetProjectListEmployee(int employeeId)
        {
            //Выбор сотрудника
            var currentUserEmployeeID = employeeId != 0 ? employeeId : _userService.GetEmployeeForCurrentUser().ID;

            //список проектов
            var listProjectsEmployeeWorks = _projectMembershipService.GetProjectsForEmployee(currentUserEmployeeID);
            ViewBag.ProjectsFromDB = listProjectsEmployeeWorks;
            ViewBag.ProjectsCount = listProjectsEmployeeWorks.Count;
            if (listProjectsEmployeeWorks.Count > 0)
            {
                ViewBag.ProjectID = null;
            }
        }

        [NonAction]
        public (DateTime, DateTime) SwitchingDate(int? week, string weekStartDate, string dateStart, string dateEnd, bool isDeclinedView = false)
        {

            DateTime.TryParse(weekStartDate, out DateTime setStartWeekDate);
            var currentWeekStartDate = DateTime.Now.StartOfWeek();
            var currentWeekEndDate = currentWeekStartDate.EndOfWeek();
            DateTime.TryParse(dateStart, out DateTime dateTimeStart);
            DateTime.TryParse(dateEnd, out DateTime dateTimeEnd);


            //Установить текущую неделю при первом входе на страницу MyHours
            if (week == null && weekStartDate == null && dateStart == null && dateEnd == null && !isDeclinedView)
            {
                ViewBag.HoursStartDate = currentWeekStartDate.ToShortDateString();
                ViewBag.HoursEndDate = currentWeekEndDate.ToShortDateString();
                return (DateTime.Parse(ViewBag.HoursStartDate), DateTime.Parse(ViewBag.HoursEndDate));
            }
            //Установить текущую неделю при первом входе на страницу MyHours -DeclinedView
            if (week == null && weekStartDate == null && dateStart == null && dateEnd == null && isDeclinedView)
            {
                ViewBag.HoursStartDate = "";
                ViewBag.HoursEndDate = "";
                return (new DateTime(), new DateTime());
            }

            //Если нажата текущая неделя
            if (week.HasValue && week == 0 && !string.IsNullOrEmpty(weekStartDate) && dateStart == null &&
                dateEnd == null)
            {
                ViewBag.HoursStartDate = currentWeekStartDate.ToShortDateString();
                ViewBag.HoursEndDate = currentWeekEndDate.ToShortDateString();
                return (DateTime.Parse(ViewBag.HoursStartDate), DateTime.Parse(ViewBag.HoursEndDate));
            }

            //если нажата клавиша назад(на прошлую неделю и другие прошлые)
            if ((week < 0 || week == null) && !string.IsNullOrEmpty(weekStartDate) && dateStart == null &&
                dateEnd == null)
            {
                var beginingLastWeek = setStartWeekDate.AddDays(week == null ? 0 : (int)week * 7);
                ViewBag.HoursStartDate = beginingLastWeek.ToShortDateString();
                ViewBag.HoursEndDate = beginingLastWeek.EndOfWeek().ToShortDateString();
                return (DateTime.Parse(ViewBag.HoursStartDate), DateTime.Parse(ViewBag.HoursEndDate));

            }

            //Если нажата клавиша вперед(на следующую неделю)
            //Если это текущая неделя, то не должно идти вперед
            if (week > 0 && !string.IsNullOrEmpty(weekStartDate) && setStartWeekDate == currentWeekStartDate && dateStart == null && dateEnd == null)
            {
                var beginingNextWeeks = setStartWeekDate.AddDays((int)week * 7);
                ViewBag.HoursStartDate = beginingNextWeeks.ToShortDateString();
                ViewBag.HoursEndDate = beginingNextWeeks.EndOfWeek().ToShortDateString();
                return (DateTime.Parse(ViewBag.HoursStartDate), DateTime.Parse(ViewBag.HoursEndDate));
            }

            //Если указаны даты в форме
            //только конечная дата
            if (week == null && string.IsNullOrEmpty(weekStartDate) && string.IsNullOrEmpty(dateStart) && !string.IsNullOrEmpty(dateEnd))
            {
                ViewBag.HoursStartDate = DateTime.MinValue.ToShortDateString();
                ViewBag.HoursEndDate = dateTimeEnd.ToShortDateString();
                return (DateTime.Parse(ViewBag.HoursStartDate), DateTime.Parse(ViewBag.HoursEndDate));
            }
            //только начальная
            if (week == null && string.IsNullOrEmpty(weekStartDate) && !string.IsNullOrEmpty(dateStart) && string.IsNullOrEmpty(dateEnd))
            {
                ViewBag.HoursStartDate = dateTimeStart.ToShortDateString();
                //до конца недели
                ViewBag.HoursEndDate = DateTime.MaxValue.ToShortDateString();
                return (DateTime.Parse(ViewBag.HoursStartDate), DateTime.Parse(ViewBag.HoursEndDate));
            }
            //начальная и конечная
            if (week == null && string.IsNullOrEmpty(weekStartDate) && !string.IsNullOrEmpty(dateStart) && !string.IsNullOrEmpty(dateEnd))
            {
                ViewBag.HoursStartDate = dateTimeStart.ToShortDateString();
                ViewBag.HoursEndDate = dateTimeEnd.ToShortDateString();
                return (DateTime.Parse(ViewBag.HoursStartDate), DateTime.Parse(ViewBag.HoursEndDate));
            }

            return (new DateTime(), new DateTime());
        }


        [HttpPost]
        public ActionResult LoadJiraMyHours(List<TSHoursRecordImportJiraViewModel> tsHoursRecordImportJiraViewModels, string selectedDateStart, string selectedDateEnd)
        {
            if (tsHoursRecordImportJiraViewModels != null)
            {
                foreach (var record in tsHoursRecordImportJiraViewModels.Where(x => x.Selected).Select(x => new { tsHoursRecord = x.TSHoursRecord, jiraProjectKey = x.JiraProjectKey, duplicate = x.Imported, jiraIssueName = x.JiraIssueName, changed = x.ChangedRecord }))
                {
                    var contentResult = new ContentResult();
                    var project = _projectService.GetByShortName(record.tsHoursRecord.Project.ShortName);
                    if (project != null)
                    {
                        var description = record.jiraIssueName + " - " + record.tsHoursRecord.Description;
                        if (record.duplicate || record.changed)
                        {
                            var tsHoursRecord = _tsHoursRecordService.Get(r =>
                                r.Where(x => x.ExternalSourceElementID == record.tsHoursRecord.ExternalSourceElementID &&
                                    (x.RecordStatus == TSRecordStatus.Editing || x.RecordStatus == TSRecordStatus.Declined || x.RecordStatus == TSRecordStatus.Approving) && x.RecordSource == TSRecordSource.JIRA).ToList()).FirstOrDefault();

                            //если изменились поля - обновить
                            if (tsHoursRecord != null && (tsHoursRecord.Project.ShortName != record.tsHoursRecord.Project.ShortName ||
                                                          tsHoursRecord.RecordDate != record.tsHoursRecord.RecordDate || tsHoursRecord.Hours.Value != record.tsHoursRecord.Hours || tsHoursRecord.Description != description))
                            {
                                contentResult = (ContentResult)MyHoursDataSave(tsHoursRecord.ID, project.ID, record.tsHoursRecord.Hours.ToString(), record.tsHoursRecord.RecordDate.ToString(),
                                    description, record.tsHoursRecord.RecordSource, record.tsHoursRecord.RecordStatus, record.tsHoursRecord.ExternalSourceElementID, true);
                                if (contentResult.Content != "true" && contentResult.Content != "declinedOfdeclinedEditing")
                                    return Json(new { Message = contentResult.Content });
                            }
                        }
                        else
                        {
                            contentResult = (ContentResult)MyHoursDataSave(null, project.ID, record.tsHoursRecord.Hours.ToString(), record.tsHoursRecord.RecordDate.ToString(),
                                description, record.tsHoursRecord.RecordSource, record.tsHoursRecord.RecordStatus, record.tsHoursRecord.ExternalSourceElementID, true);
                            if (contentResult.Content != "true" && contentResult.Content != "declinedOfdeclinedEditing")
                                return Json(new { Message = contentResult.Content });
                        }
                    }
                }
            }
            return RedirectToAction("MyHours", new { dateStart = selectedDateStart, dateEnd = selectedDateEnd });
        }

        [HttpGet]
        public ActionResult ImportJira(string hoursStartDate, string hoursEndDate, bool IsDeclinedView = false)
        {
            DateTime dateTimeHoursStartDate = new DateTime();
            DateTime dateTimeHoursEndDate = new DateTime();
            DateTime.TryParse(hoursStartDate, out dateTimeHoursStartDate);
            DateTime.TryParse(hoursEndDate, out dateTimeHoursEndDate);
            if (IsDeclinedView)
                dateTimeHoursEndDate = DateTime.Now;

            var currentUser = _userService.GetEmployeeForCurrentUser();
            var userJiraLogin = ADHelper.GetUserLoginWithoutDomainName(_userService.GetCurrentUser().UserLogin);
            var searchUrl = _jiraService.CreateUrlWorklogsByUser(dateTimeHoursStartDate, dateTimeHoursEndDate);
            var responseJson = _jiraService.GetJson(searchUrl);
            var jiraBaseModel = JsonConvert.DeserializeObject<JiraBaseModel>(responseJson);

            var selectionTSHoursRecordsDateAndUser = _tsHoursRecordService.Get(r=>r.Where(x=> x.RecordSource == TSRecordSource.JIRA 
                                                                                             && x.RecordDate >= dateTimeHoursStartDate 
                                                                                             && x.RecordDate <= dateTimeHoursEndDate && x.Employee.ID == currentUser.ID).ToList());

            var tsHoursRecordImportJiraViewModels = new List<TSHoursRecordImportJiraViewModel>();
            foreach (var issue in jiraBaseModel.Issues)
            {
                issue.Fields.WorkLogs = _jiraService.GetWorklogsByIssueId(issue.Id);

                foreach (var worklog in issue.Fields.WorkLogs)
                {
                    if (String.IsNullOrEmpty(worklog.Author.Name) == false
                        && String.IsNullOrEmpty(userJiraLogin) == false
                        && worklog.Author.Name.ToLower() == userJiraLogin.ToLower()
                        && worklog.Started >= dateTimeHoursStartDate.StartOfDay() && worklog.Started <= dateTimeHoursEndDate.EndOfDay())
                    {
                        //TODO Приходят данные в секундах. Перевести в часы, потом уже округлять
                        var hours = Math.Ceiling(((double)worklog.TimeSpentSeconds / 3600) * 4) / 4;
                        //var hours = Convert.ToDouble(worklog.TimeSpentSeconds / 3600);
                        var recordDate = worklog.Started.StartOfDay();
                        var description = string.IsNullOrEmpty(worklog.Comment) ? " " : worklog.Comment;

                        description = description.RemoveUnwantedHtmlTags();
                        description = RPCSHelper.NormalizeAndTrimString(description);

                        var projectShortName = _jiraService.GetProjectShortNameFromEpic(issue);
                        var isErrorJira = Enum.TryParse(projectShortName, out ErrorTypesJira errorTypeJira);
                        var project = _projectService.GetByShortName(projectShortName);
                        if (project == null)
                            projectShortName = string.Empty;

                        if (String.IsNullOrEmpty(projectShortName))
                            projectShortName = _jiraService.GetProjectShortNameFromExternalWorkspace(issue, userJiraLogin, recordDate, recordDate);

                        var isImportedFromJira = selectionTSHoursRecordsDateAndUser.Where(x => x.ExternalSourceElementID == worklog.Id.ToString()).ToList().Count != 0;
                        var isDeclinedAndImportedFromJira = selectionTSHoursRecordsDateAndUser.Where(x => x.ExternalSourceElementID == worklog.Id.ToString() && x.RecordStatus == TSRecordStatus.Declined).ToList().Count != 0;
                        var changedAndImportedFromJira = selectionTSHoursRecordsDateAndUser.Where(x => x.ExternalSourceElementID == worklog.Id.ToString()).ToList().FirstOrDefault();

                        bool isChangedAndImportedFromJira = changedAndImportedFromJira != null && (changedAndImportedFromJira.RecordDate != recordDate || changedAndImportedFromJira.Project.ShortName != projectShortName 
                                                                                                                                                       || changedAndImportedFromJira.Hours != hours
                                                                                                                                                       || 
                                                                string.IsNullOrEmpty(worklog.Comment) ? !string.IsNullOrEmpty(changedAndImportedFromJira.Description.Substring(changedAndImportedFromJira.Description.IndexOf(" -") + 2).Trim()) :
                                                                !changedAndImportedFromJira.Description.Substring(changedAndImportedFromJira.Description.IndexOf(" -") + 2).Contains(description));
                        bool isProjectNotFound = _projectService.GetByShortName(projectShortName) == null;

                        tsHoursRecordImportJiraViewModels.Add(new TSHoursRecordImportJiraViewModel()
                        {
                            TSHoursRecord = new TSHoursRecord()
                            {
                                Project = new Project() { ShortName = projectShortName }, 
                                RecordDate = recordDate,
                                Hours = hours,
                                Description = description.TruncateAtWord(_jiraConfig.TSHoursRecordDescriptionTruncateCharacters != null ? 
                                    Convert.ToInt16(_jiraConfig.TSHoursRecordDescriptionTruncateCharacters) : 300),
                                RecordStatus = TSRecordStatus.Editing,
                                RecordSource = TSRecordSource.JIRA,
                                ExternalSourceElementID = worklog.Id.ToString()
                            },
                            JiraIssueName = issue.Key,
                            Imported = isImportedFromJira || isDeclinedAndImportedFromJira,
                            ImportedDeclinedRecord = isDeclinedAndImportedFromJira,
                            ChangedRecord = isChangedAndImportedFromJira,
                            IsProjectNotFound = isProjectNotFound,
                            JiraProjectKey = issue.Fields.JiraProject.ProjectKey,
                            FullDescription = worklog.Comment.Length > 1500 ? RPCSHelper.NormalizeAndTrimString(worklog.Comment.Substring(0, 1500).RemoveUnwantedHtmlTags()) : RPCSHelper.NormalizeAndTrimString(worklog.Comment.RemoveUnwantedHtmlTags()),
                            ErrorType = isErrorJira ? errorTypeJira : isProjectNotFound ? ErrorTypesJira.ProjectCodeNotFound : (ErrorTypesJira?)null
                        });
                    }
                }
            }

            if (IsDeclinedView)
                tsHoursRecordImportJiraViewModels = tsHoursRecordImportJiraViewModels.Where(x => x.ImportedDeclinedRecord).ToList();
            
            ViewBag.HoursStartDate = dateTimeHoursStartDate.ToShortDateString();
            ViewBag.HoursEndDate = dateTimeHoursEndDate.ToShortDateString();
            return PartialView(tsHoursRecordImportJiraViewModels.OrderBy(date => date.TSHoursRecord.RecordDate).ThenBy(projectJira => projectJira.JiraIssueName).ToList());
        }

        [HttpGet]
        [ATSHoursRecordCreateUpdateMyHours]
        public virtual ActionResult MyHours(int? week, string weekStartDate, string dateStart, string dateEnd, int? projectID,
                                    TSRecordStatus? tsRecordStatus,
                                    string applyFilter, string isDeclined, string view)
        {

            ViewBag.isDeclined = isDeclined;
            ViewBag.SubDomain = ControllerContext.RouteData.DataTokens["area"];

            if (view == "alldeclinedhours" || view == "DeclinedHours")
            {
                ViewBag.ActionType = "DeclinedHours";
                //TODO 0- для того, чтобы взяло текущего пользователя
                SetProjectListEmployee(0);
                ViewBag.ProjectID = projectID;

                SwitchingDate(week, weekStartDate, dateStart, dateEnd, true);
                ViewBag.Week = week == null ? 0 : (int)week;

                ViewBag.DateSendApproval = _productionCalendarService.GetWorkDayOfNextWeek(DateTime.Today).ToShortDateString();
                ViewBag.TimeOpenPageMyHours = null;
            }
            else if (view == "MyHours" || view == null)
            {
                ViewBag.ActionType = "MyHours";

                var recordStatusList = new List<TSRecordStatus>
                {
                    TSRecordStatus.All, TSRecordStatus.Editing, TSRecordStatus.Approving, TSRecordStatus.PMApproved,
                    TSRecordStatus.HDApproved, TSRecordStatus.Declined, TSRecordStatus.DeclinedEditing,
                    TSRecordStatus.Archived
                };

                ViewBag.ArrayStatus = GetMyHoursRecordStatus(recordStatusList, null);

                //Установка отклоненных часов  
                double myDeclinedHours = MyDeclinedHours();

                if (!double.IsNaN(myDeclinedHours))
                    ViewBag.MyDeclinedHours = myDeclinedHours;
                else
                    ViewBag.MyDeclinedHours = null;

                ViewBag.IsDeclinedView = false;
                //Отобразить все отклоненные(если пользователь нажал на url)
                if (!string.IsNullOrEmpty(isDeclined))
                {
                    ViewBag.IsDeclinedView = true;
                    ViewBag.CurrentRecordStatus = TSRecordStatus.Declined;
                    ViewBag.ArrayStatus = GetMyHoursRecordStatus(recordStatusList, TSRecordStatus.Declined);

                    //Убрать даты
                    ViewBag.HoursStartDate = "";
                    ViewBag.HoursEndDate = "";
                }
                else if (tsRecordStatus != null)
                {
                    ViewBag.CurrentRecordStatus = tsRecordStatus;
                }
                else
                {
                    ViewBag.CurrentRecordStatus = (int)TSRecordStatus.All;
                }

                //список проектов, 0 указано, чтобы брало текущего пользователя
                SetProjectListEmployee(0);
                //Выбранный проект из формы
                ViewBag.ProjectID = projectID;

                //Указание дат
                if (string.IsNullOrEmpty(isDeclined))
                {
                    SwitchingDate(week, weekStartDate, dateStart, dateEnd);
                    ViewBag.Week = week == null ? 0 : (int)week;

                }
                else
                {
                    /*SwitchingDate(week, weekStartDate, dateStart, dateEnd, true);
                    ViewBag.Week = week == null ? 0 : (int)week;*/
                }

                ViewBag.DateSendApproval = _productionCalendarService.GetWorkDayOfNextWeek(DateTime.Today).ToShortDateString();
                //todo нужно, для того, чтобы вводить несколько трудозатрат без перезагрузки страницы
                ViewBag.TimeOpenPageMyHours = DateTime.Now.Trim(TimeSpan.TicksPerSecond);
            }
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [ATSHoursRecordCreateUpdateMyHours]
        public virtual ActionResult MyHoursSendForApprove(int? week, string weekStartDate, string dateStart, string dateEnd, int? projectID,
                                    TSRecordStatus? tsRecordStatus,
                                    string applyFilter, string sendForApprove, string rowsForApprove, string isDeclined, string view)
        {
            if (view == "MyHours" || view == null)
            {
                if (sendForApprove != null && !string.IsNullOrEmpty(rowsForApprove))
                {
                    SendForApprove(ViewBag.HoursStartDate, ViewBag.HoursEndDate, rowsForApprove);
                }
            }

            return RedirectToAction("MyHours", new
            {
                week = week,
                weekStartDate = weekStartDate,
                dateStart = dateStart,
                dateEnd = dateEnd,
                projectID = projectID,
                tsRecordStatus = tsRecordStatus,
                applyFilter = applyFilter,
                isDeclined = isDeclined,
                view = view
            });
        }

        [HttpGet]
        [ATSHoursRecordCreateUpdateMyHours]
        public FileContentResult ExportTSHoursToExcel(int? week, string weekStartDate, string searchString, int? employeeId, string dateStart, string dateEnd, int? projectId, TSRecordStatus? tsRecordStatus)
        {
            //DateTime dateTimeStart = new DateTime();
            //DateTime dateTimeEnd = new DateTime();
            //DateTime.TryParse(dateStart, out dateTimeStart);
            //DateTime.TryParse(dateEnd, out dateTimeEnd);

            var tsHoursRecordStatus = TSRecordStatus.All;
            if (tsRecordStatus != null)
                tsHoursRecordStatus = (TSRecordStatus)tsRecordStatus;

            var dateTuple = SwitchingDate(week, weekStartDate, dateStart, dateEnd);
            var tsHoursRecords = new List<TSHoursRecord>();

            Employee employee = null;
            if (string.IsNullOrEmpty(searchString))
            {
                if (employeeId.HasValue)
                {
                    employee = _employeeService.GetById((int)employeeId);
                    if (projectId.HasValue)
                        tsHoursRecords = _tsHoursRecordService.GetEmployeeTSHoursRecords(employeeId.Value, dateTuple.Item1, dateTuple.Item2, projectId.Value, tsHoursRecordStatus, null).ToList();
                    else
                        tsHoursRecords = _tsHoursRecordService.GetEmployeeTSHoursRecords(employeeId.Value, dateTuple.Item1, dateTuple.Item2, null, tsHoursRecordStatus, null).ToList();
                    tsHoursRecords = tsHoursRecords.OrderBy(x => x.RecordDate).ThenBy(x => x.Project.ShortName).ToList();
                }
            }
            else
            {
                if (employeeId.HasValue)
                {
                    SetProjectListEmployee(employeeId.Value);
                    if (projectId.HasValue)
                    {
                        if (dateTuple.Item1 == new DateTime() && dateTuple.Item2 == new DateTime() && tsRecordStatus == tsHoursRecordStatus)
                            tsHoursRecords = _tsHoursRecordService.FindHoursRecords(searchString).ToList().Where(x => x.EmployeeID == employeeId && x.ProjectID == projectId.Value).OrderBy(x => x.RecordDate).ThenBy(x => x.Project.ShortName).ToList();
                        if (dateTuple.Item1 != new DateTime() && dateTuple.Item2 == new DateTime() && tsRecordStatus == tsHoursRecordStatus)
                            tsHoursRecords = _tsHoursRecordService.FindHoursRecords(searchString).ToList().Where(x => x.EmployeeID == employeeId && x.ProjectID == projectId.Value && x.RecordDate >= dateTuple.Item1).OrderBy(x => x.RecordDate).ThenBy(x => x.Project.ShortName).ToList();
                        if (dateTuple.Item1 == new DateTime() && dateTuple.Item2 != new DateTime() && tsRecordStatus == tsHoursRecordStatus)
                            tsHoursRecords = _tsHoursRecordService.FindHoursRecords(searchString).ToList().Where(x => x.EmployeeID == employeeId && x.ProjectID == projectId.Value && x.RecordDate <= dateTuple.Item2).OrderBy(x => x.RecordDate).ThenBy(x => x.Project.ShortName).ToList();
                        if (dateTuple.Item1 != new DateTime() && dateTuple.Item2 != new DateTime() && tsRecordStatus == tsHoursRecordStatus)
                            tsHoursRecords = _tsHoursRecordService.FindHoursRecords(searchString).ToList().Where(x => x.EmployeeID == employeeId && x.ProjectID == projectId.Value && x.RecordDate >= dateTuple.Item1 && x.RecordDate <= dateTuple.Item2).OrderBy(x => x.RecordDate).ThenBy(x => x.Project.ShortName).ToList();
                    }
                    else
                    {
                        if (dateTuple.Item1 == new DateTime() && dateTuple.Item2 == new DateTime() && tsRecordStatus == tsHoursRecordStatus)
                            tsHoursRecords = _tsHoursRecordService.FindHoursRecords(searchString).ToList().Where(x => x.EmployeeID == employeeId).OrderBy(x => x.RecordDate).ThenBy(x => x.Project.ShortName).ToList();
                        if (dateTuple.Item1 != new DateTime() && dateTuple.Item2 == new DateTime() && tsRecordStatus == tsHoursRecordStatus)
                            tsHoursRecords = _tsHoursRecordService.FindHoursRecords(searchString).ToList().Where(x => x.EmployeeID == employeeId && x.RecordDate >= dateTuple.Item1).OrderBy(x => x.RecordDate).ThenBy(x => x.Project.ShortName).ToList();
                        if (dateTuple.Item1 == new DateTime() && dateTuple.Item2 != new DateTime() && tsRecordStatus == tsHoursRecordStatus)
                            tsHoursRecords = _tsHoursRecordService.FindHoursRecords(searchString).ToList().Where(x => x.EmployeeID == employeeId && x.RecordDate <= dateTuple.Item2).OrderBy(x => x.RecordDate).ThenBy(x => x.Project.ShortName).ToList();
                        if (dateTuple.Item1 != new DateTime() && dateTuple.Item2 != new DateTime() && tsRecordStatus == tsHoursRecordStatus)
                            tsHoursRecords = _tsHoursRecordService.FindHoursRecords(searchString).ToList().Where(x => x.EmployeeID == employeeId && x.RecordDate >= dateTuple.Item1 && x.RecordDate <= dateTuple.Item2).OrderBy(x => x.RecordDate).ThenBy(x => x.Project.ShortName).ToList();
                    }
                }
                else
                {
                    ViewBag.ProjectsFromDB = _projectService.Get(x => x.ToList());
                    if (projectId.HasValue)
                    {
                        if (dateTuple.Item1 == new DateTime() && dateTuple.Item2 == new DateTime() && tsRecordStatus == tsHoursRecordStatus)
                            tsHoursRecords = _tsHoursRecordService.FindHoursRecords(searchString).ToList().Where(x => x.ProjectID == projectId.Value).OrderBy(x => x.RecordDate).ThenBy(x => x.Project.ShortName).ToList();
                        if (dateTuple.Item1 != new DateTime() && dateTuple.Item2 == new DateTime() && tsRecordStatus == tsHoursRecordStatus)
                            tsHoursRecords = _tsHoursRecordService.FindHoursRecords(searchString).ToList().Where(x => x.ProjectID == projectId.Value && x.RecordDate >= dateTuple.Item1).OrderBy(x => x.RecordDate).ThenBy(x => x.Project.ShortName).ToList();
                        if (dateTuple.Item1 == new DateTime() && dateTuple.Item2 != new DateTime() && tsRecordStatus == tsHoursRecordStatus)
                            tsHoursRecords = _tsHoursRecordService.FindHoursRecords(searchString).ToList().Where(x => x.ProjectID == projectId.Value && x.RecordDate <= dateTuple.Item2).OrderBy(x => x.RecordDate).ThenBy(x => x.Project.ShortName).ToList();
                        if (dateTuple.Item1 != new DateTime() && dateTuple.Item2 != new DateTime() && tsRecordStatus == tsHoursRecordStatus)
                            tsHoursRecords = _tsHoursRecordService.FindHoursRecords(searchString).ToList().Where(x => x.ProjectID == projectId.Value && x.RecordDate >= dateTuple.Item1 && x.RecordDate <= dateTuple.Item2).OrderBy(x => x.RecordDate).ThenBy(x => x.Project.ShortName).ToList();
                    }
                    else
                    {
                        if (dateTuple.Item1 == new DateTime() && dateTuple.Item2 == new DateTime() && tsRecordStatus == tsHoursRecordStatus)
                            tsHoursRecords = _tsHoursRecordService.FindHoursRecords(searchString).ToList().OrderBy(x => x.RecordDate).ThenBy(x => x.Project.ShortName).ToList();
                        if (dateTuple.Item1 != new DateTime() && dateTuple.Item2 == new DateTime() && tsRecordStatus == tsHoursRecordStatus)
                            tsHoursRecords = _tsHoursRecordService.FindHoursRecords(searchString).ToList().Where(x => x.RecordDate >= dateTuple.Item1).OrderBy(x => x.RecordDate).ThenBy(x => x.Project.ShortName).ToList();
                        if (dateTuple.Item1 == new DateTime() && dateTuple.Item2 != new DateTime() && tsRecordStatus == tsHoursRecordStatus)
                            tsHoursRecords = _tsHoursRecordService.FindHoursRecords(searchString).ToList().Where(x => x.RecordDate <= dateTuple.Item2).OrderBy(x => x.RecordDate).ThenBy(x => x.Project.ShortName).ToList();
                        if (dateTuple.Item1 != new DateTime() && dateTuple.Item2 != new DateTime() && tsRecordStatus == tsHoursRecordStatus)
                            tsHoursRecords = _tsHoursRecordService.FindHoursRecords(searchString).ToList().Where(x => x.RecordDate >= dateTuple.Item1 && x.RecordDate <= dateTuple.Item2).OrderBy(x => x.RecordDate).ThenBy(x => x.Project.ShortName).ToList();
                    }
                }
            }

            byte[] binData = null;

            DataTable dataTable = new DataTable();

            dataTable.Columns.Add("RecordDate", typeof(DateTime)).Caption = "Дата";
            dataTable.Columns["RecordDate"].ExtendedProperties["Width"] = (double)25;
            dataTable.Columns.Add("Project", typeof(string)).Caption = "Проект";
            dataTable.Columns["Project"].ExtendedProperties["Width"] = (double)40;
            dataTable.Columns.Add("Hours", typeof(double)).Caption = "Трудозатраты (ч)";
            dataTable.Columns["Hours"].ExtendedProperties["Width"] = (double)15;
            dataTable.Columns.Add("Description", typeof(string)).Caption = "Состав работ";
            dataTable.Columns["Description"].ExtendedProperties["Width"] = (double)45;
            dataTable.Columns.Add("Created", typeof(DateTime)).Caption = "Создано";
            dataTable.Columns["Created"].ExtendedProperties["Width"] = (double)20;
            dataTable.Columns.Add("RecordStatus", typeof(string)).Caption = "Статус";
            dataTable.Columns["RecordStatus"].ExtendedProperties["Width"] = (double)20;
            dataTable.Columns.Add("RecordSource", typeof(string)).Caption = "Источник";
            dataTable.Columns["RecordSource"].ExtendedProperties["Width"] = (double)20;

            foreach (var myHoursRecord in tsHoursRecords)
                dataTable.Rows.Add(myHoursRecord.RecordDate.Value.ToShortDateString(), myHoursRecord.Project.ShortName, myHoursRecord.Hours,
                    RPCSHelper.NormalizeAndTrimString(myHoursRecord.Description), myHoursRecord.Created, myHoursRecord.RecordStatus.GetAttributeOfType<DisplayAttribute>().Name, myHoursRecord.RecordSource.GetAttributeOfType<DisplayAttribute>().Name);

            using (MemoryStream stream = new MemoryStream())
            {
                using (SpreadsheetDocument doc = SpreadsheetDocument.Create(stream, SpreadsheetDocumentType.Workbook))
                {
                    WorkbookPart workbookPart = ExcelHelper.CreateWorkbookPart(doc, "Трудозатраты сотрудника");

                    WorksheetPart rId1 = ExcelHelper.CreateWorksheetPartAndImportDataTable(workbookPart, "rId1", 1, 1, (uint)dataTable.Columns.Count,
                        "Трудозатраты: " + employee?.FullName + " На даты: " + dateTuple.Item1.ToShortDateString() + " - " + dateTuple.Item2.ToShortDateString(),
                        dataTable, 3, 1);

                    doc.WorkbookPart.Workbook.Save();
                }

                stream.Position = 0;
                BinaryReader b = new BinaryReader(stream);
                binData = b.ReadBytes((int)stream.Length);
            }
            return File(binData, ExcelHelper.ExcelContentType, "TSHoursRecords" + DateTime.Now.ToString("ddMMyyHHmmss") + ".xlsx");
        }

        [HttpGet]
        [ATSHoursRecordCreateUpdateMyHours]
        public FileContentResult ExportMyHoursDataToExcel(string hoursStartDate, string hoursEndDate, string projectId, TSRecordStatus? tsRecordStatus, bool isDeclinedView = false)
        {
            //Получение пользователя
            int userEmployeeID = _userService.GetEmployeeForCurrentUser().ID;
            int intProjectId;
            DateTime dateTimeHoursStartDate = new DateTime();
            DateTime dateTimeHoursEndDate = new DateTime();
            DateTime.TryParse(hoursStartDate, out dateTimeHoursStartDate);
            DateTime.TryParse(hoursEndDate, out dateTimeHoursEndDate);
            int.TryParse(projectId, out intProjectId);


            var tsHoursRecordStatus = TSRecordStatus.All;
            if (tsRecordStatus != null)
                tsHoursRecordStatus = (TSRecordStatus)tsRecordStatus;

            List<TSHoursRecord> recordList = new List<TSHoursRecord>();

            if (isDeclinedView == false)
            {
                recordList = _tsHoursRecordService.GetEmployeeTSHoursRecords(userEmployeeID,
                dateTimeHoursStartDate, dateTimeHoursEndDate, intProjectId, tsHoursRecordStatus, null).ToList();
            }
            else
            {
                recordList = _tsHoursRecordService.GetEmployeeTSHoursRecords(userEmployeeID, dateTimeHoursStartDate,
                    dateTimeHoursEndDate, intProjectId, TSRecordStatus.Declined, null).ToList();

                recordList.AddRange(_tsHoursRecordService.GetEmployeeTSHoursRecords(userEmployeeID, dateTimeHoursStartDate,
                    dateTimeHoursEndDate, intProjectId, TSRecordStatus.DeclinedEditing, null));
            }

            recordList = recordList.OrderBy(x => x.RecordDate).ThenBy(x => x.Project.ShortName).ToList();

            var userFullName = _userService.GetEmployeeForCurrentUser().FullName;

            byte[] binData = null;

            DataTable dataTable = new DataTable();

            dataTable.Columns.Add("RecordDate", typeof(DateTime)).Caption = "Дата";
            dataTable.Columns["RecordDate"].ExtendedProperties["Width"] = (double)12;
            dataTable.Columns.Add("Project", typeof(string)).Caption = "Проект";
            dataTable.Columns["Project"].ExtendedProperties["Width"] = (double)40;
            dataTable.Columns.Add("Hours", typeof(double)).Caption = "Трудозатраты (ч)";
            dataTable.Columns["Hours"].ExtendedProperties["Width"] = (double)15;
            dataTable.Columns.Add("Description", typeof(string)).Caption = "Состав работ";
            dataTable.Columns["Description"].ExtendedProperties["Width"] = (double)90;
            dataTable.Columns.Add("RecordStatus", typeof(string)).Caption = "Статус";
            dataTable.Columns["RecordStatus"].ExtendedProperties["Width"] = (double)20;
            dataTable.Columns.Add("PMComment", typeof(string)).Caption = "Комментарий";
            dataTable.Columns["PMComment"].ExtendedProperties["Width"] = (double)35;

            foreach (var myHoursRecord in recordList)
            {
                dataTable.Rows.Add(myHoursRecord.RecordDate.Value.ToShortDateString(), myHoursRecord.Project.ShortName, myHoursRecord.Hours,
                    myHoursRecord.Description, myHoursRecord.RecordStatus.GetAttributeOfType<DisplayAttribute>().Name, myHoursRecord.PMComment);
            }

            using (MemoryStream stream = new MemoryStream())
            {
                using (SpreadsheetDocument doc = SpreadsheetDocument.Create(stream, SpreadsheetDocumentType.Workbook))
                {
                    WorkbookPart workbookPart = ExcelHelper.CreateWorkbookPart(doc, "Трудозатраты сотрудника");

                    WorksheetPart rId1 = ExcelHelper.CreateWorksheetPartAndImportDataTable(workbookPart, "rId1", 1, 1, (uint)dataTable.Columns.Count,
                        "Трудозатраты сотрудника: " + userFullName + " На даты: " + hoursStartDate + " - " + hoursEndDate,
                        dataTable, 3, 1);

                    doc.WorkbookPart.Workbook.Save();
                }

                stream.Position = 0;
                BinaryReader b = new BinaryReader(stream);
                binData = b.ReadBytes((int)stream.Length);
            }
            return File(binData, ExcelHelper.ExcelContentType, "MyHours" + DateTime.Now.ToString("ddMMyyHHmmss") + ".xlsx");
        }

        [HttpGet]
        [ATSHoursRecordCreateUpdateMyHours]
        public string GetMyHoursData(string hoursStartDate, string hoursEndDate, string projectId,
            TSRecordStatus? tSRecordStatus, DateTime timeOpenPageMyHours, bool isDeclinedView = false)
        {
            //Получение пользователя
            int userEmployeeID = _userService.GetEmployeeForCurrentUser().ID;

            int intProjectId;
            DateTime dateTimeHoursStartDate = new DateTime();
            DateTime dateTimeHoursEndDate = new DateTime();
            DateTime.TryParse(hoursStartDate, out dateTimeHoursStartDate);
            DateTime.TryParse(hoursEndDate, out dateTimeHoursEndDate);
            int.TryParse(projectId, out intProjectId);

            var tsHoursRecordStatus = TSRecordStatus.All;
            if (tSRecordStatus != null)
                tsHoursRecordStatus = (TSRecordStatus)tSRecordStatus;

            List<TSHoursRecord> recordList = new List<TSHoursRecord>();

            if (isDeclinedView == false)
            {
                recordList = _tsHoursRecordService.GetEmployeeTSHoursRecords(userEmployeeID, dateTimeHoursStartDate,
                    dateTimeHoursEndDate, intProjectId, tsHoursRecordStatus, timeOpenPageMyHours).ToList();
            }
            else
            {
                recordList = _tsHoursRecordService.GetEmployeeTSHoursRecords(userEmployeeID, dateTimeHoursStartDate,
                    dateTimeHoursEndDate, intProjectId, TSRecordStatus.Declined, timeOpenPageMyHours).ToList();

                recordList.AddRange(_tsHoursRecordService.GetEmployeeTSHoursRecords(userEmployeeID, dateTimeHoursStartDate,
                    dateTimeHoursEndDate, intProjectId, TSRecordStatus.DeclinedEditing, timeOpenPageMyHours));
                recordList = recordList.Distinct().ToList();
            }

            //foreach (var record in recordList)
            //{
            //    if (record.Project.ProjectType?.TSApproveMode == ProjectTypeTSApproveMode.Default || record.Project.ProjectType?.TSApproveMode == ProjectTypeTSApproveMode.PM || record.Project.ProjectType == null)
            //        record.Project.ApproveHoursEmployee = record.Project.EmployeePM;
            //    else
            //        record.Project.ApproveHoursEmployee = record.Project.EmployeeCAM;
            //}

            var config = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile(new AutoMapperConfig.GetMyHoursDataProfile());
            }).CreateMapper();
            var employeeRecords = config.Map<IList<TSHoursRecord>, IList<TSHoursRecordDTO>>(recordList);
            foreach (var record in employeeRecords)
            {
                if (record.RecordSource == TSRecordSource.JIRA)
                {
                    var jiraProjectName = string.Empty;
                    string pattern = "((?:[a-z][a-z]+))(-)(\\d+)";
                    Regex r = new Regex(pattern, RegexOptions.IgnoreCase | RegexOptions.Singleline);
                    Match match = r.Match(record.Description);
                    if (match.Success)
                        jiraProjectName = match.Value;
                    record.Hyperlink = "<a href=\"" + _jiraConfig.Issue + jiraProjectName +
                                       "?focusedWorklogId="
                                       + record.ExternalSourceElementID +
                                       "&page=com.atlassian.jira.plugin.system.issuetabpanels%3Aworklog-tabpanel#worklog-"
                                       + record.ExternalSourceElementID + "\" target=\"_blank\" >" +
                                       jiraProjectName + "</a>";
                }
            }

            return JsonConvert.SerializeObject(employeeRecords.OrderBy(x => x.RecordDate));
        }

        [HttpPost]
        public ActionResult CheckErrorsWithReportingPeriod(string id)
        {
            var listIdRows = id.Split(',').Select(Int32.Parse).ToList();

            var errorMessage = string.Empty;
            foreach (var idItem in listIdRows)
            {
                try
                {
                    var tsHoursRecord = _tsHoursRecordService.GetById(idItem);

                    var reportingPeriod = _reportingPeriodService.GetAll(x => x.Month == tsHoursRecord.RecordDate.Value.Month
                                                                               && x.Year == tsHoursRecord.RecordDate.Value.Year).FirstOrDefault();
                    if (reportingPeriod == null)
                        errorMessage += "Администратор ТШ не завел отчетный период: " + tsHoursRecord.RecordDate.Value.ToString("yyyy.MM") + " .\n";
                    else if (reportingPeriod.TSRecordsEditApproveAllowedUntilDate < DateTime.Now)
                        errorMessage += "Отчетный период " + reportingPeriod.FullName + " закрыт.\n";
                }
                catch (NullReferenceException ex)
                {
                    errorMessage = "Невозможно выполнить операцию. Запись удалена, возвращена на редактирование или согласована Администратором ТШ. Перезагрузите страницу.";
                }

            }
            return Content(errorMessage);
        }

        [HttpPost]
        [OperationActionFilter(nameof(Operation.TSHoursRecordDeleteMyHours))]
        public ActionResult DeleteMyHours(string id, string ids)
        {
            //Todo Jqgrid пихает все выделенные в id. Эта логика сделана, для того, чтобы можно было свои id пропихнуть!
            var listIdRows = !string.IsNullOrEmpty(ids) ? ids.Split(',').Select(Int32.Parse).ToList() : id.Split(',').Select(Int32.Parse).ToList();
            var errorMessage = string.Empty;
            var user = _userService.GetUserDataForVersion();
            foreach (var idItem in listIdRows)
            {
                var tsHoursRecord = _tsHoursRecordService.GetById(idItem);
                if (tsHoursRecord.RecordStatus == TSRecordStatus.HDApproved)
                    errorMessage = "Невозможно удалить, в выбранных записях присутствует согласованная запись.";
                if (tsHoursRecord.RecordStatus == TSRecordStatus.PMApproved)
                    errorMessage = "Невозможно удалить, в выбранных записях присутствует согласованная запись.";
            }

            if (string.IsNullOrEmpty(errorMessage))
            {
                foreach (var idItem in listIdRows)
                {
                    var tsHoursRecord = _tsHoursRecordService.GetById(idItem);

                    switch (tsHoursRecord.RecordStatus)
                    {
                        case TSRecordStatus.Editing:
                            _tsHoursRecordService.Delete(idItem);
                            break;
                        case TSRecordStatus.Declined:
                        case TSRecordStatus.DeclinedEditing:
                        case TSRecordStatus.Approving:
                            _tsHoursRecordService.RecycleToRecycleBin(idItem, user.Item1, user.Item2);
                            break;
                    }
                    /*
                     //существет ли отчетный период на установленную дату(месяц)
                     var reportingPeriod = _reportingPeriodService.GetAll(x => x.Month == tsHoursRecord.RecordDate.Value.Month
                                                                                && x.Year == tsHoursRecord.RecordDate.Value.Year).FirstOrDefault();
                     if (reportingPeriod == null)
                         errorMessage += "Администратор ТШ не завел отчетный период: " + tsHoursRecord.RecordDate.Value.ToString("yyyy.MM") + " .\n";
                     else if (reportingPeriod.TSRecordsEditApproveAllowedUntilDate < DateTime.Now)
                         errorMessage += "Отчетный период " + reportingPeriod.FullName + " закрыт.\n";
                     else
                         _tsHoursRecordService.Delete(idItem);*/
                }
            }

            return Content(errorMessage);
        }

        [HttpGet]
        [ATSHoursRecordCreateUpdateMyHours]
        public string GetMyProjects(string hoursStartDate, string hoursEndDate)
        {
            DateTime hoursStartDateTime = new DateTime();
            DateTime hoursEndDateTime = new DateTime();
            DateTime.TryParse(hoursStartDate, out hoursStartDateTime);
            DateTime.TryParse(hoursEndDate, out hoursEndDateTime);

            DateTimeRange dateTimeRange = new DateTimeRange(hoursStartDateTime, hoursEndDateTime);

            int currentUserEmployeeID = _userService.GetEmployeeForCurrentUser().ID;

            var records = new List<object>();
            var employeeProjectIds = new List<int>();

            //все проекты на которых работает сотрудник
            if (string.IsNullOrEmpty(hoursStartDate) && string.IsNullOrEmpty(hoursEndDate))
                employeeProjectIds = _projectMembershipService.GetProjectsForEmployeeInTSMyHours(currentUserEmployeeID, new DateTimeRange(DateTime.MinValue, DateTime.MaxValue)).Select(x => x.ID).ToList();
            else
                employeeProjectIds = _projectMembershipService.GetProjectsForEmployeeInTSMyHours(currentUserEmployeeID, dateTimeRange).Select(x => x.ID).ToList();

            var recordPeriodStart = DateTime.Today.AddDays(-50);
            var recordPeriodEnd = DateTime.Today.AddDays(7);
            //выбранные трудозатраты за месяц по определенному проекту
            var lastTSHoursRecordList = _tsHoursRecordService.Get(record => record.Where(o => employeeProjectIds.Contains(o.ProjectID.Value) &&
                                                                                              o.RecordDate >= recordPeriodStart && o.RecordDate <= recordPeriodEnd && o.EmployeeID == currentUserEmployeeID).ToList());
            //Выбор проекта по популярности
            var distinctLastProjects = lastTSHoursRecordList.GroupBy(l => l.Project.ShortName)
                                             .Select(g => new
                                             {
                                                 ProjectId = g.Select(x => x.ProjectID).First(),
                                                 ProjectName = g.Key,
                                                 MaxTime = g.Select(l => l.RecordDate).Max()
                                             }).OrderByDescending(x => x.MaxTime).Take(15);

            var employeeProjects = new List<Project>();

            var distinctLastProjectIds = distinctLastProjects.Select(x => x.ProjectId);
            var distinctProjects = _projectService.Get(pList => pList.Where(p => distinctLastProjectIds.Contains(p.ID)).ToList());
            //отсортированный список. в бд записи найдется с id 5, 10, 15 - а надо 15, 5, 10
            var sortDistinctProject = new List<Project>();
            foreach (var distinctProjectId in distinctLastProjectIds)
            {
                sortDistinctProject.Add(distinctProjects.FirstOrDefault(x => x.ID == distinctProjectId));
            }

            employeeProjects.AddRange(sortDistinctProject);

            var otherProjectIds = employeeProjectIds.Where(epId => distinctLastProjectIds.All(dlpId => dlpId != epId));

            if (distinctLastProjectIds.Count() != 0
                && otherProjectIds.Count() != 0)
            {
                employeeProjects.Add(new Project()
                {
                    ID = 0,
                    ShortName = "---"
                });
            }

            if (otherProjectIds.Count() != 0)
            {
                employeeProjects.AddRange(_projectService.Get(pList => pList.Where(p => otherProjectIds.Contains(p.ID)).ToList()).OrderBy(p => p.ShortName));
            }


            foreach (var employeeProject in employeeProjects)
            {
                records.Add(new { Value = employeeProject.ID.ToString(), Text = employeeProject.ShortName });
            }

            return JsonConvert.SerializeObject(records);
        }


        [HttpPost]
        [ATSHoursRecordCreateUpdateMyHours]
        //Стоит ProjectShortName -для того, чтобы работала сортировка в jqgrid, на самом деле в него приходит ID проекта
        public ActionResult MyHoursDataSave(int? ID, int? projectShortName, string Hours, string RecordDate, string Description,
            TSRecordSource RecordSource = TSRecordSource.UserInput, TSRecordStatus RecordStatus = TSRecordStatus.Editing,
            string ExternalSourceElementID = "", bool displayJiraModalView = false, bool isEnterPressedInCopyRow = false)
        {
            var userDataVersion = _userService.GetUserDataForVersion();
            int userEmployeeID = _userService.GetEmployeeForCurrentUser().ID;

            Description = Description.RemoveUnwantedHtmlTags();
            Description = RPCSHelper.NormalizeAndTrimString(Description);


            //Todo нужно для того, чтобы при копировании в строке грида работал Enter
            if (isEnterPressedInCopyRow)
                ID = null;

            if (string.IsNullOrEmpty(Description))
                return Content("Состав работ: Поле является обязательным");


            DateTime recordDate = new DateTime();
            DateTimeRange recordTimeRange = new DateTimeRange();
            if (DateTime.TryParse(RecordDate, out recordDate))
            {
                recordTimeRange = new DateTimeRange(recordDate, recordDate);
            }
            var project = _projectService.GetById(projectShortName.Value);

            //Если человек работал на проекте до этого и у проекта нету галочка списание трудозатрат
            if (_projectMembershipService
                .GetProjectMembershipForEmployees(recordTimeRange, userEmployeeID).Count(x => projectShortName != null && x.ProjectID == (int)projectShortName) == 0
                && project.AllowTSRecordWithoutProjectMembership == false)
                return Content("Данный сотрудник не состоит в проекте. Либо данный сотрудник работал на нем ранее, но на данном проекте не доступно списание трудозатрат без участия РГ.");

            //проверка на начало дня и конец, если в бд у казано не правильно
            if (project.BeginDate.HasValue)
            {
                if (recordDate < project.BeginDate.Value.StartOfDay() || project.EndDate.HasValue && recordDate > project.EndDate.Value.StartOfDay())
                    return Content("Запись Таймшит не входит в период действия проекта");
            }
            else
                return Content("Проект '" + project.FullName + "' инициирован, не возможно списать трудозатраты");



            if (string.IsNullOrEmpty(Hours) && string.IsNullOrEmpty(RecordDate) && string.IsNullOrEmpty(Description))
            {
                //удаление записей
                if (ID != null)
                {
                    _tsHoursRecordService.Delete((int)ID);
                    return Content("true");
                }
            }
            else
            {
                double hours = Convert.ToDouble(Hours.Replace(".", ","));
                if (hours == 0)
                {
                    //return Content("false");
                    hours = 8;
                }

                //существет ли отчетный период на установленную дату(месяц)
                var reportingPeriod = _reportingPeriodService.GetAll(x => x.Month == recordDate.Month && x.Year == recordDate.Year).FirstOrDefault();
                if (reportingPeriod == null)
                    return Content("Администратор ТШ не завел отчетный период: " + recordDate.ToString("yyyy.MM") + " .");
                else if (reportingPeriod.NewTSRecordsAllowedUntilDate < DateTime.Now && !(ID != null && ID.HasValue == true))
                    return Content("В отчетном периоде " + reportingPeriod.FullName + " закрыта возможность добавления новых записей о трудозатратах.");
                else if (reportingPeriod.TSRecordsEditApproveAllowedUntilDate < DateTime.Now)
                    return Content("Отчетный период " + reportingPeriod.FullName + " закрыт.");

                var descriptionTruncateCharacters = _jiraConfig.TSHoursRecordDescriptionTruncateCharacters != null ? Convert.ToInt16(_jiraConfig.TSHoursRecordDescriptionTruncateCharacters) : 300;

                //обновление записи
                if (ID != null && ID.HasValue == true)
                {
                    if (project.AllowTSRecordOnlyWorkingDays && _productionCalendarService.GetRecordByDate(recordDate).WorkingHours == 0)
                        return Content("Списание трудозатрат на нерабочие дни для выбранного проекта запрещено.");
                    if (project.DisallowUserCreateTSRecord && displayJiraModalView == false)
                        return Content("Запрещен ручной ввод трудозатрат на данный проект");

                    TSHoursRecord tSHoursRecord = _tsHoursRecordService.GetById((int)ID);
                    var tmptsHoursRecordStatus = tSHoursRecord.RecordStatus;
                    if (tSHoursRecord != null)
                    {
                        //не обновлять запись если в ней ничего ничего не изменилось
                        if (tSHoursRecord.ProjectID == projectShortName && tSHoursRecord.Hours == hours &&
                            tSHoursRecord.Description == Description && tSHoursRecord.RecordDate == recordDate)
                        {
                            return Content("true");
                        }

                        if (tSHoursRecord.RecordStatus == TSRecordStatus.Declined || tSHoursRecord.RecordStatus == TSRecordStatus.DeclinedEditing)
                            tSHoursRecord.RecordStatus = TSRecordStatus.DeclinedEditing;
                        else
                            tSHoursRecord.RecordStatus = TSRecordStatus.Editing;

                        tSHoursRecord.EmployeeID = userEmployeeID;
                        tSHoursRecord.ProjectID = projectShortName;
                        tSHoursRecord.Hours = hours;
                        tSHoursRecord.RecordDate = Convert.ToDateTime(RecordDate);
                        tSHoursRecord.Description = Description.TruncateAtWord(descriptionTruncateCharacters);
                        _tsHoursRecordService.Update(tSHoursRecord, userDataVersion.Item1, userDataVersion.Item2);

                        if (tmptsHoursRecordStatus == TSRecordStatus.Declined || tmptsHoursRecordStatus == TSRecordStatus.DeclinedEditing)
                        {
                            return Content("declinedOfdeclinedEditing");
                        }
                        return Content("true");
                    }
                }
                //создание записи
                else
                {
                    if (project.AllowTSRecordOnlyWorkingDays && _productionCalendarService.GetRecordByDate(recordDate).WorkingHours == 0)
                        return Content("Списание трудозатрат на нерабочие дни для выбранного проекта запрещено.");
                    if (project.DisallowUserCreateTSRecord && displayJiraModalView == false)
                        return Content("Запрещен ручной ввод трудозатрат на данный проект");


                    var tSHoursRecord = new TSHoursRecord
                    {
                        EmployeeID = userEmployeeID,
                        ProjectID = projectShortName,
                        Hours = hours,
                        RecordSource = RecordSource, //TSRecordSource.UserInput 
                        RecordStatus = RecordStatus, //TSRecordStatus.Editing
                        RecordDate = Convert.ToDateTime(RecordDate),
                        Description = Description.TruncateAtWord(descriptionTruncateCharacters),
                        ExternalSourceElementID = ExternalSourceElementID
                    };
                    _tsHoursRecordService.Add(tSHoursRecord, userDataVersion.Item1, userDataVersion.Item2);
                    return Content("true");
                }
            }
            return null;
        }

        [HttpGet]
        public string GetHours()
        {
            var listDictionary = new List<object>();
            listDictionary.Add(new { Value = "0.25", Text = "0.25" });
            listDictionary.Add(new { Value = "0.5", Text = "0.5" });
            listDictionary.Add(new { Value = "0.75", Text = "0.75" });
            listDictionary.Add(new { Value = "1", Text = "1" });
            listDictionary.Add(new { Value = "1.5", Text = "1.5" });
            listDictionary.Add(new { Value = "2", Text = "2" });
            listDictionary.Add(new { Value = "2.5", Text = "2.5" });
            listDictionary.Add(new { Value = "3", Text = "3" });
            listDictionary.Add(new { Value = "3.5", Text = "3.5" });
            listDictionary.Add(new { Value = "4", Text = "4" });
            listDictionary.Add(new { Value = "4.5", Text = "4.5" });
            listDictionary.Add(new { Value = "5", Text = "5" });
            listDictionary.Add(new { Value = "5.5", Text = "5.5" });
            listDictionary.Add(new { Value = "6", Text = "6" });
            listDictionary.Add(new { Value = "6.5", Text = "6.5" });
            listDictionary.Add(new { Value = "7", Text = "7" });
            listDictionary.Add(new { Value = "7.5", Text = "7.5" });
            listDictionary.Add(new { Value = "8", Text = "8" });
            listDictionary.Add(new { Value = "11", Text = "11" });
            listDictionary.Add(new { Value = "12", Text = "12" });
            listDictionary.Add(new { Value = "15", Text = "15" });
            listDictionary.Add(new { Value = "16", Text = "16" });

            return JsonConvert.SerializeObject(listDictionary);

        }

        [NonAction]
        public void SendForApprove(string hoursStartDate, string hoursEndDate, string rowsForApprove)
        {
            var rowsForApproveList = rowsForApprove.Split(',').Select(x => Convert.ToInt32(x)).ToList();

            int userEmployeeID = _userService.GetEmployeeForCurrentUser().ID;

            DateTime dateTimeHoursStartDate = new DateTime();
            DateTime dateTimeHoursEndDate = new DateTime();
            DateTime.TryParse(hoursStartDate, out dateTimeHoursStartDate);
            DateTime.TryParse(hoursEndDate, out dateTimeHoursEndDate);

            if (rowsForApproveList.Count != 0)
            {


                //Указана начальная дата
                if (!string.IsNullOrEmpty(hoursStartDate) && string.IsNullOrEmpty(hoursEndDate))
                    _tsHoursRecordService.ChangeHoursRecordsStatus(
                        _tsHoursRecordService.Get(records => records.Where(x => x.EmployeeID == userEmployeeID
                                                                                 && (x.RecordStatus == TSRecordStatus.Editing
                                                                                     || x.RecordStatus == TSRecordStatus.Declined
                                                                                     || x.RecordStatus == TSRecordStatus.DeclinedEditing)).ToList()).AsQueryable(),
                        records => records.RecordDate >= dateTimeHoursStartDate,
                        records => records.OrderBy(x => x.RecordDate),
                        rowsForApproveList, TSRecordStatus.Approving);
                //Указана конечная дата
                else if (string.IsNullOrEmpty(hoursStartDate) && !string.IsNullOrEmpty(hoursEndDate))
                    _tsHoursRecordService.ChangeHoursRecordsStatus(
                        _tsHoursRecordService.Get(records => records.Where(x => x.EmployeeID == userEmployeeID
                        && (x.RecordStatus == TSRecordStatus.Editing
                        || x.RecordStatus == TSRecordStatus.Declined
                        || x.RecordStatus == TSRecordStatus.DeclinedEditing)).ToList()).AsQueryable(),
                        records =>
                            records.RecordDate <= dateTimeHoursEndDate,
                        records => records.OrderBy(x => x.RecordDate),
                        rowsForApproveList, TSRecordStatus.Approving);
                //Указана начальная и конечная дата
                else if (!string.IsNullOrEmpty(hoursStartDate) && !string.IsNullOrEmpty(hoursEndDate))
                {
                    _tsHoursRecordService.ChangeHoursRecordsStatus(
                        _tsHoursRecordService.Get(records => records.Where(x => x.EmployeeID == userEmployeeID
                                                                                 && (x.RecordStatus ==
                                                                                     TSRecordStatus.Editing
                                                                                     || x.RecordStatus ==
                                                                                     TSRecordStatus.Declined
                                                                                     || x.RecordStatus ==
                                                                                     TSRecordStatus.DeclinedEditing))
                            .ToList()).AsQueryable(),
                        records =>
                            records.RecordDate >= dateTimeHoursStartDate && records.RecordDate <= dateTimeHoursEndDate,
                        records => records.OrderBy(x => x.RecordDate),
                        rowsForApproveList, TSRecordStatus.Approving);

                    //получить записи, которые не входят в интервал hoursStartDate и hoursEndDate(когда можно добавлять записи не на текущую неделю)
                    var tsHoursRecordsIds = _tsHoursRecordService.Get(r => r.Where(x => x.EmployeeID == userEmployeeID && (x.RecordStatus == TSRecordStatus.Editing
                                                                                                                            || x.RecordStatus == TSRecordStatus.Declined
                                                                                                                            || x.RecordStatus == TSRecordStatus.DeclinedEditing))
                        .Where(item => rowsForApproveList.Contains(item.ID))
                        .Where(row => row.RecordDate <= dateTimeHoursStartDate || row.RecordDate >= dateTimeHoursEndDate).ToList()).Select(x => x.ID).ToList();

                    _tsHoursRecordService.ChangeHoursRecordsStatus(
                        _tsHoursRecordService.Get(records => records.Where(x => x.EmployeeID == userEmployeeID
                                                                                 && (x.RecordStatus == TSRecordStatus.Editing
                                                                                     || x.RecordStatus == TSRecordStatus.Declined
                                                                                     || x.RecordStatus == TSRecordStatus.DeclinedEditing)).ToList()).AsQueryable(),
                        null,
                        records => records.OrderBy(x => x.RecordDate),
                        tsHoursRecordsIds, TSRecordStatus.Approving);
                }
                else
                {
                    _tsHoursRecordService.ChangeHoursRecordsStatus(
                        _tsHoursRecordService.Get(records => records.Where(x => x.EmployeeID == userEmployeeID
                        && (x.RecordStatus == TSRecordStatus.Editing
                        || x.RecordStatus == TSRecordStatus.Declined
                        || x.RecordStatus == TSRecordStatus.DeclinedEditing)).ToList()).AsQueryable(),
                        records =>
                            records.RecordStatus != TSRecordStatus.All,
                        records => records.OrderBy(x => x.RecordDate),
                        rowsForApproveList, TSRecordStatus.Approving);
                }
            }
        }

        [NonAction]
        public double MyDeclinedHours()
        {
            double myDeclinedHours = 0;
            //Выбор сотрудника
            int currentUserEmployeeID = _userService.GetEmployeeForCurrentUser().ID;


            var myDeclinedHoursRecords = _tsHoursRecordService.Get(records => records.Where(x =>
                x.EmployeeID == currentUserEmployeeID && x.Hours != null && x.RecordStatus == TSRecordStatus.Declined).ToList());

            if (myDeclinedHoursRecords != null && myDeclinedHoursRecords.Count() != 0)
            {
                myDeclinedHours = (double)myDeclinedHoursRecords.Sum(x => x.Hours);
            }

            return myDeclinedHours;
        }

        #endregion

        #region Мои отклоненные трудозатраты

        [HttpGet]
        [ATSHoursRecordCreateUpdateMyHours]
        public string GetEmployeesPMWroteRejectedComments(string hoursStartDate, string hoursEndDate, string projectId)
        {
            int currentUserEmployeeID = _userService.GetEmployeeForCurrentUser().ID;

            DateTime dateTimeHoursStartDate = new DateTime();
            DateTime dateTimeHoursEndDate = new DateTime();

            int intProjectId;

            DateTime.TryParse(hoursStartDate, out dateTimeHoursStartDate);
            DateTime.TryParse(hoursEndDate, out dateTimeHoursEndDate);
            int.TryParse(projectId, out intProjectId);


            //id проектов на которых работает человек
            var projectsIdForWorkEmployee = _projectMembershipService.GetProjectsForEmployee(currentUserEmployeeID).Select(x => x.ID);

            bool isPMWritedComment;
            //получить всех пмов
            //писал ли пм коментарии по проектам на которых работает человек
            isPMWritedComment = _tsHoursRecordService.Get(records => records.Where(r => projectsIdForWorkEmployee.Any(x => x == r.ProjectID)).ToList())
                .Any(x => x.PMComment != null || x.PMComment != "");

            if (isPMWritedComment)
            {
                var pmList = new List<Employee>();
                var distinctPMComments = new List<TSHoursRecord>();

                //Если указана начальная дата
                if (!string.IsNullOrEmpty(hoursStartDate) && string.IsNullOrEmpty(hoursEndDate) && string.IsNullOrEmpty(projectId))
                {
                    distinctPMComments = _tsHoursRecordService.GetRecordsHaveManagerComments(currentUserEmployeeID,
                        projectsIdForWorkEmployee.ToList(),
                        cond =>
                            cond.RecordStatus == TSRecordStatus.Declined
                            && (cond.PMComment != null || cond.PMComment != "")
                            && cond.RecordDate >= dateTimeHoursStartDate,
                        gr => gr.GroupBy(x => x.PMComment)
                    ).ToList();
                    var projectsIdWhereHaveComment = distinctPMComments.Select(x => (int)x.ProjectID).ToList();
                    pmList = _projectMembershipService.GetManagersForProjects(projectsIdWhereHaveComment).ToList();
                }
                //Указана конечная дата
                else if (string.IsNullOrEmpty(hoursStartDate) && !string.IsNullOrEmpty(hoursEndDate) && string.IsNullOrEmpty(projectId))
                {
                    distinctPMComments = _tsHoursRecordService.GetRecordsHaveManagerComments(currentUserEmployeeID,
                        projectsIdForWorkEmployee.ToList(),
                        cond =>
                            cond.RecordStatus == TSRecordStatus.Declined
                            && (cond.PMComment != null || cond.PMComment != "")
                            && cond.RecordDate <= dateTimeHoursEndDate,
                        gr => gr.GroupBy(x => x.PMComment)
                    ).ToList();
                    var projectsIdWhereHaveComment = distinctPMComments.Select(x => (int)x.ProjectID).ToList();
                    pmList = _projectMembershipService.GetManagersForProjects(projectsIdWhereHaveComment).ToList();
                }
                //Указана начальная и конечная дата
                else if (!string.IsNullOrEmpty(hoursStartDate) && !string.IsNullOrEmpty(hoursEndDate) && string.IsNullOrEmpty(projectId))
                {
                    distinctPMComments = _tsHoursRecordService.GetRecordsHaveManagerComments(currentUserEmployeeID,
                        projectsIdForWorkEmployee.ToList(),
                        cond =>
                            cond.RecordStatus == TSRecordStatus.Declined
                            && (cond.PMComment != null || cond.PMComment != "")
                            && cond.RecordDate >= dateTimeHoursStartDate
                            && cond.RecordDate <= dateTimeHoursEndDate,
                        gr => gr.GroupBy(x => x.PMComment)
                    ).ToList();
                    var projectsIdWhereHaveComment = distinctPMComments.Select(x => (int)x.ProjectID).ToList();
                    pmList = _projectMembershipService.GetManagersForProjects(projectsIdWhereHaveComment).ToList();
                }
                //Указан проект
                else if (string.IsNullOrEmpty(hoursStartDate) && string.IsNullOrEmpty(hoursEndDate) && !string.IsNullOrEmpty(projectId))
                {
                    distinctPMComments = _tsHoursRecordService.GetRecordsHaveManagerComments(currentUserEmployeeID,
                        projectsIdForWorkEmployee.ToList(),
                        cond =>
                            cond.RecordStatus == TSRecordStatus.Declined
                            && (cond.PMComment != null || cond.PMComment != "")

                            && cond.ProjectID == intProjectId,
                        gr => gr.GroupBy(x => x.PMComment)
                    ).ToList();
                    var projectsIdWhereHaveComment = distinctPMComments.Select(x => (int)x.ProjectID).ToList();
                    pmList = _projectMembershipService.GetManagersForProjects(projectsIdWhereHaveComment).ToList();
                }
                //Начальная дата и проект
                else if (!string.IsNullOrEmpty(hoursStartDate) && string.IsNullOrEmpty(hoursEndDate) && !string.IsNullOrEmpty(projectId))
                {
                    distinctPMComments = _tsHoursRecordService.GetRecordsHaveManagerComments(currentUserEmployeeID,
                        projectsIdForWorkEmployee.ToList(),
                        cond =>
                            cond.RecordStatus == TSRecordStatus.Declined
                            && (cond.PMComment != null || cond.PMComment != "")
                            && cond.RecordDate >= dateTimeHoursStartDate
                            && cond.ProjectID == intProjectId,
                        gr => gr.GroupBy(x => x.PMComment)
                    ).ToList();
                    var projectsIdWhereHaveComment = distinctPMComments.Select(x => (int)x.ProjectID).ToList();
                    pmList = _projectMembershipService.GetManagersForProjects(projectsIdWhereHaveComment).ToList();
                }
                //Конечная дата и проект
                else if (string.IsNullOrEmpty(hoursStartDate) && !string.IsNullOrEmpty(hoursEndDate) && !string.IsNullOrEmpty(projectId))
                {
                    distinctPMComments = _tsHoursRecordService.GetRecordsHaveManagerComments(currentUserEmployeeID,
                        projectsIdForWorkEmployee.ToList(),
                        cond =>
                            cond.RecordStatus == TSRecordStatus.Declined
                            && (cond.PMComment != null || cond.PMComment != "")

                            && cond.RecordDate <= dateTimeHoursEndDate
                            && cond.ProjectID == intProjectId,
                        gr => gr.GroupBy(x => x.PMComment)
                    ).ToList();
                    var projectsIdWhereHaveComment = distinctPMComments.Select(x => (int)x.ProjectID).ToList();
                    pmList = _projectMembershipService.GetManagersForProjects(projectsIdWhereHaveComment).ToList();

                }
                //Начальная, конечная дата и проект
                else if (!string.IsNullOrEmpty(hoursStartDate) && !string.IsNullOrEmpty(hoursEndDate) && !string.IsNullOrEmpty(projectId))
                {
                    distinctPMComments = _tsHoursRecordService.GetRecordsHaveManagerComments(currentUserEmployeeID,
                        projectsIdForWorkEmployee.ToList(),
                        cond =>
                            cond.RecordStatus == TSRecordStatus.Declined
                            && (cond.PMComment != null || cond.PMComment != "")
                            && cond.RecordDate >= dateTimeHoursStartDate
                            && cond.RecordDate <= dateTimeHoursEndDate
                            && cond.ProjectID == intProjectId,
                        gr => gr.GroupBy(x => x.PMComment)
                    ).ToList();
                    var projectsIdWhereHaveComment = distinctPMComments.Select(x => (int)x.ProjectID).ToList();
                    pmList = _projectMembershipService.GetManagersForProjects(projectsIdWhereHaveComment).ToList();
                }
                //если ничего не указано
                else
                {
                    distinctPMComments = _tsHoursRecordService.GetRecordsHaveManagerComments(currentUserEmployeeID,
                        projectsIdForWorkEmployee.ToList(),
                        cond => cond.RecordStatus == TSRecordStatus.Declined && (cond.PMComment != null || cond.PMComment != ""),
                        gr => gr.GroupBy(x => x.PMComment)
                    ).ToList();
                    var projectsIdWhereHaveComment = distinctPMComments.Select(x => (int)x.ProjectID).ToList();
                    pmList = _projectMembershipService.GetManagersForProjects(projectsIdWhereHaveComment).ToList();
                }

                return GetPMsRejectedComments(pmList, distinctPMComments);

            }
            return JsonConvert.SerializeObject("");
        }

        [NonAction]
        private string GetPMsRejectedComments(List<Employee> pmList, List<TSHoursRecord> tSHoursRecords)
        {
            var dataForGrid = new List<object>();
            var idCounter = 1;
            foreach (var pm in pmList)
            {
                foreach (var record in tSHoursRecords)
                {
                    //Какой пм владеет каким проектом
                    if (_projectMembershipService.GetProjectsForManager(pm.ID).Any(x => x.ID == record.ProjectID))
                    {
                        var project = _projectService.GetById((int)record.ProjectID);
                        dataForGrid.Add(
                            new
                            {
                                ID = idCounter++,
                                EmployeeID = project.ShortName + " " + pm.LastName + " " +
                                             pm.FirstName + " " + pm.MidName + " Коментарий: " +
                                             record.PMComment,
                                ProjectId = project.ID,
                                Comment = record.PMComment,
                            }
                        );
                    }
                }
            }

            return JsonConvert.SerializeObject(dataForGrid);
        }


        [HttpGet]
        [ATSHoursRecordCreateUpdateMyHours]
        public string GetEmployeeDeclinedHours(string hoursStartDate, string hoursEndDate, string projectShortName, string commentFromPM)
        {
            var projectIdInt = _projectService.GetByShortName(projectShortName).ID;

            commentFromPM = commentFromPM.Replace(" ", "").Replace(Environment.NewLine, "").Trim().ToLower();


            IList<TSHoursRecord> rejectedHours = new List<TSHoursRecord>();
            var hoursStartDateTime = new DateTime();
            var hoursEndDateTime = new DateTime();

            if (!string.IsNullOrEmpty(hoursStartDate))
                hoursStartDateTime = Convert.ToDateTime(hoursStartDate);
            if (!string.IsNullOrEmpty(hoursEndDate))
                hoursEndDateTime = Convert.ToDateTime(hoursEndDate);

            if (!string.IsNullOrEmpty(hoursStartDate) && string.IsNullOrEmpty(hoursEndDate))
            {
                rejectedHours = _tsHoursRecordService.Get(records => records.Where(x =>
                    x.ProjectID == projectIdInt &&
                    (x.RecordStatus == TSRecordStatus.Declined || x.RecordStatus == TSRecordStatus.DeclinedEditing)
                    && (x.PMComment != null && x.PMComment.Replace(" ", "").Replace(Environment.NewLine, "").Trim().ToLower() == commentFromPM)
                    && x.RecordDate >= hoursStartDateTime).ToList());
            }
            else if (string.IsNullOrEmpty(hoursStartDate) && !string.IsNullOrEmpty(hoursEndDate))
            {
                rejectedHours = _tsHoursRecordService.Get(records => records.Where(x =>
                    x.ProjectID == projectIdInt &&
                    (x.RecordStatus == TSRecordStatus.Declined || x.RecordStatus == TSRecordStatus.DeclinedEditing)
                    && (x.PMComment != null && x.PMComment.Replace(" ", "").Replace(Environment.NewLine, "").Trim().ToLower() == commentFromPM)
                    && x.RecordDate <= hoursEndDateTime).ToList());
            }
            else if (!string.IsNullOrEmpty(hoursStartDate) && !string.IsNullOrEmpty(hoursEndDate))
            {
                rejectedHours = _tsHoursRecordService.Get(records => records.Where(x =>
                    x.ProjectID == projectIdInt && (x.RecordStatus == TSRecordStatus.Declined || x.RecordStatus == TSRecordStatus.DeclinedEditing)
                                                && (x.PMComment != null && x.PMComment.Replace(" ", "").Replace(Environment.NewLine, "").Trim().ToLower() == commentFromPM)
                                                && x.RecordDate >= hoursStartDateTime &&
                                                x.RecordDate <= hoursEndDateTime).ToList());

            }
            else if (string.IsNullOrEmpty(hoursStartDate) && string.IsNullOrEmpty(hoursEndDate))
            {
                rejectedHours = _tsHoursRecordService.Get(records => records.Where(x =>
                    x.ProjectID == projectIdInt && (x.RecordStatus == TSRecordStatus.Declined || x.RecordStatus == TSRecordStatus.DeclinedEditing)
                                                && (x.PMComment != null && x.PMComment.Replace(" ", "").Replace(Environment.NewLine, "").Trim().ToLower() == commentFromPM)
                                                ).ToList());
            }

            var config = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile(new AutoMapperConfig.GetEmployeeDeclinedDataProfile());
            }).CreateMapper();
            var employeeRecords = config.Map<List<TSHoursRecord>, IList<TSHoursRecordDTO>>(rejectedHours.ToList());

            return JsonConvert.SerializeObject(employeeRecords);
        }

        #endregion

        #region Отчет о полноте заполнения ТШ 

        [HttpGet]
        [OperationActionFilter(nameof(Operation.TSCompletenessReportViewForManagedEmployees))]
        public ActionResult ExportTSCompletenessReportToExcel(int? frcDepartmentID, int? autonomousDepartmentID, int? year, int? month)
        {
            ApplicationUser user = _applicationUserService.GetUser();
            var currentUserEmployee = _userService.GetEmployeeForCurrentUser();

            byte[] binData = null;

            DataTable dataTable = new DataTable();
            dataTable.Columns.Add("DepartmentShortName", typeof(string)).Caption = "Код";
            dataTable.Columns["DepartmentShortName"].ExtendedProperties["Width"] = (double)8;
            dataTable.Columns.Add("EmployeePosition", typeof(string)).Caption = "Подразделение / должность";
            dataTable.Columns["EmployeePosition"].ExtendedProperties["Width"] = (double)40;
            dataTable.Columns.Add("EmployeeFullName", typeof(string)).Caption = "Сотрудник";
            dataTable.Columns["EmployeeFullName"].ExtendedProperties["Width"] = (double)50;
            dataTable.Columns.Add("EmployeeCategory", typeof(string)).Caption = "Категория";
            dataTable.Columns["EmployeeCategory"].ExtendedProperties["Width"] = (double)30;
            dataTable.Columns.Add("PlanHours", typeof(double)).Caption = "План";
            dataTable.Columns["PlanHours"].ExtendedProperties["Width"] = (double)17;
            dataTable.Columns.Add("EnteredHours", typeof(double)).Caption = "Введено";
            dataTable.Columns["EnteredHours"].ExtendedProperties["Width"] = (double)17;
            dataTable.Columns.Add("ApprovedHours", typeof(double)).Caption = "Согласовано";
            dataTable.Columns["ApprovedHours"].ExtendedProperties["Width"] = (double)17;
            dataTable.Columns.Add("DeclinedHours", typeof(double)).Caption = "Отклонено";
            dataTable.Columns["DeclinedHours"].ExtendedProperties["Width"] = (double)17;
            dataTable.Columns.Add("OverHours", typeof(double)).Caption = "Переработка";
            dataTable.Columns["OverHours"].ExtendedProperties["Width"] = (double)17;
            dataTable.Columns.Add("UnderHours", typeof(double)).Caption = "Недозагрузка";
            dataTable.Columns["UnderHours"].ExtendedProperties["Width"] = (double)17;
            dataTable.Columns.Add("_ISGROUPROW_", typeof(bool)).Caption = "_ISGROUPROW_";

            double monthPlanHours = Convert.ToDouble(_productionCalendarService.GetSumWorkingHoursForMonth(year.Value, month.Value));

            IList<Department> frcDepartmentList = null;

            if (_applicationUserService.HasAccess(Operation.TSCompletenessReportView))
                frcDepartmentList = _departmentService.Get(departments => departments.Where(d => d.IsFinancialCentre).OrderBy(d => d.ShortName).ToList());
            else if (_applicationUserService.HasAccess(Operation.TSCompletenessReportViewForManagedEmployees) &&
                     !_applicationUserService.HasAccess(Operation.TSCompletenessReportView))
                frcDepartmentList = _departmentService.Get(departments =>
                        departments.Where(d => d.IsFinancialCentre).OrderBy(d => d.ShortName).ToList())
                    .Where(x => _applicationUserService.IsDepartmentManager(x.ID) == true).ToList();


            IList<Department> autonomousDepartmentList = null;

            if (frcDepartmentList.Count == 0) // если нет ЦФО, то работает эта ветка
            {
                if (_applicationUserService.HasAccess(Operation.TSCompletenessReportView))
                    autonomousDepartmentList = _departmentService.Get(departments => departments.Where(d => d.IsAutonomous).OrderBy(d => d.ShortName).ToList());
                else if (_applicationUserService.HasAccess(Operation.TSCompletenessReportViewForManagedEmployees) && !_applicationUserService.HasAccess(Operation.TSCompletenessReportView))
                    autonomousDepartmentList = _departmentService.Get(departments => departments
                    .Where(d => d.IsAutonomous && (d.DepartmentManagerID == currentUserEmployee.ID || d.DepartmentManagerAssistantID == currentUserEmployee.ID))
                    .OrderBy(d => d.ShortName).ToList());
            }
            else if (frcDepartmentID.HasValue && autonomousDepartmentID.HasValue) // иначе так получаем подразделения
                autonomousDepartmentList = _departmentService.GetChildDepartments(frcDepartmentID.Value, true).Where(d => d.IsAutonomous).OrderBy(d => d.ShortName).ToList();

            int departmentID = 0;
            if (autonomousDepartmentID.HasValue && year.HasValue && month.HasValue && autonomousDepartmentList.Where(d => d.ID == autonomousDepartmentID).FirstOrDefault() != null)
                departmentID = autonomousDepartmentID.Value;
            else if (frcDepartmentID.HasValue && year.HasValue && month.HasValue && frcDepartmentList.Where(d => d.ID == frcDepartmentID).FirstOrDefault() != null)
                departmentID = frcDepartmentID.Value;

            DateTime periodStartDate = new DateTime(year.Value, month.Value, 1);
            DateTime periodEndDate = periodStartDate.LastDayOfMonth();

            if (departmentID != 0)
            {
                var employeesInDepartment = _employeeService.GetEmployeesInDepartment(departmentID, true)
                    .Where(e => e.EnrollmentDate != null && e.EnrollmentDate <= periodEndDate && (e.DismissalDate == null || e.DismissalDate >= periodStartDate))
                    .OrderBy(x => x.Department.ShortName).GroupBy(x => x.Department.ShortName)
                    .SelectMany(x => x).ToList();

                //Сделана оптимизация для того, чтобы к БД выполнялся только один запрос, а не количество запросов, равное количеству сотрудников. Суммирование трудозатрат выполняется в памяти.
                List<TSHoursRecord> recordList = _tsHoursRecordService.Get(records => records.Where(x =>
                    x.RecordDate >= periodStartDate && x.RecordDate <= periodEndDate).ToList()).ToList();

                foreach (var group in employeesInDepartment.GroupBy(e => e.Department.ShortName))
                {

                    dataTable.Rows.Add(group.First().Department.ShortName,
                        ((group.First().Department != null) ? group.First().Department.Title : ""),
                        "", "", null, null, null, null, null, null, true);

                    foreach (var employee in group)
                    {
                        List<TSHoursRecord> employeeRecordList = recordList.Where(r => r.EmployeeID == employee.ID).ToList();

                        double employeePlanHours = monthPlanHours;

                        if (periodStartDate < employee.EnrollmentDate
                            || (employee.DismissalDate != null && periodEndDate > employee.DismissalDate))
                        {
                            employeePlanHours = _productionCalendarService.GetSumWorkingHoursForDateRange(new DateTimeRange((periodStartDate >= employee.EnrollmentDate) ? periodStartDate : employee.EnrollmentDate.Value,
                                (employee.DismissalDate == null || periodEndDate <= employee.DismissalDate) ? periodEndDate : employee.DismissalDate.Value));
                        }

                        double enteredHours = Math.Round(employeeRecordList.Where(r => new TSRecordStatus[] { TSRecordStatus.Editing, TSRecordStatus.Declined, TSRecordStatus.DeclinedEditing, TSRecordStatus.Approving, TSRecordStatus.PMApproved, TSRecordStatus.HDApproved }.Contains(r.RecordStatus))
                            .Select(c => c.Hours ?? 0)
                            .DefaultIfEmpty()
                            .Sum(h => h), 2);

                        double approvedHours = Math.Round(employeeRecordList.Where(r => new TSRecordStatus[] { TSRecordStatus.PMApproved, TSRecordStatus.HDApproved }.Contains(r.RecordStatus))
                            .Select(c => c.Hours ?? 0)
                            .DefaultIfEmpty()
                            .Sum(h => h), 2);

                        double declinedHours = Math.Round(employeeRecordList.Where(r => new TSRecordStatus[] { TSRecordStatus.Declined, TSRecordStatus.DeclinedEditing }.Contains(r.RecordStatus))
                            .Select(c => c.Hours ?? 0)
                            .DefaultIfEmpty()
                            .Sum(h => h), 2);

                        var employeeCategory = _employeeCategoryService.GetEmployeeCategoryForDate(employee.ID, (employee.DismissalDate == null || periodEndDate < employee.DismissalDate) ? periodEndDate : employee.DismissalDate.Value);
                        string employeeCategoryDisplayName = string.Empty;
                        if (employeeCategory != null)
                        {
                            if (employeeCategory.CategoryType == EmployeeCategoryType.FreelancerPiecework || employeeCategory.CategoryType == EmployeeCategoryType.ExtContragentEmployee)
                                continue;

                            if (employeeCategory.EmploymentRatio.HasValue)
                                employeePlanHours = employeePlanHours * (double)employeeCategory.EmploymentRatio.Value;
                            employeeCategoryDisplayName = ((DisplayAttribute)(employeeCategory.CategoryType.GetType().GetMember(employeeCategory.CategoryType.ToString()).First().GetCustomAttributes(true)[0])).Name;
                        }

                        bool isShowPlanHours = employeeCategory == null || employeeCategory.CategoryType == EmployeeCategoryType.Regular || employeeCategory.CategoryType == EmployeeCategoryType.Temporary;

                        dataTable.Rows.Add(
                            employee.Department.ShortName,
                            employee.EmployeePositionTitle,
                            employee.FullName,
                            employeeCategoryDisplayName, //_employeeCategoryService.GetEmployeeCategoryForDate(employee.ID, (employee.DismissalDate == null || periodEndDate < employee.DismissalDate) ? periodEndDate : employee.DismissalDate.Value),
                            isShowPlanHours ? (double?)employeePlanHours : null,
                            enteredHours, //_tsHoursRecordService.GetEmployeeTsHoursRecordSummHoursMonthByStatus(selectedYear, selectedMonth, employee.ID, TSRecordStatus.Editing, TSRecordStatus.Declined, TSRecordStatus.DeclinedEditing, TSRecordStatus.Approving, TSRecordStatus.PMApproved, TSRecordStatus.HDApproved),
                            approvedHours, //_tsHoursRecordService.GetEmployeeTsHoursRecordSummHoursMonthByStatus(selectedYear, selectedMonth, employee.ID, TSRecordStatus.PMApproved, TSRecordStatus.HDApproved),
                            declinedHours,//_tsHoursRecordService.GetEmployeeTsHoursRecordSummHoursMonthByStatus(selectedYear, selectedMonth, employee.ID, TSRecordStatus.Declined, TSRecordStatus.DeclinedEditing)
                            isShowPlanHours ? (double?)((employeePlanHours < approvedHours) ? approvedHours - employeePlanHours : 0) : null,
                            isShowPlanHours ? (double?)((employeePlanHours > approvedHours) ? employeePlanHours - approvedHours : 0) : null,
                            false
                        );
                    }
                }
            }

            using (MemoryStream stream = new MemoryStream())
            {
                using (SpreadsheetDocument doc =
                    SpreadsheetDocument.Create(stream, SpreadsheetDocumentType.Workbook))
                {
                    WorkbookPart workbookPart = ExcelHelper.CreateWorkbookPart(doc, "Отчет по ТШ");

                    WorksheetPart rId1 = ExcelHelper.CreateWorksheetPartAndImportDataTable(workbookPart, "rId1", 1, 1,
                        (uint)dataTable.Columns.Count,
                        "Отчет о заполнении ТШ за " + new DateTime(year.Value, month.Value, 1).ToString("MMMM", CultureInfo.CreateSpecificCulture("ru")) + " " + year,
                        dataTable, 3, 1);

                    doc.WorkbookPart.Workbook.Save();
                }

                stream.Position = 0;
                BinaryReader b = new BinaryReader(stream);
                binData = b.ReadBytes((int)stream.Length);
            }
            return File(binData, ExcelHelper.ExcelContentType, "TSCompletenessReport" + DateTime.Now.ToString("ddMMyyHHmmss") + ".xlsx");

        }


        [HttpGet]
        [OperationActionFilter(nameof(Operation.TSCompletenessReportViewForManagedEmployees))]
        public ActionResult TSCompletenessReport(int? frcDepartmentID, int? autonomousDepartmentID, int? year, int? month)
        {
            var selectedMonth = month ?? DateTime.Today.Month;
            var selectedYear = year ?? DateTime.Today.Year;

            ViewBag.Months = new SelectList(Enumerable.Range(0, 13).Select(x =>
                new SelectListItem()
                {
                    Text = (x != 0) ? CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(x) + " (" + x + ")" : "-не выбрано-",
                    Value = (x != 0) ? x.ToString() : "",
                }), "Value", "Text", selectedMonth);

            SelectList yearsSelectList = new SelectList(Enumerable.Range(GlobalVariables.BudgetLimitStartYear, DateTime.Today.Year - GlobalVariables.BudgetLimitStartYear + 10).Select(x =>
                new SelectListItem()
                {
                    Text = (x != GlobalVariables.BudgetLimitStartYear) ? x.ToString() : "-не выбрано-",
                    Value = (x != GlobalVariables.BudgetLimitStartYear) ? x.ToString() : "",
                }), "Value", "Text", selectedYear);

            ViewBag.Years = yearsSelectList;
            var user = _applicationUserService.GetUser();

            IList<Department> frcDepartmentSelectList = null;

            if (_applicationUserService.HasAccess(Operation.TSCompletenessReportView))
                frcDepartmentSelectList = _departmentService.Get(departments => departments.Where(d => d.IsFinancialCentre).OrderBy(d => d.ShortName).ToList());
            else if (_applicationUserService.HasAccess(Operation.TSCompletenessReportViewForManagedEmployees) &&
                      !_applicationUserService.HasAccess(Operation.TSCompletenessReportView))
                frcDepartmentSelectList = _departmentService.Get(departments =>
                        departments.Where(d => d.IsFinancialCentre).OrderBy(d => d.ShortName).ToList())
                    .Where(x => _applicationUserService.IsDepartmentManager(x.ID) == true).ToList();

            IList<Department> autonomousDepartmentSelectList = null;

            if (frcDepartmentSelectList == null || frcDepartmentSelectList.Count == 0) // если нет цфо, то работает эта ветка
            {
                if (_applicationUserService.HasAccess(Operation.TSCompletenessReportView))
                    autonomousDepartmentSelectList = _departmentService.Get(departments => departments.Where(d => d.IsAutonomous).OrderBy(d => d.ShortName).ToList());
                else if (_applicationUserService.HasAccess(Operation.TSCompletenessReportViewForManagedEmployees) && !_applicationUserService.HasAccess(Operation.TSCompletenessReportView))
                    autonomousDepartmentSelectList = _departmentService.Get(departments => departments
                            .Where(d => d.IsAutonomous)
                            .OrderBy(d => d.ShortName).ToList())
                        .Where(x => _applicationUserService.IsDepartmentManager(x.ID) == true).ToList();
            }
            else if (frcDepartmentID.HasValue) // иначе так получаем подразделения
                autonomousDepartmentSelectList = _departmentService.GetChildDepartments(frcDepartmentID.Value, true).Where(d => d.IsAutonomous).OrderBy(d => d.ShortName).ToList();

            var selectedFRCDepartment = frcDepartmentSelectList?.Where(d => d.ID == frcDepartmentID).FirstOrDefault();
            var selectedAutonomousDepartment = autonomousDepartmentSelectList?.Where(d => d.ID == autonomousDepartmentID).FirstOrDefault();

            if (selectedFRCDepartment == null)
            {
                if (selectedAutonomousDepartment != null
                    && frcDepartmentSelectList != null && frcDepartmentSelectList.Count != 0)
                {
                    selectedFRCDepartment = selectedAutonomousDepartment;
                    while (selectedFRCDepartment != null
                        && selectedFRCDepartment.IsFinancialCentre == false
                        && selectedFRCDepartment.ParentDepartment != null)
                    {
                        selectedFRCDepartment = frcDepartmentSelectList.Where(d => d.ID == selectedFRCDepartment.ParentDepartmentID).FirstOrDefault();
                    }

                    if (selectedFRCDepartment != null && selectedFRCDepartment.IsFinancialCentre == false)
                    {
                        selectedFRCDepartment = null;
                    }
                }

                if (selectedFRCDepartment != null && selectedAutonomousDepartment != null)
                {
                    if (!(frcDepartmentID == selectedFRCDepartment.ID
                        && autonomousDepartmentID == selectedAutonomousDepartment.ID
                        && year == selectedYear && month == selectedMonth))
                    {
                        return RedirectToAction("TSCompletenessReport", new
                        {
                            frcDepartmentID = selectedFRCDepartment.ID,
                            autonomousDepartmentID = selectedAutonomousDepartment.ID,
                            year = selectedYear,
                            month = selectedMonth
                        });
                    }
                    else
                    {
                        return StatusCode(StatusCodes.Status400BadRequest);
                    }
                }
                else
                {
                    selectedFRCDepartment = frcDepartmentSelectList.FirstOrDefault();
                    if (selectedFRCDepartment != null)
                    {
                        if (!(frcDepartmentID == selectedFRCDepartment.ID
                            && year == selectedYear && month == selectedMonth))
                        {
                            return RedirectToAction("TSCompletenessReport", new { frcDepartmentID = selectedFRCDepartment.ID, year = selectedYear, month = selectedMonth });
                        }
                        else
                        {
                            return StatusCode(StatusCodes.Status400BadRequest);
                        }
                    }
                    else if (selectedAutonomousDepartment == null)
                    {
                        selectedAutonomousDepartment = autonomousDepartmentSelectList.FirstOrDefault();
                        if (selectedAutonomousDepartment != null)
                        {
                            if (!(autonomousDepartmentID == selectedAutonomousDepartment.ID
                                && year == selectedYear && month == selectedMonth))
                            {
                                return RedirectToAction("TSCompletenessReport", new { autonomousDepartmentID = selectedAutonomousDepartment.ID, year = selectedYear, month = selectedMonth });
                            }
                            else
                            {
                                return StatusCode(StatusCodes.Status400BadRequest);
                            }

                        }
                        else
                        {
                            return StatusCode(StatusCodes.Status403Forbidden);
                        }
                    }
                }
            }

            ViewBag.SelectedMonth = selectedMonth;
            ViewBag.SelectedYear = selectedYear;
            ViewBag.SelectedFRCDepartmentID = selectedFRCDepartment?.ID;
            ViewBag.SelectedAutonomousDepartmentID = selectedAutonomousDepartment?.ID;

            DateTime periodStartDate = new DateTime(selectedYear, selectedMonth, 1);
            DateTime periodEndDate = periodStartDate.LastDayOfMonth();

            if (frcDepartmentSelectList != null)
            {
                ViewBag.FRCDepartmentID = new SelectList(frcDepartmentSelectList.ToList(), "ID", "FullName", selectedFRCDepartment?.ID);
            }

            if (autonomousDepartmentSelectList != null)
            {
                ViewBag.AutonomousDepartmentID = new SelectList(autonomousDepartmentSelectList.ToList(), "ID", "FullName", selectedAutonomousDepartment?.ID);
            }

            int departmentID = 0;
            if (autonomousDepartmentID.HasValue && autonomousDepartmentSelectList.Where(d => d.ID == autonomousDepartmentID).FirstOrDefault() != null)
                departmentID = autonomousDepartmentID.Value;
            else if (frcDepartmentID.HasValue && frcDepartmentSelectList.Where(d => d.ID == frcDepartmentID).FirstOrDefault() != null)
                departmentID = frcDepartmentID.Value;


            if (departmentID != 0)
            {
                var department = _departmentService.GetById(departmentID);

                department.EmployeesInDepartment = _employeeService.GetEmployeesInDepartment(departmentID, true)
                    .Where(e => e.EnrollmentDate != null && e.EnrollmentDate <= periodEndDate && (e.DismissalDate == null || e.DismissalDate >= periodStartDate))
                    .OrderBy(x => x.Department.ShortName).GroupBy(x => x.Department.ShortName)
                    .SelectMany(x => x).ToList();

                var employeesViewModel = new List<TSHoursRecordTSCompletenessReportEmployeeViewModel>();

                int monthPlanHours = _productionCalendarService.GetSumWorkingHoursForDateRange(new DateTimeRange(periodStartDate, periodEndDate));

                //Сделана оптимизация для того, чтобы к БД выполнялся только один запрос, а не количество запросов, равное количеству сотрудников. Суммирование трудозатрат выполняется в памяти.
                List<TSHoursRecord> recordList = _tsHoursRecordService.Get(records => records.Where(x =>
                    x.RecordDate >= periodStartDate && x.RecordDate <= periodEndDate).ToList()).ToList();

                foreach (var employee in department.EmployeesInDepartment)
                {
                    List<TSHoursRecord> employeeRecordList = recordList.Where(r => r.EmployeeID == employee.ID).ToList();

                    int employeePlanHours = monthPlanHours;

                    if (periodStartDate < employee.EnrollmentDate
                        || (employee.DismissalDate != null && periodEndDate > employee.DismissalDate))
                    {
                        employeePlanHours = _productionCalendarService.GetSumWorkingHoursForDateRange(new DateTimeRange((periodStartDate >= employee.EnrollmentDate) ? periodStartDate : employee.EnrollmentDate.Value,
                        (employee.DismissalDate == null || periodEndDate <= employee.DismissalDate) ? periodEndDate : employee.DismissalDate.Value));
                    }

                    var employeeCategory = _employeeCategoryService.GetEmployeeCategoryForDate(employee.ID, (employee.DismissalDate == null || periodEndDate < employee.DismissalDate) ? periodEndDate : employee.DismissalDate.Value);
                    string employeeCategoryDisplayName = string.Empty;
                    if (employeeCategory != null)
                    {
                        if (employeeCategory.CategoryType == EmployeeCategoryType.FreelancerPiecework || employeeCategory.CategoryType == EmployeeCategoryType.ExtContragentEmployee)
                            continue;

                        if (employeeCategory.EmploymentRatio.HasValue)
                            employeePlanHours = (int)(employeePlanHours * employeeCategory.EmploymentRatio.Value);
                        employeeCategoryDisplayName = ((DisplayAttribute)(employeeCategory.CategoryType.GetType().GetMember(employeeCategory.CategoryType.ToString()).First().GetCustomAttributes(true)[0])).Name;
                    }

                    employeesViewModel.Add(new TSHoursRecordTSCompletenessReportEmployeeViewModel()
                    {
                        ID = employee.ID,
                        LastName = employee.LastName,
                        FirstName = employee.FirstName,
                        MidName = employee.MidName,
                        EmployeePositionTitle = employee.EmployeePositionTitle,
                        EmployeeCategory = employeeCategoryDisplayName, //_employeeCategoryService.GetEmployeeCategoryForDate(employee.ID, (employee.DismissalDate == null || periodEndDate < employee.DismissalDate) ? periodEndDate : employee.DismissalDate.Value),
                        DepartmentID = employee.DepartmentID.Value,
                        DepartmentShortName = employee.Department.ShortName,
                        DepartmentTitle = employee.Department.Title,
                        DepartmentIsFinancialCentre = employee.Department.IsFinancialCentre,
                        IsAutonomousDepartment = employee.Department.IsAutonomous,
                        PlanHours = employeePlanHours,
                        CategoryType = employeeCategory != null ? (EmployeeCategoryType?)employeeCategory.CategoryType : null,
                        EnteredHours = Math.Round(employeeRecordList.Where(r => new TSRecordStatus[] { TSRecordStatus.Editing, TSRecordStatus.Declined, TSRecordStatus.DeclinedEditing, TSRecordStatus.Approving, TSRecordStatus.PMApproved, TSRecordStatus.HDApproved }.Contains(r.RecordStatus))
                        .Select(c => c.Hours ?? 0)
                        .DefaultIfEmpty()
                        .Sum(h => h), 2), //_tsHoursRecordService.GetEmployeeTsHoursRecordSummHoursMonthByStatus(selectedYear, selectedMonth, employee.ID, TSRecordStatus.Editing, TSRecordStatus.Declined, TSRecordStatus.DeclinedEditing, TSRecordStatus.Approving, TSRecordStatus.PMApproved, TSRecordStatus.HDApproved),
                        ApprovedHours = Math.Round(employeeRecordList.Where(r => new TSRecordStatus[] { TSRecordStatus.PMApproved, TSRecordStatus.HDApproved }.Contains(r.RecordStatus))
                        .Select(c => c.Hours ?? 0)
                        .DefaultIfEmpty()
                        .Sum(h => h), 2), //_tsHoursRecordService.GetEmployeeTsHoursRecordSummHoursMonthByStatus(selectedYear, selectedMonth, employee.ID, TSRecordStatus.PMApproved, TSRecordStatus.HDApproved),
                        DeclinedHours = Math.Round(employeeRecordList.Where(r => new TSRecordStatus[] { TSRecordStatus.Declined, TSRecordStatus.DeclinedEditing }.Contains(r.RecordStatus))
                        .Select(c => c.Hours ?? 0)
                        .DefaultIfEmpty()
                        .Sum(h => h), 2)//_tsHoursRecordService.GetEmployeeTsHoursRecordSummHoursMonthByStatus(selectedYear, selectedMonth, employee.ID, TSRecordStatus.Declined, TSRecordStatus.DeclinedEditing)
                    });
                }
                var departmentViewModel = new TSHoursRecordTSCompletenessReportDepartmentViewModel()
                {
                    Department = department,
                    Employees = employeesViewModel
                };
                return View(departmentViewModel);
            }

            return View();
        }

        [HttpGet]
        [OperationActionFilter(nameof(Operation.TSCompletenessReportViewForManagedEmployees))]
        public ActionResult ExportTSCompletenessReportDetailsToExcel(int? frcDepartmentID, int? autonomousDepartmentID, int year, int month, int employeeID)
        {
            ApplicationUser user = _applicationUserService.GetUser();

            byte[] binData = null;

            DataTable dataTable = new DataTable();

            dataTable.Columns.Add("RecordDate", typeof(DateTime)).Caption = "Дата";
            dataTable.Columns["RecordDate"].ExtendedProperties["Width"] = (double)11;
            dataTable.Columns.Add("Project", typeof(string)).Caption = "Проект";
            dataTable.Columns["Project"].ExtendedProperties["Width"] = (double)40;
            dataTable.Columns.Add("Hours", typeof(double)).Caption = "Трудозатраты (ч)";
            dataTable.Columns["Hours"].ExtendedProperties["Width"] = (double)15;
            dataTable.Columns.Add("Description", typeof(string)).Caption = "Состав работ";
            dataTable.Columns["Description"].ExtendedProperties["Width"] = (double)90;
            dataTable.Columns.Add("Created", typeof(DateTime)).Caption = "Создано";
            dataTable.Columns["Created"].ExtendedProperties["Width"] = (double)20;
            dataTable.Columns.Add("RecordStatus", typeof(string)).Caption = "Статус";
            dataTable.Columns["RecordStatus"].ExtendedProperties["Width"] = (double)20;
            dataTable.Columns.Add("RecordSource", typeof(string)).Caption = "Источник";
            dataTable.Columns["RecordSource"].ExtendedProperties["Width"] = (double)20;
            dataTable.Columns.Add("PMComment", typeof(string)).Caption = "Комментарий РП";
            dataTable.Columns["PMComment"].ExtendedProperties["Width"] = (double)20;

            double planTsHoursRecords = Convert.ToDouble(_productionCalendarService.GetSumWorkingHoursForMonth(year, month));

            IList<Employee> managedEmployeeList = null;

            if (_applicationUserService.HasAccess(Operation.TSCompletenessReportView))
                managedEmployeeList = _employeeService.Get(x => x.Include(e => e.Department).Include(e => e.EmployeePosition).ToList()).ToList();
            else if (_applicationUserService.HasAccess(Operation.TSCompletenessReportViewForManagedEmployees) &&
                      !_applicationUserService.HasAccess(Operation.TSCompletenessReportView))
                managedEmployeeList = _employeeService.GetAllManagedEmployees(_applicationUserService.GetUser().ManagedDepartments).ToList();

            DateTime periodStartDate = new DateTime(year, month, 1);
            DateTime periodEndDate = periodStartDate.LastDayOfMonth();

            Employee employee = null;

            if (managedEmployeeList != null && managedEmployeeList.Count != 0)
            {
                employee = managedEmployeeList.Where(e => e.ID == employeeID).FirstOrDefault();

                ViewBag.EmployeeFullName = employee.FullName;
            }

            if (employee != null)
            {
                var recordList = _tsHoursRecordService.GetEmployeeTSHoursRecords(employee.ID, periodStartDate,
                        periodEndDate, null, TSRecordStatus.All, null).OrderBy(x => x.Employee.LastName).ThenBy(x => x.RecordDate).ToList();

                foreach (var record in recordList)
                {
                    dataTable.Rows.Add(record.RecordDate, record.Project.ShortName, record.Hours,
                        record.Description, record.Created,
                        record.RecordStatus.GetAttributeOfType<DisplayAttribute>().Name,
                        record.RecordSource.GetAttributeOfType<DisplayAttribute>().Name,
                        record.PMComment);
                }
            }

            using (MemoryStream stream = new MemoryStream())
            {
                using (SpreadsheetDocument doc =
                    SpreadsheetDocument.Create(stream, SpreadsheetDocumentType.Workbook))
                {
                    WorkbookPart workbookPart = ExcelHelper.CreateWorkbookPart(doc, "Отчет по ТШ");

                    WorksheetPart rId1 = ExcelHelper.CreateWorksheetPartAndImportDataTable(workbookPart, "rId1", 1, 1,
                        (uint)dataTable.Columns.Count,
                        "Отчет о заполнении ТШ за " + new DateTime(year, month, 1).ToString("MMMM", CultureInfo.CreateSpecificCulture("ru")) + " " + year + " - " + employee.FullName,
                        dataTable, 3, 1);

                    doc.WorkbookPart.Workbook.Save();
                }

                stream.Position = 0;
                BinaryReader b = new BinaryReader(stream);
                binData = b.ReadBytes((int)stream.Length);
            }
            return File(binData, ExcelHelper.ExcelContentType, "TSCompletenessReportDetails" + DateTime.Now.ToString("ddMMyyHHmmss") + ".xlsx");

        }

        [HttpGet]
        [OperationActionFilter(nameof(Operation.TSCompletenessReportViewForManagedEmployees))]
        public ActionResult TSCompletenessReportDetails(int? frcDepartmentID, int? autonomousDepartmentID, int? year, int? month, int employeeID)
        {
            var selectedMonth = month ?? DateTime.Today.Month;
            var selectedYear = year ?? DateTime.Today.Year;
            var selectedFRCDepartment = _departmentService.GetById(frcDepartmentID ?? 0);
            var selectedAutonomousDepartment = _departmentService.GetById(autonomousDepartmentID ?? 0);

            while (selectedFRCDepartment != null
                && selectedFRCDepartment.ParentDepartment != null
                && selectedFRCDepartment.IsFinancialCentre == false)
            {
                selectedFRCDepartment = _departmentService.GetById(selectedFRCDepartment.ParentDepartmentID.Value);
            }

            while (selectedAutonomousDepartment != null
                && selectedAutonomousDepartment.ParentDepartment != null
                && selectedAutonomousDepartment.IsAutonomous == false)
            {
                selectedAutonomousDepartment = _departmentService.GetById(selectedAutonomousDepartment.ParentDepartmentID.Value);
            }

            ViewBag.FRCDepartmentID = selectedFRCDepartment?.ID;
            ViewBag.AutonomousDepartmentID = selectedAutonomousDepartment?.ID;

            ViewBag.Months = new SelectList(Enumerable.Range(0, 13).Select(x =>
                new SelectListItem()
                {
                    Text = (x != 0) ? CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(x) + " (" + x + ")" : "-не выбрано-",
                    Value = (x != 0) ? x.ToString() : "",
                }), "Value", "Text", selectedMonth);

            SelectList yearsSelectList = new SelectList(Enumerable.Range(GlobalVariables.BudgetLimitStartYear, DateTime.Today.Year - GlobalVariables.BudgetLimitStartYear + 10).Select(x =>
                new SelectListItem()
                {
                    Text = (x != GlobalVariables.BudgetLimitStartYear) ? x.ToString() : "-не выбрано-",
                    Value = (x != GlobalVariables.BudgetLimitStartYear) ? x.ToString() : "",
                }), "Value", "Text", selectedYear);

            ViewBag.Years = yearsSelectList;

            ViewBag.EmployeeID = employeeID;

            ViewBag.SelectedMonth = selectedMonth;
            ViewBag.SelectedYear = selectedYear;

            var user = _applicationUserService.GetUser();

            IList<Employee> managedEmployeeList = null;

            if (_applicationUserService.HasAccess(Operation.TSCompletenessReportView))
                managedEmployeeList = _employeeService.Get(x => x.Include(e => e.Department).Include(e => e.EmployeePosition).ToList()).ToList();
            else if (_applicationUserService.HasAccess(Operation.TSCompletenessReportViewForManagedEmployees) &&
                      !_applicationUserService.HasAccess(Operation.TSCompletenessReportView))
                managedEmployeeList = _employeeService.GetAllManagedEmployees(_applicationUserService.GetUser().ManagedDepartments).ToList();

            DateTime periodStartDate = new DateTime(selectedYear, selectedMonth, 1);
            DateTime periodEndDate = periodStartDate.LastDayOfMonth();

            Employee employee = null;

            if (managedEmployeeList != null && managedEmployeeList.Count() != 0)
            {
                employee = managedEmployeeList.Where(e => e.ID == employeeID).FirstOrDefault();
                ViewBag.EmployeeFullName = employee.FullName;
            }

            if (employee != null)
            {
                return View(_tsHoursRecordService.GetEmployeeTSHoursRecords(employee.ID, periodStartDate,
                        periodEndDate, null, TSRecordStatus.All, null).OrderBy(x => x.Employee.LastName).ThenBy(x => x.RecordDate).ToList());
            }
            else
            {
                return StatusCode(StatusCodes.Status403Forbidden);
            }
        }

        #endregion


        #region Отчет о согласовании трудозатрат РП 
        [OperationActionFilter(nameof(Operation.TSApproveHoursReportViewForManagedEmployees))]
        public ActionResult ExportTSApproveHoursReportToExcel(int? departmentID, int? year, int? month)
        {
            ApplicationUser user = _applicationUserService.GetUser();
            var currentUserEmployee = _userService.GetEmployeeForCurrentUser();

            IList<Department> departmentList = null;

            byte[] binData = null;

            DataTable dataTable = new DataTable();
            dataTable.Columns.Add("DepartmentShortName", typeof(string)).Caption = "Код";
            dataTable.Columns["DepartmentShortName"].ExtendedProperties["Width"] = (double)8;
            dataTable.Columns.Add("EmployeeFullName", typeof(string)).Caption = "Подразделение / Руководитель проекта";
            dataTable.Columns["EmployeeFullName"].ExtendedProperties["Width"] = (double)40;
            dataTable.Columns.Add("ProjectShortName", typeof(string)).Caption = "Проект";
            dataTable.Columns["ProjectShortName"].ExtendedProperties["Width"] = (double)50;
            dataTable.Columns.Add("ApprovedHours", typeof(double)).Caption = "Кол-во часов на согласовании";
            dataTable.Columns["ApprovedHours"].ExtendedProperties["Width"] = (double)30;
            dataTable.Columns.Add("_ISGROUPROW_", typeof(bool)).Caption = "_ISGROUPROW_";

            if (_applicationUserService.HasAccess(Operation.TSApproveHoursReportView))
                departmentList = _departmentService.Get(departments => departments.Where(d => d.IsFinancialCentre).OrderBy(d => d.ShortName).ToList());
            else if (_applicationUserService.HasAccess(Operation.TSApproveHoursReportViewForManagedEmployees) &&
                      !_applicationUserService.HasAccess(Operation.TSApproveHoursReportView))
                departmentList = _departmentService.Get(departments =>
                        departments.Where(d => d.IsFinancialCentre).OrderBy(d => d.ShortName).ToList())
                    .Where(x => _applicationUserService.IsDepartmentManager(x.ID) == true).ToList();

            DateTime periodStartDate = new DateTime(year.Value, month.Value, 1);
            DateTime periodEndDate = periodStartDate.LastDayOfMonth();

            if (departmentID.HasValue && year.HasValue && month.HasValue
                && departmentList.Where(d => d.ID == departmentID).FirstOrDefault() != null)
            {
                var employeesInDepartment = _employeeService.GetEmployeesInDepartment(departmentID.Value, true)
                    .OrderBy(x => x.Department.ShortName).GroupBy(x => x.Department.ShortName)
                    .SelectMany(x => x).ToList();

                //Сделана оптимизация для того, чтобы к БД выполнялся только один запрос, а не количество запросов, равное количеству сотрудников. Суммирование трудозатрат выполняется в памяти.
                List<TSHoursRecord> recordList = _tsHoursRecordService.Get(records => records.Where(x =>
                    x.RecordDate >= periodStartDate && x.RecordDate <= periodEndDate && x.RecordStatus == TSRecordStatus.Approving).ToList()).ToList();

                foreach (var group in employeesInDepartment.GroupBy(e => e.Department.ShortName))
                {
                    bool groupRowAdded = false;

                    foreach (var employee in group.ToList())
                    {
                        foreach (var approvingRecords in recordList.Where(r => r.Project.ApproveHoursEmployeeID == employee.ID)
                            .ToList().OrderBy(r => r.Project.ShortName).GroupBy(r => r.Project.ShortName).Select(y => y))
                        {
                            double approvingHours = approvingRecords.Select(c => c.Hours ?? 0)
                                .DefaultIfEmpty()
                                .Sum(h => h);

                            if (groupRowAdded == false)
                            {
                                dataTable.Rows.Add(group.First().Department.ShortName,
                                    ((group.First().Department != null) ? group.First().Department.Title : ""),
                                    "", null, true);

                                groupRowAdded = true;
                            }

                            dataTable.Rows.Add(
                                employee.Department.ShortName,
                                employee.FullName,
                                approvingRecords.First().Project.ShortName,
                                approvingHours, false
                            );
                        }
                    }

                }
            }

            using (MemoryStream stream = new MemoryStream())
            {
                using (SpreadsheetDocument doc =
                    SpreadsheetDocument.Create(stream, SpreadsheetDocumentType.Workbook))
                {
                    WorkbookPart workbookPart = ExcelHelper.CreateWorkbookPart(doc, "Отчет по ТШ");

                    WorksheetPart rId1 = ExcelHelper.CreateWorksheetPartAndImportDataTable(workbookPart, "rId1", 1, 1,
                        (uint)dataTable.Columns.Count,
                        "Отчет о согласовании трудозатрат руководителями проектов за " + new DateTime(year.Value, month.Value, 1).ToString("MMMM", CultureInfo.CreateSpecificCulture("ru")) + " " + year,
                        dataTable, 3, 1);

                    doc.WorkbookPart.Workbook.Save();
                }

                stream.Position = 0;
                BinaryReader b = new BinaryReader(stream);
                binData = b.ReadBytes((int)stream.Length);
            }
            return File(binData, ExcelHelper.ExcelContentType, "TSApproveHoursReport" + DateTime.Now.ToString("ddMMyyHHmmss") + ".xlsx");
        }

        [HttpGet]
        [OperationActionFilter(nameof(Operation.TSApproveHoursReportViewForManagedEmployees))]
        public ActionResult ExportTSApproveHoursReportDetailsToExcel(int departmentID, int? projectID, int year, int month, int employeeID)
        {
            ApplicationUser user = _applicationUserService.GetUser();
            var currentUserEmployee = _userService.GetEmployeeForCurrentUser();

            IList<Department> departmentList = null;


            byte[] binData = null;

            DataTable dataTable = new DataTable();

            dataTable.Columns.Add("EmployeeFullName", typeof(string)).Caption = "ФИО";
            dataTable.Columns["EmployeeFullName"].ExtendedProperties["Width"] = (double)40;
            dataTable.Columns.Add("RecordDate", typeof(DateTime)).Caption = "Дата";
            dataTable.Columns["RecordDate"].ExtendedProperties["Width"] = (double)11;
            dataTable.Columns.Add("Project", typeof(string)).Caption = "Проект";
            dataTable.Columns["Project"].ExtendedProperties["Width"] = (double)40;
            dataTable.Columns.Add("Hours", typeof(double)).Caption = "Трудозатраты (ч)";
            dataTable.Columns["Hours"].ExtendedProperties["Width"] = (double)15;
            dataTable.Columns.Add("Description", typeof(string)).Caption = "Состав работ";
            dataTable.Columns["Description"].ExtendedProperties["Width"] = (double)90;
            dataTable.Columns.Add("Created", typeof(DateTime)).Caption = "Создано";
            dataTable.Columns["Created"].ExtendedProperties["Width"] = (double)20;
            dataTable.Columns.Add("_ISGROUPROW_", typeof(bool)).Caption = "_ISGROUPROW_";

            if (_applicationUserService.HasAccess(Operation.TSCompletenessReportView))
                departmentList = _departmentService.Get(departments => departments.Where(d => d.IsFinancialCentre).OrderBy(d => d.ShortName).ToList());
            else if (_applicationUserService.HasAccess(Operation.TSCompletenessReportViewForManagedEmployees) &&
                      !_applicationUserService.HasAccess(Operation.TSCompletenessReportView))
                departmentList = _departmentService.Get(departments =>
                        departments.Where(d => d.IsFinancialCentre).OrderBy(d => d.ShortName).ToList())
                    .Where(x => _applicationUserService.IsDepartmentManager(x.ID) == true).ToList();

            DateTime periodStartDate = new DateTime(year, month, 1);
            DateTime periodEndDate = periodStartDate.LastDayOfMonth();

            Employee employee = null;

            if (departmentList != null && departmentList.Count() != 0
                && departmentList.Where(d => d.ID == departmentID).FirstOrDefault() != null)
            {
                employee = _employeeService.GetEmployeesInDepartment(departmentID, true)
                    .Where(e => e.ID == employeeID).FirstOrDefault();

                ViewBag.EmployeeFullName = employee.FullName;
            }

            if (employee != null)
            {
                var recordList = _tsHoursRecordService.Get(records => records.Where(x =>
                    x.RecordDate >= periodStartDate && x.RecordDate <= periodEndDate && x.RecordStatus == TSRecordStatus.Approving
                    && (projectID == null || x.ProjectID == projectID)).ToList()).Where(x => x.Project.ApproveHoursEmployeeID == employeeID).ToList();

                foreach (var group in recordList.OrderBy(r => r.Employee.FullName).GroupBy(r => r.Employee.FullName))
                {
                    dataTable.Rows.Add(group.FirstOrDefault().Employee.FullName,
                            null, "", null,
                            "", null, true);

                    foreach (var record in group.OrderBy(r => r.RecordDate))
                    {
                        dataTable.Rows.Add(record.Employee.FullName,
                            record.RecordDate, record.Project.ShortName, record.Hours,
                            record.Description, record.Created, false);
                    }
                }
            }

            using (MemoryStream stream = new MemoryStream())
            {
                using (SpreadsheetDocument doc =
                    SpreadsheetDocument.Create(stream, SpreadsheetDocumentType.Workbook))
                {
                    WorkbookPart workbookPart = ExcelHelper.CreateWorkbookPart(doc, "Отчет по ТШ");

                    WorksheetPart rId1 = ExcelHelper.CreateWorksheetPartAndImportDataTable(workbookPart, "rId1", 1, 1,
                        (uint)dataTable.Columns.Count,
                        "Трудозатраты на согласовании за " + new DateTime(year, month, 1).ToString("MMMM", CultureInfo.CreateSpecificCulture("ru")) + " " + year + " - " + employee.FullName,
                        dataTable, 3, 1);

                    doc.WorkbookPart.Workbook.Save();
                }

                stream.Position = 0;
                BinaryReader b = new BinaryReader(stream);
                binData = b.ReadBytes((int)stream.Length);
            }
            return File(binData, ExcelHelper.ExcelContentType, "TSApproveHoursReportDetails" + DateTime.Now.ToString("ddMMyyHHmmss") + ".xlsx");

        }

        [HttpGet]
        [OperationActionFilter(nameof(Operation.TSApproveHoursReportViewForManagedEmployees))]
        public ActionResult TSApproveHoursReportDetails(int? departmentID, int? projectID, int? year, int? month, int employeeID)
        {
            var selectedMonth = month ?? DateTime.Today.Month;
            var selectedYear = year ?? DateTime.Today.Year;
            var selectedDepartment = _departmentService.GetById(departmentID ?? 0);

            while (selectedDepartment.ParentDepartment != null
                   && selectedDepartment.IsFinancialCentre == false)
            {
                selectedDepartment = _departmentService.GetById(selectedDepartment.ParentDepartmentID.Value);
            }

            ViewBag.DepartmentID = selectedDepartment.ID;
            ViewBag.ProjectID = projectID;

            ViewBag.Months = new SelectList(Enumerable.Range(0, 13).Select(x =>
                new SelectListItem()
                {
                    Text = (x != 0) ? CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(x) + " (" + x + ")" : "-не выбрано-",
                    Value = (x != 0) ? x.ToString() : "",
                }), "Value", "Text", selectedMonth);

            SelectList yearsSelectList = new SelectList(Enumerable.Range(GlobalVariables.BudgetLimitStartYear, DateTime.Today.Year - GlobalVariables.BudgetLimitStartYear + 10).Select(x =>
                new SelectListItem()
                {
                    Text = (x != GlobalVariables.BudgetLimitStartYear) ? x.ToString() : "-не выбрано-",
                    Value = (x != GlobalVariables.BudgetLimitStartYear) ? x.ToString() : "",
                }), "Value", "Text", selectedYear);

            ViewBag.Years = yearsSelectList;

            ViewBag.EmployeeID = employeeID;

            ViewBag.SelectedMonth = selectedMonth;
            ViewBag.SelectedYear = selectedYear;

            var user = _applicationUserService.GetUser();

            IList<Department> departmentList = null;

            if (_applicationUserService.HasAccess(Operation.TSApproveHoursReportView))
                departmentList = _departmentService.Get(departments => departments.Where(d => d.IsFinancialCentre).OrderBy(d => d.ShortName).ToList());
            else if (_applicationUserService.HasAccess(Operation.TSApproveHoursReportViewForManagedEmployees) &&
                      !_applicationUserService.HasAccess(Operation.TSApproveHoursReportView))
                departmentList = _departmentService.Get(departments =>
                        departments.Where(d => d.IsFinancialCentre).OrderBy(d => d.ShortName).ToList())
                    .Where(x => _applicationUserService.IsDepartmentManager(x.ID) == true).ToList();

            DateTime periodStartDate = new DateTime(selectedYear, selectedMonth, 1);
            DateTime periodEndDate = periodStartDate.LastDayOfMonth();

            Employee employee = null;

            if (departmentList != null && departmentList.Count() != 0
                && departmentList.Where(d => d.ID == selectedDepartment.ID).FirstOrDefault() != null)
            {
                employee = _employeeService.GetEmployeesInDepartment(departmentID.Value, true)
                    .Where(e => e.ID == employeeID).FirstOrDefault();

                ViewBag.EmployeeFullName = employee.FullName;
            }

            if (employee != null)
            {
                //если нажал на сотрудника
                if (projectID == null)
                {
                    return View(_tsHoursRecordService.Get(records => records
                        .Where(x => x.RecordDate.Value.Year == year &&
                                    x.RecordDate.Value.Month == month && x.RecordStatus == TSRecordStatus.Approving)
                        .ToList()).Where(x => x.Project.ApproveHoursEmployeeID == employeeID).ToList());
                } //нажал на проект
                else
                    return View(_tsHoursRecordService.Get(records => records
                        .Where(x => x.ProjectID == projectID && x.RecordDate.Value.Year == year &&
                                    x.RecordDate.Value.Month == month && x.RecordStatus == TSRecordStatus.Approving)
                        .ToList()).Where(x => x.Project.ApproveHoursEmployeeID == employeeID).ToList());
            }
            else
            {
                return StatusCode(StatusCodes.Status400BadRequest);
            }
        }

        [HttpGet]
        [OperationActionFilter(nameof(Operation.TSApproveHoursReportViewForManagedEmployees))]
        public ActionResult TSApproveHoursReport(int? departmentID, int? year, int? month)
        {
            var selectedMonth = month ?? DateTime.Today.Month;
            var selectedYear = year ?? DateTime.Today.Year;

            ViewBag.Months = new SelectList(Enumerable.Range(0, 13).Select(x =>
                new SelectListItem()
                {
                    Text = (x != 0) ? CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(x) + " (" + x + ")" : "-не выбрано-",
                    Value = (x != 0) ? x.ToString() : "",
                }), "Value", "Text", selectedMonth);

            SelectList yearsSelectList = new SelectList(Enumerable.Range(GlobalVariables.BudgetLimitStartYear, DateTime.Today.Year - GlobalVariables.BudgetLimitStartYear + 10).Select(x =>
                new SelectListItem()
                {
                    Text = (x != GlobalVariables.BudgetLimitStartYear) ? x.ToString() : "-не выбрано-",
                    Value = (x != GlobalVariables.BudgetLimitStartYear) ? x.ToString() : "",
                }), "Value", "Text", selectedYear);

            ViewBag.Years = yearsSelectList;
            var user = _applicationUserService.GetUser();

            IList<Department> departmentSelectList = null;

            if (_applicationUserService.HasAccess(Operation.TSApproveHoursReportView))
                departmentSelectList = _departmentService.Get(departments => departments.Where(d => d.IsFinancialCentre).OrderBy(d => d.ShortName).ToList());
            else if (_applicationUserService.HasAccess(Operation.TSApproveHoursReportViewForManagedEmployees) &&
                      !_applicationUserService.HasAccess(Operation.TSApproveHoursReportView))
                departmentSelectList = _departmentService.Get(departments =>
                        departments.Where(d => d.IsFinancialCentre).OrderBy(d => d.ShortName).ToList())
                    .Where(x => _applicationUserService.IsDepartmentManager(x.ID) == true).ToList();

            var selectedDepartment = departmentSelectList?.Where(d => d.ID == departmentID).FirstOrDefault();

            if (selectedDepartment == null)
            {
                selectedDepartment = departmentSelectList.FirstOrDefault();
                if (selectedDepartment != null)
                {
                    if (!(departmentID == selectedDepartment.ID
                          && year == selectedYear && month == selectedMonth))
                    {
                        return RedirectToAction("TSApproveHoursReport", new { departmentID = selectedDepartment.ID, year = selectedYear, month = selectedMonth });
                    }
                    else
                    {
                        return StatusCode(StatusCodes.Status400BadRequest);
                    }
                }
                else
                {
                    return StatusCode(StatusCodes.Status403Forbidden);
                }
            }

            ViewBag.DepartmentID = new SelectList(departmentSelectList.ToList(),
                    "ID", "FullName", selectedDepartment.ID);

            ViewBag.SelectedMonth = selectedMonth;
            ViewBag.SelectedYear = selectedYear;
            ViewBag.SelectedDepartment = selectedDepartment?.ID;

            DateTime periodStartDate = new DateTime(selectedYear, selectedMonth, 1);
            DateTime periodEndDate = periodStartDate.LastDayOfMonth();

            if (departmentID.HasValue && year.HasValue && month.HasValue)
            {
                var department = _departmentService.GetById(departmentID.Value);
                department.EmployeesInDepartment = _employeeService.GetEmployeesInDepartment(departmentID.Value, true)
                    .OrderBy(x => x.Department.ShortName).GroupBy(x => x.Department.ShortName)
                    .SelectMany(x => x).ToList();

                var employeesViewModel = new List<TSHoursRecordTSApproveHoursReportEmployeeViewModel>();

                //Сделана оптимизация для того, чтобы к БД выполнялся только один запрос, а не количество запросов, равное количеству сотрудников. Суммирование трудозатрат выполняется в памяти.
                List<TSHoursRecord> recordList = _tsHoursRecordService.Get(records => records.Where(x =>
                    x.RecordDate >= periodStartDate && x.RecordDate <= periodEndDate && x.RecordStatus == TSRecordStatus.Approving).ToList()).ToList();

                foreach (var employee in department.EmployeesInDepartment)
                {
                    foreach (var approvingRecords in recordList.Where(r => r.Project.ApproveHoursEmployeeID == employee.ID)
                        .ToList().OrderBy(r => r.Project.ShortName).GroupBy(r => r.Project.ShortName).Select(y => y))
                    {
                        double approvingHours = approvingRecords.Select(c => c.Hours ?? 0)
                            .DefaultIfEmpty()
                            .Sum(h => h);

                        employeesViewModel.Add(new TSHoursRecordTSApproveHoursReportEmployeeViewModel()
                        {
                            ID = employee.ID,
                            FullName = employee.FullName,
                            ProjectId = approvingRecords.First().Project.ID,
                            ProjectShortName = approvingRecords.First().Project.ShortName,
                            DepartmentID = employee.DepartmentID.Value,
                            DepartmentShortName = employee.Department.ShortName,
                            DepartmentTitle = employee.Department.Title,
                            DepartmentIsFinancialCentre = employee.Department.IsFinancialCentre,
                            ApprovingHours = approvingHours
                        });
                    }
                }
                var departmentViewModel = new TSHoursRecordTSApproveHoursReportDepartmentViewModel()
                {
                    Department = department,
                    Employees = employeesViewModel
                };
                return View(departmentViewModel);
            }

            return View();
        }

        #endregion
    }

}