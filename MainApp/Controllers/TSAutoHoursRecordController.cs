using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
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
    public class TSAutoHoursRecordController : Controller
    {
        private readonly IEmployeeService _employeeService;
        private readonly IProjectService _projectService;
        private readonly IUserService _userService;
        private readonly ITSAutoHoursRecordService _tsAutoHoursRecordService;
        private readonly IReportingPeriodService _reportingPeriodService;
        private readonly ITSHoursRecordService _tsHoursRecordService;
        private readonly IProjectMembershipService _projectMembershipService;
        private readonly IProductionCalendarService _productionCalendarService;
        private readonly IApplicationUserService _applicationUserService;
        private readonly IServiceService _serviceService;

        public TSAutoHoursRecordController(IEmployeeService employeeService,
                                           IProjectService projectService,
                                           IUserService userService,
                                           ITSAutoHoursRecordService tsAutoHoursRecordService,
                                           IReportingPeriodService reportingPeriodService,
                                           ITSHoursRecordService tsHoursRecordService,
                                           IProjectMembershipService projectMembershipService,
                                           IProductionCalendarService productionCalendarService, 
                                           IApplicationUserService applicationUserService,
                                           IServiceService serviceService)
        {
            if (employeeService == null)
                throw new ArgumentException(nameof(employeeService));
            if (projectService == null)
                throw new ArgumentException(nameof(projectService));
            if (userService == null)
                throw new ArgumentException(nameof(userService));
            if (tsAutoHoursRecordService == null)
                throw new ArgumentException(nameof(tsAutoHoursRecordService));
            if (reportingPeriodService == null)
                throw new ArgumentException(nameof(reportingPeriodService));
            if (productionCalendarService == null)
                throw new ArgumentException(nameof(productionCalendarService));

            _employeeService = employeeService;
            _projectService = projectService;
            _userService = userService;
            _tsAutoHoursRecordService = tsAutoHoursRecordService;
            _reportingPeriodService = reportingPeriodService;
            _tsHoursRecordService = tsHoursRecordService;
            _projectMembershipService = projectMembershipService;
            _productionCalendarService = productionCalendarService;
            _applicationUserService = applicationUserService;
            _serviceService = serviceService;
        }

        [NonAction]
        private void SetViewBag(TSAutoHoursRecord tsAutoHoursRecord)
        {
            if (tsAutoHoursRecord == null)
            {
                ViewBag.EmployeeID = new SelectList(_employeeService.GetCurrentEmployees(new DateTimeRange(DateTime.Today, DateTime.Today)), "ID", "FullName");
                ViewBag.ProjectID = new SelectList(_projectService.GetAll("", "", "", ProjectStatus.All, null), "ID", "ShortName");
            }
            else
            {
                ViewBag.EmployeeID = new SelectList(_employeeService.GetCurrentEmployees(new DateTimeRange(DateTime.Today, DateTime.Today)), "ID", "FullName", tsAutoHoursRecord?.EmployeeID ?? 1);
                ViewBag.ProjectID = new SelectList(_projectService.GetAll("", "", "", ProjectStatus.All, null), "ID", "ShortName", tsAutoHoursRecord?.ProjectID ?? 1);
            }
        }

        [HttpGet]
        [OperationActionFilter(nameof(Operation.TSAutoHoursRecordView))]
        public ActionResult Index(int? page)
        {
            var tsAutoHoursRecords = _tsAutoHoursRecordService.GetAll().OrderBy(f => f.FullName);
            int pageSize = 10;
            int pageNumber = (page ?? 1);

            return View(tsAutoHoursRecords.ToPagedList(pageNumber, pageSize));
        }

        [HttpGet]
        [OperationActionFilter(nameof(Operation.TSAutoHoursRecordView))]
        public ActionResult Details(int? id, int? version)
        {
            if (id == null)
            {
                return StatusCode(StatusCodes.Status400BadRequest);
            }

            TSAutoHoursRecord tsAutoHoursRecord = _tsAutoHoursRecordService.GetById((int)id);
            if (tsAutoHoursRecord == null)
            {
                return StatusCode(StatusCodes.Status404NotFound);
            }

            if (version != null && version.HasValue)
            {
                var recordVersion = _tsAutoHoursRecordService.GetVersion(id.Value, version.Value);
                if (recordVersion == null)
                    return StatusCode(StatusCodes.Status404NotFound);
                recordVersion.Versions = new List<TSAutoHoursRecord>().AsEnumerable();
                return View(recordVersion);
            }

            tsAutoHoursRecord = _tsAutoHoursRecordService.GetById(id.Value);

            return View(tsAutoHoursRecord);
        }

        [HttpGet]
        [OperationActionFilter(nameof(Operation.TSAutoHoursRecordCreateUpdate))]
        public ActionResult Create()
        {
            SetViewBag(null);
            return View();
        }

        [HttpPost]
        [OperationActionFilter(nameof(Operation.TSAutoHoursRecordCreateUpdate))]
        [ValidateAntiForgeryToken]
        public ActionResult Create(TSAutoHoursRecord tsAutoHoursRecord)
        {
            if (tsAutoHoursRecord.BeginDate > tsAutoHoursRecord.EndDate)
            {
                ModelState.AddModelError("BeginDate", "Дата начала не может быть больше даты конца.");
            }
            var reportingPeriodForBeginDate = _reportingPeriodService.GetAll(x => x.Month == tsAutoHoursRecord.BeginDate.Value.Month && x.Year == tsAutoHoursRecord.BeginDate.Value.Year).FirstOrDefault();
            var reportingPeriodForEndDate = _reportingPeriodService.GetAll(x => x.Month == tsAutoHoursRecord.EndDate.Value.Month && x.Year == tsAutoHoursRecord.EndDate.Value.Year).FirstOrDefault();

            if (reportingPeriodForBeginDate != null && reportingPeriodForBeginDate.NewTSRecordsAllowedUntilDate < DateTime.Now)
                ModelState.AddModelError("BeginDate", "Отчетный период закрыт");
            if (reportingPeriodForEndDate != null && reportingPeriodForEndDate.NewTSRecordsAllowedUntilDate < DateTime.Now)
                ModelState.AddModelError("EndDate", "Отчетный период закрыт");


            if (ModelState.IsValid)
            {
                _tsAutoHoursRecordService.Add(tsAutoHoursRecord);
                return RedirectToAction("Index");
            }

            SetViewBag(null);
            return View(tsAutoHoursRecord);
        }

        [HttpGet]
        [OperationActionFilter(nameof(Operation.TSAutoHoursRecordCreateUpdate))]
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return StatusCode(StatusCodes.Status400BadRequest);
            }

            TSAutoHoursRecord tsAutoHoursRecord = _tsAutoHoursRecordService.GetById((int)id);
            if (tsAutoHoursRecord == null)
            {
                return StatusCode(StatusCodes.Status404NotFound);
            }
            SetViewBag(tsAutoHoursRecord);
            return View(tsAutoHoursRecord);
        }


        [HttpPost]
        [OperationActionFilter(nameof(Operation.TSAutoHoursRecordCreateUpdate))]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(TSAutoHoursRecord tsAutoHoursRecord)
        {

            if (tsAutoHoursRecord.BeginDate > tsAutoHoursRecord.EndDate)
            {
                ModelState.AddModelError("BeginDate", "Дата начала не может быть больше даты конца.");
            }

            var reportingPeriodForBeginDate = _reportingPeriodService.GetAll(x => x.Month == tsAutoHoursRecord.BeginDate.Value.Month && x.Year == tsAutoHoursRecord.BeginDate.Value.Year).FirstOrDefault();
            var reportingPeriodForEndDate = _reportingPeriodService.GetAll(x => x.Month == tsAutoHoursRecord.EndDate.Value.Month && x.Year == tsAutoHoursRecord.EndDate.Value.Year).FirstOrDefault();

            var originalItem = _tsAutoHoursRecordService.GetById(tsAutoHoursRecord.ID);

            if (originalItem.EmployeeID != tsAutoHoursRecord.EmployeeID)
            {
                ModelState.AddModelError("EmployeeID", "Изменение сотрудника в созданной записи автозагрузки запрещено.");
            }

            //Дату начала в закрытый перод менить нельзя
            if (reportingPeriodForBeginDate != null && reportingPeriodForBeginDate.NewTSRecordsAllowedUntilDate < DateTime.Now
                                                    && originalItem.BeginDate != tsAutoHoursRecord.BeginDate)
                ModelState.AddModelError("BeginDate", "Начальную дату нельзя изменить");
            //Дату окончания в закрытый перод менить нельзя
            if (reportingPeriodForEndDate != null && reportingPeriodForEndDate.NewTSRecordsAllowedUntilDate < DateTime.Now
                                                    && originalItem.EndDate != tsAutoHoursRecord.EndDate)
                ModelState.AddModelError("EndDate", "Конечную дату нельзя изменить");


            if ((reportingPeriodForBeginDate != null && reportingPeriodForEndDate != null) &&
                (reportingPeriodForBeginDate.NewTSRecordsAllowedUntilDate < DateTime.Now || reportingPeriodForEndDate.NewTSRecordsAllowedUntilDate < DateTime.Now) &&
                        originalItem.DayHours != tsAutoHoursRecord.DayHours)
                ModelState.AddModelError("DayHours", "Нельзя изменить количество часов в закрытом периоде");


            if (ModelState.IsValid)
            {
                _tsAutoHoursRecordService.Update(tsAutoHoursRecord);
                //TODO добавить в том случае если это нужно администратору ТШ
                //_taskTimesheetProcessing.ProcessTSAutoHoursRecords(currentEmployee, tsAutoHoursRecord);
                return RedirectToAction("Index");
            }
            SetViewBag(tsAutoHoursRecord);
            return View(tsAutoHoursRecord);
        }

        [HttpGet]
        [OperationActionFilter(nameof(Operation.TSAutoHoursRecordDelete))]
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return StatusCode(StatusCodes.Status400BadRequest);
            }

            TSAutoHoursRecord tsAutoHoursRecord = _tsAutoHoursRecordService.GetById(id.Value);
            if (tsAutoHoursRecord == null)
            {
                return StatusCode(StatusCodes.Status404NotFound);
            }
            return View(tsAutoHoursRecord);
        }

        // POST: TSAutoHoursRecord/Delete/5
        [HttpPost, ActionName("Delete")]
        [OperationActionFilter(nameof(Operation.TSAutoHoursRecordDelete))]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            var tsAutoHoursRecord = _tsAutoHoursRecordService.GetById(id);
            var user = _userService.GetUserDataForVersion();
            var isDeleted = true;
            var errorOnParentTSHoursRecord = string.Empty;
            var errorOnTSAutoHoursRecord = string.Empty;
            if (tsAutoHoursRecord != null)
            {
                var reportingPeriodForBeginDate = _reportingPeriodService.GetAll(x => x.Month == tsAutoHoursRecord.BeginDate.Value.Month && x.Year == tsAutoHoursRecord.BeginDate.Value.Year).FirstOrDefault();
                var reportingPeriodForEndDate = _reportingPeriodService.GetAll(x => x.Month == tsAutoHoursRecord.EndDate.Value.Month && x.Year == tsAutoHoursRecord.EndDate.Value.Year).FirstOrDefault();
                if (reportingPeriodForBeginDate != null && reportingPeriodForEndDate != null &&
                    (reportingPeriodForEndDate.NewTSRecordsAllowedUntilDate > DateTime.Now &&
                     reportingPeriodForBeginDate.NewTSRecordsAllowedUntilDate > DateTime.Now))
                {
                    var parentHoursRecords = _tsHoursRecordService.Get(x => x.Where(t => t.ParentTSAutoHoursRecordID == id).ToList());
                    if (parentHoursRecords != null)
                    {
                        foreach (var hoursRecord in parentHoursRecords)
                        {
                            var recycleBinInDBRelationParentTSHoursRecord = _serviceService.HasRecycleBinInDBRelation(hoursRecord);
                            if (recycleBinInDBRelationParentTSHoursRecord.hasRelated == false)
                            {
                                var recycleToRecycleBinParentTSHoursRecords = _tsAutoHoursRecordService.RecycleToRecycleBin(hoursRecord.ID, user.Item1, user.Item2);
                                isDeleted = recycleToRecycleBinParentTSHoursRecords.toRecycleBin;
                                errorOnParentTSHoursRecord = recycleToRecycleBinParentTSHoursRecords.toRecycleBin ? string.Empty : recycleToRecycleBinParentTSHoursRecords.relatedClassId;
                            }
                            else
                                errorOnParentTSHoursRecord = recycleBinInDBRelationParentTSHoursRecord.relatedInDBClassId;
                        }
                    }

                    var recycleBinInDBRelationTSAutoHoursRecord = _serviceService.HasRecycleBinInDBRelation(new TSAutoHoursRecord() { ID = id });
                    if (recycleBinInDBRelationTSAutoHoursRecord.hasRelated == false)
                    {
                        var recycleToRecycleBinTSAutoHoursRecords = _tsAutoHoursRecordService.RecycleToRecycleBin(id, user.Item1, user.Item2);
                        isDeleted = recycleToRecycleBinTSAutoHoursRecords.toRecycleBin;
                        errorOnTSAutoHoursRecord = recycleToRecycleBinTSAutoHoursRecords.toRecycleBin ? string.Empty : recycleToRecycleBinTSAutoHoursRecords.relatedClassId;
                    }
                    else
                        errorOnTSAutoHoursRecord = recycleBinInDBRelationTSAutoHoursRecord.relatedInDBClassId;
                }
            }

            if (!isDeleted)
            {
                if (!string.IsNullOrEmpty(errorOnParentTSHoursRecord))
                    ViewBag.RecycleBinError = "Невозможно удалить, так как на удаляемый элемент ссылаются другие элементы в системе." +
                                          $"Сначала необходимо удалить элементы, которые ссылаются на данный элемент. {errorOnParentTSHoursRecord}";
                else if (!string.IsNullOrEmpty(errorOnTSAutoHoursRecord))
                    ViewBag.RecycleBinError = "Невозможно удалить, так как на удаляемый элемент ссылаются другие элементы в системе." +
                                              $"Сначала необходимо удалить элементы, которые ссылаются на данный элемент. {errorOnTSAutoHoursRecord}";
                return View(tsAutoHoursRecord);
            }
            return RedirectToAction("Index");
        }

        [HttpGet]
        [OperationActionFilter(nameof(Operation.TSAutoHoursRecordView))]
        public ActionResult AutoHours(string year)
        {
            if (String.IsNullOrEmpty(year) == true)
            {
                ViewBag.CurrentYear = DateTime.Now.Year.ToString();
            }
            else
            {
                ViewBag.CurrentYear = year;
            }

            ViewBag.Years = new SelectList(_productionCalendarService.GetAllRecords().Select(x => new { x.Year }).Distinct().OrderBy(x => x.Year).ToList(), "Year", "Year", ViewBag.CurrentYear);

            return View();
        }

        [OperationActionFilter(nameof(Operation.TSAutoHoursRecordView))]
        public string GetAutoHoursData(string year, string projectID)
        {
            ApplicationUser user = _applicationUserService.GetUser();
            int userEmployeeID = _applicationUserService.GetEmployeeID();

            int currentYear = DateTime.Now.Year;
            if (String.IsNullOrEmpty(year) == false)
            {
                currentYear = Convert.ToInt32(year);
            }

            List<Employee> managedEmployees = _employeeService.GetAllManagedEmployees(_applicationUserService.GetUser().ManagedDepartments).ToList();

            DateTime startDate = new DateTime(currentYear, 1, 1);
            DateTime endDate = new DateTime(currentYear, 12, 31);

            List<TSAutoHoursRecord> tsAutoHoursRecords = new List<TSAutoHoursRecord>();
            tsAutoHoursRecords = _tsAutoHoursRecordService.Get(x =>
                x.Include(t => t.Employee).Include(t => t.Project)
                    .Where(t => (t.BeginDate <= endDate && t.EndDate >= startDate)).ToList()
                    .OrderBy(t => t.Employee.FullName).ToList()).ToList();

            var config = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile(new AutoMapperConfig.GetAutoHoursDataProfile());
            }).CreateMapper();
            var tsRecords = config.Map<List<TSAutoHoursRecord>, IList<TSAutoHoursRecordDTO>>(tsAutoHoursRecords);

            return JsonConvert.SerializeObject(tsRecords);
        }

        [HttpGet]
        [OperationActionFilter(nameof(Operation.TSAutoHoursRecordCreateUpdate))]
        public FileContentResult ExportTsAutoHoursRecordToExcel()
        {
            byte[] binData = null;

            var autoHoursRecords = _tsAutoHoursRecordService.GetAll();

            DataTable dataTable = new DataTable();
            dataTable.Columns.Add("FullName", typeof(string)).Caption = "ФИО";
            dataTable.Columns["FullName"].ExtendedProperties["Width"] = (double)40;
            dataTable.Columns.Add("BeginDate", typeof(DateTime)).Caption = "Дата начала действия";
            dataTable.Columns["BeginDate"].ExtendedProperties["Width"] = (double)25;
            dataTable.Columns.Add("EndDate", typeof(DateTime)).Caption = "Дата окончания действия";
            dataTable.Columns["EndDate"].ExtendedProperties["Width"] = (double)25;
            dataTable.Columns.Add("Project", typeof(string)).Caption = "Проект";
            dataTable.Columns["Project"].ExtendedProperties["Width"] = (double)40;
            dataTable.Columns.Add("DayHours", typeof(double)).Caption = "Трудозатраты в день (ч)";
            dataTable.Columns["DayHours"].ExtendedProperties["Width"] = (double)25;

            foreach (var autoHoursRecord in autoHoursRecords)
            {
                dataTable.Rows.Add(autoHoursRecord.Employee.FullName,
                    autoHoursRecord.BeginDate.Value,
                    autoHoursRecord.EndDate.Value,
                    autoHoursRecord.Project.ShortName,
                    autoHoursRecord.DayHours);
            }

            using (MemoryStream stream = new MemoryStream())
            {
                using (SpreadsheetDocument doc = SpreadsheetDocument.Create(stream, SpreadsheetDocumentType.Workbook))
                {
                    WorkbookPart workbookPart = ExcelHelper.CreateWorkbookPart(doc, "Автозагрузка сотрудников");

                    WorksheetPart rId1 = ExcelHelper.CreateWorksheetPartAndImportDataTable(workbookPart, "rId1", 1, 1, (uint)dataTable.Columns.Count,
                        "Автозагрузка сотрудников", dataTable, 3, 1);

                    doc.WorkbookPart.Workbook.Save();
                }

                stream.Position = 0;
                BinaryReader b = new BinaryReader(stream);
                binData = b.ReadBytes((int)stream.Length);
            }
            return File(binData, ExcelHelper.ExcelContentType, "AutoHoursReport" + DateTime.Now.ToString("ddMMyyHHmmss") + ".xlsx");
        }

        [NonAction]
        private List<DateTime> GetListUncreatedReportPeriods(DateTime beginDate, DateTime endDate)
        {
            var listDatesBetweenTwoDates = GetDatesBetweenTwoDates(beginDate, endDate);
            var resultList = new List<DateTime>();
            foreach (var date in listDatesBetweenTwoDates)
            {
                if (_reportingPeriodService.GetAll(x => x.Month == date.Month && x.Year == date.Year)
                        .FirstOrDefault() == null)
                {
                    resultList.Add(date);

                }
            }
            return resultList;
        }

        [NonAction]
        private string CreateErroMessage(List<DateTime> listDateTimes)
        {
            var error = string.Empty;
            foreach (var date in listDateTimes)
            {
                error += date.ToString("MMMM", CultureInfo.CreateSpecificCulture("ru")) + "  " + date.Year + " не создан в отчетном периоде!\n\r";
            }
            return error;
        }

        [NonAction]
        private List<DateTime> GetDatesBetweenTwoDates(DateTime beginDate, DateTime endDate)
        {
            var listDatesBetweenTwoDates = new List<DateTime>();
            var countMonthBetweenTwoDates = ((endDate.Year - endDate.Year) * 12) + endDate.Month - beginDate.Month;
            for (int i = 0; i < countMonthBetweenTwoDates; i++)
            {
                listDatesBetweenTwoDates.Add(beginDate.AddMonths(i));
            }
            return listDatesBetweenTwoDates;
        }

        [OperationActionFilter(nameof(Operation.TSAutoHoursRecordCreateUpdate))]
        public ActionResult AutoHoursDataSave(int? ID, int? ProjectID, int? EmployeeID, string DayHours, string BeginDate, string EndDate, bool del = false)
        {
            TSAutoHoursRecord tsAutoHoursRecord = null;
            if (ID != null && ID.HasValue == true)
            {
                tsAutoHoursRecord = _tsAutoHoursRecordService.GetById((int)ID);
            }

            var beginDateTime = DateTime.TryParse(BeginDate, out var outBeginDateTime) ? outBeginDateTime : (DateTime)tsAutoHoursRecord.BeginDate;
            var endDateTime = DateTime.TryParse(EndDate, out var outEndDateTime) ? outEndDateTime : (DateTime)tsAutoHoursRecord.EndDate;
            var user = _userService.GetUserDataForVersion();
            var listUncreatedReportPeriods = GetListUncreatedReportPeriods(beginDateTime, endDateTime);
            //в данных датах нету не созданных месяцев
            if (listUncreatedReportPeriods.Count == 0)
            {

                //начало и конец отчетного периода
                var reportingPeriodForBeginDate = _reportingPeriodService
                    .GetAll(x => x.Month == beginDateTime.Month && x.Year == beginDateTime.Year).FirstOrDefault();
                var reportingPeriodForEndDate = _reportingPeriodService
                    .GetAll(x => x.Month == endDateTime.Month && x.Year == endDateTime.Year).FirstOrDefault();

                if (reportingPeriodForBeginDate == null || reportingPeriodForEndDate == null)
                    return Content("Не создан отчетный период.");

                //удаление записи
                if (del != false && ID != null)
                {
                    if (reportingPeriodForBeginDate != null && reportingPeriodForEndDate != null &&
                        (reportingPeriodForEndDate.NewTSRecordsAllowedUntilDate > DateTime.Now &&
                         reportingPeriodForBeginDate.NewTSRecordsAllowedUntilDate > DateTime.Now))
                    {
                        //Удаление связанных записей с таймшитом
                        var parentHoursRecords = _tsHoursRecordService.Get(x => x.Where(t => t.ParentTSAutoHoursRecordID == ID).ToList());
                        if (parentHoursRecords != null && parentHoursRecords.Count > 0)
                        {
                            _tsHoursRecordService.RemoveRange(parentHoursRecords);
                        }
                        var recycleBinInDBRelationTSAutoHoursRecord = _serviceService.HasRecycleBinInDBRelation(new TSAutoHoursRecord() { ID = (int)ID });
                        if (recycleBinInDBRelationTSAutoHoursRecord.hasRelated == false)
                        {
                            var recycleToRecycleBinTSAutoHoursRecord = _tsAutoHoursRecordService.RecycleToRecycleBin((int)ID, user.Item1, user.Item2);
                            if (!recycleToRecycleBinTSAutoHoursRecord.toRecycleBin)
                                return Content("Невозможно удалить, так как на удаляемый элемент ссылаются другие элементы в системе" +
                                               $".Сначала необходимо удалить элементы, которые ссылаются на данный элемент. {recycleToRecycleBinTSAutoHoursRecord.relatedClassId}");
                        }
                        else
                            return Content("Невозможно удалить, так как на удаляемый элемент ссылаются другие элементы в системе" +
                                           $".Сначала необходимо удалить элементы, которые ссылаются на данный элемент. {recycleBinInDBRelationTSAutoHoursRecord.relatedInDBClassId}");

                        return Content("true");
                    }

                    return Content("Отчетный период закрыт на " + reportingPeriodForBeginDate.FullName + " закрыт!");
                }
                //обновление записей
                else
                {
                    double hours = Convert.ToDouble(DayHours.Replace(".", ","));

                    //обновление/редакрировании найденной записи
                    if (tsAutoHoursRecord != null)
                    {
                        if (beginDateTime > endDateTime)
                        {
                            return Content("Дата начала не может быть больше даты окончания периода.");
                        }
                        var employee = _employeeService.GetById((int)EmployeeID);

                        if (tsAutoHoursRecord.EmployeeID != EmployeeID)
                        {
                            return Content("Изменение сотрудника в созданной записи автозагрузки запрещено.");
                        }

                        if (employee.EnrollmentDate == null || employee.EnrollmentDate.HasValue == false)
                            return Content("Сотрудник еще не принят на работу.");

                        if (employee.DismissalDate != null)
                        {
                            //Нельзя указывать дату окончания после даты принятия сотрудника на работу
                            if (endDateTime > employee.DismissalDate)
                                return Content("Автозагрузка не может заканчиваться позже даты увольнения сотрудника.");
                        }

                        if (employee.EnrollmentDate != null)
                        {
                            //Нельзя указывать дату начала до даты принятия сотрудника на работу
                            if (beginDateTime < employee.EnrollmentDate)
                            {
                                return Content("Автозагрузка не может начинаться ранее даты приема сотрудника");
                            }
                        }

                        //есть ли у человек проекты
                        //отключена проверка, так как сотрудников не добавляют в РГ проекта, у кого есть автозагрузка
                        /*
                        if (_projectMembershipService.GetProjectsForEmployee((int)tsAutoHoursRecord.EmployeeID).Count != 0)
                        {
                            //работает ли этот человек не состоить в этом проекте
                            if (_projectMembershipService.GetProjectMembershipForEmployees(
                                        new DateTimeRange((DateTime)tsAutoHoursRecord.BeginDate,
                                            (DateTime)tsAutoHoursRecord.EndDate), (int)tsAutoHoursRecord.EmployeeID)
                                    .Count(x => x.ProjectID == ProjectID) == 0)
                                return Content("Данный сотрудник не состоит в проекте, либо неправильно указана дата начала или окончания действия.");
                        }
                        else
                            return Content("У данного сотрудника нету проектов.");
                            */

                        var tsAutoHoursRecordInDb = _tsAutoHoursRecordService.GetById(tsAutoHoursRecord.ID);
                        //дату начала в закрытом переоде менять нельзя
                        if (reportingPeriodForBeginDate != null && reportingPeriodForBeginDate
                                                                    .NewTSRecordsAllowedUntilDate < DateTime.Now
                                                                && tsAutoHoursRecordInDb.BeginDate != beginDateTime)
                            return Content("Начальную дату в закрытом периоде изменять нельзя.");
                        //Дату окончания в закрытый перод менить нельзя
                        if (reportingPeriodForEndDate != null && reportingPeriodForEndDate
                                                                  .NewTSRecordsAllowedUntilDate < DateTime.Now
                                                              && endDateTime < tsAutoHoursRecordInDb.BeginDate.Value.LastDayOfMonth())
                            return Content("Конечную дату в закрытом периоде нельзя поставить меньше последнего дня отчетного месяца.");

                        if (((reportingPeriodForBeginDate != null &&
                              reportingPeriodForBeginDate.NewTSRecordsAllowedUntilDate < DateTime.Now)
                             || (reportingPeriodForEndDate != null &&
                                 reportingPeriodForEndDate.NewTSRecordsAllowedUntilDate < DateTime.Now))
                            && tsAutoHoursRecordInDb.DayHours != hours)
                            return Content("Нельзя изменить количество часов в закрытом периоде.");

                        tsAutoHoursRecord.EmployeeID = EmployeeID;
                        tsAutoHoursRecord.ProjectID = ProjectID;
                        tsAutoHoursRecord.BeginDate = Convert.ToDateTime(BeginDate);
                        tsAutoHoursRecord.EndDate = Convert.ToDateTime(EndDate);
                        tsAutoHoursRecord.DayHours = hours;

                        _tsAutoHoursRecordService.Update(tsAutoHoursRecord);
                        //TODO добавить
                        //_taskTimesheetProcessing.ProcessTSAutoHoursRecords(currentEmployee, newtsAutoHoursRecord);
                        return Content("true");
                    }
                    //Создание записи
                    else
                    {
                        //если начальная дата больше конечной
                        if (beginDateTime > endDateTime)
                        {
                            return Content("Дата начала не может быть больше даты окончания периода.");
                        }

                        var employee = _employeeService.GetById((int)EmployeeID);

                        if (employee.EnrollmentDate == null || employee.EnrollmentDate.HasValue == false)
                            return Content("Сотрудник еще не принят на работу.");

                        if (employee.DismissalDate != null)
                        {
                            //Нельзя указывать дату окончания после даты принятия сотрудника на работу
                            if (endDateTime > employee.DismissalDate)
                                return Content("Автозагрузка не может заканчиваться позже даты увольнения сотрудника.");
                        }

                        if (employee.EnrollmentDate != null)
                        {
                            //Нельзя указывать дату начала до даты принятия сотрудника на работу
                            if (beginDateTime < employee.EnrollmentDate)
                            {
                                return Content("Автозагрузка не может начинаться ранее даты приема сотрудника");
                            }
                        }

                        //есть ли у человек проекты
                        //отключена проверка, так как сотрудников не добавляют в РГ проекта, у кого есть автозагрузка
                        /*
                        if (EmployeeID != null && _projectMembershipService.GetProjectsForEmployee((int)EmployeeID).Count != 0)
                        {
                            //работает ли этот человек не состоить в этом проекте
                            if (_projectMembershipService.GetProjectMembershipForEmployees(
                                        new DateTimeRange(beginDateTime,
                                            endDateTime), (int)EmployeeID)
                                    .Count(x => x.ProjectID == ProjectID) == 0)
                                return Content("Данный сотрудник не состоит в проекте, либо неправильно указана дата начала или окончания действия.");
                        }
                        else
                            return Content("У данного сотрудника нету проектов.");
                        */


                        if (reportingPeriodForBeginDate != null &&
                            reportingPeriodForBeginDate.NewTSRecordsAllowedUntilDate < DateTime.Now)
                            return Content("Отчетный период закрыт.");
                        if (reportingPeriodForEndDate != null &&
                            reportingPeriodForEndDate.NewTSRecordsAllowedUntilDate < DateTime.Now)
                            return Content("Отчетный период закрыт.");

                        tsAutoHoursRecord = new TSAutoHoursRecord
                        {
                            EmployeeID = EmployeeID,
                            ProjectID = ProjectID,
                            BeginDate = Convert.ToDateTime(BeginDate),
                            EndDate = Convert.ToDateTime(EndDate),
                            DayHours = hours
                        };
                        _tsAutoHoursRecordService.Add(tsAutoHoursRecord);

                        //TODO добавить
                        //_taskTimesheetProcessing.ProcessTSAutoHoursRecords(currentEmployee, tsAutoHoursRecord);

                        return Content("true");
                    }
                }
            }
            return Content(CreateErroMessage(listUncreatedReportPeriods));
        }

        [OperationActionFilter(nameof(Operation.TSAutoHoursRecordView))]
        public string GetProjects(string hoursStartDate, string hoursEndDate)
        {
            if (hoursStartDate == null)
            {
                hoursStartDate = DateTime.Today.AddDays(-(int)DateTime.Today.DayOfWeek + 1).ToShortDateString();
            }

            if (hoursEndDate == null)
            {
                hoursEndDate = Convert.ToDateTime(hoursStartDate).AddDays(7).AddSeconds(-1).ToShortDateString();
            }

            var projectList = _projectService.Get(x => x.ToList().OrderBy(p => p.ShortName).ToList());

            List<SelectListItem> records = new List<SelectListItem>();

            int userEmployeeID = _applicationUserService.GetEmployeeID();

            foreach (Project project in projectList)
            {
                /*var projectTeam = db.ProjectMembers.Where(pm => pm.ProjectID != null && pm.ProjectID == project.ID).ToList();
                if (projectTeam != null)
                {
                    if (projectTeam.Where(pm => pm.EmployeeID == currentUserEmployeeID
                        && (pm.MembershipDateEnd == null || pm.MembershipDateEnd >= Convert.ToDateTime(hoursStartDate))
                        && (pm.MembershipDateBegin == null || pm.MembershipDateBegin <= Convert.ToDateTime(hoursEndDate))
                        ).ToList().Count() > 0)
                    {
                        records.Add(new SelectListItem { Value = project.ID.ToString(), Text = project.ShortName });
                    }
                }*/
                records.Add(new SelectListItem { Value = project.ID.ToString(), Text = project.ShortName });
            }

            return JsonConvert.SerializeObject(records,
                new JsonSerializerSettings()
                {
                    ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore
                });
        }

        public bool checkAutoHoursCellValue(string value, string column, out string message)
        {
            // Дата завершения действия автозагрузки не может отстоять дальше 12 месяцев от даты начала действия автозагрузки (считаем пока от текущей даты)
            string maxDate = DateTime.Today.AddMonths(12).ToShortDateString();

            // Дата начала действия автозагрузки не может быть ранее первого рабочего дня текущего месяца
            DateTime dtStart = new DateTime(DateTime.Now.Year, 1/*DateTime.Now.Month*/, 1); // исправлено (!) - не  может быть ранее даты начала текущего года
            string minDate = dtStart.ToShortDateString();
            if (dtStart.DayOfWeek == DayOfWeek.Saturday)
            {
                minDate = dtStart.AddDays(2).ToShortDateString();
            }

            if (dtStart.DayOfWeek == DayOfWeek.Sunday)
            {
                minDate = dtStart.AddDays(1).ToShortDateString();
            }

            if (column == "Дата начала")
            {
                DateTime incomeDate = Convert.ToDateTime(value);
                if (incomeDate < Convert.ToDateTime(minDate))
                {
                    message = "Дата начала действия автозагрузки не может быть ранее первого рабочего дня текущего месяца (" + minDate + ")";
                    return false;
                }
            }


            if (column == "Дата завершения")
            {
                DateTime incomeDate = Convert.ToDateTime(value);
                if (incomeDate > Convert.ToDateTime(maxDate))
                {
                    message = "Дата завершения действия автозагрузки не может отстоять дальше 12 месяцев от сегодняшней даты";
                    return false;
                }
            }

            message = "";
            return true;
        }

        [HttpPost]
        [OperationActionFilter(nameof(Operation.TSAutoHoursRecordCreateUpdate))]
        public string ValidateRow(string value, string column)
        {
            string messageText = "";
            //все проверки выполняются в момент сохранения
            /*if (!checkAutoHoursCellValue(value, column, out messageText))
            {
                return JsonConvert.SerializeObject(new { status = "error", message = messageText });
            }*/

            return JsonConvert.SerializeObject(new { status = "", message = "" });
        }

        [HttpGet]
        [OperationActionFilter(nameof(Operation.TSAutoHoursRecordView))]
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

            var employeesList = _employeeService.Get(x => x.ToList().OrderBy(e => e.FullName).ToList());

            List<SelectListItem> records = new List<SelectListItem>();

            int userEmployeeID = _applicationUserService.GetEmployeeID();

            foreach (Employee employee in employeesList)
            {
                records.Add(new SelectListItem { Value = employee.ID.ToString(), Text = employee.FullName });
            }

            return JsonConvert.SerializeObject(records,
                new JsonSerializerSettings()
                {
                    ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore
                });
        }

        [HttpGet]
        [OperationActionFilter(nameof(Operation.TSAutoHoursRecordView))]
        public string GetHours()
        {
            List<SelectListItem> records = new List<SelectListItem>();
            records.Add(new SelectListItem { Value = "0.5", Text = "0.5" });
            records.Add(new SelectListItem { Value = "1", Text = "1" });
            records.Add(new SelectListItem { Value = "1.5", Text = "1.5" });
            records.Add(new SelectListItem { Value = "2", Text = "2" });
            records.Add(new SelectListItem { Value = "2.5", Text = "2.5" });
            records.Add(new SelectListItem { Value = "3", Text = "3" });
            records.Add(new SelectListItem { Value = "3.5", Text = "3.5" });
            records.Add(new SelectListItem { Value = "4", Text = "4" });
            records.Add(new SelectListItem { Value = "4.5", Text = "4.5" });
            records.Add(new SelectListItem { Value = "5", Text = "5" });
            records.Add(new SelectListItem { Value = "5.5", Text = "5.5" });
            records.Add(new SelectListItem { Value = "6", Text = "6" });
            records.Add(new SelectListItem { Value = "6.5", Text = "6.5" });
            records.Add(new SelectListItem { Value = "7", Text = "7" });
            records.Add(new SelectListItem { Value = "7.5", Text = "7.5" });
            records.Add(new SelectListItem { Value = "8", Text = "8" });

            return JsonConvert.SerializeObject(records);
        }
    }
}
