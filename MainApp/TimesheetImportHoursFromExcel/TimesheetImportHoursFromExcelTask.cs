using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Core.BL.Interfaces;
using Core.Common;
using Core.Models;


namespace MainApp.TimesheetImportHoursFromExcel
{
    public class TimesheetImportHoursFromExcelTask : LongRunningTaskBase
    {
        private readonly ITSHoursRecordService _tsHoursRecordService;
        private readonly IProductionCalendarService _productionCalendarService;
        private readonly IEmployeeService _employeeService;
        private readonly IProjectService _projectService;
        private readonly IUserService _userService;


        public TimesheetImportHoursFromExcelTask(ITSHoursRecordService tsHoursRecordService,
            IProductionCalendarService productionCalendarService,
            IEmployeeService employeeService,
            IProjectService projectService,
            IUserService userService)
        {
            _tsHoursRecordService = tsHoursRecordService ?? throw new ArgumentNullException(nameof(tsHoursRecordService));
            _productionCalendarService = productionCalendarService;
            _employeeService = employeeService;
            _projectService = projectService;
            _userService = userService;
        }

        private LongRunningTaskReport ImportTSHoursRecords(DataTable timesheetHoursRecordSheetDataTable, int reportMonth, int reportHoursInMonth,
            int reportYear, bool onlyValidate, bool rewriteTSHoursRecords, string currentUserName, string currentUserSID)
        {
            LongRunningTaskReport report = new LongRunningTaskReport("Отчет о загрузке трудозатрат за месяц", "");
            var dateNow = DateTime.Now;
            var countEmployee = 0;

            DateTime recordDate = _productionCalendarService.GetLastWorkDayInMonth(reportYear, reportMonth).CalendarDate;
            Hashtable projectIdsByShortName = new Hashtable();

            for (int k = 1; k <= timesheetHoursRecordSheetDataTable.Columns.Count - 1; k++)
            {
                try
                {
                    var projectShortName = timesheetHoursRecordSheetDataTable.Rows[0][k].ToString().Trim();
                    Project project = _projectService.GetByShortName(projectShortName);

                    if (project != null)
                    {
                        projectIdsByShortName[project.ShortName] = project.ID;
                    }
                    else
                    {
                        report.AddReportEvent("Проект: " + projectShortName + " не найден в БД Проекты");
                    }
                }
                catch (Exception)
                {

                }
            }

            List<TSHoursRecord> existTSHoursRecords = _tsHoursRecordService.GetAll(x => x.RecordSource == TSRecordSource.ExcelImportByMonth
                                                            && x.RecordDate == recordDate).ToList();

            for (int i = 1; i <= timesheetHoursRecordSheetDataTable.Rows.Count - 1; i++)
            {
                var employeeFullName = timesheetHoursRecordSheetDataTable.Rows[i][0].ToString().Trim();
                if (!string.IsNullOrEmpty(employeeFullName))
                {
                    SetStatus(60 + countEmployee * 39 / timesheetHoursRecordSheetDataTable.Rows.Count, "Обработка записей по сотруднику: " + employeeFullName);

                    Employee employee = _employeeService.FindEmployeeByFullName(employeeFullName);

                    if (employee != null)
                    {
                        if (onlyValidate == false)
                        {
                            for (int j = 1; j <= timesheetHoursRecordSheetDataTable.Columns.Count - 1; j++)
                            {
                                try
                                {

                                    var projectShortName = timesheetHoursRecordSheetDataTable.Rows[0][j].ToString().Trim();
                                    var hours = timesheetHoursRecordSheetDataTable.Rows[i][j].ToString().Trim();

                                    SetStatus(60 + countEmployee * 39 / timesheetHoursRecordSheetDataTable.Rows.Count, "Обработка записи по сотруднику: " + employeeFullName + " на проект: " + projectShortName);

                                    if (!string.IsNullOrEmpty(employeeFullName) && !string.IsNullOrEmpty(hours))
                                    {
                                        if (projectIdsByShortName.ContainsKey(projectShortName))
                                        {
                                            int projectID = Convert.ToInt32(projectIdsByShortName[projectShortName]);
                                            var isHours = double.TryParse(hours.Replace('.', ','), out double doubleHours);

                                            if (isHours == false)
                                            {
                                                report.AddReportEvent("Для сотрудника: " + employeeFullName + " на проект: " + projectShortName + " значение часов - не число.");
                                            }
                                            else
                                            {
                                                //Удаление записи если уставновлена галочка
                                                if (rewriteTSHoursRecords)
                                                {
                                                    var tsHoursRecord = existTSHoursRecords.Where(x =>
                                                                x.EmployeeID == employee.ID
                                                                /*&& x.RecordSource == TSRecordSource.ExcelImportByMonth*/
                                                                && x.ProjectID == projectID
                                                            /*&& x.RecordDate == recordDate*/)
                                                        .FirstOrDefault();
                                                    if (tsHoursRecord != null)
                                                    {
                                                        _tsHoursRecordService.Delete(tsHoursRecord.ID);
                                                        report.AddReportEvent(
                                                            "Успешное удаление записи трудозатрат для сотрудника: " + employeeFullName + ", проект: " +
                                                            projectShortName + ", часы: " + tsHoursRecord.Hours);
                                                    }
                                                }

                                                if (doubleHours != 0)
                                                {
                                                    _tsHoursRecordService.Add(
                                                    new TSHoursRecord()
                                                    {
                                                        EmployeeID = employee.ID,
                                                        ProjectID = projectID,
                                                        RecordDate = recordDate,
                                                        Hours = doubleHours,
                                                        Description = "Импорт трудозатрат из Excel: " + dateNow,
                                                        RecordStatus = TSRecordStatus.PMApproved,
                                                        RecordSource = TSRecordSource.ExcelImportByMonth
                                                    }, currentUserName, currentUserSID);

                                                    report.AddReportEvent("Запись добавлена. Сотрудник: " + employeeFullName + ", проект: " + projectShortName + ", часы: " + doubleHours);

                                                    if (doubleHours > reportHoursInMonth)
                                                    {
                                                        report.AddReportEvent("Указанное количество часов для " + employeeFullName + " на проект: " + projectShortName + " превышает количество рабочих часов в месяце.");
                                                    }
                                                }
                                            }
                                        }
                                    }

                                }
                                catch (Exception e)
                                {
                                    throw e;
                                }
                            }
                        }
                    }
                    else
                    {
                        report.AddReportEvent("Сотрудник: " + employeeFullName + " не найден в БД Сотрудники");
                    }

                    countEmployee++;
                }
            }

            return report;
        }

        public TimesheetImportHoursFromExcelResult ProcessLongRunningAction(string userIdentityName, string fileId, DataTable timesheetHoursRecordSheetDataTable, int reportMonth,
            int reportHoursInMonth, int reportYear, bool onlyValidate, bool rewriteTSHoursRecords, string currentUserName, string currentUserSID)
        {
            var htmlReport = string.Empty;
            var htmlErrorReport = string.Empty;
            taskId = fileId;

            LongRunningTaskReport report = null;
            try
            {
                SetStatus(0, "Старт загрузки...");
                SetStatus(1, "Обработка файла Excel");
                report = ImportTSHoursRecords(timesheetHoursRecordSheetDataTable, reportMonth, reportYear, reportHoursInMonth, onlyValidate, rewriteTSHoursRecords, currentUserName, currentUserSID);
                SetStatus(100, "Загрузка завершена");
            }
            catch (Exception e)
            {
                SetStatus(-1, "Ошибка: " + e.Message);
                htmlErrorReport += "<br>" + e.Message + "<br>" + e.StackTrace + "<br>" + e.TargetSite;
            }

            try
            {
                if (report != null)
                    htmlReport = report.GenerateHtmlReport();
            }
            catch (Exception e)
            {
                SetStatus(-1, "Ошибка: " + e.Message);
                htmlErrorReport += "<br>" + e.Message + "<br>" + e.StackTrace + "<br>" + e.TargetSite.ToString();
            }

            return new TimesheetImportHoursFromExcelResult()
            {
                userInitiationgReport = userIdentityName,
                fileId = fileId,
                fileHtmlReport = new List<string>() { htmlReport, htmlErrorReport }
            };
        }
    }
}
