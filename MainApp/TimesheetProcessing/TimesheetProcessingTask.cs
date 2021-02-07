using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using Core;
using Core.BL.Interfaces;
using Core.Common;
using Core.Config;
using Core.Extensions;
using Core.Helpers;
using Core.JIRA;
using Core.Models;
using MainApp.Helpers;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;








namespace MainApp.TimesheetProcessing
{
    public class TimesheetProcessingTask : LongRunningTaskBase
    {
        private readonly ITSAutoHoursRecordService _tsAutoHoursRecordService;
        private readonly IVacationRecordService _vacationRecordService;
        private readonly IReportingPeriodService _reportingPeriodService;
        private readonly IProductionCalendarService _productionCalendarService;
        private readonly ITSHoursRecordService _tsHoursRecordService;
        private readonly IUserService _userService;
        private readonly IProjectService _projectService;
        private readonly IEmployeeService _employeeService;
        private readonly IProjectMembershipService _projectMembershipService;
        private readonly ITimesheetService _timesheetService;
        private JiraConfig _jiraConfig;
        private readonly IJiraService _jiraService;
        private readonly IProjectExternalWorkspaceService _projectExternalWorkspaceService;
        private TimesheetConfig _timesheetConfig;
        private SMTPConfig _smtpConfig;

        public TimesheetProcessingTask(ITSAutoHoursRecordService tsAutoHoursRecordService,
                                       IVacationRecordService vacationRecordService,
                                       IReportingPeriodService reportingPeriodService,
                                       IProductionCalendarService productionCalendarService,
                                       ITSHoursRecordService tsHoursRecordService,
                                       IUserService userService,
                                       IProjectService projectService,
                                       IEmployeeService employeeService,
                                       IProjectMembershipService projectMembershipService,
                                       ITimesheetService timesheetService, IOptions<TimesheetConfig> timesheetOptions, IOptions<SMTPConfig> smtpOptions, IOptions<JiraConfig> jiraOptions, IJiraService jiraService,
                                       IProjectExternalWorkspaceService projectExternalWorkspaceService)
        {
            _tsAutoHoursRecordService = tsAutoHoursRecordService ?? throw new ArgumentNullException(nameof(tsAutoHoursRecordService));
            _vacationRecordService = vacationRecordService ?? throw new ArgumentNullException(nameof(vacationRecordService));
            _reportingPeriodService = reportingPeriodService ?? throw new ArgumentNullException(nameof(reportingPeriodService));
            _productionCalendarService = productionCalendarService ?? throw new ArgumentNullException(nameof(productionCalendarService));
            _tsHoursRecordService = tsHoursRecordService ?? throw new ArgumentNullException(nameof(tsHoursRecordService));
            _userService = userService ?? throw new ArgumentNullException(nameof(userService));
            _projectService = projectService ?? throw new ArgumentNullException(nameof(projectService));
            _employeeService = employeeService;
            _projectMembershipService = projectMembershipService;
            _timesheetService = timesheetService;
            _jiraConfig = jiraOptions.Value;
            _jiraService = jiraService;
            _projectExternalWorkspaceService = projectExternalWorkspaceService;
            _timesheetConfig = timesheetOptions.Value;
            _smtpConfig = smtpOptions.Value;
        }

        //Дописать
        public LongRunningTaskReport ProcessVacationRecords(Employee employee = null, VacationRecord specifiedVacationRecord = null)
        {
            LongRunningTaskReport report = new LongRunningTaskReport("Отчет об обработке данных Timesheet", "Обработка записей отпусков");

            try
            {
                var currentDateTime = DateTime.Now.Date;
                (string, string) currUser = ("", "");
                try
                {
                    currUser = _userService.GetUserDataForVersion();
                }
                catch (Exception e)
                {
                    currUser.Item1 = "";
                    currUser.Item2 = "";
                }

                DateTime timesheetProcessingProcessVacationRecordsStartDate = DateTime.MinValue;

                try
                {
                    if (String.IsNullOrEmpty(_timesheetConfig.ProcessingProcessVacationRecordsStartDate))
                    {
                        timesheetProcessingProcessVacationRecordsStartDate = DateTime.Parse(_timesheetConfig.ProcessingProcessVacationRecordsStartDate);
                    }
                }
                catch (Exception)
                {
                    timesheetProcessingProcessVacationRecordsStartDate = DateTime.MinValue;
                }



                //открытые отчетные месяцы
                var listReportingPeriods = _reportingPeriodService
                                            .GetAll(periods => periods.NewTSRecordsAllowedUntilDate >= currentDateTime).OrderBy(p => p.FullName).ToList();

                if (listReportingPeriods == null || listReportingPeriods.Count == 1)
                {
                    report.AddReportEvent("[Предупреждение] Не созданы отчетные периоды, либо нет открытых отчетных периодов на текущий момент времени");
                }
                else
                {
                    report.AddReportEvent("Количество открытых отчетных периодов на текущий момент времени: " + listReportingPeriods.Count.ToString());
                }

                int j = 0;
                foreach (var reportingPeriod in listReportingPeriods)
                {
                    //Дата начала отчетного месяца с первого дня месяца
                    var firstDayOfReportingPeriod = new DateTime(reportingPeriod.Year, reportingPeriod.Month, 1).FirstDayOfMonth();
                    //Дата окончания отчетного месяца - 31.01 или другое
                    var lastDayOfReportingPeriod = new DateTime(reportingPeriod.Year, reportingPeriod.Month, 1).LastDayOfMonth();

                    report.AddReportEvent("Отчетный период: " + reportingPeriod.FullName);
                    SetStatus(60 + j * 19 / listReportingPeriods.Count(), "Обработка записей отпусков, отчетный период: " + reportingPeriod.FullName);

                    if (firstDayOfReportingPeriod <= DateTime.Today)
                    {
                        IList<VacationRecord> vacationRecordList = null;

                        //Записи, которые попадают в отчетный период по принципу пересечения периодов
                        if (employee != null && specifiedVacationRecord != null)
                        {
                            vacationRecordList = _vacationRecordService.Get(records => records.Where(x =>
                                x.EmployeeID == employee.ID && x.ID == specifiedVacationRecord.ID &&
                                x.VacationEndDate >= firstDayOfReportingPeriod && x.VacationBeginDate <= lastDayOfReportingPeriod).ToList());
                        }
                        else if (employee != null)
                        {
                            vacationRecordList = _vacationRecordService.Get(records => records.Where(x =>
                                x.EmployeeID == employee.ID &&
                                x.VacationEndDate >= firstDayOfReportingPeriod && x.VacationBeginDate <= lastDayOfReportingPeriod).ToList());
                        }
                        else
                        {
                            vacationRecordList = _vacationRecordService.Get(records => records.Where(x =>
                                 (x.VacationEndDate >= firstDayOfReportingPeriod && x.VacationBeginDate <= lastDayOfReportingPeriod)).ToList());
                        }

                        //найдены записи отпусков
                        if (vacationRecordList != null && vacationRecordList.Count != 0)
                        {
                            foreach (var vacationRecord in vacationRecordList)
                            {
                                SetStatus(60 + j * 19 / listReportingPeriods.Count(),
                                    "Обработка записей отпусков, отчетный период: " + reportingPeriod.FullName
                                    + ", ID: " + vacationRecord.ID.ToString()
                                    + ", ФИО: " + vacationRecord.Employee.FullName);

                                DateTime beginDateInReportingPeriod = (vacationRecord.VacationBeginDate == null || firstDayOfReportingPeriod > vacationRecord.VacationBeginDate) ? firstDayOfReportingPeriod : vacationRecord.VacationBeginDate;
                                DateTime endDateInReportingPeriod = (vacationRecord.VacationEndDate == null || lastDayOfReportingPeriod < vacationRecord.VacationEndDate) ? lastDayOfReportingPeriod : vacationRecord.VacationEndDate;

                                //Обрабатываем отпуск в отчетном периоде, если он не в будущем и пересекается с периодом, в который сотрудник работал
                                if (beginDateInReportingPeriod <= DateTime.Today
                                    && (vacationRecord.Employee.EnrollmentDate == null || (vacationRecord.Employee.EnrollmentDate <= DateTime.Today && vacationRecord.Employee.EnrollmentDate <= endDateInReportingPeriod))
                                    && (vacationRecord.Employee.DismissalDate == null || vacationRecord.Employee.DismissalDate >= beginDateInReportingPeriod))
                                {
                                    //если сотрудник был принят позже даты начала отпуска или даты начала отчетного периода
                                    if (vacationRecord.Employee.EnrollmentDate != null
                                        && vacationRecord.Employee.EnrollmentDate >= beginDateInReportingPeriod)
                                    {
                                        beginDateInReportingPeriod = vacationRecord.Employee.EnrollmentDate.Value;
                                    }

                                    // если сотрудник был уволен раньше даты окончания отпуска или даты окончания отчетного периода
                                    if (vacationRecord.Employee.DismissalDate != null
                                        && vacationRecord.Employee.DismissalDate <= endDateInReportingPeriod)
                                    {
                                        endDateInReportingPeriod = vacationRecord.Employee.DismissalDate.Value;
                                    }

                                    var listDatesBetweenTwoDates = beginDateInReportingPeriod.Range((endDateInReportingPeriod > DateTime.Today) ? DateTime.Today : endDateInReportingPeriod).ToList();

                                    // проходим по каждому дню отпуска
                                    foreach (var vacationDate in listDatesBetweenTwoDates)
                                    {
                                        var productionCalendarRecord = _productionCalendarService.GetAllRecords()
                                                                        .FirstOrDefault(x => x.CalendarDate == vacationDate)
                                                                        ?? throw new ArgumentNullException("На " + vacationDate.ToShortDateString() + "не заведен производственный календарь!");


                                        var vacationProjectId = _reportingPeriodService.GetAll(x => x.Month == vacationDate.Month && x.Year == vacationDate.Year).Select(x => x.VacationProjectID).FirstOrDefault();
                                        var vacationNoPaidProjectId = _reportingPeriodService.GetAll(x => x.Month == vacationDate.Month && x.Year == vacationDate.Year).Select(x => x.VacationNoPaidProjectID).FirstOrDefault();

                                        var projectIDForTSHoursRecord = (vacationRecord.VacationType == VacationRecordType.VacationPaid) ? vacationProjectId : vacationNoPaidProjectId;

                                        //только рабочие дни, нужная дополнительная проверка на количество рабочих часов
                                        if (productionCalendarRecord != null && productionCalendarRecord.WorkingHours != 0)
                                        {
                                            //5.3.1. созданные ранее записи автозагрузки на дату отпуска, которые должны быть удалены
                                            var tsHoursRecordsOfAutoHoursRecords = _tsHoursRecordService.Get(records => records.Where(x =>
                                                x.ParentTSAutoHoursRecordID != null &&
                                                x.RecordDate == productionCalendarRecord.CalendarDate &&
                                                x.EmployeeID == vacationRecord.EmployeeID &&
                                                x.RecordSource == TSRecordSource.AutoPercentAssign
                                            ).ToList());

                                            bool isTSHoursRecordsOfAutoHoursRecordsExist = tsHoursRecordsOfAutoHoursRecords != null && tsHoursRecordsOfAutoHoursRecords.Count() != 0;

                                            if (isTSHoursRecordsOfAutoHoursRecordsExist)
                                            {
                                                foreach (var tsHoursRecord in tsHoursRecordsOfAutoHoursRecords)
                                                {
                                                    if (tsHoursRecord.RecordDate >= timesheetProcessingProcessVacationRecordsStartDate)
                                                    {
                                                        string reportEventDescription = "Запись ТШ удалена." +
                                                                                        " ID записи автозагрузки: " + tsHoursRecord.ParentTSAutoHoursRecordID +
                                                                                        " Сотрудник: " + ((tsHoursRecord.Employee == null) ? "" : tsHoursRecord.Employee.FullName) +
                                                                                        " Отчетная дата: " + tsHoursRecord.RecordDate.Value.ToShortDateString() +
                                                                                        " Проект: " + ((tsHoursRecord.Project == null) ? "" : tsHoursRecord.Project.ShortName);
                                                        _tsHoursRecordService.Delete(tsHoursRecord.ID);
                                                        report.AddReportEvent(reportEventDescription);
                                                    }
                                                }
                                            }

                                            //проверка наличия записи отпуска(если  до этого была)
                                            var tsRecordOfVacationRecord = _tsHoursRecordService.Get(records => records.Where(x => x.ParentVacationRecordID == vacationRecord.ID &&
                                                     x.RecordDate == productionCalendarRecord.CalendarDate
                                                ).ToList()).FirstOrDefault();

                                            //если запись запись отпуска не найдена
                                            if (tsRecordOfVacationRecord == null)
                                            {
                                                var newTsHoursRecord = new TSHoursRecord()
                                                {
                                                    ParentVacationRecordID = vacationRecord.ID,
                                                    RecordDate = productionCalendarRecord.CalendarDate,
                                                    Hours = productionCalendarRecord.WorkingHours,
                                                    EmployeeID = vacationRecord.EmployeeID,
                                                    Description = "Отпуск",
                                                    ProjectID = projectIDForTSHoursRecord,
                                                    RecordSource = TSRecordSource.Vacantion,
                                                    RecordStatus = TSRecordStatus.HDApproved
                                                };

                                                var projectShortName = _projectService.GetById(projectIDForTSHoursRecord).ShortName;
                                                if (newTsHoursRecord.RecordDate >= timesheetProcessingProcessVacationRecordsStartDate)
                                                {
                                                    _tsHoursRecordService.Add(newTsHoursRecord, currUser.Item1,
                                                        currUser.Item2);
                                                    report.AddReportEvent("Успешное создание записи отпуска" +
                                                                          " Отпуск: " + vacationRecord.FullName +
                                                                          " Отчетная дата: " +
                                                                          newTsHoursRecord.RecordDate.Value
                                                                              .ToShortDateString() +
                                                                          " Количество часов: " +
                                                                          newTsHoursRecord.Hours +
                                                                          " Проект: " + projectShortName);

                                                }
                                            }
                                            else
                                            {
                                                var projectShortName = _projectService.GetById(projectIDForTSHoursRecord).ShortName;

                                                if (tsRecordOfVacationRecord.Hours != productionCalendarRecord.WorkingHours
                                                    || tsRecordOfVacationRecord.EmployeeID != vacationRecord.EmployeeID
                                                    || tsRecordOfVacationRecord.ProjectID != projectIDForTSHoursRecord)
                                                {
                                                    tsRecordOfVacationRecord.Hours = productionCalendarRecord.WorkingHours;
                                                    tsRecordOfVacationRecord.EmployeeID = vacationRecord.EmployeeID;
                                                    tsRecordOfVacationRecord.Description = "Отпуск";
                                                    tsRecordOfVacationRecord.ProjectID = projectIDForTSHoursRecord;

                                                    _tsHoursRecordService.UpdateWithoutVersion(tsRecordOfVacationRecord);
                                                }

                                                report.AddReportEvent("Успешное обновление записи отпуска" +
                                                                           " Отпуск: " + vacationRecord.Employee.FullName +
                                                                           " Отчетная дата: " + tsRecordOfVacationRecord.RecordDate.Value.ToShortDateString() +
                                                                           " Количество часов: " + tsRecordOfVacationRecord.Hours +
                                                                           " Проект: " + projectShortName);
                                            }

                                        }
                                        //проверить наличие связанной созданной записи ТШ TSHoursRecord, созданной на основании записи отпуска
                                        else
                                        {
                                            var tsRecordOfVacationRecord = _tsHoursRecordService.Get(records => records.Where(x =>
                                                x.ParentVacationRecordID == vacationRecord.ID
                                                && x.RecordDate == productionCalendarRecord.CalendarDate).ToList()).FirstOrDefault();
                                            //если запись найдена
                                            if (tsRecordOfVacationRecord != null)
                                            {
                                                _tsHoursRecordService.Delete(tsRecordOfVacationRecord.ID);
                                                report.AddReportEvent("Удалена запись " + tsRecordOfVacationRecord.RecordDate.Value.ToShortDateString() + " перепадающая на выходной день.");
                                            }

                                        }


                                    }
                                }

                                //пункт 5.3.3
                                var tsHoursRecordsOfVacationRecord = _tsHoursRecordService.Get(records => records.Where(x =>
                                    x.ParentVacationRecordID != null &&
                                    x.ParentVacationRecordID == vacationRecord.ID && x.RecordSource == TSRecordSource.Vacantion).ToList());

                                if (tsHoursRecordsOfVacationRecord != null)
                                {
                                    foreach (var tsHoursRecord in tsHoursRecordsOfVacationRecord)
                                    {
                                        if (tsHoursRecord.RecordDate < tsHoursRecord.ParentVacationRecord.VacationBeginDate
                                            || tsHoursRecord.RecordDate > tsHoursRecord.ParentVacationRecord.VacationEndDate
                                            || (tsHoursRecord.Employee.DismissalDate.HasValue && tsHoursRecord.RecordDate > tsHoursRecord.Employee.DismissalDate)
                                            || (tsHoursRecord.Employee.EnrollmentDate.HasValue && tsHoursRecord.RecordDate < tsHoursRecord.Employee.EnrollmentDate)
                                            || (tsHoursRecord.RecordDate < timesheetProcessingProcessVacationRecordsStartDate))
                                        {
                                            _tsHoursRecordService.Delete(tsHoursRecord.ID);
                                            report.AddReportEvent("Успешное удаление записи отпуска" +
                                                                       " Отпуск: " + vacationRecord.FullName +
                                                                       " Отчетная дата: " + tsHoursRecord.RecordDate.Value.ToShortDateString() +
                                                                       " Количество часов: " + tsHoursRecord.Hours);
                                        }
                                    }
                                }
                            }
                        }
                        else
                        {
                            report.AddReportEvent("Не найдено отпусков, попадающих в отчетный период");
                        }
                    }
                    j++;
                }

            }
            catch (Exception e)
            {
                SetStatus(-1, "Ошибка: " + e.Message);
                report.AddReportEvent(e.Message + " " + e.StackTrace + " " + e.TargetSite.ToString());
            }

            return report;
        }

        public LongRunningTaskReport ProcessTSAutoHoursRecords(Employee employee = null, TSAutoHoursRecord specifiedAutoHoursRecord = null)
        {
            LongRunningTaskReport report = new LongRunningTaskReport("Отчет об обработке данных Timesheet", "Обработка записей автозагрузки");

            try
            {
                var currentDateTime = DateTime.Now.Date;
                //открытые отчетные месяцы
                var listReportingPeriods = _reportingPeriodService
                    .GetAll(periods => periods.NewTSRecordsAllowedUntilDate >= currentDateTime).OrderBy(p => p.FullName).ToList();

                if (listReportingPeriods == null || listReportingPeriods.Count == 1)
                {
                    report.AddReportEvent("[Предупреждение] Не созданы отчетные периоды, либо нет открытых отчетных периодов на текущий момент времени");
                }
                else
                {
                    report.AddReportEvent("Количество открытых отчетных периодов на текущий момент времени: " + listReportingPeriods.Count.ToString());
                }

                (string, string) currUser = ("", "");
                try
                {
                    currUser = _userService.GetUserDataForVersion();
                }
                catch (Exception e)
                {
                    currUser.Item1 = "";
                    currUser.Item2 = "";
                }

                int j = 0;
                foreach (var reportingPeriod in listReportingPeriods)
                {
                    //Дата начала отчетного месяца с первого дня месяца
                    var firstDayOfReportingPeriod = new DateTime(reportingPeriod.Year, reportingPeriod.Month, 1).FirstDayOfMonth();
                    //Дата окончания отчетного месяца - 31.01 или другое
                    var lastDayOfReportingPeriod = new DateTime(reportingPeriod.Year, reportingPeriod.Month, 1).LastDayOfMonth();

                    report.AddReportEvent("Отчетный период: " + reportingPeriod.FullName);
                    SetStatus(80 + j * 19 / listReportingPeriods.Count(), "Обработка записей автозагрузки, отчетный период: " + reportingPeriod.FullName);

                    if (firstDayOfReportingPeriod <= DateTime.Today)
                    {
                        IList<TSAutoHoursRecord> tsAutoHoursRecordList = null;

                        //Записи, которые попадают в отчетный период по принципу пересечения периодов
                        if (employee != null && specifiedAutoHoursRecord != null)
                        {
                            tsAutoHoursRecordList = _tsAutoHoursRecordService.Get(records => records.Where(x =>
                                x.EmployeeID == employee.ID && x.ID == specifiedAutoHoursRecord.ID &&
                                x.EndDate >= firstDayOfReportingPeriod && x.BeginDate <= lastDayOfReportingPeriod).ToList());
                        }
                        else if (employee != null)
                        {
                            tsAutoHoursRecordList = _tsAutoHoursRecordService.Get(records => records.Where(x =>
                                x.EmployeeID == employee.ID &&
                                x.EndDate >= firstDayOfReportingPeriod && x.BeginDate <= lastDayOfReportingPeriod).ToList());
                        }
                        else
                        {
                            tsAutoHoursRecordList = _tsAutoHoursRecordService.Get(records => records.Where(x =>
                                 (x.EndDate >= firstDayOfReportingPeriod && x.BeginDate <= lastDayOfReportingPeriod)).ToList());
                        }


                        //найдены записи автозагрузки
                        if (tsAutoHoursRecordList != null && tsAutoHoursRecordList.Count != 0)
                        {
                            //Запись автозагрузки - для каждого человека бьется потом по каждому дню
                            foreach (var tsAutoHoursRecord in tsAutoHoursRecordList)
                            {
                                SetStatus(80 + j * 19 / listReportingPeriods.Count(),
                                    "Обработка записей автозагрузки, отчетный период: " + reportingPeriod.FullName
                                    + ", ID: " + tsAutoHoursRecord.ID.ToString()
                                    + ", ФИО: " + tsAutoHoursRecord.Employee.FullName);

                                DateTime beginDateInReportingPeriod = (tsAutoHoursRecord.BeginDate == null || firstDayOfReportingPeriod > tsAutoHoursRecord.BeginDate) ? firstDayOfReportingPeriod : tsAutoHoursRecord.BeginDate.Value;
                                DateTime endDateInReportingPeriod = (tsAutoHoursRecord.EndDate == null || lastDayOfReportingPeriod < tsAutoHoursRecord.EndDate) ? lastDayOfReportingPeriod : tsAutoHoursRecord.EndDate.Value;

                                //Обрабатываем автозагрузку в отчетном периоде, если она не в будущем и пересекается с периодом, в который сотрудник работал
                                if (beginDateInReportingPeriod <= DateTime.Today
                                    && (tsAutoHoursRecord.Employee.EnrollmentDate == null || (tsAutoHoursRecord.Employee.EnrollmentDate <= DateTime.Today && tsAutoHoursRecord.Employee.EnrollmentDate <= endDateInReportingPeriod))
                                    && (tsAutoHoursRecord.Employee.DismissalDate == null || tsAutoHoursRecord.Employee.DismissalDate >= beginDateInReportingPeriod))
                                {
                                    //если сотрудник был принят позже даты начала отпуска или даты начала отчетного периода
                                    if (tsAutoHoursRecord.Employee.EnrollmentDate != null
                                        && tsAutoHoursRecord.Employee.EnrollmentDate >= beginDateInReportingPeriod)
                                    {
                                        beginDateInReportingPeriod = tsAutoHoursRecord.Employee.EnrollmentDate.Value;
                                    }

                                    // если сотрудник был уволен раньше даты окончания отпуска или даты окончания отчетного периода
                                    if (tsAutoHoursRecord.Employee.DismissalDate != null
                                        && tsAutoHoursRecord.Employee.DismissalDate <= endDateInReportingPeriod)
                                    {
                                        endDateInReportingPeriod = tsAutoHoursRecord.Employee.DismissalDate.Value;
                                    }

                                    var listDatesBetweenTwoDates = beginDateInReportingPeriod.Range((endDateInReportingPeriod > DateTime.Today) ? DateTime.Today : endDateInReportingPeriod).ToList();

                                    //Проходим по каждому дню автозагрузки
                                    foreach (var tsAutoHoursDate in listDatesBetweenTwoDates)
                                    {
                                        var productionCalendarRecord = _productionCalendarService.GetRecordByDate(tsAutoHoursDate)
                                            ?? throw new ArgumentNullException("На " + tsAutoHoursDate.ToShortDateString() + "не заведен производственный календарь!");

                                        //Связанная запись с таймшитом на основании записи отпуска
                                        var tsHoursRecordsForVacantion = _tsHoursRecordService.Get(records => records.Where(x =>
                                            x.RecordDate == productionCalendarRecord.CalendarDate &&
                                            x.EmployeeID == tsAutoHoursRecord.EmployeeID &&
                                            x.RecordSource == TSRecordSource.Vacantion).ToList()).FirstOrDefault();

                                        bool isTSHoursRecordsForVacantionExist = tsHoursRecordsForVacantion != null;

                                        //только рабочие дни согласно производственному календарю
                                        if (productionCalendarRecord != null && productionCalendarRecord.WorkingHours != 0)
                                        {
                                            //проверка связанных записей в таймшите на основании отпуска не найдена
                                            if (!isTSHoursRecordsForVacantionExist)
                                            {

                                                var tsRecordOfAutoHoursRecord = _tsHoursRecordService.Get(records => records.Where(x => x.ParentTSAutoHoursRecordID == tsAutoHoursRecord.ID &&
                                                                                                                 x.RecordDate == productionCalendarRecord.CalendarDate &&
                                                                                                                 x.EmployeeID == tsAutoHoursRecord.EmployeeID).ToList()).FirstOrDefault();

                                                double hours = Math.Round((productionCalendarRecord.WorkingHours * (tsAutoHoursRecord.DayHours.Value / 8)), 1); //SKIPR-491 округление до 0,1
                                                //Если запись ТШ, созданная на основании записи автозагрузки не найдена
                                                if (tsRecordOfAutoHoursRecord == null)
                                                {

                                                    var newTsHoursRecord = new TSHoursRecord()
                                                    {
                                                        ParentTSAutoHoursRecordID = tsAutoHoursRecord.ID,
                                                        RecordDate = productionCalendarRecord.CalendarDate,
                                                        EmployeeID = tsAutoHoursRecord.EmployeeID,
                                                        Description = "Автозагрузка",
                                                        ProjectID = tsAutoHoursRecord.ProjectID,
                                                        Hours = hours,
                                                        RecordSource = TSRecordSource.AutoPercentAssign,
                                                        RecordStatus = TSRecordStatus.HDApproved

                                                    };


                                                    _tsHoursRecordService.Add(newTsHoursRecord, currUser.Item1, currUser.Item2);


                                                    report.AddReportEvent("Запись ТШ добавлена." +
                                                                                   " ID записи автозагрузки: " + tsAutoHoursRecord.ID +
                                                                                   " Сотрудник: " + tsAutoHoursRecord.Employee.FullName +
                                                                                   " Отчетная дата: " + productionCalendarRecord.CalendarDate.ToShortDateString() +
                                                                                   " Количество часов: " + newTsHoursRecord.Hours +
                                                                                   " Проект: " + tsAutoHoursRecord.Project.ShortName);


                                                }
                                                //Если запись ТШ, созданная на основании записи автозагрузки найдена
                                                else
                                                {
                                                    if (tsRecordOfAutoHoursRecord.Hours != hours
                                                        || tsRecordOfAutoHoursRecord.EmployeeID != tsAutoHoursRecord.EmployeeID
                                                        || tsRecordOfAutoHoursRecord.ProjectID != tsAutoHoursRecord.ProjectID)
                                                    {
                                                        tsRecordOfAutoHoursRecord.Hours = hours;
                                                        tsRecordOfAutoHoursRecord.EmployeeID = tsAutoHoursRecord.EmployeeID;
                                                        tsRecordOfAutoHoursRecord.Description = "Автозагрузка";
                                                        tsRecordOfAutoHoursRecord.ProjectID = tsAutoHoursRecord.ProjectID;

                                                        _tsHoursRecordService.UpdateWithoutVersion(tsRecordOfAutoHoursRecord);
                                                    }


                                                    report.AddReportEvent("Запись ТШ обновлена." +
                                                                               " ID записи автозагрузки: " + tsAutoHoursRecord.ID +
                                                                               " Сотрудник: " + tsAutoHoursRecord.Employee.FullName +
                                                                               " Отчетная дата: " + productionCalendarRecord.CalendarDate.ToShortDateString() +
                                                                               " Количество часов: " + tsRecordOfAutoHoursRecord.Hours +
                                                                               " Проект: " + tsAutoHoursRecord.Project.ShortName);
                                                }


                                            }
                                            //todo Если запись ТШ TSHoursRecord, созданная на основании записи отпуска найдена, то дальнейшие действия (в рамках шага 6.3.1) не выполняются
                                        }
                                        //Праздники или выходные 6.3.2
                                        else
                                        {
                                            var relatedTsHoursRecordOfAutoHoursRecord = _tsHoursRecordService.Get(records => records.Where(x =>
                                                x.ParentTSAutoHoursRecordID == tsAutoHoursRecord.ID &&
                                                x.RecordDate == productionCalendarRecord.CalendarDate &&
                                                x.EmployeeID == tsAutoHoursRecord.EmployeeID).ToList()).FirstOrDefault();

                                            if (relatedTsHoursRecordOfAutoHoursRecord != null)
                                            {
                                                string reportEventDescription = "Запись ТШ удалена." +
                                                                                " ID записи автозагрузки: " + tsAutoHoursRecord.ID +
                                                                                " Сотрудник: " + tsAutoHoursRecord.Employee.FullName +
                                                                                " Отчетная дата: " + productionCalendarRecord.CalendarDate.ToShortDateString() +
                                                                                " Проект: " + tsAutoHoursRecord.Project.ShortName;
                                                _tsHoursRecordService.Delete(relatedTsHoursRecordOfAutoHoursRecord.ID);
                                                report.AddReportEvent(reportEventDescription);
                                            }
                                        }

                                    }
                                }

                                //6.3.3  Проверить наличие записей ТШ, созданных на основании записей автозагрузки, которые выходят за период автозагрузки
                                var tsHoursRecordsOfAutoHoursRecord = _tsHoursRecordService.Get(records => records.Where(x =>
                                    x.ParentTSAutoHoursRecordID != null &&
                                    x.ParentTSAutoHoursRecordID == tsAutoHoursRecord.ID &&
                                    x.RecordSource == TSRecordSource.AutoPercentAssign).ToList());

                                if (tsHoursRecordsOfAutoHoursRecord != null)
                                {
                                    foreach (var tsHoursRecord in tsHoursRecordsOfAutoHoursRecord)
                                    {
                                        if (tsHoursRecord.RecordDate < tsHoursRecord.ParentTSAutoHoursRecord.BeginDate
                                            || tsHoursRecord.RecordDate > tsHoursRecord.ParentTSAutoHoursRecord.EndDate
                                            || (tsHoursRecord.Employee.DismissalDate.HasValue && tsHoursRecord.RecordDate > tsHoursRecord.Employee.DismissalDate)
                                            || (tsHoursRecord.Employee.EnrollmentDate.HasValue && tsHoursRecord.RecordDate < tsHoursRecord.Employee.EnrollmentDate))
                                        {
                                            string reportEventDescription = "Запись ТШ удалена." +
                                                                            " ID записи автозагрузки: " + tsAutoHoursRecord.ID +
                                                                            " Сотрудник: " + tsAutoHoursRecord.Employee.FullName +
                                                                            " Отчетная дата: " + tsHoursRecord.RecordDate.Value.ToShortDateString() +
                                                                            " Проект: " + ((tsAutoHoursRecord.Project == null) ? "" : tsAutoHoursRecord.Project.ShortName);
                                            _tsHoursRecordService.Delete(tsHoursRecord.ID);
                                            report.AddReportEvent(reportEventDescription);
                                        }

                                    }
                                }
                            }
                        }
                        else
                        {
                            report.AddReportEvent("Не найдено записей автозагрузки, попадающий в отчетный период");
                        }
                    }
                    j++;
                }

            }
            catch (Exception e)
            {
                SetStatus(-1, "Ошибка: " + e.Message);
                report.AddReportEvent(e.Message + " " + e.StackTrace + " " + e.TargetSite.ToString());
            }

            return report;
        }

        public LongRunningTaskReport SyncWithExternalTimesheet(DateTime periodStartDate, DateTime periodEndDate,
            bool deleteSyncedRecordsBeforeSync, bool updateAlreadyAddedRecords, bool getHours, bool getVacations,
            bool stopOnError,
            int batchSaveRecordsLimit)
        {
            LongRunningTaskReport report = new LongRunningTaskReport("Отчет об обработке данных Timesheet", "Синхронизация с внешним ТШ");

            //Timesheet ts = new Timesheet();

            if (periodStartDate == DateTime.MinValue
                && periodEndDate == DateTime.MinValue)
            {
                periodStartDate = DateTime.Today.FirstDayOfMonth();
                periodEndDate = DateTime.Today.AddMonths(12);

                DateTime previousMonthFirstDay = periodStartDate.AddDays(-1).FirstDayOfMonth();

                ReportingPeriod previousMonthReportingPeriod = _reportingPeriodService.GetAll(x => x.Month == previousMonthFirstDay.Month && x.Year == previousMonthFirstDay.Year).FirstOrDefault();

                if (previousMonthReportingPeriod != null &&
                    previousMonthReportingPeriod.NewTSRecordsAllowedUntilDate >= DateTime.Today)
                {
                    periodStartDate = previousMonthFirstDay;
                }
            }

            int monthCount = periodEndDate.Month - periodStartDate.Month + 1;

            report.AddReportEvent("Синхронизация с внешним ТШ - старт.");


            report.AddReportEvent("Получение данных из внешнего ТШ за период: " + periodStartDate.ToShortDateString() + " - " + periodEndDate.ToShortDateString());
            if (_timesheetService.GetDataFromTimeSheetDB(this, periodStartDate.ToString("yyyy-MM-dd"), periodEndDate.ToString("yyyy-MM-dd"), monthCount, getVacations) == true)
            {

                if (getHours == true)
                {
                    report.AddReportEvent("Передача трудозатрат в ТШ за период: " + periodStartDate.ToShortDateString() + " - " + periodEndDate.ToShortDateString());
                    _timesheetService.SyncTSHoursRecordsWithExternalTimesheet(this, report,
                        periodStartDate, periodEndDate, deleteSyncedRecordsBeforeSync, updateAlreadyAddedRecords,
                        stopOnError, batchSaveRecordsLimit);
                    report.AddReportEvent("Передача трудозатрат в ТШ  - завершено.");
                }

                if (getVacations == true)
                {
                    report.AddReportEvent("Передача записей отпусков в за период: " + periodStartDate.ToShortDateString() + " - " + periodEndDate.ToShortDateString());
                    _timesheetService.SyncVacationRecordsWithExternalTimesheet(this, report,
                        periodStartDate, periodEndDate, deleteSyncedRecordsBeforeSync, updateAlreadyAddedRecords,
                        stopOnError, batchSaveRecordsLimit);
                    report.AddReportEvent("Передача записей отпусков в - завершено.");
                }
            }
            else
            {
                report.AddReportEvent("(!) Произошла ошибка при получении данных из внешнего ТШ, синхронизация не выполнена.");
            }

            report.AddReportEvent("Синхронизация с внешним ТШ - завершено.");

            return report;
        }

        public LongRunningTaskReport SendTSEmailNotification(DateTime sendTSEmailNotificationsAtDate)
        {
            LongRunningTaskReport report = new LongRunningTaskReport("Отчет об обработке данных Timesheet", "Рассылка e-mail уведомлений");
            bool allowSendTSEmailNotifications = false;
            string subjectPrefix = "";


            try
            {
                allowSendTSEmailNotifications = _timesheetConfig.AllowSendTSEmailNotifications;
            }
            catch (Exception)
            {
                allowSendTSEmailNotifications = false;
            }

            if (allowSendTSEmailNotifications == false)
            {
                report.AddReportEvent("Рассылка e-mail уведомлений заблокирована.");
                return report;
            }

            try
            {
                if (!string.IsNullOrEmpty(_timesheetConfig.EmailNotificationsSubjectPrefix))
                {
                    subjectPrefix = _timesheetConfig.EmailNotificationsSubjectPrefix.Trim() + " ";
                }

            }
            catch (Exception)
            { }


            //Сообщения должны рассылаться по рабочим дням. Условие на сегодняшний день и указанный в поле
            if (_productionCalendarService.GetAllRecords().Any(cal => cal.CalendarDate == sendTSEmailNotificationsAtDate) &&
                    (_productionCalendarService.GetAllRecords().FirstOrDefault(cal => cal.CalendarDate == sendTSEmailNotificationsAtDate).WorkingHours > 0 ||
                     _productionCalendarService.GetAllRecords().FirstOrDefault(cal => cal.CalendarDate == sendTSEmailNotificationsAtDate).WorkingHours == 0))
            {
                report.AddReportEvent("Отправка email уведомлений ТШ - старт.");
                SetStatus(90, "Отправка email уведомлений ТШ - старт.");

                //если это сегодняшний день(неделя)
                DateTime? todayFirstWorkDay = null;
                DateTime? todaySecondWorkDay = null;
                DateTime? todayThirdWorkDay = null;
                DateTime? todayFourthWorkDay = null;
                DateTime? todayFifthWorkDay = null;

                if (sendTSEmailNotificationsAtDate == DateTime.Today)
                {
                    todayFirstWorkDay = _productionCalendarService.GetSpecifiedWorkDayInCurrentWeek(1)?.CalendarDate;
                    todaySecondWorkDay = _productionCalendarService.GetSpecifiedWorkDayInCurrentWeek(2)?.CalendarDate;
                    todayThirdWorkDay = _productionCalendarService.GetSpecifiedWorkDayInCurrentWeek(3)?.CalendarDate;
                    todayFourthWorkDay = _productionCalendarService.GetSpecifiedWorkDayInCurrentWeek(4)?.CalendarDate;
                    todayFifthWorkDay = _productionCalendarService.GetSpecifiedWorkDayInCurrentWeek(5)?.CalendarDate;
                }
                //получить первый, второй и третий и тюд рабочей день на указанную дату
                var firstWorkDay = (todayFirstWorkDay != null && todayFirstWorkDay == sendTSEmailNotificationsAtDate)
                                   || sendTSEmailNotificationsAtDate == _productionCalendarService.GetSpecifiedWorkDayInSelectedWeek(1, sendTSEmailNotificationsAtDate)?.CalendarDate;
                var secondWorkDay = (todaySecondWorkDay != null && todaySecondWorkDay == sendTSEmailNotificationsAtDate)
                    || sendTSEmailNotificationsAtDate == _productionCalendarService.GetSpecifiedWorkDayInSelectedWeek(2, sendTSEmailNotificationsAtDate)?.CalendarDate;
                var thirdWorkDay = (todayThirdWorkDay != null && todayThirdWorkDay == sendTSEmailNotificationsAtDate)
                    || sendTSEmailNotificationsAtDate == _productionCalendarService.GetSpecifiedWorkDayInSelectedWeek(3, sendTSEmailNotificationsAtDate)?.CalendarDate;
                var fourthWorkDay = (todayFourthWorkDay != null && todayFourthWorkDay == sendTSEmailNotificationsAtDate)
                                    || sendTSEmailNotificationsAtDate == _productionCalendarService.GetSpecifiedWorkDayInSelectedWeek(4, sendTSEmailNotificationsAtDate)?.CalendarDate;
                var fifthWorkDay = (todayFifthWorkDay != null && todayFifthWorkDay == sendTSEmailNotificationsAtDate)
                                   || sendTSEmailNotificationsAtDate == _productionCalendarService.GetSpecifiedWorkDayInSelectedWeek(5, sendTSEmailNotificationsAtDate)?.CalendarDate; ;

                //TODO Рассылка уведомлений о трудозатратах
                try
                {
                    if (firstWorkDay)
                    {
                        report.AddReportEvent("Рассылка email уведомлений о необходимости ввода трудозатрат - старт.");
                        SetStatus(90, "Рассылка email уведомлений о необходимости ввода трудозатрат - старт.");

                        foreach (var employeeTuple in SendEmailOfHoursRecordForWeek(sendTSEmailNotificationsAtDate))
                        {
                            if (employeeTuple.employee == null)
                                report.AddReportEvent("Не указан сотрудник.");
                            else if (string.IsNullOrEmpty(employeeTuple.email) && employeeTuple.employee != null)
                                report.AddReportEvent("У сотрудника " + employeeTuple.employee.FullName +
                                                       "не указан email.");
                            else if (!string.IsNullOrEmpty(employeeTuple.email) && employeeTuple.employee != null &&
                                     string.IsNullOrEmpty(employeeTuple.emailText))
                                report.AddReportEvent("Для сотрудника " + employeeTuple.employee.FullName + "( " + employeeTuple.email + ") " +
                                                       "не сформирован текст email уведомления.");
                            else
                            {
                                string messageTitle = "Срочно введите трудозатраты за прошедшую неделю";
                                string actionsHtml = "";
                                string taskUrl = _smtpConfig.LinkMyHours;

                                actionsHtml += @"<a href='" + taskUrl + "' title='Откроется страница для ввода трудозатрат...' >Ввести трудозатраты за отчетную неделю</a>&nbsp;&nbsp;";
                                actionsHtml += "\r\n";
                                if (_smtpConfig.TimesheetDontSendTSEmailNotificationsTo.Split(';').Any(a => a.Contains(employeeTuple.email)) == false
                                    && (!string.IsNullOrEmpty(_smtpConfig.TimesheetSendTSEmailNotificationsOnlyTo)
                                        && _smtpConfig.TimesheetSendTSEmailNotificationsOnlyTo.Split(';').Any(a => a.Contains(employeeTuple.email))) ||
                                    string.IsNullOrEmpty(_smtpConfig.TimesheetSendTSEmailNotificationsOnlyTo))
                                {
                                    var rpcsUser = _userService.GetUserByLogin(employeeTuple.employee.ADLogin);
                                    if (rpcsUser != null)
                                    {
                                        if (rpcsUser.AllowSendEmailNotifications)
                                        {
                                            RPCSEmailHelper.SendHtmlEmailViaSMTP(employeeTuple.email,
                                                subjectPrefix + messageTitle,
                                                 _smtpConfig.FromEmail,
                                                null,
                                                RPCSEmailHelper.GetSimpleHtmlEmailBody(messageTitle,
                                                    employeeTuple.emailText,
                                                    actionsHtml),
                                                null,
                                                null);
                                            report.AddReportEvent("Отправка email уведомления [" + messageTitle + "] для " + employeeTuple.employee.FullName + " (" + employeeTuple.email + ") " + " - выполнена.");
                                        }
                                        else
                                            report.AddReportEvent("Отправка email уведомления [" + messageTitle + "] для " + employeeTuple.employee.FullName + " (" + employeeTuple.email + ") " + " - не разрешено Администратором ТШ.");
                                    }
                                    else
                                        report.AddReportEvent("Отправка email уведомления [" + messageTitle + "] для " + employeeTuple.employee.FullName + " (" + employeeTuple.email + ") " + " - не выполнена, у сотрудника нету доступа в систему.");
                                }
                                else
                                    report.AddReportEvent("Отправка email уведомления [" + messageTitle + "] для " + employeeTuple.employee.FullName + " (" + employeeTuple.email + ") " + " - не выполнена, так как email адрес добавлен в исключения.");
                            }
                        }

                        report.AddReportEvent("Рассылка email уведомлений о необходимости ввода трудозатрат - завершено.");
                        SetStatus(90, "Рассылка email уведомлений о необходимости ввода трудозатрат - завершено.");
                    }
                }
                catch (Exception e)
                {
                    report.AddReportEvent(e.Message + " " + e.StackTrace + " " + e.TargetSite.ToString());
                }

                //TODO Рассылка email для РП
                try
                {
                    if (firstWorkDay || secondWorkDay)
                    {
                        report.AddReportEvent("Рассылка email уведомлений о необходимости согласования трудозатрат РП за отчетную неделю - старт.");
                        SetStatus(90, "Рассылка email уведомлений о необходимости согласования трудозатрат РП за отчетную неделю - старт.");

                        foreach (var employeeTuple in SendEmailEmployeeHaveApproveTSRecords(sendTSEmailNotificationsAtDate))
                        {
                            if (employeeTuple.employee == null)
                                report.AddReportEvent("Не указан сотрудник.");
                            else if (string.IsNullOrEmpty(employeeTuple.email) && employeeTuple.employee != null)
                                report.AddReportEvent("У сотрудника " + employeeTuple.employee.FullName +
                                                       "не указан email.");
                            else if (!string.IsNullOrEmpty(employeeTuple.email) && employeeTuple.employee != null &&
                                     string.IsNullOrEmpty(employeeTuple.emailText))
                                report.AddReportEvent("Для сотрудника " + employeeTuple.employee.FullName + "( " + employeeTuple.email + ") " +
                                                       "не сформирован текст email уведомления.");
                            else
                            {
                                string messageTitle = "Согласуйте трудозатраты по Вашим проектам";
                                string actionsHtml = "";
                                string taskUrl = _smtpConfig.LinkApproveHours;

                                actionsHtml += @"<a href='" + taskUrl + "' title='Откроется страница для согласования трудозатрат...' >Согласовать трудозатраты</a>&nbsp;&nbsp;";
                                actionsHtml += "\r\n";

                                if (_smtpConfig.TimesheetDontSendTSEmailNotificationsTo.Split(';').Any(a => a.Contains(employeeTuple.email)) == false
                                    && (!string.IsNullOrEmpty(_smtpConfig.TimesheetSendTSEmailNotificationsOnlyTo)
                                        && _smtpConfig.TimesheetSendTSEmailNotificationsOnlyTo.Split(';').Any(a => a.Contains(employeeTuple.email))) ||
                                    string.IsNullOrEmpty(_smtpConfig.TimesheetSendTSEmailNotificationsOnlyTo))
                                {
                                    var rpcsUser = _userService.GetUserByLogin(employeeTuple.employee.ADLogin);
                                    if (rpcsUser != null)
                                    {
                                        if (rpcsUser.AllowSendEmailNotifications)
                                        {
                                            RPCSEmailHelper.SendHtmlEmailViaSMTP(employeeTuple.email,
                                                subjectPrefix + messageTitle,
                                                 _smtpConfig.FromEmail,
                                                null,
                                                RPCSEmailHelper.GetSimpleHtmlEmailBody(messageTitle,
                                                    employeeTuple.emailText,
                                                    actionsHtml),
                                                null,
                                                null);
                                            report.AddReportEvent("Отправка email уведомления [" + messageTitle + "] для " + employeeTuple.employee.FullName + " (" + employeeTuple.email + ") " + " - выполнена.");
                                        }
                                        else
                                            report.AddReportEvent("Отправка email уведомления [" + messageTitle + "] для " + employeeTuple.employee.FullName + " (" + employeeTuple.email + ") " + " - не разрешено Администратором ТШ.");
                                    }
                                    else
                                        report.AddReportEvent("Отправка email уведомления [" + messageTitle + "] для " + employeeTuple.employee.FullName + " (" + employeeTuple.email + ") " + " - не выполнена, у сотрудника нету доступа в систему.");
                                }
                                else
                                    report.AddReportEvent("Отправка email уведомления [" + messageTitle + "] для " + employeeTuple.employee.FullName + " (" + employeeTuple.email + ") " + " - не выполнена, так как email адрес добавлен в исключения.");
                            }
                        }

                        report.AddReportEvent("Рассылка email уведомлений о необходимости согласования трудозатрат РП за отчетную неделю - завершено.");
                        SetStatus(90, "Рассылка email уведомлений о необходимости согласования трудозатрат РП за отчетную неделю - завершено.");
                    }
                }
                catch (Exception e)
                {
                    report.AddReportEvent(e.Message + " " + e.StackTrace + " " + e.TargetSite.ToString());
                }

                //TOdo отправить отклоненные трудозатраты
                try
                {
                    report.AddReportEvent("Рассылка email уведомлений о наличии отклоненных трудозатрат - старт.");
                    SetStatus(90, "Рассылка email уведомлений о наличии отклоненных трудозатрат - старт.");

                    foreach (var employeeTuple in SendEmailDeclinedAuthorTS(sendTSEmailNotificationsAtDate))
                    {
                        if (employeeTuple.employee == null)
                            report.AddReportEvent("Не указан сотрудник.");
                        else if (string.IsNullOrEmpty(employeeTuple.email) && employeeTuple.employee != null)
                            report.AddReportEvent("У сотрудника " + employeeTuple.employee.FullName +
                                                   "не указан email.");
                        else if (!string.IsNullOrEmpty(employeeTuple.email) && employeeTuple.employee != null &&
                                 string.IsNullOrEmpty(employeeTuple.emailText))
                            report.AddReportEvent("Для сотрудника " + employeeTuple.employee.FullName + "( " + employeeTuple.email + ") " +
                                                   "не сформирован текст email уведомления.");
                        else
                        {
                            string messageTitle = "Ваши трудозатраты отклонены";
                            string actionsHtml = "";
                            string taskUrl = _smtpConfig.LinkDeclinedHours;

                            actionsHtml += @"<a href='" + taskUrl + "' title='Откроется страница для просмотра и редактирования трудозатрат...' >Просмотреть и отредактировать отклоненные трудозатраты</a>&nbsp;&nbsp;";
                            actionsHtml += "\r\n";
                            if (_smtpConfig.TimesheetDontSendTSEmailNotificationsTo.Split(';').Any(a => a.Contains(employeeTuple.email)) == false
                                && (!string.IsNullOrEmpty(_smtpConfig.TimesheetSendTSEmailNotificationsOnlyTo)
                                    && _smtpConfig.TimesheetSendTSEmailNotificationsOnlyTo.Split(';').Any(a => a.Contains(employeeTuple.email))) ||
                                string.IsNullOrEmpty(_smtpConfig.TimesheetSendTSEmailNotificationsOnlyTo))
                            {
                                var rpcsUser = _userService.GetUserByLogin(employeeTuple.employee.ADLogin);
                                if (rpcsUser != null)
                                {
                                    if (rpcsUser.AllowSendEmailNotifications)
                                    {
                                        RPCSEmailHelper.SendHtmlEmailViaSMTP(employeeTuple.email,
                                            subjectPrefix + messageTitle,
                                            _smtpConfig.FromEmail,
                                            null,
                                            RPCSEmailHelper.GetSimpleHtmlEmailBody(messageTitle + " РП/КАМ",
                                                employeeTuple.emailText,
                                                actionsHtml),
                                            null,
                                            null);
                                        report.AddReportEvent("Отправка email уведомления [" + messageTitle + "] для " + employeeTuple.employee.FullName + " (" + employeeTuple.email + ") " + " - выполнена.");
                                    }
                                    else
                                        report.AddReportEvent("Отправка email уведомления [" + messageTitle + "] для " + employeeTuple.employee.FullName + " (" + employeeTuple.email + ") " + " - не разрешено Администратором ТШ.");
                                }
                                else
                                    report.AddReportEvent("Отправка email уведомления [" + messageTitle + "] для " + employeeTuple.employee.FullName + " (" + employeeTuple.email + ") " + " - не выполнена, у сотрудника нету доступа в систему.");
                            }
                            else
                                report.AddReportEvent("Отправка email уведомления [" + messageTitle + "] для " + employeeTuple.employee.FullName + " (" + employeeTuple.email + ") " + " - не выполнена, так как email адрес добавлен в исключения.");
                        }
                    }

                    report.AddReportEvent("Рассылка email уведомлений о наличии отклоненных трудозатрат - завершено.");
                    SetStatus(90, "Рассылка email уведомлений о наличии отклоненных трудозатрат - завершено.");
                }
                catch (Exception e)
                {
                    report.AddReportEvent(e.Message + " " + e.StackTrace + " " + e.TargetSite.ToString());
                }

                //todo согласовать трудозатраты для PM
                try
                {
                    if (thirdWorkDay || fourthWorkDay || fifthWorkDay)
                    {
                        report.AddReportEvent("Рассылка email уведомлений о просрочке согласования трудозатрат РП - старт.");
                        SetStatus(90, "Рассылка email уведомлений о просрочке согласования трудозатрат РП - старт.");

                        foreach (var employeeTuple in SendEmailEmployeeUrgentlyHaveApproveTSRecords(sendTSEmailNotificationsAtDate))
                        {
                            if (employeeTuple.employee == null)
                                report.AddReportEvent("Не указан сотрудник.");
                            else if (string.IsNullOrEmpty(employeeTuple.email) && employeeTuple.employee != null)
                                report.AddReportEvent("У сотрудника " + employeeTuple.employee.FullName +
                                                       "не указан email.");
                            else if (!string.IsNullOrEmpty(employeeTuple.email) && employeeTuple.employee != null &&
                                     string.IsNullOrEmpty(employeeTuple.emailText))
                                report.AddReportEvent("Для сотрудника " + employeeTuple.employee.FullName + "( " + employeeTuple.email + ") " +
                                                       "не сформирован текст email уведомления.");
                            else
                            {
                                string messageTitle = "Срочно согласуйте трудозатраты по Вашим проектам";
                                string actionsHtml = "";
                                string taskUrl = _smtpConfig.LinkApproveHours;

                                actionsHtml += @"<a href='" + taskUrl + "' title='Откроется страница для согласования трудозатрат...' >Согласовать трудозатраты</a>&nbsp;&nbsp;";
                                actionsHtml += "\r\n";

                                if (_smtpConfig.TimesheetDontSendTSEmailNotificationsTo
                                        .Split(';').Any(a => a.Contains(employeeTuple.email)) == false
                                    && (!string.IsNullOrEmpty(_smtpConfig.TimesheetSendTSEmailNotificationsOnlyTo)
                                        && _smtpConfig.TimesheetSendTSEmailNotificationsOnlyTo
                                            .Split(';').Any(a => a.Contains(employeeTuple.email))) ||
                                    string.IsNullOrEmpty(_smtpConfig.TimesheetSendTSEmailNotificationsOnlyTo))
                                {
                                    var rpcsUser = _userService.GetUserByLogin(employeeTuple.employee.ADLogin);
                                    if (rpcsUser != null)
                                    {
                                        if (rpcsUser.AllowSendEmailNotifications)
                                        {
                                            RPCSEmailHelper.SendHtmlEmailViaSMTP(employeeTuple.email,
                                                subjectPrefix + messageTitle,
                                                _smtpConfig.FromEmail,
                                                null,
                                                RPCSEmailHelper.GetSimpleHtmlEmailBody(messageTitle,
                                                    employeeTuple.emailText,
                                                    actionsHtml),
                                                null,
                                                null);
                                            report.AddReportEvent("Отправка email уведомления [" + messageTitle + "] для " + employeeTuple.employee.FullName + " (" + employeeTuple.email + ") " + " - выполнена.");
                                        }
                                        else
                                            report.AddReportEvent("Отправка email уведомления [" + messageTitle + "] для " + employeeTuple.employee.FullName + " (" + employeeTuple.email + ") " + " - не разрешено Администратором ТШ.");
                                    }
                                    else
                                        report.AddReportEvent("Отправка email уведомления [" + messageTitle + "] для " + employeeTuple.employee.FullName + " (" + employeeTuple.email + ") " + " - не выполнена, у сотрудника нету доступа в систему.");
                                }
                                else
                                    report.AddReportEvent("Отправка email уведомления [" + messageTitle + "] для " + employeeTuple.employee.FullName + " (" + employeeTuple.email + ") " + " - не выполнена, так как email адрес добавлен в исключения.");
                            }
                        }

                        report.AddReportEvent("Рассылка email уведомлений о просрочке согласования трудозатрат РП - завершено.");
                        SetStatus(90, "Рассылка email уведомлений о просрочке согласования трудозатрат РП - завершено.");
                    }
                }
                catch (Exception e)
                {
                    report.AddReportEvent(e.Message + " " + e.StackTrace + " " + e.TargetSite.ToString());
                }

                report.AddReportEvent("Отправка email уведомлений ТШ - завершено.");
                SetStatus(90, "Отправка email уведомлений ТШ - завершено.");
            }

            return report;
        }

        private List<(Employee employee, string emailText)> SendEmailEmployeeNotImportedTSHoursRecordsFromJira(List<(Employee employee, List<(DateTime RecordDate, string JiraIssue, int WorklogId, string Description, double Hours)> records)> employeeTupleList)
        {
            var listEmployeeEmailsTuple = new List<(Employee employee, string emailText)>();
            foreach (var employeeTuple in employeeTupleList)
            {
                if (!string.IsNullOrEmpty(employeeTuple.employee.Email) && employeeTuple.records.Count != 0)
                {
                    var emailText = "<br>" + employeeTuple.employee.FullName + ",<br><br>" +
                                    "У Вас есть записи трудозатрат в Jira, которые не могу быть импортированы в систему Timesheet по причине " +
                                    "отсутствия или введения некорректного кода проекта в Jira по задачам: <br>";
                    RPCSHtmlReport htmlReport = new RPCSHtmlReport();
                    var rowCount = 0;
                    htmlReport.AddHeaderColumn("№");
                    htmlReport.AddHeaderColumn("Отчетная дата");
                    htmlReport.AddHeaderColumn("Задача в Jira");
                    htmlReport.AddHeaderColumn("Состав работ");
                    htmlReport.AddHeaderColumn("Трудозатраты (ч)");
                    foreach (var record in employeeTuple.records.OrderBy(x => x.RecordDate))
                    {
                        rowCount++;
                        try
                        {
                            var jiraIssueLink = "<a href=\"" + _jiraConfig.Issue + record.JiraIssue +
                                 "?focusedWorklogId="
                                 + record.WorklogId +
                                 "&page=com.atlassian.jira.plugin.system.issuetabpanels%3Aworklog-tabpanel#worklog-"
                                 + record.WorklogId + "\" target=\"_blank\" >" +
                                 record.JiraIssue + "</a>";
                            htmlReport.AddReportRow(rowCount.ToString(), record.RecordDate.ToShortDateString(),
                                jiraIssueLink, record.Description,
                                record.Hours.RoundingIntegersAndFractionToTwoDigits());
                        }
                        catch (Exception)
                        {
                        }
                    }

                    emailText += htmlReport.GetHtmlReportContent(null);
                    listEmployeeEmailsTuple.Add((employeeTuple.employee, emailText));
                }
            }

            return listEmployeeEmailsTuple;
        }

        private LongRunningTaskReport SyncWithJIRA(DateTime periodStartDate, DateTime periodEndDate,
            bool deleteJIRASyncedRecordsBeforeSync, DateTime syncWithJIRAAtDate, bool sendEmailNotifications)
        {
            LongRunningTaskReport report = new LongRunningTaskReport("Отчет об обработке данных Timesheet", "Синхронизация данных с Jira");
            string subjectPrefix = string.Empty;

            report.AddReportEvent("Загрузка трудозатрат из JIRA - старт");
            SetStatus(60, "Загрузка трудозатрат из JIRA - старт");

            try
            {
                if (_timesheetConfig.EmailNotificationsSubjectPrefix != null)
                {
                    subjectPrefix = _timesheetConfig.EmailNotificationsSubjectPrefix.Trim() + " ";
                }

            }
            catch (Exception) { }

            try
            {
                if ((periodStartDate == DateTime.MinValue || periodEndDate == DateTime.MinValue)
                    && syncWithJIRAAtDate != DateTime.MinValue)
                {
                    //Автоматически синхронизация с Jira выполняется по понедельникам
                    if (_productionCalendarService.GetAllRecords().Any(cal => cal.CalendarDate == syncWithJIRAAtDate))
                    {
                        if (syncWithJIRAAtDate.DayOfWeek == DayOfWeek.Monday)
                        {
                            //прошлая неделя
                            periodStartDate = syncWithJIRAAtDate.PreviousWeekStart();
                            periodEndDate = syncWithJIRAAtDate.PreviousWeekEnd();
                        }
                    }
                }


                if (periodStartDate != DateTime.MinValue && periodEndDate != DateTime.MinValue
                    && periodStartDate <= periodEndDate)
                {
                    report.AddReportEvent("Загрузка трудозатрат из JIRA за период: " + periodStartDate.ToShortDateString() + " - " + periodEndDate.ToShortDateString());

                    if (deleteJIRASyncedRecordsBeforeSync)
                    {
                        var removeTSHoursRecordsJira = _tsHoursRecordService.Get(x =>
                            x.Where(r => r.RecordSource == TSRecordSource.JIRA && (r.RecordDate >= periodStartDate && r.RecordDate <= periodEndDate)).ToList());
                        _tsHoursRecordService.RemoveRange(removeTSHoursRecordsJira);
                    }

                    var descriptionTruncateCharacters = _jiraConfig.TSHoursRecordDescriptionTruncateCharacters != null ? Convert.ToInt16(_jiraConfig.TSHoursRecordDescriptionTruncateCharacters ) : 300;

                    var usersLogin = _userService.GetList().Select(x => x.UserLogin);
                    var employeeNotifyList = new List<(Employee employee, List<(DateTime RecordDate, string JiraIssue, int WorklogId, string Description, double Hours)> records)>();
                    foreach (var userLogin in usersLogin)
                    {
                        try
                        {
                            //получить данные из JIra за прошлую неделю по пользователю
                            var userJiraLogin = ADHelper.GetUserLoginWithoutDomainName(userLogin);
                            var employee = _employeeService.Get(x => x.Where(e => e.ADLogin.ToLower() == userLogin).ToList()).FirstOrDefault();
                            if (employee != null)
                            {
                                var rpcsUser = _userService.GetUserByLogin(employee.ADLogin);

                                var searchUrl = _jiraService.CreateUrlWorklogsByUser(periodStartDate, periodEndDate, userJiraLogin);
                                var responseJson = _jiraService.GetJson(searchUrl);
                                var jiraBaseModel = JsonConvert.DeserializeObject<JiraBaseModel>(responseJson);
                                var tsHoursRecordJiraList = new List<TSHoursRecord>();

                                var jiraWorklogNotImportedRecordList = new List<(DateTime RecordDate, string JiraIssue, int WorklogId, string Description, double Hours)>();
                                foreach (var issue in jiraBaseModel.Issues)
                                {
                                    issue.Fields.WorkLogs = _jiraService.GetWorklogsByIssueId(issue.Id);
                                    foreach (var worklog in issue.Fields.WorkLogs)
                                    {
                                        if (String.IsNullOrEmpty(worklog.Author.Name) == false
                                            && String.IsNullOrEmpty(userJiraLogin) == false
                                            && worklog.Author.Name.ToLower() == userJiraLogin.ToLower()
                                            && worklog.Started >= periodStartDate.StartOfDay() && worklog.Started <= periodEndDate.EndOfDay())
                                        {
                                            var hours = Math.Ceiling(((double)worklog.TimeSpentSeconds / 3600) * 4) / 4;
                                            var recordDate = worklog.Started.Date;

                                            var projectShortName = _jiraService.GetProjectShortNameFromEpic(issue);
                                            var project = _projectService.GetByShortName(projectShortName);
                                            if (project == null)
                                                projectShortName = string.Empty;

                                            if (String.IsNullOrEmpty(projectShortName))
                                                project = _projectService.GetByShortName(_jiraService.GetProjectShortNameFromExternalWorkspace(issue, userJiraLogin, recordDate, recordDate));

                                            if (project != null)
                                            {
                                                if (project.AutoImportTSRecordFromJIRA)
                                                {

                                                    var jiraProjectName = issue.Key;
                                                    var description = jiraProjectName + " - " + worklog.Comment;

                                                    description = description.RemoveUnwantedHtmlTags();
                                                    description = RPCSHelper.NormalizeAndTrimString(description);

                                                    tsHoursRecordJiraList.Add(new TSHoursRecord()
                                                    {
                                                        Project = project,
                                                        ProjectID = project.ID,
                                                        RecordDate = recordDate,
                                                        Hours = hours,
                                                        Description = description,
                                                        RecordStatus = TSRecordStatus.Approving,
                                                        RecordSource = TSRecordSource.JIRA,
                                                        ExternalSourceElementID = worklog.Id.ToString()
                                                    });

                                                    report.AddReportEvent("Получена запись ТШ из Jira. Проект: " + project.ShortName + ", дата: " + recordDate.ToShortDateString() + ", сотрудник: " + employee.FullName + ", описание: " + description);
                                                    SetStatus(60, "Получена запись ТШ из Jira. Проект: " + project.ShortName + ", дата: " + recordDate.ToShortDateString() + ", сотрудник: " + employee.FullName);
                                                }
                                                else
                                                    report.AddReportEvent("Для проекта " + project.ShortName + " не разрешен автоматический импорт данных по трудозатратам из Jira");

                                            }//Если проект был не найден
                                            else if (sendEmailNotifications && rpcsUser != null && rpcsUser.AllowSendEmailNotifications)
                                            {
                                                if (String.IsNullOrEmpty(worklog.Author.Name) == false && String.IsNullOrEmpty(userJiraLogin) == false
                                                                                                           && worklog.Author.Name.ToLower() == userJiraLogin.ToLower() &&
                                                                                                           worklog.Started >= periodStartDate.StartOfDay() && worklog.Started <= periodEndDate.EndOfDay())
                                                {
                                                    var jiraProjectName = issue.Key;
                                                    jiraWorklogNotImportedRecordList.Add((recordDate, jiraProjectName, worklog.Id, worklog.Comment.TruncateAtWord(descriptionTruncateCharacters), hours));
                                                }
                                            }
                                        }
                                    }
                                }

                                employeeNotifyList.Add((employee, jiraWorklogNotImportedRecordList.OrderBy(x => x.RecordDate).ThenBy(x => x.JiraIssue).ToList()));
                                //загрузка тш из jira
                                foreach (var tsHoursRecordJira in tsHoursRecordJiraList)
                                {
                                    var existingTSHoursRecord = _tsHoursRecordService.Get(x =>
                                        x.Where(r => r.ExternalSourceElementID == tsHoursRecordJira.ExternalSourceElementID
                                            /*&& (r.RecordStatus == TSRecordStatus.Editing || r.RecordStatus == TSRecordStatus.DeclinedEditing || r.RecordStatus == TSRecordStatus.Approving)*/
                                            //если оставить это условие, то будут создаваться дубликаты записей, которые в ТШ уже утверждены РП
                                            && r.RecordSource == TSRecordSource.JIRA).ToList()).FirstOrDefault();
                                    if (existingTSHoursRecord != null)
                                    {
                                        //обновление записи
                                        if (existingTSHoursRecord.Project.AllowTSRecordOnlyWorkingDays && _productionCalendarService.GetRecordByDate(existingTSHoursRecord.RecordDate.Value)
                                                .WorkingHours == 0)
                                        {
                                            report.AddReportEvent("Списание трудозатрат на нерабочие дни для проекта " + existingTSHoursRecord.Project.ShortName + " запрещено.");
                                            continue;
                                        }

                                        //не обновлять если ничего не изменилось
                                        if (existingTSHoursRecord.ProjectID == tsHoursRecordJira.ProjectID
                                            && existingTSHoursRecord.Hours == tsHoursRecordJira.Hours
                                            && existingTSHoursRecord.Description == tsHoursRecordJira.Description
                                            && existingTSHoursRecord.RecordDate == tsHoursRecordJira.RecordDate)
                                        {
                                            //report.AddReportEvent("Запись " + existingTSHoursRecord.Project.ShortName + " " + existingTSHoursRecord.RecordDate.Value.ToShortDateString() + " не изменялась.");
                                            continue;
                                        }

                                        //если запись уже имеет статут "Согласовано РП" - не изменять
                                        if (existingTSHoursRecord.RecordStatus == TSRecordStatus.PMApproved ||
                                            existingTSHoursRecord.RecordStatus == TSRecordStatus.HDApproved)
                                        {
                                            report.AddReportEvent("Запись ТШ уже согласована. Проект: " + existingTSHoursRecord.Project.ShortName + ", дата: " + existingTSHoursRecord.RecordDate.Value.ToShortDateString() +
                                                ", сотрудник: " + employee.FullName + ", описание: " + tsHoursRecordJira.Description);
                                            continue;
                                        }

                                        existingTSHoursRecord.EmployeeID = employee.ID;
                                        existingTSHoursRecord.ProjectID = tsHoursRecordJira.ProjectID;
                                        existingTSHoursRecord.Hours = tsHoursRecordJira.Hours;
                                        existingTSHoursRecord.RecordDate = tsHoursRecordJira.RecordDate;
                                        existingTSHoursRecord.Description =
                                            tsHoursRecordJira.Description.TruncateAtWord(descriptionTruncateCharacters);
                                        _tsHoursRecordService.UpdateWithoutVersion(existingTSHoursRecord);

                                        report.AddReportEvent("Обновлена запись ТШ. Проект: " + tsHoursRecordJira.Project.ShortName + ", дата: " + tsHoursRecordJira.RecordDate.Value.ToShortDateString() +
                                                              ", сотрудник: " + employee.FullName + ", описание: " + tsHoursRecordJira.Description);
                                        SetStatus(60, "Обновлена запись ТШ. Проект: " + tsHoursRecordJira.Project.ShortName + ", дата: " + tsHoursRecordJira.RecordDate.Value.ToShortDateString() +
                                            ", сотрудник: " + employee.FullName);

                                    }
                                    else
                                    {
                                        if (tsHoursRecordJira.Project.AllowTSRecordOnlyWorkingDays &&
                                            _productionCalendarService
                                                .GetRecordByDate(tsHoursRecordJira.RecordDate.Value).WorkingHours == 0)
                                        {
                                            report.AddReportEvent(
                                                "Списание трудозатрат на нерабочие дни для проекта " +
                                                tsHoursRecordJira.Project.ShortName + " запрещено.");
                                            continue;
                                        }

                                        var tsHoursRecord = new TSHoursRecord
                                        {
                                            EmployeeID = employee.ID,
                                            ProjectID = tsHoursRecordJira.ProjectID,
                                            Hours = tsHoursRecordJira.Hours,
                                            RecordSource = tsHoursRecordJira.RecordSource,
                                            RecordStatus = TSRecordStatus.Approving,
                                            RecordDate = tsHoursRecordJira.RecordDate,
                                            Description =
                                                tsHoursRecordJira.Description.TruncateAtWord(
                                                    descriptionTruncateCharacters),
                                            ExternalSourceElementID = tsHoursRecordJira.ExternalSourceElementID
                                        };
                                        _tsHoursRecordService.Add(tsHoursRecord);

                                        report.AddReportEvent("Создана запись ТШ. Проект: " +
                                                              tsHoursRecordJira.Project.ShortName + ", дата: " +
                                                              tsHoursRecordJira.RecordDate.Value.ToShortDateString() +
                                                              ", сотрудник: " + employee.FullName + ", описание: " +
                                                              tsHoursRecordJira.Description);
                                        SetStatus(60, "Создана запись ТШ. Проект: " + tsHoursRecordJira.Project.ShortName + ", дата: " + tsHoursRecordJira.RecordDate.Value.ToShortDateString() +
                                            ", сотрудник: " + employee.FullName);
                                    }
                                }
                            }
                        }
                        catch (WebException ex)
                        {
                            using (var stream = ex.Response.GetResponseStream())
                            using (var reader = new StreamReader(stream))
                            {
                                report.AddReportEvent(reader.ReadToEnd());
                            }
                        }
                        catch (Exception e)
                        {
                            SetStatus(-1, "Ошибка: " + e.Message);
                            report.AddReportEvent(e.Message + " " + e.StackTrace + " " + e.TargetSite.ToString());
                        }
                    }
                    //Формирование емейл сообщения о неимпортированных проектах

                    if (sendEmailNotifications == true)
                    {
                        report.AddReportEvent("Формирование Email сообщений о неимпортированных проектах - старт");
                        foreach (var employeeNotify in SendEmailEmployeeNotImportedTSHoursRecordsFromJira(employeeNotifyList))
                        {
                            string messageTitle = "Ваши трудозатраты не могут быть импортированы из Jira";
                            string taskUrl = _smtpConfig.LinkMyHours;
                            string actionsHtml = @"<a href='" + taskUrl + "' title='Откроется страница для согласования трудозатрат...' >Посмотреть мои трудозатраты</a>&nbsp;&nbsp;";
                            actionsHtml += "\r\n";

                            RPCSEmailHelper.SendHtmlEmailViaSMTP(employeeNotify.employee.Email,
                                subjectPrefix + messageTitle,
                                _smtpConfig.FromEmail,
                                null,
                                RPCSEmailHelper.GetSimpleHtmlEmailBody("Записи трудозатрат не могут быть импортированы из Jira",
                                    employeeNotify.emailText,
                                    actionsHtml),
                                null,
                                null);
                            report.AddReportEvent("Отправка email уведомления о не импортированных трудозатратах [" + messageTitle + "] для " + employeeNotify.employee.FullName + " (" + employeeNotify.employee.Email + ") " + " - выполнена.");
                        }
                    }
                }
                else
                {
                    report.AddReportEvent("Получение данных из Jira не выполнялось.");
                }
            }
            catch (Exception e)
            {
                SetStatus(-1, "Ошибка: " + e.Message);
                report.AddReportEvent(e.Message + " " + e.StackTrace + " " + e.TargetSite.ToString());
            }

            report.AddReportEvent("Загрузка трудозатрат из JIRA - завершено");
            SetStatus(60, "Загрузка трудозатрат из JIRA - завершено");

            return report;
        }

        private List<(Employee employee, string email, string emailText)> SendEmailEmployeeUrgentlyHaveApproveTSRecords(DateTime specifiedDate)
        {
            var listEmployeeEmails = new List<(Employee employee, string email, string emailText)>();
            var employeeList = _employeeService.GetCurrentEmployees(new DateTimeRange(specifiedDate, specifiedDate)).Where(e => String.IsNullOrEmpty(e.Email) == false);

            foreach (var employee in employeeList)
            {
                SetStatus(90, "Проверка просрочки согласования трудозатрат РП: " + employee.FullName);

                var haveRecordsForApproving = false;
                var rowCount = 0;

                var emailText = "<br>" + employee.FullName + ",<br><br>" + "У Вас есть трудозатраты на согласовании, которые необходимо срочно согласовать. <br>";

                RPCSHtmlReport htmlReport = new RPCSHtmlReport();

                htmlReport.AddHeaderColumn("№");
                htmlReport.AddHeaderColumn("Сотрудник");
                htmlReport.AddHeaderColumn("Отчетная дата");
                htmlReport.AddHeaderColumn("Состав работ");
                htmlReport.AddHeaderColumn("Трудозатраты (ч)");

                //получить все проекты РП где он указан
                var projectsPM = _projectService.Get(x => x.ToList()).Where(p => p.ApproveHoursEmployeeID == employee.ID).ToList();
                foreach (var project in projectsPM)
                {
                    var projectInfo = "Проект: " + project.ShortName;
                    //Если указана в поле дата - для тестирования
                    var specifiedDateTime = specifiedDate.PreviousWeekEnd();
                    var recordsTsApprovingGroupByEmployee = _tsHoursRecordService.Get(records => records.Where(x =>
                       x.RecordStatus == TSRecordStatus.Approving &&
                       x.ProjectID == project.ID &&
                       x.RecordDate <= specifiedDateTime).ToList()).GroupBy(p => p.Employee);

                    bool projectInfoAdded = false;
                    foreach (var employeeRecords in recordsTsApprovingGroupByEmployee)
                    {
                        haveRecordsForApproving = true;
                        foreach (var record in employeeRecords.OrderBy(x => x.RecordDate))
                        {
                            if (projectInfoAdded == false)
                            {
                                htmlReport.AddReportSection(projectInfo);
                                projectInfoAdded = true;
                            }
                            rowCount++;
                            try
                            {
                                htmlReport.AddReportRow(rowCount.ToString(),
                                    employeeRecords.First().Employee.FullName,
                                    record.RecordDate.Value.ToShortDateString(),
                                    record.Description,
                                    record.Hours.Value.RoundingIntegersAndFractionToTwoDigits());

                            }
                            catch (Exception)
                            {
                            }
                        }
                    }
                }

                emailText += htmlReport.GetHtmlReportContent(null);

                if (haveRecordsForApproving)
                {
                    listEmployeeEmails.Add((employee, employee.Email, emailText));
                }
            }
            return listEmployeeEmails;
        }


        private List<(Employee employee, string email, string emailText)> SendEmailDeclinedAuthorTS(DateTime specifiedDate)
        {
            var listEmployeeEmails = new List<(Employee employee, string email, string emailText)>();
            var employeeList = _employeeService.GetCurrentEmployees(new DateTimeRange(specifiedDate, specifiedDate)).Where(e => String.IsNullOrEmpty(e.Email) == false);
            foreach (var employee in employeeList)
            {
                SetStatus(90, "Получение отклоненных трудозатрат: " + employee.FullName);

                var emailText = string.Empty;
                var tsHoursRecords = _tsHoursRecordService.Get(records => records.Where(x => x.RecordStatus == TSRecordStatus.Declined && x.EmployeeID == employee.ID).ToList());
                if (tsHoursRecords.Count != 0)
                {
                    emailText += employee.FullName + ",<br><br>";
                    var approveHoursEmployeeOfTSHoursRecordList = tsHoursRecords.Select(pr => pr.Project.ApproveHoursEmployee).Distinct();

                    emailText += "У Вас есть трудозатраты, которые отклонили <b>РП/КАМ</b>: <br><br>";

                    RPCSHtmlReport htmlReport = new RPCSHtmlReport();

                    htmlReport.AddHeaderColumn("№");
                    htmlReport.AddHeaderColumn("Отчетная дата");
                    htmlReport.AddHeaderColumn("Проект");
                    htmlReport.AddHeaderColumn("Состав работ");
                    htmlReport.AddHeaderColumn("Трудозатраты (ч)");
                    htmlReport.AddHeaderColumn("Отклонено с комментарием");

                    var rowCount = 0;
                    foreach (var approveEmployee in approveHoursEmployeeOfTSHoursRecordList)
                    {

                        htmlReport.AddReportSection(approveEmployee.FullName + " отклонил трудозатраты:");
                        //все отклоненные даты.Конкретный проект ни конкретного РП
                        foreach (var tsHoursRecord in tsHoursRecords.Where(x => x.Project.ApproveHoursEmployeeID == approveEmployee.ID))
                        {
                            rowCount++;
                            try
                            {
                                htmlReport.AddReportRow(rowCount.ToString(),
                                            tsHoursRecord.RecordDate.Value.ToShortDateString(),
                                            tsHoursRecord.Project.ShortName,
                                            tsHoursRecord.Description,
                                            tsHoursRecord.Hours.Value.RoundingIntegersAndFractionToTwoDigits(),
                                            (string.IsNullOrEmpty(tsHoursRecord.PMComment) ? "-" : tsHoursRecord.PMComment));
                            }
                            catch (Exception)
                            {

                            }
                        }
                    }

                    emailText += htmlReport.GetHtmlReportContent(null);

                    emailText += " <br>Вам необходимо срочно отредактировать свои трудозатраты и повторно направить их на согласование. <br>";
                    listEmployeeEmails.Add((employee, employee.Email, emailText));
                }

            }

            return listEmployeeEmails;
        }

        //Тodo у руководителя проекта есть записи ТШ
        private List<(Employee employee, string email, string emailText)> SendEmailEmployeeHaveApproveTSRecords(DateTime specifiedDate)
        {
            var listEmployeeEmails = new List<(Employee employee, string email, string emailText)>();
            var employeeList = _employeeService.GetCurrentEmployees(new DateTimeRange(specifiedDate, specifiedDate)).Where(e => String.IsNullOrEmpty(e.Email) == false);

            //пройти по всем РП
            foreach (var employee in employeeList)
            {
                SetStatus(90, "Проверка наличия трудозатрат на согласовании РП за отчетную неделю: " + employee.FullName);

                var haveRecordsForApproving = false;
                var rowCount = 0;

                var emailText = employee.FullName + ",<br><br>" + "У Вас есть трудозатраты на согласовании, которое нужно выполнить не позднее третьего рабочего дня недели, следующей за отчетной. <br>";

                RPCSHtmlReport htmlReport = new RPCSHtmlReport();

                htmlReport.AddHeaderColumn("№");
                htmlReport.AddHeaderColumn("Сотрудник");
                htmlReport.AddHeaderColumn("Отчетная дата");
                htmlReport.AddHeaderColumn("Состав работ");
                htmlReport.AddHeaderColumn("Трудозатраты (ч)");

                //получить все их проекты
                var projectsForPM = _projectService.Get(x => x.ToList()).Where(p => p.ApproveHoursEmployeeID == employee.ID).ToList();
                //пройтись по каждому проекты
                foreach (var project in projectsForPM)
                {
                    var projectInfo = "Проект: " + project.ShortName;

                    //Если указана в поле дата - для тестирования
                    var specifiedDateTime = specifiedDate.PreviousWeekEnd();

                    var recordsTsApprovingGroupByEmployee = _tsHoursRecordService.Get(records => records.Where(x =>
                        x.RecordStatus == TSRecordStatus.Approving &&
                        x.ProjectID == project.ID &&
                        x.RecordDate <= specifiedDateTime).ToList()).GroupBy(p => p.Employee);

                    bool projectInfoAdded = false;
                    //пройтись по всем записям сотрудника
                    foreach (var employeeRecords in recordsTsApprovingGroupByEmployee)
                    {
                        haveRecordsForApproving = true;
                        foreach (var record in employeeRecords.OrderBy(x => x.RecordDate))
                        {
                            if (projectInfoAdded == false)
                            {
                                htmlReport.AddReportSection(projectInfo);
                                projectInfoAdded = true;
                            }

                            rowCount++;
                            try
                            {
                                htmlReport.AddReportRow(rowCount.ToString(),
                                employeeRecords.First().Employee.FullName,
                                record.RecordDate.Value.ToShortDateString(),
                                record.Description,
                                record.Hours.Value.RoundingIntegersAndFractionToTwoDigits());
                            }
                            catch (Exception)
                            {

                            }
                        }
                    }
                }

                emailText += htmlReport.GetHtmlReportContent(null);

                if (haveRecordsForApproving)
                {
                    listEmployeeEmails.Add((employee, employee.Email, emailText));
                }
            }
            return listEmployeeEmails;
        }


        //прошла неделя и сотрудник не отправил на согласование трудозатраты
        private List<(Employee employee, string email, string emailText)> SendEmailOfHoursRecordForWeek(DateTime specifiedDate)
        {
            var listEmployeeEmails = new List<(Employee employee, string email, string emailText)>();
            var employeeList = _employeeService.GetCurrentEmployees(new DateTimeRange(specifiedDate, specifiedDate)).Where(e => String.IsNullOrEmpty(e.Email) == false && (e.EnrollmentDate == null || e.EnrollmentDate <= specifiedDate.PreviousWeekEnd()));

            foreach (var employee in employeeList)
            {
                SetStatus(90, "Проверка введенных трудозатрат: " + employee.FullName);

                //открытые отчетные месяцы
                var listReportingPeriods = _reportingPeriodService
                    .GetAll(periods => periods.NewTSRecordsAllowedUntilDate >= specifiedDate).OrderBy(p => p.FullName).ToList();

                RPCSHtmlReport htmlReport = new RPCSHtmlReport();

                htmlReport.AddHeaderColumn("№");
                htmlReport.AddHeaderColumn("Неделя");
                htmlReport.AddHeaderColumn("Рабочее время (ч)");
                htmlReport.AddHeaderColumn("Ваши трудозатраты (ч)");
                htmlReport.AddHeaderColumn("Недозагрузка (ч)");

                var rowCount = 0;

                var emailText = employee.FullName + ",<br><br>Вы не отправили на согласование свои трудозатраты за недели:<br>";
                //пройтись по всем открытым месяцам, для каждого сотрудника
                foreach (var reportingPeriod in listReportingPeriods)
                {
                    //Дата начала отчетного месяца с первого дня месяца
                    var firstDayOfReportingPeriod = new DateTime(reportingPeriod.Year, reportingPeriod.Month, 1).FirstDayOfMonth();
                    //Дата окончания отчетного месяца - 31.01 или другое
                    var lastDayOfReportingPeriod = new DateTime(reportingPeriod.Year, reportingPeriod.Month, 1).LastDayOfMonth();

                    if (specifiedDate.PreviousWeekEnd() < firstDayOfReportingPeriod)
                        continue;

                    var employeeWorkingCalendarDatesForEmployee = _productionCalendarService
                        .GetRecordsForDateRange(new DateTimeRange(firstDayOfReportingPeriod, specifiedDate.PreviousWeekEnd() < lastDayOfReportingPeriod ? specifiedDate.PreviousWeekEnd() : lastDayOfReportingPeriod))
                            .Where(cal => cal.WorkingHours > 0).Select(x => x.CalendarDate);

                    var listWorkingCalendarWeeksForEmployee = employeeWorkingCalendarDatesForEmployee.GroupBy(x =>
                        CultureInfo.CurrentCulture.DateTimeFormat.Calendar
                            .GetWeekOfYear(x, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday)).Select(gx => new
                            {
                                Week = gx.Key,
                                Year = gx.GroupBy(x => x.Year).Select(x => x.Key).First(),
                                Dates = gx.ToList(),
                            });

                    //проходим по каждой неделе
                    foreach (var workingCalendarWeekForEmployee in listWorkingCalendarWeeksForEmployee)
                    {
                        //количество часов в данной неделе
                        var sumWorkingHoursInCalendar = 0;
                        foreach (var workingDate in workingCalendarWeekForEmployee.Dates)
                        {
                            sumWorkingHoursInCalendar += _productionCalendarService.GetRecordByDate(workingDate).WorkingHours;
                        }

                        ;
                        //количество рабочих часов в неделю с учетом статусов
                        var sumWorkingHoursInTSForEmployee = _tsHoursRecordService
                            .GetRecordsForWeek(workingCalendarWeekForEmployee.Year, workingCalendarWeekForEmployee.Week,
                                employee.ID)
                            .Where(status => status.RecordStatus == TSRecordStatus.Approving ||
                                             status.RecordStatus == TSRecordStatus.PMApproved ||
                                             status.RecordStatus == TSRecordStatus.HDApproved
                                             || status.RecordStatus == TSRecordStatus.Declined ||
                                             status.RecordStatus == TSRecordStatus.DeclinedEditing).Sum(x => x.Hours);
                        if (sumWorkingHoursInCalendar != 0 && sumWorkingHoursInTSForEmployee.HasValue &&
                            sumWorkingHoursInCalendar - sumWorkingHoursInTSForEmployee > 0)
                        {

                            rowCount++;
                            try
                            {
                                htmlReport.AddReportRow(rowCount.ToString(),
                                    workingCalendarWeekForEmployee.Dates.First().ToShortDateString() + " - " + workingCalendarWeekForEmployee.Dates.Last().ToShortDateString(),
                                    sumWorkingHoursInCalendar.ToString(),
                                    sumWorkingHoursInTSForEmployee.Value.RoundingIntegersAndFractionToTwoDigits(),
                                    (sumWorkingHoursInCalendar - sumWorkingHoursInTSForEmployee).Value.RoundingIntegersAndFractionToTwoDigits());
                            }
                            catch (Exception)
                            {
                            }
                        }
                    }
                }
                emailText += htmlReport.GetHtmlReportContent(null);
                if (rowCount != 0)
                {
                    listEmployeeEmails.Add((employee, employee.Email, emailText));
                }
            }

            return listEmployeeEmails;
        }

        public TimesheetProcessingResult ProcessLongRunningAction(string userIdentityName, string id,
            bool syncWithExternalTimesheet,
            DateTime syncWithExtTSPeriodStart, DateTime syncWithExtTSPeriodEnd,
            bool deleteExtTSSyncedRecordsBeforeSync, bool updateExtTSAlreadyAddedRecords, bool getHoursFromExternalTimesheet, bool getVacationsFromExternalTimesheet, bool stopOnSyncWithExternalTSError, int batchSaveRecordsLimitOnSyncWithExternalTS,
            bool processVacationRecords,
            bool processTSAutoHoursRecords,
            bool sendTSEmailNotifications,
            DateTime sendTSEmailNotificationsAtDate,
            bool syncWithJIRA,
            DateTime syncWithJIRAPeriodStart, DateTime syncWithJIRAPeriodEnd,
            bool deleteJIRASyncedRecordsBeforeSync, DateTime syncWithJIRAAtDate, bool syncWithJIRASendEmailNotifications)
        {

            taskId = id;

            LongRunningTaskReport syncWithExternalTimesheetProcessingReport = null;
            var htmlSyncWithExternalTimesheetReport = string.Empty;

            LongRunningTaskReport vacationRecordsProcessingReport = null;
            var htmlVacationReport = string.Empty;

            LongRunningTaskReport tsAutoHoursRecordsProcessingReport = null;
            var htmlAutoHoursReport = string.Empty;

            LongRunningTaskReport tsEmailNotification = null;
            var htmlTSEmailNotificationReport = string.Empty;

            LongRunningTaskReport syncWithJIRAReport = null;
            var htmlSyncWithJIRAReport = string.Empty;

            var htmlErrorReport = string.Empty;

            try
            {
                SetStatus(0, "Старт синхронизации...");

                if (syncWithExternalTimesheet == true)
                {
                    syncWithExternalTimesheetProcessingReport = SyncWithExternalTimesheet(syncWithExtTSPeriodStart, syncWithExtTSPeriodEnd,
                        deleteExtTSSyncedRecordsBeforeSync, updateExtTSAlreadyAddedRecords,
                        getHoursFromExternalTimesheet, getVacationsFromExternalTimesheet,
                        stopOnSyncWithExternalTSError, batchSaveRecordsLimitOnSyncWithExternalTS);
                }
                if (syncWithJIRA)
                {
                    SetStatus(60, "Синхронизация с Jira");
                    syncWithJIRAReport = SyncWithJIRA(syncWithJIRAPeriodStart, syncWithJIRAPeriodEnd,
                        deleteJIRASyncedRecordsBeforeSync, syncWithJIRAAtDate, syncWithJIRASendEmailNotifications);
                }

                if (processVacationRecords == true)
                {
                    SetStatus(60, "Обработка записей отпусков");
                    vacationRecordsProcessingReport = ProcessVacationRecords();
                }

                if (processTSAutoHoursRecords)

                {
                    SetStatus(80, "Обработка записей автозагрузки");
                    tsAutoHoursRecordsProcessingReport = ProcessTSAutoHoursRecords();
                }

                if (sendTSEmailNotifications)
                {
                    SetStatus(90, "Отправка e-mail уведомлений");
                    tsEmailNotification = SendTSEmailNotification(sendTSEmailNotificationsAtDate);
                }

                SetStatus(100, "Обработка завершена");
            }
            catch (Exception e)
            {
                SetStatus(-1, "Ошибка: " + e.Message);
                htmlErrorReport += "<br>" + e.Message + "<br>" + e.StackTrace + "<br>" + e.TargetSite.ToString();
            }

            try
            {
                if (syncWithExternalTimesheetProcessingReport != null)
                    htmlSyncWithExternalTimesheetReport = syncWithExternalTimesheetProcessingReport.GenerateHtmlReport();
                if (vacationRecordsProcessingReport != null)
                    htmlVacationReport = vacationRecordsProcessingReport.GenerateHtmlReport();
                if (tsAutoHoursRecordsProcessingReport != null)
                    htmlAutoHoursReport = tsAutoHoursRecordsProcessingReport.GenerateHtmlReport();
                if (tsEmailNotification != null)
                    htmlTSEmailNotificationReport = tsEmailNotification.GenerateHtmlReport();
                if (syncWithJIRAReport != null)
                    htmlSyncWithJIRAReport = syncWithJIRAReport.GenerateHtmlReport();
            }
            catch (Exception e)
            {
                SetStatus(-1, "Ошибка: " + e.Message);
                htmlErrorReport += "<br>" + e.Message + "<br>" + e.StackTrace + "<br>" + e.TargetSite.ToString();
            }

            return new TimesheetProcessingResult()
            {
                fileId = id,
                fileHtmlReport = new List<string>() { htmlSyncWithExternalTimesheetReport, htmlVacationReport, htmlAutoHoursReport, htmlErrorReport, htmlTSEmailNotificationReport, htmlSyncWithJIRAReport }
            };
        }
    }
}
