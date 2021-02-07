using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.IO;
using System.Linq;
using System.Net;
using AutoMapper;
using Core;
using Core.BL.Interfaces;
using Core.Extensions;
using Core.Helpers;
using Core.Models;
using Core.Models.RBAC;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using MainApp.App_Start;
using MainApp.RBAC.Attributes;
using MainApp.ViewModels.ProjectStatusRecord;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.DependencyInjection;





using X.PagedList;


namespace MainApp.Controllers
{
    public class ProjectStatusRecordController : Controller
    {
        private readonly IProjectTypeService _projectTypeService;
        private readonly IProjectStatusRecordService _projectStatusRecordService;
        private readonly IProjectService _projectService;
        private readonly IProjectScheduleEntryService _projectScheduleEntryService;
        private readonly IUserService _userService;
        private readonly IProjectMembershipService _projectMembershipService;
        private readonly IProductionCalendarService _productionCalendarService;
        private readonly IApplicationUserService _applicationUserService;
        private readonly IProjectStatusRecordEntryService _projectStatusRecordEntryService;
        private readonly IDepartmentService _departmentService;
        private readonly IServiceService _serviceService;
        private int _pageSize { get; set; } = 10;

        public ProjectStatusRecordController(IServiceProvider serviceProvider)
        {

            _projectTypeService = serviceProvider.GetService<IProjectTypeService>();
            _projectStatusRecordService = serviceProvider.GetService<IProjectStatusRecordService>();
            _projectService = serviceProvider.GetService<IProjectService>();
            _projectScheduleEntryService = serviceProvider.GetService<IProjectScheduleEntryService>();
            _userService = serviceProvider.GetService<IUserService>();
            _projectMembershipService = serviceProvider.GetService<IProjectMembershipService>();
            _productionCalendarService = serviceProvider.GetService<IProductionCalendarService>();
            _applicationUserService = serviceProvider.GetService<IApplicationUserService>();
            _projectStatusRecordEntryService = serviceProvider.GetService<IProjectStatusRecordEntryService>();
            _departmentService = serviceProvider.GetService<IDepartmentService>();
            _serviceService = serviceProvider.GetService<IServiceService>();
        }

        [OperationActionFilter(nameof(Operation.ProjectListView))]
        public ActionResult Index()
        {
            ApplicationUser user = _applicationUserService.GetUser();

            var projectStatusRecords = _projectStatusRecordService.GetAllForUserOrderByCreatedDesc(user);
            return View(projectStatusRecords.ToList());
        }

        [HttpGet]
        [OperationActionFilter(nameof(Operation.ProjectListView))]
        public ActionResult AllProjectLastStatus(int? page, string pageSize, int? departmentID, int? projectStatusId, int? projectTypeID, string searchString)
        {
            int countItems = 1;
            if ((!string.IsNullOrEmpty(pageSize) || string.IsNullOrEmpty(pageSize)) && projectStatusId == null)
            {
                int.TryParse(pageSize, out int newPageSize);
                _pageSize = newPageSize == 0 ? _pageSize : newPageSize;
                // countItems = _projectStatusRecordService.GetCount();
            }
            else if ((!string.IsNullOrEmpty(pageSize) || string.IsNullOrEmpty(pageSize)) && projectStatusId != null)
                _pageSize = 1;

            ApplicationUser user = _applicationUserService.GetUser();
            int userEmployeeID = _applicationUserService.GetEmployeeID();
            var currentUserEmployee = _userService.GetEmployeeForCurrentUser();
            var selectedDepartment = _departmentService.GetById(departmentID ?? 0);

            page = page.HasValue ? page : 1;
            ViewBag.PageSize = _pageSize;

            /////
            ViewBag.CurrentFilter = searchString;
            ViewBag.SearchProjects = new SelectList(_projectService.GetAll(null, null, null, ProjectStatus.All, null), "ID", "ShortName");
            var projectIds = _projectService.GetAll(null, null, searchString, ProjectStatus.All, null).Select(x => x.ID).ToList();

            ViewBag.ProjectTypeID = _projectTypeService.Get(types => types.OrderBy(t => t.ShortName).ToList());
            ViewBag.CurrentProjectTypeID = projectTypeID;
            /////
            ViewBag.SetProjectStatusId = projectStatusId != null;

            ViewBag.TodayPlus7WorkingDays = _productionCalendarService.AddWorkingDaysToDate(DateTime.Today, 7);

            IList<Department> departmentSelectList = null;

            if (_applicationUserService.HasAccess(Operation.ProjectCreateUpdate))
                departmentSelectList = _departmentService.Get(departments => departments.Where(d => d.IsFinancialCentre).OrderBy(d => d.ShortName).ToList());
            else if (_applicationUserService.HasAccess(Operation.ProjectMyDepartmentProjectView) && !_applicationUserService.HasAccess(Operation.ProjectCreateUpdate))
                departmentSelectList = _departmentService.Get(departments =>
                    departments.Where(d => d.IsFinancialCentre && (d.DepartmentManagerID == currentUserEmployee.ID || d.DepartmentPAID == currentUserEmployee.ID)).OrderBy(d => d.ShortName).ToList());

            if (selectedDepartment != null)
            {
                ViewBag.DepartmentID = new SelectList(departmentSelectList.ToList(), "ID", "FullName", selectedDepartment.ID);
                ViewBag.SelectedDepartmentID = selectedDepartment?.ID;
            }
            else
                ViewBag.DepartmentID = new SelectList(departmentSelectList.ToList(), "ID", "FullName");

            DateTime todayPlus20WorkingDays = _productionCalendarService.AddWorkingDaysToDate(DateTime.Today, 20);

            IList<ProjectStatusRecord> projestStatusRecords = null;
            IList<ProjectStatusRecordDetailsViewModel> projectLastStatusViewModelList = new List<ProjectStatusRecordDetailsViewModel>();

            if (projectStatusId.HasValue)
            {
                /*
                projestStatusRecords = _projectStatusRecordService.GetAllForUserOrderByCreatedDesc(user)
                .Where(records => projectStatusId.HasValue && records.ID == projectStatusId.Value)
                .Where(records => records.ProjectID.HasValue && projectIds.Contains(records.ProjectID.Value))
                // .Where(records => !projectTypeID.HasValue || projectTypeID.HasValue && records.Project != null && records.Project.ProjectTypeID == projectTypeID)
                .GroupBy(prs => prs.Project.ShortName).Select(p => p.FirstOrDefault()).Skip((page.Value - 1) * _pageSize).Take(_pageSize).ToList();
                */
                var results = _projectStatusRecordService.GetAllForUserOrderByCreatedDesc(user)
                    .Where(records => projectStatusId.HasValue && records.ID == projectStatusId.Value)
                    .Where(records => records.ProjectID.HasValue && projectIds.Contains(records.ProjectID.Value));
                if (projectTypeID.HasValue && departmentID.HasValue)
                    results = results.Where(records => records.Project.ProjectTypeID == projectTypeID && records.Project.DepartmentID == departmentID.Value);
                else if (projectTypeID.HasValue)
                    results = results.Where(records => records.Project.ProjectTypeID == projectTypeID);
                else if (departmentID.HasValue)
                    results = results.Where(records => records.Project.DepartmentID == departmentID);

                results = results.GroupBy(prs => prs.Project.ShortName).Select(p => p.FirstOrDefault());
                countItems = results.Count();
                projestStatusRecords = results.Skip((page.Value - 1) * _pageSize).Take(_pageSize).ToList();
            }
            else
            {
                var results = _projectStatusRecordService.GetAllForUserOrderByCreatedDesc(user)
                    .Where(records => records.ProjectID.HasValue && projectIds.Contains(records.ProjectID.Value));
                /*
                projestStatusRecords = _projectStatusRecordService.GetAllForUserOrderByCreatedDesc(user)
                    .Where(records => records.ProjectID.HasValue && projectIds.Contains(records.ProjectID.Value)).ToList();
                    */
                if (projectTypeID.HasValue && departmentID.HasValue)
                    results = results.Where(records => records.Project.ProjectTypeID == projectTypeID && records.Project.DepartmentID == departmentID.Value);
                else if (projectTypeID.HasValue)
                    results = results.Where(records => records.Project.ProjectTypeID == projectTypeID);
                else if (departmentID.HasValue)
                    results = results.Where(records => records.Project.DepartmentID == departmentID);

                results = results.GroupBy(prs => prs.Project.ShortName).Select(p => p.FirstOrDefault());
                countItems = results.Count();
                // .Where(records => !projectTypeID.HasValue || projectTypeID.HasValue && records.Project != null && records.Project.ProjectTypeID == projectTypeID)
                projestStatusRecords = results.Skip((page.Value - 1) * _pageSize).Take(_pageSize).ToList();
            }

            foreach (var projectStatusRecord in projestStatusRecords)
            {
                projectLastStatusViewModelList.Add(new ProjectStatusRecordDetailsViewModel()
                {
                    ProjectStatusRecord = projectStatusRecord,
                    ProjectMembers = _projectMembershipService.GetActualMembersForProject(projectStatusRecord.ProjectID.Value, new DateTimeRange(DateTime.Now, DateTime.Now))
                        .Where(prm => prm.ProjectRole != null
                                    && (prm.ProjectRole.RoleType == ProjectRoleType.CAM
                                     || prm.ProjectRole.RoleType == ProjectRoleType.TPM
                                     || prm.ProjectRole.RoleType == ProjectRoleType.PM
                                     || prm.ProjectRole.RoleType == ProjectRoleType.Analyst)).ToList(),
                    ProjectScheduleEntryList = _projectScheduleEntryService.Get(pseList => pseList.Where(pse => pse.ProjectID == projectStatusRecord.ProjectID.Value
                        && (pse.ProjectScheduleEntryType != null
                        || pse.IncludeInProjectStatusRecord == true
                        || (pse.DateCompleted != null && projectStatusRecord.ProjectStatusBeginDate != null && projectStatusRecord.ProjectStatusEndDate != null && pse.DateCompleted >= projectStatusRecord.ProjectStatusBeginDate && pse.DateCompleted <= projectStatusRecord.ProjectStatusEndDate)
                        || (pse.DateCompleted == null && pse.ExpectedDueDate != null && pse.ExpectedDueDate < todayPlus20WorkingDays)
                        || (pse.DateCompleted == null && pse.DueDate != null && pse.DueDate < todayPlus20WorkingDays)
                        ))
                        .OrderBy(pse => pse.ExpectedDueDate).ToList())
                });
            }

            var config = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile(new AutoMapperConfig.ProjectStatusRecordAllProjectLastStatus());
            }).CreateMapper();

            var projectStatusRecordCreateProjectStatusViewModel = config.Map<IList<ProjectStatusRecordDetailsViewModel>, IList<ProjectStatusRecordDetailsViewModel>>(projectLastStatusViewModelList);

            var pageList = new StaticPagedList<ProjectStatusRecordDetailsViewModel>(projectStatusRecordCreateProjectStatusViewModel, page.Value, _pageSize, countItems);

            return View(pageList);
        }

        [HttpGet]
        [OperationActionFilter(nameof(Operation.ProjectListView))]
        public FileContentResult AllProjectLastStatusToExcel(int? projectTypeID, int? departmentID, string searchString)
        {
            byte[] binData = null;
            DataTable dataTable = new DataTable();

            dataTable.Columns.Add("RowNum", typeof(ExcelCell)).Caption = "№";
            dataTable.Columns["RowNum"].ExtendedProperties["Width"] = (double)4;


            dataTable.Columns.Add("ProjectShortName", typeof(string)).Caption = ExpressionExtension.GetPropertyName((ProjectStatusRecord x) => x.ProjectID);
            dataTable.Columns["ProjectShortName"].ExtendedProperties["Width"] = (double)30;

            dataTable.Columns.Add(nameof(ProjectStatusRecord.StatusPeriodName), typeof(string)).Caption = ExpressionExtension.GetPropertyName((ProjectStatusRecord x) => x.StatusPeriodName);
            dataTable.Columns[nameof(ProjectStatusRecord.StatusPeriodName)].ExtendedProperties["Width"] = (double)30;

            dataTable.Columns.Add("ProjectInfo", typeof(ExcelCell)).Caption = "Информация о проекте";
            dataTable.Columns["ProjectInfo"].ExtendedProperties["Width"] = (double)55;

            dataTable.Columns.Add(nameof(ProjectScheduleEntry.ContractNum), typeof(string)).Caption = ExpressionExtension.GetPropertyName((ProjectScheduleEntry x) => x.ContractNum);
            dataTable.Columns[nameof(ProjectScheduleEntry.ContractNum)].ExtendedProperties["Width"] = (double)30;

            dataTable.Columns.Add("ProjectScheduleEntryTitle", typeof(string)).Caption = "Веха";
            dataTable.Columns["ProjectScheduleEntryTitle"].ExtendedProperties["Width"] = (double)35;

            dataTable.Columns.Add(nameof(ProjectScheduleEntry.ContractStageNum), typeof(string)).Caption = ExpressionExtension.GetPropertyName((ProjectScheduleEntry x) => x.ContractStageNum);
            dataTable.Columns[nameof(ProjectScheduleEntry.ContractStageNum)].ExtendedProperties["Width"] = (double)30;

            dataTable.Columns.Add(nameof(ProjectScheduleEntry.WorkResult), typeof(string)).Caption = ExpressionExtension.GetPropertyName((ProjectScheduleEntry x) => x.WorkResult);
            dataTable.Columns[nameof(ProjectScheduleEntry.WorkResult)].ExtendedProperties["Width"] = (double)30;

            dataTable.Columns.Add(nameof(ProjectScheduleEntry.Amount), typeof(ExcelCell)).Caption = ExpressionExtension.GetPropertyName((ProjectScheduleEntry x) => x.Amount);
            dataTable.Columns[nameof(ProjectScheduleEntry.Amount)].ExtendedProperties["Width"] = (double)20;

            dataTable.Columns.Add("ProjectScheduleEntryDueDate", typeof(ExcelCell)).Caption = ExpressionExtension.GetPropertyName((ProjectScheduleEntry x) => x.DueDate);
            dataTable.Columns["ProjectScheduleEntryDueDate"].ExtendedProperties["Width"] = (double)11;
            dataTable.Columns.Add("ProjectScheduleEntryExpectedDueDate", typeof(ExcelCell)).Caption = ExpressionExtension.GetPropertyName((ProjectScheduleEntry x) => x.ExpectedDueDate);
            dataTable.Columns["ProjectScheduleEntryExpectedDueDate"].ExtendedProperties["Width"] = (double)11;
            dataTable.Columns.Add("ProjectScheduleEntryDateCompleted", typeof(ExcelCell)).Caption = ExpressionExtension.GetPropertyName((ProjectScheduleEntry x) => x.DateCompleted);
            dataTable.Columns["ProjectScheduleEntryDateCompleted"].ExtendedProperties["Width"] = (double)11;

            dataTable.Columns.Add(nameof(ProjectStatusRecordEntry.ProjectScheduleEntryComments), typeof(string)).Caption = ExpressionExtension.GetPropertyName((ProjectStatusRecordEntry x) => x.ProjectScheduleEntryComments);
            dataTable.Columns[nameof(ProjectStatusRecordEntry.ProjectScheduleEntryComments)].ExtendedProperties["Width"] = (double)26;

            dataTable.Columns.Add(nameof(ProjectStatusRecord.RiskIndicatorFlag), typeof(string)).Caption = "Р";
            dataTable.Columns[nameof(ProjectStatusRecord.RiskIndicatorFlag)].ExtendedProperties["Width"] = (double)4;
            dataTable.Columns.Add(nameof(ProjectStatusRecord.StatusInfoText), typeof(ExcelCell)).Caption = ExpressionExtension.GetPropertyName((ProjectStatusRecord x) => x.StatusInfoText);
            dataTable.Columns[nameof(ProjectStatusRecord.StatusInfoText)].ExtendedProperties["Width"] = (double)26;

            dataTable.Columns.Add(nameof(ProjectStatusRecord.ExternalDependenciesInfo), typeof(ExcelCell)).Caption = ExpressionExtension.GetPropertyName((ProjectStatusRecord x) => x.ExternalDependenciesInfo);
            dataTable.Columns[nameof(ProjectStatusRecord.ExternalDependenciesInfo)].ExtendedProperties["Width"] = (double)26;

            dataTable.Columns.Add("SupervisorComments", typeof(ExcelCell)).Caption = ExpressionExtension.GetPropertyName((ProjectStatusRecord x) => x.SupervisorComments);
            dataTable.Columns["SupervisorComments"].ExtendedProperties["Width"] = (double)26;

            dataTable.Columns.Add(nameof(ProjectStatusRecord.Created), typeof(ExcelCell)).Caption = ExpressionExtension.GetPropertyName((ProjectStatusRecord x) => x.Created);
            dataTable.Columns[nameof(ProjectStatusRecord.Created)].ExtendedProperties["Width"] = (double)11;

            dataTable.Columns.Add(nameof(ProjectStatusRecord.Author), typeof(string)).Caption = ExpressionExtension.GetPropertyName((ProjectStatusRecord x) => x.Author);
            dataTable.Columns[nameof(ProjectStatusRecord.Author)].ExtendedProperties["Width"] = (double)26;

            ApplicationUser user = _applicationUserService.GetUser();
            int userEmployeeID = _applicationUserService.GetEmployeeID();
            List<ProjectStatusRecord> projectStatusRecords = new List<ProjectStatusRecord>();
            List<ProjectMember> projectMembers = new List<ProjectMember>();
            List<ProjectScheduleEntry> projectScheduleEntryList = new List<ProjectScheduleEntry>();

            var projectIds = _projectService.GetAll(null, null, searchString, ProjectStatus.All, null).Select(x => x.ID).ToList();

            var results = _projectStatusRecordService.GetAllForUserOrderByCreatedDesc(user)
                .Where(records => records.ProjectID.HasValue && projectIds.Contains(records.ProjectID.Value));

            if (projectTypeID.HasValue && departmentID.HasValue)
                results = results.Where(records => records.Project.ProjectTypeID == projectTypeID && records.Project.DepartmentID == departmentID.Value);
            else if (projectTypeID.HasValue)
                results = results.Where(records => records.Project.ProjectTypeID == projectTypeID);
            else if (departmentID.HasValue)
                results = results.Where(records => records.Project.DepartmentID == departmentID);

            projectStatusRecords = results.GroupBy(prs => prs.Project.ShortName).Select(p => p.FirstOrDefault()).ToList();
            // projectStatusRecords = _projectStatusRecordService.GetAllForUserOrderByCreatedDesc(user).GroupBy(prs => prs.Project.ShortName).Select(p => p.FirstOrDefault()).ToList();


            DateTime todayPlus20WorkingDays = _productionCalendarService.AddWorkingDaysToDate(DateTime.Today, 20);
            DateTime todayPlus7WorkingDays = _productionCalendarService.AddWorkingDaysToDate(DateTime.Today, 7);

            foreach (var projectStatusRecord in projectStatusRecords)
            {
                projectMembers.AddRange(_projectMembershipService.GetActualMembersForProject(projectStatusRecord.ProjectID.Value, new DateTimeRange(DateTime.Today, DateTime.Today))
                    .Where(prm => prm.ProjectRole != null
                                && (prm.ProjectRole.RoleType == ProjectRoleType.CAM
                                 || prm.ProjectRole.RoleType == ProjectRoleType.TPM
                                 || prm.ProjectRole.RoleType == ProjectRoleType.PM
                                 || prm.ProjectRole.RoleType == ProjectRoleType.Analyst)).ToList());
                projectScheduleEntryList.AddRange(_projectScheduleEntryService.Get(pseList => pseList.Where(pse => pse.ProjectID == projectStatusRecord.ProjectID.Value
                    && (pse.ProjectScheduleEntryType != null
                        || pse.IncludeInProjectStatusRecord == true
                        || (pse.DateCompleted != null && projectStatusRecord.ProjectStatusBeginDate != null && projectStatusRecord.ProjectStatusEndDate != null && pse.DateCompleted >= projectStatusRecord.ProjectStatusBeginDate && pse.DateCompleted <= projectStatusRecord.ProjectStatusEndDate)
                        || (pse.DateCompleted == null && pse.ExpectedDueDate != null && pse.ExpectedDueDate < todayPlus20WorkingDays)
                        || (pse.DateCompleted == null && pse.DueDate != null && pse.DueDate < todayPlus20WorkingDays)
                        ))
                        .OrderBy(pse => pse.ExpectedDueDate).ToList()));
            }

            var rowNumCount = 0;
            foreach (var record in projectStatusRecords)
            {
                rowNumCount++;

                var scheduleEntryList = projectScheduleEntryList.Where(pse => pse.ProjectID == record.ProjectID.Value).OrderBy(pse => pse.ExpectedDueDate).ToList();
                var statusRecordEntryList = _projectStatusRecordEntryService.Get(psreList => psreList.Where(psre => psre.ProjectStatusRecordID == record.ID).ToList());

                string projectInfo = record.Project.Title +
                                ((record.Project.Organisation != null) ? " (" + record.Project.Organisation?.ShortName + ") " : "") + "\n" +
                                CreateMemberInfoForProjectInfoText(record.Project, projectMembers.Where(pr => pr.ProjectID == record.ProjectID.Value).ToList());

                ExcelCell rowNumCell = new ExcelCell()
                {
                    Value = rowNumCount.ToString(),
                    Style = new ExcelCellStyle() { DataFormat = ExcelCellStyle.CellValueDataFormat.ForcedText },
                    RowSpan = ((scheduleEntryList.Count() > 1) ? scheduleEntryList.Count() : 1)
                };

                ExcelCell projectInfoCell = new ExcelCell()
                {
                    Value = projectInfo,
                    Style = new ExcelCellStyle() { DataFormat = ExcelCellStyle.CellValueDataFormat.ForcedText },
                    RowSpan = ((scheduleEntryList.Count() > 1) ? scheduleEntryList.Count() : 1)
                };

                ExcelCell projectProgressCell = new ExcelCell()
                {
                    Value = record.StatusInfoText,
                    Style = new ExcelCellStyle() { DataFormat = ExcelCellStyle.CellValueDataFormat.ForcedText },
                    RowSpan = ((scheduleEntryList.Count() > 1) ? scheduleEntryList.Count() : 1)
                };

                ExcelCell externalDependenciesInfoCell = new ExcelCell()
                {
                    Value = record.ExternalDependenciesInfo,
                    Style = new ExcelCellStyle() { DataFormat = ExcelCellStyle.CellValueDataFormat.ForcedText },
                    RowSpan = ((scheduleEntryList.Count() > 1) ? scheduleEntryList.Count() : 1)
                };

                ExcelCell supervisorCommentsCell = new ExcelCell()
                {
                    Value = record.SupervisorComments,
                    Style = new ExcelCellStyle() { DataFormat = ExcelCellStyle.CellValueDataFormat.ForcedText },
                    RowSpan = ((scheduleEntryList.Count() > 1) ? scheduleEntryList.Count() : 1)
                };

                ExcelCell createdProjectStatusCell = new ExcelCell()
                {
                    Value = record.Created.Value.ToShortDateString(),
                    Style = new ExcelCellStyle() { DataFormat = ExcelCellStyle.CellValueDataFormat.Date },
                    RowSpan = ((scheduleEntryList.Count() > 1) ? scheduleEntryList.Count() : 1)
                };


                dataTable.Rows.Add(
                    rowNumCell,
                    record.Project.ShortName,
                    record.StatusPeriodName + " (" + record.ProjectStatusBeginDate?.ToString("dd.MM") + " - " + record.ProjectStatusEndDate?.ToString("dd.MM") + ")",
                    projectInfoCell,
                    null,
                    "", null, null, null,
                    null,
                    null,
                    null,
                    null,
                    record.RiskIndicatorFlag.GetAttributeOfType<DisplayAttribute>().Name == "Все" ? "-" : record.RiskIndicatorFlag.GetAttributeOfType<DisplayAttribute>().Name.Substring(0, 1),
                    projectProgressCell,
                    externalDependenciesInfoCell,
                    supervisorCommentsCell,
                    createdProjectStatusCell,
                    record.Author);


                bool firstScheduleEntry = true;
                foreach (var entry in scheduleEntryList)
                {
                    var statusRecordEntry = statusRecordEntryList.Where(sre => sre.ProjectScheduleEntryID == entry.ID).FirstOrDefault();

                    ExcelCell dueDateCell = null;
                    ExcelCell expectedDueDateCell = null;
                    ExcelCell dateCompletedCell = null;
                    ExcelCell amountCell = null;
                    ExcelCell createdCell = null;


                    if (entry.Amount != null)
                        amountCell = new ExcelCell()
                        {
                            Value = entry.Amount,
                            Style = new ExcelCellStyle() { DataFormat = ExcelCellStyle.CellValueDataFormat.Decimal }
                        };

                    if (record.Created != null)
                        createdCell = new ExcelCell()
                        {
                            Value = record.Created.Value.ToShortDateString(),
                            Style = new ExcelCellStyle() { DataFormat = ExcelCellStyle.CellValueDataFormat.Date }
                        };

                    if (entry.DueDate != null)
                    {
                        dueDateCell = new ExcelCell()
                        {
                            Value = entry.DueDate.Value,
                            Style = new ExcelCellStyle() { DataFormat = ExcelCellStyle.CellValueDataFormat.Date }
                        };

                        if (entry.DateCompleted == null)
                        {
                            if (entry.DueDate < DateTime.Today)
                            {
                                dueDateCell.Style.FontBold = true;
                                dueDateCell.Style.FillColor = ExcelCellStyle.CellFillColor.Red;
                            }
                            else if (entry.DueDate <= todayPlus7WorkingDays)
                            {
                                dueDateCell.Style.FontBold = true;
                                dueDateCell.Style.FillColor = ExcelCellStyle.CellFillColor.Yellow;
                            }
                        }
                    }
                    else
                    {
                        dueDateCell = null;
                    }

                    if (entry.ExpectedDueDate != null)
                    {
                        expectedDueDateCell = new ExcelCell()
                        {
                            Value = entry.ExpectedDueDate.Value,
                            Style = new ExcelCellStyle() { DataFormat = ExcelCellStyle.CellValueDataFormat.Date }
                        };

                        if (entry.DateCompleted == null)
                        {
                            if (entry.ExpectedDueDate < DateTime.Today)
                            {
                                expectedDueDateCell.Style.FontBold = true;
                                expectedDueDateCell.Style.FillColor = ExcelCellStyle.CellFillColor.Red;
                            }
                            else if (entry.ExpectedDueDate <= todayPlus7WorkingDays)
                            {
                                expectedDueDateCell.Style.FontBold = true;
                                expectedDueDateCell.Style.FillColor = ExcelCellStyle.CellFillColor.Yellow;
                            }
                        }
                    }
                    else
                    {
                        expectedDueDateCell = null;
                    }

                    if (entry.DateCompleted != null)
                    {
                        dateCompletedCell = new ExcelCell()
                        {
                            Value = entry.DateCompleted.Value,
                            Style = new ExcelCellStyle() { DataFormat = ExcelCellStyle.CellValueDataFormat.Date }
                        };
                    }
                    else
                    {
                        dateCompletedCell = null;
                    }

                    if (firstScheduleEntry == false)
                    {
                        dataTable.Rows.Add(
                                null,
                                record.Project.ShortName,
                                record.StatusPeriodName + " (" + record.ProjectStatusBeginDate?.ToString("dd.MM") + " - " + record.ProjectStatusEndDate?.ToString("dd.MM") + ")",
                                null,
                                null,
                                "", null, null, null,
                                null,
                                null,
                                null,
                                null,
                                null,
                                null, null, null);
                    }
                    else
                    {
                        firstScheduleEntry = false;
                    }
                    
                    dataTable.Rows[dataTable.Rows.Count - 1][nameof(ProjectScheduleEntry.ContractNum)] = entry.ContractNum;
                    dataTable.Rows[dataTable.Rows.Count - 1]["ProjectScheduleEntryTitle"] = entry.Title;
                    dataTable.Rows[dataTable.Rows.Count - 1][nameof(ProjectScheduleEntry.ContractStageNum)] = entry.ContractStageNum;
                    dataTable.Rows[dataTable.Rows.Count - 1][nameof(ProjectScheduleEntry.WorkResult)] = entry.WorkResult;
                    dataTable.Rows[dataTable.Rows.Count - 1][nameof(ProjectScheduleEntry.Amount)] = amountCell;
                    dataTable.Rows[dataTable.Rows.Count - 1]["ProjectScheduleEntryDueDate"] = dueDateCell;
                    dataTable.Rows[dataTable.Rows.Count - 1]["ProjectScheduleEntryExpectedDueDate"] = expectedDueDateCell;
                    dataTable.Rows[dataTable.Rows.Count - 1]["ProjectScheduleEntryDateCompleted"] = dateCompletedCell;

                    dataTable.Rows[dataTable.Rows.Count - 1][nameof(ProjectStatusRecordEntry.ProjectScheduleEntryComments)] = statusRecordEntry?.ProjectScheduleEntryComments;

                    dataTable.Rows[dataTable.Rows.Count - 1][nameof(ProjectStatusRecord.RiskIndicatorFlag)] = record.RiskIndicatorFlag.GetAttributeOfType<DisplayAttribute>().Name == "Все" ? "-" : record.RiskIndicatorFlag.GetAttributeOfType<DisplayAttribute>().Name.Substring(0, 1);

                    dataTable.Rows[dataTable.Rows.Count - 1][nameof(ProjectStatusRecord.Created)] = createdCell;
                    dataTable.Rows[dataTable.Rows.Count - 1][nameof(ProjectStatusRecord.Author)] = record.Author;

                }
            }

            using (MemoryStream stream = new MemoryStream())
            {
                using (SpreadsheetDocument doc = SpreadsheetDocument.Create(stream, SpreadsheetDocumentType.Workbook))
                {
                    WorkbookPart workbookPart = ExcelHelper.CreateWorkbookPart(doc, "Отчет");

                    WorksheetPart rId1 = ExcelHelper.CreateWorksheetPartAndImportDataTable(workbookPart, "rId1", 1, 1, (uint)dataTable.Columns.Count,
                        "Статус по всем проектам", dataTable, 3, 1);

                    doc.WorkbookPart.Workbook.Save();
                }

                stream.Position = 0;
                BinaryReader b = new BinaryReader(stream);
                binData = b.ReadBytes((int)stream.Length);
            }

            return File(binData, ExcelHelper.ExcelContentType, "AllProjectLastStatus" + DateTime.Now.ToString("ddMMyyHHmmss") + ".xlsx");
        }

        private string CreateProjectProgressText(ProjectStatusRecord projectStatusRecord)
        {
            var result = string.Empty;
            if (!string.IsNullOrEmpty(projectStatusRecord.StatusText))
                result += ExpressionExtension.GetPropertyName(() => projectStatusRecord.StatusText) + ": " + "\n" + projectStatusRecord.StatusText + "\n";
            if (!string.IsNullOrEmpty(projectStatusRecord.ProblemsText))
                result += ExpressionExtension.GetPropertyName(() => projectStatusRecord.ProblemsText) + ": " + "\n" + projectStatusRecord.ProblemsText + "\n";
            if (!string.IsNullOrEmpty(projectStatusRecord.ProposedSolutionText))
                result += ExpressionExtension.GetPropertyName(() => projectStatusRecord.ProposedSolutionText) + ": " + "\n" + projectStatusRecord.ProposedSolutionText + "\n";
            return result;
        }

        private string CreateProjectSheduleEntriesText(List<ProjectScheduleEntry> projectScheduleEntries)
        {
            if (projectScheduleEntries.Count == 0)
                return "";
            var result = string.Empty;
            foreach (var projectScheduleEntry in projectScheduleEntries)
            {
                if (projectScheduleEntry.DueDate != null
                    || projectScheduleEntry.ExpectedDueDate != null
                    || projectScheduleEntry.DateCompleted != null)
                {
                    result += projectScheduleEntry.Title + " - " +
                              (projectScheduleEntry.DueDate.HasValue ? projectScheduleEntry.DueDate.Value.ToShortDateString() : "")
                              + " / " + (projectScheduleEntry.ExpectedDueDate.HasValue ? projectScheduleEntry.ExpectedDueDate.Value.ToShortDateString() : "")
                              + " / " + (projectScheduleEntry.DateCompleted.HasValue ? projectScheduleEntry.DateCompleted.Value.ToShortDateString() : "") + "\n";
                }
            }
            return result;
        }

        private string CreateMemberInfoForProjectInfoText(Project project, List<ProjectMember> projectMembers)
        {
            var result = string.Empty;

            result += "КАМ: " + ((project.EmployeeCAM != null) ? project.EmployeeCAM.FullName : "") + "\n";
            result += "РП: " + ((project.EmployeePM != null) ? project.EmployeePM.FullName : "") + "\n";

            foreach (var projectMember in projectMembers)
            {
                if (projectMember.Employee != null)
                {
                    if (projectMember.ProjectRole != null && projectMember.ProjectRole.RoleType == ProjectRoleType.Analyst)
                    {
                        result += "Аналитик: " + projectMember.Employee.FullName + "\n";
                    }
                    if (projectMember.ProjectRole != null && projectMember.ProjectRole.RoleType == ProjectRoleType.TPM)
                    {
                        result += "ТРП: " + projectMember.Employee.FullName + "\n";
                    }
                }
            }
            return result;
        }

        [AProjectStatusRecordDetailsView]
        public ActionResult Details(int? id, int? version)
        {
            if (id == null)
            {
                return StatusCode(StatusCodes.Status400BadRequest);
            }
            ProjectStatusRecord projectStatusRecord = _projectStatusRecordService.GetById((int)id);
            if (projectStatusRecord == null)
            {
                return StatusCode(StatusCodes.Status404NotFound);
            }

            ViewBag.TodayPlus7WorkingDays = _productionCalendarService.AddWorkingDaysToDate(DateTime.Today, 7);

            if (version != null && version.HasValue)
            {
                var statusRecordVersion = _projectStatusRecordService.GetVersion(id.Value, version.Value);
                if (statusRecordVersion == null)
                    return StatusCode(StatusCodes.Status404NotFound);
                statusRecordVersion.Versions = new List<ProjectStatusRecord>().AsEnumerable();
                return View(new ProjectStatusRecordDetailsViewModel()
                {
                    ProjectStatusRecord = statusRecordVersion,
                    ProjectMembers = new List<ProjectMember>(),
                    ProjectScheduleEntryList = new List<ProjectScheduleEntry>()
                });
            }

            DateTime todayPlus20WorkingDays = _productionCalendarService.AddWorkingDaysToDate(DateTime.Today, 20);

            var projectStatusRecordDetailsViewModel = new ProjectStatusRecordDetailsViewModel()
            {
                ProjectStatusRecord = projectStatusRecord,
                ProjectMembers = _projectMembershipService.GetActualMembersForProject(projectStatusRecord.ProjectID.Value, new DateTimeRange(DateTime.Now, DateTime.Now))
                        .Where(pr => pr.ProjectRole.RoleType == ProjectRoleType.CAM
                                     || pr.ProjectRole.RoleType == ProjectRoleType.TPM
                                     || pr.ProjectRole.RoleType == ProjectRoleType.PM
                                     || pr.ProjectRole.RoleType == ProjectRoleType.Analyst).ToList(),
                ProjectScheduleEntryList = _projectScheduleEntryService.Get(pseList => pseList.Where(pse => pse.ProjectID == projectStatusRecord.ProjectID.Value
                    && (pse.ProjectScheduleEntryType != null
                    || pse.IncludeInProjectStatusRecord == true
                    || (pse.DateCompleted != null && projectStatusRecord.ProjectStatusBeginDate != null && projectStatusRecord.ProjectStatusEndDate != null && pse.DateCompleted >= projectStatusRecord.ProjectStatusBeginDate && pse.DateCompleted <= projectStatusRecord.ProjectStatusEndDate)
                    || (pse.DateCompleted == null && pse.ExpectedDueDate != null && pse.ExpectedDueDate < todayPlus20WorkingDays)
                    || (pse.DateCompleted == null && pse.DueDate != null && pse.DueDate < todayPlus20WorkingDays)
                    )).OrderBy(pse => pse.ExpectedDueDate).ToList())
            };

            return View(projectStatusRecordDetailsViewModel);
        }

        [AProjectStatusRecordCreateUpdate]
        public ActionResult Create()
        {
            ProjectStatusRecord projectStatusRecord = new ProjectStatusRecord
            {
                StatusPeriodName = "-"
            };

            ViewBag.ProjectID = new SelectList(_projectService.GetAll("", "", "", ProjectStatus.All, null), "ID", "ShortName");
            ViewBag.ArrayStatus = new SelectList(ProjectStatusRiskIndicatorFlag.All.GetCollectionList<ProjectStatusRiskIndicatorFlag>(x => x != ProjectStatusRiskIndicatorFlag.All)
                .Select(x =>
                {
                    return new SelectListItem
                    {
                        Text = x.GetAttributeOfType<DisplayAttribute>().Name,
                        Value = (Convert.ToInt32(x)).ToString()
                    };
                }), "Value", "Text");
            return View(projectStatusRecord);
        }

        [OperationActionFilter(nameof(Operation.ProjectListView))]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(ProjectStatusRecord projectStatusRecord)
        {
            if (projectStatusRecord.ProjectStatusBeginDate != null && projectStatusRecord.ProjectStatusEndDate != null)
            {
                projectStatusRecord.StatusPeriodName = projectStatusRecord.ProjectStatusBeginDate.Value.ToString("dd/MM/yy") + " - " + projectStatusRecord.ProjectStatusEndDate.Value.ToString("dd/MM/yy");
            }
            else
            {
                projectStatusRecord.StatusPeriodName = "";
            }

            if (ModelState.IsValid)
            {
                _projectStatusRecordService.Add(projectStatusRecord);
                return RedirectToAction("Index");
            }

            ViewBag.ProjectID = new SelectList(_projectService.GetAll("", "", "", ProjectStatus.All, null), "ID", "ShortName", projectStatusRecord.ProjectID);
            ViewBag.ArrayStatus = new SelectList(ProjectStatusRiskIndicatorFlag.All.GetCollectionList<ProjectStatusRiskIndicatorFlag>(x => x != ProjectStatusRiskIndicatorFlag.All)
                .Select(x =>
                {
                    return new SelectListItem
                    {
                        Text = x.GetAttributeOfType<DisplayAttribute>().Name,
                        Value = (Convert.ToInt32(x)).ToString()
                    };
                }), "Value", "Text");
            return View(projectStatusRecord);
        }

        [AProjectStatusRecordCreateUpdate]
        public ActionResult Edit(int? id)
        {
            if (id == null)
                return StatusCode(StatusCodes.Status400BadRequest);
            ProjectStatusRecord projectStatusRecord = _projectStatusRecordService.GetById((int)id);

            if (projectStatusRecord == null)
                return StatusCode(StatusCodes.Status404NotFound);
            ViewBag.ArrayStatus = new SelectList(ProjectStatusRiskIndicatorFlag.All.GetCollectionList<ProjectStatusRiskIndicatorFlag>(x => x != ProjectStatusRiskIndicatorFlag.All)
                .Select(x =>
                {
                    return new SelectListItem
                    {
                        Text = x.GetAttributeOfType<DisplayAttribute>().Name,
                        Value = (Convert.ToInt32(x)).ToString()
                    };
                }), "Value", "Text");

            var textListWeeksOfYear = new List<string>();
            textListWeeksOfYear.AddRange(DateTimeExtention.CreateListWeeksWithDates(DateTime.Today.AddYears(-1).Year, 10, 11, 12));
            textListWeeksOfYear.AddRange(DateTimeExtention.CreateListWeeksWithDates(DateTime.Today.Year));
            textListWeeksOfYear.AddRange(DateTimeExtention.CreateListWeeksWithDates(DateTime.Today.AddYears(1).Year, 01, 02, 03));

            ViewBag.StartPeriods = new SelectList(textListWeeksOfYear.Select(x =>
                new SelectListItem()
                {
                    Text = (x != "") ? x : "-не выбрано-",
                    Value = (x != "") ? x.Split(' ')[0] : "",
                }), "Value", "Text");

            projectStatusRecord.ProjectStatusBeginDate = DateTime.Today;
            projectStatusRecord.ProjectStatusEndDate = DateTime.Today;
            projectStatusRecord.ProjectID = projectStatusRecord.ProjectID;

            var config = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile(new AutoMapperConfig.UpdateProjectStatusProfile());
            }).CreateMapper();

            var projectStatusRecordList = new List<ProjectStatusRecordEntryViewModel>();
            foreach (var projectScheduleEntry in _projectScheduleEntryService.GetAll(x => x.ProjectID == (int)projectStatusRecord.ProjectID).OrderBy(dueDate => dueDate.DueDate).ToList())
            {
                var projectStatusRecordEntry = _projectStatusRecordEntryService.Get(x =>
                    x.Where(r =>
                        r.ProjectScheduleEntryID == projectScheduleEntry.ID &&
                        r.ProjectStatusRecordID == (int)projectStatusRecord.ID).ToList()).FirstOrDefault();
                projectStatusRecordList.Add(new ProjectStatusRecordEntryViewModel()
                {
                    ProjectStatusRecordEntry = projectStatusRecordEntry,
                    ProjectScheduleEntry = projectScheduleEntry
                });
            }

            var projectStatusRecordUpdateProjectStatusViewModel = config.Map<ProjectStatusRecordCreateUpdateViewModel, ProjectStatusRecordCreateUpdateViewModel>(
                new ProjectStatusRecordCreateUpdateViewModel()
                {
                    ProjectStatusRecord = projectStatusRecord,
                    ProjectStatusRecordEntryList = projectStatusRecordList,
                    Project = projectStatusRecord.Project
                });

            return View(projectStatusRecordUpdateProjectStatusViewModel);
        }

        [OperationActionFilter(nameof(Operation.ProjectListView))]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(ProjectStatusRecordCreateUpdateViewModel projectStatusRecordViewModel)
        {
            if (_projectStatusRecordService.Get(x => x.Where(row => row.ProjectID == projectStatusRecordViewModel.ProjectStatusRecord.ProjectID
                                                                    && row.StatusPeriodName == projectStatusRecordViewModel.ProjectStatusRecord.StatusPeriodName && row.ID != projectStatusRecordViewModel.ProjectStatusRecord.ID).ToList()).Any())
                ModelState.AddModelError("ProjectStatusRecord.StatusPeriodName", "Отчет по статусу проекта за выбранную отчетную неделю уже создан. Редактирование отчета по статусу доступно на закладке \"Статус отчеты\" карточки проекта.");

            if (ModelState.IsValid)
            {
                var currUser = _userService.GetUserDataForVersion();
                projectStatusRecordViewModel.ProjectStatusRecord.ProjectStatusBeginDate = DateTimeExtention.FirstDateOfWeekISO8601(Convert.ToInt32(projectStatusRecordViewModel.ProjectStatusRecord.StatusPeriodName.Split('-')[1]),
                    Convert.ToInt32(projectStatusRecordViewModel.ProjectStatusRecord.StatusPeriodName.Split('-')[0]));
                projectStatusRecordViewModel.ProjectStatusRecord.ProjectStatusEndDate = DateTimeExtention.LastDateOfWeekISO8601(Convert.ToInt32(projectStatusRecordViewModel.ProjectStatusRecord.StatusPeriodName.Split('-')[1]),
                    Convert.ToInt32(projectStatusRecordViewModel.ProjectStatusRecord.StatusPeriodName.Split('-')[0]));
                var projectStatusRecord = _projectStatusRecordService.Update(projectStatusRecordViewModel.ProjectStatusRecord);
                string returnUrl = Url.Action("Details", "Project", new { id = projectStatusRecordViewModel.ProjectStatusRecord.ProjectID + "#statusrecords" }).Replace("%23", "#");

                if (projectStatusRecordViewModel.ProjectStatusRecordEntryList != null)
                    foreach (var projectStatusRecordEntryViewModel in projectStatusRecordViewModel.ProjectStatusRecordEntryList)
                    {

                        if (!_projectStatusRecordEntryService.Get(x => x.Where(row => row.ProjectStatusRecordID == projectStatusRecord.ID && row.ProjectScheduleEntryID == projectStatusRecordEntryViewModel.ProjectScheduleEntry.ID).ToList()).Any())
                        {
                            _projectStatusRecordEntryService.Add(new ProjectStatusRecordEntry()
                            {
                                ProjectStatusRecordID = projectStatusRecord.ID,
                                ProjectScheduleEntryID = projectStatusRecordEntryViewModel.ProjectScheduleEntry.ID,
                                ExpectedDueDate = projectStatusRecordEntryViewModel.ProjectScheduleEntry.ExpectedDueDate,
                                DateCompleted = projectStatusRecordEntryViewModel.ProjectScheduleEntry.DateCompleted,
                                ProjectScheduleEntryComments = projectStatusRecordEntryViewModel.ProjectStatusRecordEntry.ProjectScheduleEntryComments
                            });
                        }
                        else
                        {
                            var tmpProjectStatusRecordEntry = _projectStatusRecordEntryService.Get(x => x.Where(row => row.ProjectStatusRecordID == projectStatusRecord.ID
                                                                                                                     && row.ProjectScheduleEntryID == projectStatusRecordEntryViewModel.ProjectScheduleEntry.ID).ToList()).FirstOrDefault();
                            if (tmpProjectStatusRecordEntry != null)
                            {
                                tmpProjectStatusRecordEntry.DateCompleted = projectStatusRecordEntryViewModel.ProjectScheduleEntry.DateCompleted;
                                tmpProjectStatusRecordEntry.ProjectScheduleEntryComments = projectStatusRecordEntryViewModel.ProjectStatusRecordEntry.ProjectScheduleEntryComments;
                                _projectStatusRecordEntryService.Update(tmpProjectStatusRecordEntry);
                            }
                        }
                        var tmpProjectScheduleEntry = _projectScheduleEntryService.GetById(projectStatusRecordEntryViewModel.ProjectScheduleEntry.ID);
                        tmpProjectScheduleEntry.ExpectedDueDate = projectStatusRecordEntryViewModel.ProjectScheduleEntry.ExpectedDueDate;
                        tmpProjectScheduleEntry.DateCompleted = projectStatusRecordEntryViewModel.ProjectScheduleEntry.DateCompleted;
                        _projectScheduleEntryService.Update(tmpProjectScheduleEntry, currUser.Item1, currUser.Item2);
                    }

                return new RedirectResult(returnUrl);
            }
            ViewBag.ArrayStatus = new SelectList(ProjectStatusRiskIndicatorFlag.All.GetCollectionList<ProjectStatusRiskIndicatorFlag>(x => x != ProjectStatusRiskIndicatorFlag.All)
                .Select(x =>
                {
                    return new SelectListItem
                    {
                        Text = x.GetAttributeOfType<DisplayAttribute>().Name,
                        Value = (Convert.ToInt32(x)).ToString()
                    };
                }), "Value", "Text");

            var textListWeeksOfYear = new List<string>();
            textListWeeksOfYear.AddRange(DateTimeExtention.CreateListWeeksWithDates(DateTime.Today.AddYears(-1).Year, 10, 11, 12));
            textListWeeksOfYear.AddRange(DateTimeExtention.CreateListWeeksWithDates(DateTime.Today.Year));
            textListWeeksOfYear.AddRange(DateTimeExtention.CreateListWeeksWithDates(DateTime.Today.AddYears(1).Year, 01, 02, 03));

            ViewBag.StartPeriods = new SelectList(textListWeeksOfYear.Select(x =>
                new SelectListItem()
                {
                    Text = (x != "") ? x : "-не выбрано-",
                    Value = (x != "") ? x.Split(' ')[0] : "",
                }), "Value", "Text");
            return View(projectStatusRecordViewModel);
        }

        [HttpGet]
        [AProjectStatusRecordCreateUpdate]
        public ActionResult CreateProjectStatus(int? projectid)
        {
            if (projectid == null)
            {
                return StatusCode(StatusCodes.Status400BadRequest);
            }

            ViewBag.ArrayStatus = new SelectList(ProjectStatusRiskIndicatorFlag.All.GetCollectionList<ProjectStatusRiskIndicatorFlag>(x => x != ProjectStatusRiskIndicatorFlag.All)
                .Select(x =>
                {
                    return new SelectListItem
                    {
                        Text = x.GetAttributeOfType<DisplayAttribute>().Name,
                        Value = (Convert.ToInt32(x)).ToString()
                    };
                }), "Value", "Text");

            var config = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile(new AutoMapperConfig.CreateProjectStatusProfile());
            }).CreateMapper();

            var textListWeeksOfYear = new List<string>();
            textListWeeksOfYear.AddRange(DateTimeExtention.CreateListWeeksWithDates(DateTime.Today.AddYears(-1).Year, 10, 11, 12));
            textListWeeksOfYear.AddRange(DateTimeExtention.CreateListWeeksWithDates(DateTime.Today.Year));
            textListWeeksOfYear.AddRange(DateTimeExtention.CreateListWeeksWithDates(DateTime.Today.AddYears(1).Year, 01, 02, 03));

            ViewBag.StartPeriods = new SelectList(textListWeeksOfYear.Select(x =>
                new SelectListItem()
                {
                    Text = (x != "") ? x : "-не выбрано-",
                    Value = (x != "") ? x.Split(' ')[0] : "",
                }), "Value", "Text");


            var projectStatusRecordList = new List<ProjectStatusRecordEntryViewModel>();
            foreach (var projectScheduleEntry in _projectScheduleEntryService.GetAll(x => x.ProjectID == (int)projectid).OrderBy(dueDate => dueDate.DueDate).ToList())
            {
                projectStatusRecordList.Add(new ProjectStatusRecordEntryViewModel()
                {
                    ProjectStatusRecordEntry = new ProjectStatusRecordEntry() { ProjectStatusRecordID = 1, ProjectScheduleEntryID = 1 },
                    ProjectScheduleEntry = projectScheduleEntry
                });
            }
            var projectStatusRecordCreateProjectStatusViewModel = config.Map<ProjectStatusRecordCreateUpdateViewModel, ProjectStatusRecordCreateUpdateViewModel>(
                new ProjectStatusRecordCreateUpdateViewModel()
                {
                    ProjectStatusRecord = new ProjectStatusRecord() { ProjectID = (int)projectid, ProjectStatusBeginDate = DateTime.Today, ProjectStatusEndDate = DateTime.Today }, //вот тут надо будет убрать
                    ProjectStatusRecordEntryList = projectStatusRecordList,
                    Project = _projectService.GetById((int)projectid)
                });

            return View(projectStatusRecordCreateProjectStatusViewModel);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CreateProjectStatus(ProjectStatusRecordCreateUpdateViewModel projectStatusUpdateViewModel)
        {
            if (_projectStatusRecordService.Get(x => x.Where(row => row.ProjectID == projectStatusUpdateViewModel.ProjectStatusRecord.ProjectID
                                                                    && row.StatusPeriodName == projectStatusUpdateViewModel.ProjectStatusRecord.StatusPeriodName).ToList()).Any())
                ModelState.AddModelError("ProjectStatusRecord.StatusPeriodName", "Отчет по статусу проекта за выбранную отчетную неделю уже создан. Редактирование отчета по статусу доступно на закладке \"Статус отчеты\" карточки проекта.");

            if (ModelState.IsValid)
            {
                var currUser = _userService.GetUserDataForVersion();
                projectStatusUpdateViewModel.ProjectStatusRecord.Created = DateTime.Now;
                projectStatusUpdateViewModel.ProjectStatusRecord.ProjectStatusBeginDate = DateTimeExtention.FirstDateOfWeekISO8601(Convert.ToInt32(projectStatusUpdateViewModel.ProjectStatusRecord.StatusPeriodName.Split('-')[1]),
                        Convert.ToInt32(projectStatusUpdateViewModel.ProjectStatusRecord.StatusPeriodName.Split('-')[0]));
                projectStatusUpdateViewModel.ProjectStatusRecord.ProjectStatusEndDate = DateTimeExtention.LastDateOfWeekISO8601(Convert.ToInt32(projectStatusUpdateViewModel.ProjectStatusRecord.StatusPeriodName.Split('-')[1]),
                    Convert.ToInt32(projectStatusUpdateViewModel.ProjectStatusRecord.StatusPeriodName.Split('-')[0]));
                var projectStatusRecord = _projectStatusRecordService.Add(projectStatusUpdateViewModel.ProjectStatusRecord);
                if (projectStatusUpdateViewModel.ProjectStatusRecordEntryList != null)
                    foreach (var projectStatusRecordEntryViewModel in projectStatusUpdateViewModel.ProjectStatusRecordEntryList)
                    {
                        _projectStatusRecordEntryService.Add(new ProjectStatusRecordEntry()
                        {
                            ProjectStatusRecordID = projectStatusRecord.ID,
                            ProjectScheduleEntryID = projectStatusRecordEntryViewModel.ProjectScheduleEntry.ID,
                            ExpectedDueDate = projectStatusRecordEntryViewModel.ProjectScheduleEntry.ExpectedDueDate,
                            DateCompleted = projectStatusRecordEntryViewModel.ProjectScheduleEntry.DateCompleted,
                            ProjectScheduleEntryComments = projectStatusRecordEntryViewModel.ProjectStatusRecordEntry.ProjectScheduleEntryComments

                        });
                        var tmpProjectScheduleEntry = _projectScheduleEntryService.GetById(projectStatusRecordEntryViewModel.ProjectScheduleEntry.ID);
                        tmpProjectScheduleEntry.ExpectedDueDate = projectStatusRecordEntryViewModel.ProjectScheduleEntry.ExpectedDueDate;
                        tmpProjectScheduleEntry.DateCompleted = projectStatusRecordEntryViewModel.ProjectScheduleEntry.DateCompleted;
                        _projectScheduleEntryService.Update(tmpProjectScheduleEntry, currUser.Item1, currUser.Item2);
                    }
                string returnUrl = Url.Action("Details", "Project", new { id = projectStatusUpdateViewModel.ProjectStatusRecord.ProjectID + "#statusrecords" }).Replace("%23", "#");
                return new RedirectResult(returnUrl);
            }

            ViewBag.ArrayStatus = new SelectList(ProjectStatusRiskIndicatorFlag.All.GetCollectionList<ProjectStatusRiskIndicatorFlag>(x => x != ProjectStatusRiskIndicatorFlag.All)
                .Select(x =>
                {
                    return new SelectListItem
                    {
                        Text = x.GetAttributeOfType<DisplayAttribute>().Name,
                        Value = (Convert.ToInt32(x)).ToString()
                    };
                }), "Value", "Text");

            var textListWeeksOfYear = new List<string>();
            textListWeeksOfYear.AddRange(DateTimeExtention.CreateListWeeksWithDates(DateTime.Today.AddYears(-1).Year, 10, 11, 12));
            textListWeeksOfYear.AddRange(DateTimeExtention.CreateListWeeksWithDates(DateTime.Today.Year));
            textListWeeksOfYear.AddRange(DateTimeExtention.CreateListWeeksWithDates(DateTime.Today.AddYears(1).Year, 01, 02, 03));

            ViewBag.StartPeriods = new SelectList(textListWeeksOfYear.Select(x =>
                new SelectListItem()
                {
                    Text = (x != "") ? x : "-не выбрано-",
                    Value = (x != "") ? x.Split(' ')[0] : "",
                }), "Value", "Text");

            return View(projectStatusUpdateViewModel);
        }


        [OperationActionFilter(nameof(Operation.ProjectStatusRecordDelete))]
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return StatusCode(StatusCodes.Status400BadRequest);
            }

            ProjectStatusRecord projectStatusRecord = _projectStatusRecordService.GetById((int)id);
            if (projectStatusRecord == null)
            {
                return StatusCode(StatusCodes.Status404NotFound);
            }
            return View(projectStatusRecord);
        }


        [OperationActionFilter(nameof(Operation.ProjectStatusRecordDelete))]
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            var projectStatusRecord = _projectStatusRecordService.GetById(id);
            var user = _userService.GetUserDataForVersion();
            var recycleBinInDBRelation = _serviceService.HasRecycleBinInDBRelation(projectStatusRecord);
            if (recycleBinInDBRelation.hasRelated == false)
            {
                var recycleToRecycleBin = _projectStatusRecordService.RecycleToRecycleBin(projectStatusRecord.ID, user.Item1, user.Item2);
                if (!recycleToRecycleBin.toRecycleBin)
                {
                    ViewBag.RecycleBinError =
                        "Невозможно удалить, так как на удаляемый элемент ссылаются другие элементы в системе." +
                        "Сначала необходимо удалить элементы, которые ссылаются на данный элемент. " +
                        recycleToRecycleBin.relatedClassId;
                    return View(projectStatusRecord);
                }
            }
            else
            {
                ViewBag.RecycleBinError =
                    "Невозможно удалить, так как на удаляемый элемент ссылаются другие элементы в системе." +
                    $"Сначала необходимо удалить элементы, которые ссылаются на данный элемент. {recycleBinInDBRelation.relatedInDBClassId}";
                return View(projectStatusRecord);
            }
            return RedirectToAction("Index");
        }



        protected ActionResult ProjectStatusRecordDetails(ProjectStatusRecordDetailsViewModel projectStatusRecordDetailsViewModel)
        {
            return PartialView(projectStatusRecordDetailsViewModel);
        }
    }
}
