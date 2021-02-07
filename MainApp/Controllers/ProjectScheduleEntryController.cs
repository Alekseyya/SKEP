using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using Core.BL.Interfaces;
using Core.Helpers;
using Core.Models;
using Core.Models.RBAC;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using MainApp.RBAC.Attributes;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;




using X.PagedList;

namespace MainApp.Controllers
{
    public class ProjectScheduleEntryController : Controller
    {
        private readonly IProjectScheduleEntryService _projectScheduleEntryService;
        private readonly IProjectScheduleEntryTypeService _projectScheduleEntryTypeService;
        private readonly IUserService _userService;
        private readonly IProjectService _projectService;
        private readonly IProductionCalendarService _productionCalendarService;
        private readonly IProjectTypeService _projectTypeService;
        private readonly IApplicationUserService _applicationUserService;
        private readonly IServiceService _serviceService;
        private readonly int _pageSize = 10;

        public ProjectScheduleEntryController(IProjectScheduleEntryService projectScheduleEntryService,
            IProjectScheduleEntryTypeService projectScheduleEntryTypeService,
            IUserService userService,
            IProjectService projectService,
            IProductionCalendarService productionCalendarService, IProjectTypeService projectTypeService, IApplicationUserService applicationUserService, IServiceService serviceService)
        {
            _projectScheduleEntryService = projectScheduleEntryService;
            _projectScheduleEntryTypeService = projectScheduleEntryTypeService;
            _userService = userService;
            _projectService = projectService;
            _productionCalendarService = productionCalendarService;
            _projectTypeService = projectTypeService;
            _applicationUserService = applicationUserService;
            _serviceService = serviceService;
        }

        [HttpGet]
        [OperationActionFilter(nameof(Operation.ProjectScheduleEntryView))]
        public ActionResult Index(int? page)
        {
            page = page.HasValue ? page : 1;
            var projectScheduleEntryList = _projectScheduleEntryService.Get(pseList => pseList
                .OrderBy(pse => pse.DueDate)
                .Skip((page.Value - 1) * _pageSize)
                .Take(_pageSize)
                .Include(pse => pse.Project)
                .ToList());
            int countItems = _projectScheduleEntryService.GetCount();
            var pageList = new StaticPagedList<ProjectScheduleEntry>(projectScheduleEntryList, page.Value, _pageSize, countItems);

            return View(pageList);
        }

        [OperationActionFilter(nameof(Operation.ProjectListView))]
        public ActionResult AllProjectScheduleDueDateStatus(int? projectTypeID, string searchString)
        {
            var user = _applicationUserService.GetUser();
            int userEmployeeID = _applicationUserService.GetEmployeeID();

            ViewBag.CurrentFilter = searchString;

            ViewBag.SearchProjects = new SelectList(_projectService.GetAll(null, null, null, ProjectStatus.All, null), "ID", "ShortName");

            var projectIds = _projectService.GetAll(null, null, searchString, ProjectStatus.All, null).Select(x => x.ID).ToList();

            ViewBag.ProjectTypeID = _projectTypeService.Get(x => x.ToList().OrderBy(pt => pt.FullName).ToList());
            ViewBag.CurrentProjectTypeID = projectTypeID;

            ViewBag.TodayPlus3WorkingDays = _productionCalendarService.AddWorkingDaysToDate(DateTime.Today, 3);

            IList<ProjectScheduleEntry> projectScheduleEntryList;

            if (projectTypeID != null && projectTypeID.HasValue == true)
            {
                ViewBag.CurrentProjectTypeID = projectTypeID.Value;

                projectScheduleEntryList = _projectScheduleEntryService.GetAllForUser(user)
                    .Where(pse => pse.ProjectScheduleEntryType != null && projectIds.Contains(pse.ProjectID))
                    .OrderBy(pse => pse.ProjectScheduleEntryType.WBSCode)
                    .Where(pse => pse.Project != null && pse.Project.ProjectTypeID == projectTypeID)
                    .ToList();

                IList<ProjectScheduleEntryType> projectScheduleEntryTypeList = _projectScheduleEntryTypeService.Get(psetList => psetList
                   .Where(pset => pset.ProjectTypeID == projectTypeID)
                   .ToList());

                if (projectScheduleEntryTypeList != null && projectScheduleEntryTypeList.Count() != 0)
                {
                    foreach (var projectScheduleEntryType in projectScheduleEntryTypeList)
                    {
                        var projectScheduleEntry = new ProjectScheduleEntry()
                        {
                            ProjectScheduleEntryTypeID = projectScheduleEntryType.ID,
                            ProjectScheduleEntryType = projectScheduleEntryType,
                            Title = projectScheduleEntryType.Title
                        };

                        projectScheduleEntryList.Add(projectScheduleEntry);
                    }
                }
            }
            else
            {
                projectScheduleEntryList = _projectScheduleEntryService.GetAllForUser(user)
                .Where(pse => pse.ProjectScheduleEntryType != null && projectIds.Contains(pse.ProjectID))
                .ToList();
            }

            return View(projectScheduleEntryList.OrderBy(pse => pse.ProjectScheduleEntryType.WBSCode));
        }

        [HttpGet]
        [OperationActionFilter(nameof(Operation.ProjectListView))]
        public FileContentResult AllProjectScheduleDueDateStatusToExcel(int? projectTypeID, string searchString)
        {
            byte[] binData = null;
            DataTable dataTable = new DataTable();

            dataTable.Columns.Add("RowNum", typeof(string)).Caption = "№";
            dataTable.Columns["RowNum"].ExtendedProperties["Width"] = (double)5;
            dataTable.Columns.Add("ProjectTitle", typeof(string)).Caption = "Проект";
            dataTable.Columns["ProjectTitle"].ExtendedProperties["Width"] = (double)30;
            dataTable.Columns.Add("ProjectEmployeeCAM", typeof(string)).Caption = "КАМ";
            dataTable.Columns["ProjectEmployeeCAM"].ExtendedProperties["Width"] = (double)17;
            dataTable.Columns.Add("ProjectEmployeePM", typeof(string)).Caption = "РП";
            dataTable.Columns["ProjectEmployeePM"].ExtendedProperties["Width"] = (double)17;
            dataTable.Columns.Add("ProjectDepartment", typeof(string)).Caption = "Исполнитель";
            dataTable.Columns["ProjectDepartment"].ExtendedProperties["Width"] = (double)14;

            ViewBag.CurrentFilter = searchString;
            var projectIds = _projectService.GetAll(null, null, searchString, ProjectStatus.All, null).Select(x => x.ID).ToList();

            var user = _applicationUserService.GetUser();
            int userEmployeeID = _applicationUserService.GetEmployeeID();

            IList<ProjectScheduleEntry> projectScheduleEntryList;

            if (projectTypeID != null && projectTypeID.HasValue == true)
            {
                ViewBag.CurrentProjectTypeID = projectTypeID.Value;

                projectScheduleEntryList = _projectScheduleEntryService.GetAllForUser(user)
                    .Where(pse => pse.ProjectScheduleEntryType != null && projectIds.Contains(pse.ProjectID))
                    .OrderBy(pse => pse.ProjectScheduleEntryType.WBSCode)
                    .Where(pse => pse.Project != null && pse.Project.ProjectTypeID == projectTypeID)
                    .ToList();

                IList<ProjectScheduleEntryType> projectScheduleEntryTypeList = _projectScheduleEntryTypeService.Get(psetList => psetList
                   .Where(pset => pset.ProjectTypeID == projectTypeID)
                   .ToList());

                if (projectScheduleEntryTypeList != null && projectScheduleEntryTypeList.Count() != 0)
                {
                    foreach (var projectScheduleEntryType in projectScheduleEntryTypeList)
                    {
                        var projectScheduleEntry = new ProjectScheduleEntry()
                        {
                            ProjectScheduleEntryTypeID = projectScheduleEntryType.ID,
                            ProjectScheduleEntryType = projectScheduleEntryType,
                            Title = projectScheduleEntryType.Title
                        };

                        projectScheduleEntryList.Add(projectScheduleEntry);
                    }
                }
            }
            else
            {
                projectScheduleEntryList = _projectScheduleEntryService.GetAllForUser(user)
                // .Where(pse => pse.ProjectScheduleEntryType != null)
                .Where(pse => pse.ProjectScheduleEntryType != null && projectIds.Contains(pse.ProjectID))
                .ToList();
            }

            int colNum = 1;
            foreach (var group in projectScheduleEntryList.OrderBy(x => x.ProjectScheduleEntryType.WBSCode).GroupBy(x => x.ProjectScheduleEntryType.WBSCode))
            {
                dataTable.Columns.Add("ProjectScheduleEntry_DueDate_" + group.First().ProjectScheduleEntryTypeID.ToString(), typeof(ExcelCell)).Caption = "план";
                dataTable.Columns["ProjectScheduleEntry_DueDate_" + group.First().ProjectScheduleEntryTypeID.ToString()].ExtendedProperties["Width"] = (double)11;
                dataTable.Columns["ProjectScheduleEntry_DueDate_" + group.First().ProjectScheduleEntryTypeID.ToString()].ExtendedProperties["SubCaption1"] = group.First().ProjectScheduleEntryType.Title;
                dataTable.Columns["ProjectScheduleEntry_DueDate_" + group.First().ProjectScheduleEntryTypeID.ToString()].ExtendedProperties["SubCaptionColumnSpan1"] = 2;

                dataTable.Columns.Add("ProjectScheduleEntry_DateCompleted_" + group.First().ProjectScheduleEntryTypeID.ToString(), typeof(DateTime)).Caption = "факт";
                dataTable.Columns["ProjectScheduleEntry_DateCompleted_" + group.First().ProjectScheduleEntryTypeID.ToString()].ExtendedProperties["Width"] = (double)11;
                dataTable.Columns["ProjectScheduleEntry_DateCompleted_" + group.First().ProjectScheduleEntryTypeID.ToString()].ExtendedProperties["SubCaption1"] = "";

                colNum++;
            }

            DateTime todayPlus3WorkingDays = _productionCalendarService.AddWorkingDaysToDate(DateTime.Today, 3);

            int rowNum = 1;
            foreach (var group in projectScheduleEntryList.Where(x => x.Project != null).GroupBy(x => x.Project.ShortName))
            {
                dataTable.Rows.Add(
                    rowNum,
                    group.FirstOrDefault().Project.ShortName,
                    (group.First().Project.EmployeeCAMID != null) ? group.First().Project.EmployeeCAM.FullName : "",
                    (group.First().Project.EmployeePMID != null) ? group.First().Project.EmployeePM.FullName : "",
                    (group.First().Project.ProductionDepartmentID != null) ? group.First().Project.ProductionDepartment.DisplayShortTitle : "");

                foreach (var itemGroup in projectScheduleEntryList.OrderBy(x => x.ProjectScheduleEntryType.WBSCode).GroupBy(x => x.ProjectScheduleEntryType.WBSCode))
                {
                    var projectScheduleEntry = group.Where(pse => pse.ProjectScheduleEntryTypeID == itemGroup.First().ProjectScheduleEntryTypeID).FirstOrDefault();

                    if (projectScheduleEntry != null)
                    {
                        if (projectScheduleEntry.DueDate != null)
                        {
                            ExcelCell excelCell = new ExcelCell()
                            {
                                Value = projectScheduleEntry.DueDate.Value,
                                Style = new ExcelCellStyle() { DataFormat = ExcelCellStyle.CellValueDataFormat.Date }
                            };

                            if (projectScheduleEntry.DateCompleted == null)
                            {
                                if (projectScheduleEntry.DueDate < DateTime.Today)
                                {
                                    excelCell.Style.FontBold = true;
                                    excelCell.Style.FillColor = ExcelCellStyle.CellFillColor.Red;
                                }
                                else if (projectScheduleEntry.DueDate <= todayPlus3WorkingDays)
                                {
                                    excelCell.Style.FontBold = true;
                                    excelCell.Style.FillColor = ExcelCellStyle.CellFillColor.Yellow;
                                }
                            }

                            dataTable.Rows[dataTable.Rows.Count - 1]["ProjectScheduleEntry_DueDate_" + projectScheduleEntry.ProjectScheduleEntryTypeID.ToString()] = excelCell;
                        }

                        if (projectScheduleEntry.DateCompleted != null)
                        {
                            dataTable.Rows[dataTable.Rows.Count - 1]["ProjectScheduleEntry_DateCompleted_" + projectScheduleEntry.ProjectScheduleEntryTypeID.ToString()] = projectScheduleEntry.DateCompleted.Value;
                        }
                    }
                }

                rowNum++;
            }

            using (MemoryStream stream = new MemoryStream())
            {
                using (SpreadsheetDocument doc = SpreadsheetDocument.Create(stream, SpreadsheetDocumentType.Workbook))
                {
                    WorkbookPart workbookPart = ExcelHelper.CreateWorkbookPart(doc, "Отчет");

                    WorksheetPart rId1 = ExcelHelper.CreateWorksheetPartAndImportDataTable(workbookPart, "rId1", 1, 1, (uint)dataTable.Columns.Count,
                        "Статус завершения вех", dataTable, 3, 1);

                    doc.WorkbookPart.Workbook.Save();
                }

                stream.Position = 0;
                BinaryReader b = new BinaryReader(stream);
                binData = b.ReadBytes((int)stream.Length);
            }

            return File(binData, ExcelHelper.ExcelContentType, "AllProjectScheduleDueDateStatus" + DateTime.Now.ToString("ddMMyyHHmmss") + ".xlsx");
        }

        [HttpGet]
        [AProjectScheduleEntryCreateUpdate]
        public ActionResult Create(int? projectid)
        {
            ViewBag.Projects = new SelectList(_projectService.GetAll(null, null, null, ProjectStatus.All, null).Where(p => p.ID == projectid || projectid == null), "ID", "ShortName");
            ViewBag.AllowDuplicateScheduleEntry = false;
            var projectScheduleEntry = new ProjectScheduleEntry();
            if (projectid.HasValue)
                projectScheduleEntry.ProjectID = projectid.Value;
            // SetViewBag(null);

            ViewBag.ShortNameFromList = true;
            ViewBag.ProjectId = projectid;

            if (projectid != null)
            {
                var project = _projectService.GetById(projectid.Value);

                
                ViewBag.ProjectScheduleEntryTypeID = new SelectList(_projectScheduleEntryTypeService.Get(psetList => psetList
                    // .Where(pset => projectScheduleEntryTypeIds.All(psetId => psetId.Value != pset.ID) && (pset.ProjectTypeID == project.ProjectTypeID || pset.ProjectTypeID == null)).ToList()
                    .Where(pset => pset.ProjectTypeID == project.ProjectTypeID || pset.ProjectTypeID == null).ToList()
                    .OrderBy(pset => pset.WBSCode).ToList()), "ID", "WBSCodeName");
            }
            else
            {
                ViewBag.ProjectScheduleEntryTypeID = new SelectList(_projectScheduleEntryTypeService.Get(psetList => psetList.OrderBy(pset => pset.WBSCode).ToList()), "ID", "WBSCodeName");
            }
            return View(projectScheduleEntry);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [OperationActionFilter(nameof(Operation.ProjectListView))]
        public ActionResult Create(ProjectScheduleEntry projectScheduleEntry, string modelTitle)
        {
            string shortNameFromList = "ShortNameFromList";
            ModelState.Clear();

            var shortNameEditMode = Request.Query["shortNameEditMode"].ToString();
            bool isShortNameFromList = shortNameEditMode.Equals(shortNameFromList, StringComparison.OrdinalIgnoreCase);

            string allowDuplicateScheduleEntryValue = Request.Query["AllowDuplicateScheduleEntry"].ToString();
            bool allowDuplicateScheduleEntry = string.IsNullOrEmpty(allowDuplicateScheduleEntryValue) ? false : allowDuplicateScheduleEntryValue.Contains("true");
            if (isShortNameFromList)
            {
                if (projectScheduleEntry.ProjectScheduleEntryTypeID.HasValue)
                    projectScheduleEntry.Title = _projectScheduleEntryTypeService.GetById(projectScheduleEntry.ProjectScheduleEntryTypeID.Value).Title;
                else
                    ModelState.AddModelError("ProjectScheduleEntryTypeID", "Требуется поле Значение из справочника.");

                if (projectScheduleEntry.ProjectScheduleEntryTypeID.HasValue)
                {
                    var result = _projectScheduleEntryService.Get(entries => entries
                    .Where(pse => pse.ProjectScheduleEntryTypeID.HasValue && pse.ProjectScheduleEntryTypeID.Value == projectScheduleEntry.ProjectScheduleEntryTypeID.Value
                    && pse.ProjectID == projectScheduleEntry.ProjectID).ToList());
                    int count = result.Count();
                    if (count > 0 && !allowDuplicateScheduleEntry)
                    {
                        ModelState.AddModelError("AllowDuplicateScheduleEntry", "Данный тип вехи уже был использован, необходимо разрешить повторное создание вехи.");
                    }
                    else if (count > 0)
                    {
                        int num = count + 1;
                        projectScheduleEntry.Title += $" ({num})";
                    }
                }
                ModelState.Remove("Title");
            }
            else
            {
                projectScheduleEntry.Title = modelTitle;
                ViewBag.ModelTitle = modelTitle;
                if (!string.IsNullOrEmpty(modelTitle))
                {
                    string trimmedValue = modelTitle.Trim();
                    IList<ProjectScheduleEntry> resultEntries = null;
                    resultEntries = _projectScheduleEntryService.Get(entries => entries.Where(entry =>
                    entry.ProjectID == projectScheduleEntry.ProjectID
                    && entry.Title.Equals(trimmedValue, StringComparison.OrdinalIgnoreCase)
                    || (entry.ProjectScheduleEntryType != null && entry.ProjectScheduleEntryType.Title.Equals(trimmedValue, StringComparison.OrdinalIgnoreCase))).ToList());

                    int count = resultEntries.Count();
                    if (count > 0)
                    {
                        ModelState.AddModelError("Title", "Пользователь должен указать уникальное для данного проекта название вехи.");
                    }
                }
            }

            this.TryValidateModel(projectScheduleEntry);
            if (isShortNameFromList)
                ModelState.Remove("Title");

            if (ModelState.IsValid)
            {
                _projectScheduleEntryService.Add(projectScheduleEntry);
                string returnUrl = Url.Action("Details", "Project", new { id = projectScheduleEntry.ProjectID + "#scheduleentries" }).Replace("%23", "#");
                return new RedirectResult(returnUrl);
            }
            
            ViewBag.ShortNameFromList = isShortNameFromList;
            ViewBag.AllowDuplicateScheduleEntry = allowDuplicateScheduleEntry;
            var project = _projectService.GetById(projectScheduleEntry.ProjectID);
            ViewBag.ProjectId = project.ID;

            if (project != null)
            {
                int projectid = project.ID;
                ViewBag.Projects = new SelectList(_projectService.GetAll(null, null, null, ProjectStatus.All, null).Where(p => p.ID == projectid), "ID", "ShortName");

                ViewBag.ProjectScheduleEntryTypeID = new SelectList(_projectScheduleEntryTypeService.Get(psetList => psetList
                    .Where(pset => pset.ProjectTypeID == project.ProjectTypeID || pset.ProjectTypeID == null).ToList()
                    .OrderBy(pset => pset.WBSCode).ToList()), "ID", "WBSCodeName");
            }
            else
            {
                ViewBag.Projects = new SelectList(_projectService.GetAll(null, null, null, ProjectStatus.All, null), "ID", "ShortName", projectScheduleEntry.ProjectID);
                ViewBag.ProjectScheduleEntryTypeID = new SelectList(_projectScheduleEntryTypeService.Get(psetList => psetList.OrderBy(pset => pset.WBSCode).ToList()), "ID", "WBSCodeName", projectScheduleEntry.ProjectScheduleEntryTypeID);
            }
            return View(projectScheduleEntry);
        }

        [HttpGet]
        [AProjectScheduleEntryCreateUpdate]
        public ActionResult Edit(int? id)
        {
            if (!id.HasValue)
                return StatusCode(StatusCodes.Status400BadRequest);

            var projectScheduleEntry = _projectScheduleEntryService.GetById(id.Value);
            if (projectScheduleEntry == null)
                return StatusCode(StatusCodes.Status404NotFound);

            ViewBag.ShortNameFromList = projectScheduleEntry.ProjectScheduleEntryTypeID.HasValue;
            ViewBag.Projects = new SelectList(_projectService.GetAll(null, null, null, ProjectStatus.All, null).Where(p => p.ID == projectScheduleEntry.ProjectID), "ID", "ShortName");
            return View(projectScheduleEntry);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [OperationActionFilter(nameof(Operation.ProjectListView))]
        public ActionResult Edit(ProjectScheduleEntry projectScheduleEntry, string modelTitle)
        {
            var currentUser = _userService.GetUserDataForVersion();
            projectScheduleEntry.Title = modelTitle.Trim();
            ModelState.Clear();
            if (!projectScheduleEntry.ProjectScheduleEntryTypeID.HasValue)
            {
                var searchResults = _projectScheduleEntryService
                    .Get(entries => entries.Where(entry => entry.Title.Equals(projectScheduleEntry.Title, StringComparison.OrdinalIgnoreCase)
                                                           && entry.ID != projectScheduleEntry.ID)
                        .ToList());
                if (searchResults.Count > 0)
                {
                    ModelState.AddModelError("Title", "Пользователь должен указать уникальное для данного проекта название вехи.");
                }
            }
            this.TryValidateModel(projectScheduleEntry);
            if (ModelState.IsValid)
            {
                _projectScheduleEntryService.Update(projectScheduleEntry, currentUser.Item1, currentUser.Item2);
                string returnUrl = Url.Action("Details", "Project", new { id = projectScheduleEntry.ProjectID + "#scheduleentries" }).Replace("%23", "#");
                return new RedirectResult(returnUrl);
            }

            ViewBag.ShortNameFromList = projectScheduleEntry.ProjectScheduleEntryTypeID.HasValue;
            ViewBag.Projects = new SelectList(_projectService.GetAll(null, null, null, ProjectStatus.All, null).Where(p => p.ID == projectScheduleEntry.ProjectID), "ID", "ShortName");
            return View(projectScheduleEntry);
        }

        [HttpGet]
        [AProjectScheduleEntryDetailsView]
        public ActionResult Details(int? id, int? version)
        {
            if (id == null)
                return StatusCode(StatusCodes.Status400BadRequest);

            if (version != null && version.HasValue)
            {
                var projectScheduleEntryVersion = _projectScheduleEntryService.GetVersion(id.Value, version.Value);
                if (projectScheduleEntryVersion == null)
                    return StatusCode(StatusCodes.Status404NotFound);
                projectScheduleEntryVersion.Versions = new List<ProjectScheduleEntry>().AsEnumerable();
                return View(projectScheduleEntryVersion);
            }

            var projectScheduleEntry = _projectScheduleEntryService.GetById(id.Value, true);

            if (projectScheduleEntry == null)
                return StatusCode(StatusCodes.Status404NotFound);

            projectScheduleEntry.Project = _projectService.GetById(projectScheduleEntry.ProjectID);

            return View(projectScheduleEntry);
        }

        [HttpGet]
        [OperationActionFilter(nameof(Operation.ProjectScheduleEntryDelete))]
        public ActionResult Delete(int? id)
        {
            if (id == null)
                return StatusCode(StatusCodes.Status400BadRequest);

            var projectScheduleEntry = _projectScheduleEntryService.GetById(id.Value);

            if (projectScheduleEntry == null)
                return StatusCode(StatusCodes.Status404NotFound);

            projectScheduleEntry.Project = _projectService.GetById(projectScheduleEntry.ProjectID);

            ViewBag.Projects = new SelectList(_projectService.GetAll(null, null, null, ProjectStatus.All, null), "ID", "ShortName");

            return View(projectScheduleEntry);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [OperationActionFilter(nameof(Operation.ProjectScheduleEntryDelete))]
        public ActionResult DeleteConfirmed(int? id)
        {
            if (id == null)
                return StatusCode(StatusCodes.Status400BadRequest);

            var projectScheduleEntry = _projectScheduleEntryService.GetById(id.Value);

            if (projectScheduleEntry == null)
                return StatusCode(StatusCodes.Status404NotFound);
            var user = _userService.GetUserDataForVersion();

            int projectId = projectScheduleEntry.ProjectID;
            var recycleBinInDBRelation = _serviceService.HasRecycleBinInDBRelation(projectScheduleEntry);
            if (recycleBinInDBRelation.hasRelated == false)
            {
                var recycleToRecycleBin = _projectScheduleEntryService.RecycleToRecycleBin(projectScheduleEntry.ID, user.Item1, user.Item2);
                if (!recycleToRecycleBin.toRecycleBin)
                {
                    ViewBag.RecycleBinError =
                        "Невозможно удалить, так как на удаляемый элемент ссылаются другие элементы в системе." +
                        "Сначала необходимо удалить элементы, которые ссылаются на данный элемент. " +
                        recycleToRecycleBin.relatedClassId;
                    return View(projectScheduleEntry);
                }
            }
            else
            {
                ViewBag.RecycleBinError =
                    "Невозможно удалить, так как на удаляемый элемент ссылаются другие элементы в системе." +
                    $"Сначала необходимо удалить элементы, которые ссылаются на данный элемент. {recycleBinInDBRelation.relatedInDBClassId}";
                return View(projectScheduleEntry);
            }

            string returnUrl = Url.Action("Details", "Project", new { id = projectId + "#scheduleentries" }).Replace("%23", "#");
            return new RedirectResult(returnUrl);
            // return RedirectToAction("Details", "Project", new { id = projectScheduleEntry.ProjectID });
        }

        [NonAction]
        private void SetViewBag(ProjectScheduleEntry projectScheduleEntry)
        {
            if (projectScheduleEntry == null)
            {
                // ViewBag.EmployeeID = new SelectList(_employeeService.GetCurrentEmployees(), "ID", "FullName");
                // ViewBag.ProjectID = new SelectList(_projectService.GetAll("", "", "", ProjectStatus.All, null), "ID", "ShortName");
            }
            else
            {
                // ViewBag.EmployeeID = new SelectList(_employeeService.GetCurrentEmployees(), "ID", "FullName", tsAutoHoursRecord?.EmployeeID ?? 1);
                // ViewBag.ProjectID = new SelectList(_projectService.GetAll("", "", "", ProjectStatus.All, null), "ID", "ShortName", tsAutoHoursRecord?.ProjectID ?? 1);
            }
        }

    }
}
