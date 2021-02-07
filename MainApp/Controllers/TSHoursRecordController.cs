using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Text.RegularExpressions;
using AutoMapper;
using Core;
using Core.BL.Interfaces;
using Core.Config;
using Core.Extensions;
using Core.Helpers;
using Core.Models;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using MainApp.App_Start;
using MainApp.Common;
using MainApp.Dto;
using MainApp.RBAC.Attributes;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Options;











namespace MainApp.Controllers
{
    public class TSHoursRecordController : BaseTimesheetController
    {
        private readonly JiraConfig _jiraConfig;

        public TSHoursRecordController(IEmployeeService employeeService, ITSHoursRecordService tsHoursRecordService,
                IProjectService projectService,
                IProjectMembershipService projectMembershipService,
                IUserService userService,
                ITSAutoHoursRecordService autoHoursRecordService,
                IVacationRecordService vacationRecordService,
                IReportingPeriodService reportingPeriodService,
                IDepartmentService departmentService,
                IProductionCalendarService productionCalendarService,
                IEmployeeCategoryService employeeCategoryService, IJiraService jiraService, IOptions<JiraConfig> jiraOptions, IApplicationUserService applicationUserService) : base(employeeService,
            tsHoursRecordService, projectService, projectMembershipService, userService,
            autoHoursRecordService, vacationRecordService, reportingPeriodService, departmentService, productionCalendarService, employeeCategoryService, jiraService, jiraOptions, applicationUserService)
        {
            _jiraConfig = jiraOptions.Value;
        }

        #region Согласование таймшита руководителем проекта

        [HttpGet]
        [ATSHoursRecordPMApproveHours]
        public string GetEmployees(string hoursStartDate, string hoursEndDate)
        {
            if (hoursStartDate == null)
            {
                hoursStartDate = DateTime.Today.AddDays(-(int)DateTime.Today.DayOfWeek + 1).ToShortDateString();
            }

            if (hoursEndDate == null)
            {
                hoursEndDate = Convert.ToDateTime(hoursStartDate).AddDays(7).AddSeconds(-1).ToShortDateString();
            }

            DateTime startDate = DateTime.MinValue;
            DateTime endDate = DateTime.MaxValue;

            if (String.IsNullOrEmpty(hoursStartDate) == false)
            {
                try
                {
                    startDate = Convert.ToDateTime(hoursStartDate).Date;
                }
                catch (Exception)
                {
                    startDate = DateTime.MinValue;
                }
            }

            if (String.IsNullOrEmpty(hoursEndDate) == false)
            {
                try
                {
                    endDate = Convert.ToDateTime(hoursEndDate).Date;
                }
                catch (Exception)
                {
                    endDate = DateTime.MaxValue;
                }
            }

            var employeesList = _employeeService.GetCurrentEmployees(new DateTimeRange(startDate, endDate));

            List<SelectListItem> records = new List<SelectListItem>();

            int currentUserEmployeeID = _userService.GetEmployeeForCurrentUser().ID;

            foreach (Employee employee in employeesList)
            {
                records.Add(new SelectListItem { Value = employee.ID.ToString(), Text = employee.FullName });
            }

            return JsonConvert.SerializeObject(records);
        }


        [NonAction]
        private Dictionary<int, int> ParseApproveAndDeclineRows(string currentRow)
        {
            var dictionaryRows = new Dictionary<int, int>();
            var rows = currentRow.Split(',');
            foreach (var row in rows)
            {
                var tmpRow = row.Split(';');
                dictionaryRows.Add(int.Parse(tmpRow[0]), int.Parse(tmpRow[1]));
            }
            return dictionaryRows;
        }

        [HttpGet]
        [ATSHoursRecordPMApproveHours]
        public ActionResult ApproveHours(string dateStart, string dateEnd, int? projectID, string applyFilter,
                         TSRecordStatus? tsRecordStatus
                                        /*, string approveHours, string declineHours, string rowsSelected, string declineReason*/)
        {
            /*bool isApproveRow = true, isDeclineRow = true;

            if (isApproveRow == false || isDeclineRow == false)
            {
                ViewBag.ChangedData = "Версия записей изменилось, обновление страницы!";
            }
            else
            {
                ViewBag.ChangedData = "";
            }*/

            ViewBag.ChangedData = "";

            //Получиние текущего пользователя
            int employeeId = _userService.GetEmployeeForCurrentUser().ID;

            //Получить проекты, которыми управляет пм
            var listProjectsOfPM = _projectService.Get(x => x.Where(p => p.EmployeePM.ID == employeeId).ToList());

            ViewBag.ProjectsFromDB = listProjectsOfPM;

            if (dateStart != null) { ViewBag.HoursStartDate = dateStart; }
            if (dateEnd != null) { ViewBag.HoursEndDate = dateEnd; }
            ViewBag.ProjectID = projectID;

            if (tsRecordStatus != null)
                ViewBag.CurrentRecordStatus = tsRecordStatus;
            else
                ViewBag.CurrentRecordStatus = TSRecordStatus.Approving;

            var newTSRecordStatus = new List<TSRecordStatus> { TSRecordStatus.Approving, TSRecordStatus.All };
            var arrayStatus = new SelectList(Enum.GetValues(typeof(TSRecordStatus)).Cast<TSRecordStatus>()
                .Where(n => newTSRecordStatus.Contains(n))
                .Select(x => new SelectListItem
                {

                    Text = x == TSRecordStatus.All ? "Все записи" : x.GetAttributeOfType<DisplayAttribute>().Name,
                    Value = (Convert.ToInt32(x)).ToString(),
                    Selected = (x == ViewBag.CurrentRecordStatus)
                }), "Value", "Text");


            ViewBag.ArrayStatus = arrayStatus;

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [ATSHoursRecordPMApproveHours]
        public ActionResult ApproveHoursApproveDecline(string dateStart, string dateEnd, int? projectID, string applyFilter,
                         TSRecordStatus? tsRecordStatus, string approveHours, string declineHours, string rowsSelected, string declineReason)
        {
            bool isApproveRow = true, isDeclineRow = true;

            if (approveHours != null)
            {
                if (rowsSelected != null)
                {
                    var dictionaryRows = ParseApproveAndDeclineRows(rowsSelected);
                    isApproveRow = ApproveRecords(dictionaryRows);
                }
            }

            if (declineHours != null)
            {
                if (rowsSelected != null)
                {
                    var declinedRows = ParseApproveAndDeclineRows(rowsSelected);
                    isDeclineRow = DeclineRecords(declinedRows, declineReason);
                }
            }

            return RedirectToAction("ApproveHours", new
            {
                dateStart = dateStart,
                dateEnd = dateEnd,
                projectID = projectID,
                tsRecordStatus = tsRecordStatus,
                applyFilter = applyFilter
            });
        }


        [HttpGet]
        [ATSHoursRecordPMApproveHours]
        public FileContentResult ExportApproveHoursToExcel(string hoursStartDate, string hoursEndDate, string projectId, TSRecordStatus? tsRecordStatus)
        {

            //Получение пользователя
            int userEmployeeID = _userService.GetEmployeeForCurrentUser().ID;

            int intProjectId;
            DateTime startDate = DateTime.Today;
            DateTime endDate = DateTime.Today;
            DateTime.TryParse(hoursStartDate, out startDate);
            DateTime.TryParse(hoursEndDate, out endDate);
            startDate = startDate == DateTime.MinValue ? DateTime.MinValue : startDate;
            endDate = endDate == DateTime.MinValue ? DateTime.MaxValue : endDate;

            int.TryParse(projectId, out intProjectId);


            var tsHoursRecordStatus = TSRecordStatus.All;
            if (tsRecordStatus != null)
                tsHoursRecordStatus = (TSRecordStatus)tsRecordStatus;

            var recordList = _tsHoursRecordService.GetTSRecordsForApproval(userEmployeeID,
                startDate, endDate, intProjectId, tsHoursRecordStatus);
            var projectsFullName = recordList.Select(x => x.Project.ShortName)
                .GroupBy(x => x).Select(group => group.FirstOrDefault()).OrderBy(x => x).ToList();

            var listEmployeesId = recordList.GroupBy(x => x.EmployeeID)
                .Select(x => x.Key).ToList();
            var listEmployees = _employeeService.GetCurrentEmployees(new DateTimeRange(startDate, endDate)).Where(x => listEmployeesId.Any(y => y.Value == x.ID)).OrderBy(x => x.LastName);

            var projectList = string.Join(",", projectsFullName);
            byte[] binData = null;

            DataTable dataTable = new DataTable();

            dataTable.Columns.Add("EmployeeFullName", typeof(string)).Caption = "Сотрудник";
            dataTable.Columns["EmployeeFullName"].ExtendedProperties["Width"] = (double)40;
            dataTable.Columns.Add("RecordDate", typeof(DateTime)).Caption = "Отчетная дата";
            dataTable.Columns["RecordDate"].ExtendedProperties["Width"] = (double)12;
            dataTable.Columns.Add("Project", typeof(string)).Caption = "Проект";
            dataTable.Columns["Project"].ExtendedProperties["Width"] = (double)40;
            dataTable.Columns.Add("Hours", typeof(double)).Caption = "Трудозатраты (ч)";
            dataTable.Columns["Hours"].ExtendedProperties["Width"] = (double)15;
            dataTable.Columns.Add("Description", typeof(string)).Caption = "Состав работ";
            dataTable.Columns["Description"].ExtendedProperties["Width"] = (double)90;
            dataTable.Columns.Add("RecordStatus", typeof(string)).Caption = "Статус";
            dataTable.Columns["RecordStatus"].ExtendedProperties["Width"] = (double)20;


            //Может быть много сотрудников, для созгласования трудозатрат
            foreach (var employee in listEmployees)
            {
                foreach (var employeeHoursRecord in recordList)
                {
                    if (employeeHoursRecord.EmployeeID == employee.ID)
                        dataTable.Rows.Add(employee.FullName, employeeHoursRecord.RecordDate.Value,
                            employeeHoursRecord.Project.ShortName, employeeHoursRecord.Hours,
                            RPCSHelper.NormalizeAndTrimString(employeeHoursRecord.Description), employeeHoursRecord.RecordStatus.GetAttributeOfType<DisplayAttribute>().Name);
                }
            }

            using (MemoryStream stream = new MemoryStream())
            {
                using (SpreadsheetDocument doc = SpreadsheetDocument.Create(stream, SpreadsheetDocumentType.Workbook))
                {
                    WorkbookPart workbookPart = ExcelHelper.CreateWorkbookPart(doc, "Трудозатраты сотрудников");

                    WorksheetPart rId1 = ExcelHelper.CreateWorksheetPartAndImportDataTable(workbookPart, "rId1", 1, 1, (uint)dataTable.Columns.Count,
                        "Трудозатраты по проектам: " + projectList + ", на даты: " + hoursStartDate + " - " + hoursEndDate + ", со статусом: " + tsRecordStatus.GetAttributeOfType<DisplayAttribute>().Name,
                        dataTable, 3, 1);

                    doc.WorkbookPart.Workbook.Save();
                }

                stream.Position = 0;
                BinaryReader b = new BinaryReader(stream);
                binData = b.ReadBytes((int)stream.Length);
            }
            return File(binData, ExcelHelper.ExcelContentType, "ApproveHours" + DateTime.Now.ToString("ddMMyyHHmmss") + ".xlsx");
        }

        [ATSHoursRecordPMApproveHours]
        public string GetDataForApproval(string hoursStartDate, string hoursEndDate, string projectId, TSRecordStatus? tsRecordStatus)
        {
            int employeeID = _userService.GetEmployeeForCurrentUser().ID;

            var dateTimeHoursStartDate = Convert.ToDateTime(hoursStartDate);
            var dateTimeHoursEndDate = Convert.ToDateTime(hoursEndDate);
            var intProjectId = Convert.ToInt32(projectId);

            var tsHoursRecordStatus = TSRecordStatus.All;
            if (tsRecordStatus != null)
                tsHoursRecordStatus = (TSRecordStatus)tsRecordStatus;

            var recordList = _tsHoursRecordService.GetTSRecordsForApproval(employeeID,
                dateTimeHoursStartDate, dateTimeHoursEndDate, intProjectId, tsHoursRecordStatus).ToList();

            var config = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile(new AutoMapperConfig.GetEmployeesApproveProfile());
            }).CreateMapper();
            var employeeRecords = config.Map<List<TSHoursRecord>, List<TSHoursRecordDTO>>(recordList);
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
                    record.Hyperlink = "<a href=\"" +  _jiraConfig.Issue + jiraProjectName +
                                       "?focusedWorklogId="
                                       + record.ExternalSourceElementID +
                                       "&page=com.atlassian.jira.plugin.system.issuetabpanels%3Aworklog-tabpanel#worklog-"
                                       + record.ExternalSourceElementID + "\" target=\"_blank\" >" +
                                       jiraProjectName + "</a>";
                }
            }


            return JsonConvert.SerializeObject(employeeRecords.OrderBy(x => x.EmployeeID).ThenBy(x => x.RecordDate));
        }

        [HttpPost]
        [ATSHoursRecordPMApproveHours]
        public bool ApproveRecords(Dictionary<int, int> currentRows)
        {
            if (currentRows.Count != 0)
            {
                var tsHoursRecords = _tsHoursRecordService.Get(records => records.Where(x => x.RecordStatus == TSRecordStatus.Approving).ToList())
                    .Where(t => currentRows.Any(x => x.Key == t.ID && x.Value == t.VersionNumber))
                    .Select(c =>
                    {
                        c.RecordStatus = TSRecordStatus.PMApproved;
                        return c;
                    }).ToList();
                if (tsHoursRecords.Count != 0)
                {
                    _tsHoursRecordService.Update(tsHoursRecords);
                    return true;
                }
            }
            return false;
        }

        [HttpPost]
        [ATSHoursRecordPMApproveHours]
        public bool DeclineRecords(Dictionary<int, int> currentRows, string comment)
        {
            var currApplicationUser = _userService.GetUserDataForVersion();
            if (!String.IsNullOrEmpty(comment) && currentRows.Count != 0)
            {
                foreach (var row in currentRows)
                {
                    TSHoursRecord tSHoursRecord = _tsHoursRecordService.GetById(row.Key);

                    if (tSHoursRecord != null && tSHoursRecord.VersionNumber == row.Value)
                    {

                        tSHoursRecord.RecordStatus = TSRecordStatus.Declined;
                        tSHoursRecord.PMComment = comment;

                        _tsHoursRecordService.Update(tSHoursRecord, currApplicationUser.Item1, currApplicationUser.Item2);
                    }
                    else
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        #endregion

    }
}
