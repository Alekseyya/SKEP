using System;
using System.Collections.Generic;
using System.Data;
using System.Collections;
using Core.BL.Interfaces;
using Core.Common;


namespace MainApp.ReportGenerators
{
    public class ProjectsHoursReportParams
    {
        public string ID { get; set; }
        public string UserIdentityName { get; set; }
        public string PeriodStart { get; set; }
        public string PeriodEnd { get; set; }
        public DateTime PeriodStartDate { get; set; }
        public DateTime PeriodEndDate { get; set; }
        public bool GroupByMonth { get; set; }
        public bool SaveResultsInDB { get; set; }
        public bool UseTSHoursRecordsOnly { get; set; }
        public bool UseTSHoursRecords { get; set; }
        public bool UseTSAutoHoursRecords { get; set; }
        public bool AddToReportNotInDBEmplyees { get; set; }
        public bool ShowEmployeeDataInReport { get; set; }
        public bool GetDataFromDaykassa { get; set; }
        public DataTable EmployeePayrollSheetDataTable { get; set; }
        public DataTable ProjectsOtherCostsSheetDataTable { get; set; }
        public List<int> DepartmentsIDs { get; set; }
        public Hashtable MonthsWorkingHours { get; set; }
    }


    public class ProjectsHoursReportGeneratorTask : LongRunningTaskBase
    {
        private readonly ITimesheetService _timesheetService;

        public ProjectsHoursReportGeneratorTask(ITimesheetService timesheetService) : base()
        {
            _timesheetService = timesheetService;
        }


        public ReportGeneratorResult ProcessLongRunningAction(ProjectsHoursReportParams reportParams)
        {
            var htmlErrorReport = string.Empty;

            taskId = reportParams.ID;

            byte[] binData = null;

            try
            {
                int year = 2017;
                int monthCount = 1;
                string periodName = "-";
                int monthWorkHours = 0;
                bool consolidatedReport = false;
                var periodStartDate = reportParams.PeriodStartDate;
                var periodEndDate = reportParams.PeriodEndDate;

                SetStatus(0, "Старт формирования отчета...");
                if (String.IsNullOrEmpty(reportParams.PeriodStart) == false)
                {
                    string[] periodTokens = reportParams.PeriodStart.Split('|');
                    string[] periodDateTokens = periodTokens[0].Split('.');

                    year = Convert.ToInt32(periodDateTokens[1]);
                    monthCount = 1;
                    periodName = "-";

                    if (periodDateTokens[0].Equals("*") == true)
                    {
                        periodStartDate = new DateTime(year, 1, 1);
                        periodEndDate = new DateTime(year, 12, DateTime.DaysInMonth(year, 12));

                        monthCount = 12;

                        periodName = year.ToString();
                    }
                    else
                    {
                        int month = Convert.ToInt32(periodDateTokens[0]);

                        periodStartDate = new DateTime(year, month, 1);
                        periodEndDate = new DateTime(year, month, DateTime.DaysInMonth(year, month));

                        monthCount = 1;

                        periodName = periodTokens[0];
                    }

                    monthWorkHours = Convert.ToInt32(periodTokens[1]);
                    if (!String.IsNullOrEmpty(reportParams.PeriodEnd))
                    {
                        string[] periodEndTokens = reportParams.PeriodEnd.Split('|');
                        string[] periodEndDateTokens = periodEndTokens[0].Split('.');

                        int month = Convert.ToInt32(periodEndDateTokens[0]);
                        periodEndDate = new DateTime(year, month, DateTime.DaysInMonth(year, month));
                        periodName = periodName + "-" + periodEndTokens[0];

                        var startMonth = Convert.ToInt32(periodDateTokens[0]);
                        monthCount = month - startMonth + 1;
                        monthWorkHours = CalculateMonthsWorkHours(reportParams.MonthsWorkingHours, startMonth, month);
                    }
                }
                else
                {
                    year = periodStartDate.Year;

                    monthCount = 12;

                    periodName = year.ToString();

                    consolidatedReport = true;
                }

                bool externalTSGetDataFromTimeSheetDBResult = false;

                if (reportParams.UseTSHoursRecordsOnly == false
                    && _timesheetService.IsExternalTSAllowed() == true)
                {
                    externalTSGetDataFromTimeSheetDBResult = _timesheetService.GetDataFromTimeSheetDB(this, periodStartDate.ToString("yyyy-MM-dd"), periodEndDate.ToString("yyyy-MM-dd"), monthCount, false);
                }
                else
                {
                    externalTSGetDataFromTimeSheetDBResult = false;
                }


                if (reportParams.UseTSHoursRecordsOnly == true
                    || externalTSGetDataFromTimeSheetDBResult == true)
                {
                    if (reportParams.UseTSHoursRecordsOnly == true)
                    {
                        _timesheetService.GetDataFromTSHoursRecords(this, periodStartDate, periodEndDate, true);
                    }
                    else
                    {
                        if (reportParams.UseTSAutoHoursRecords)
                            _timesheetService.GetDataFromTSAutoHoursRecords(this);

                        if (reportParams.UseTSHoursRecords)
                            _timesheetService.GetDataFromTSHoursRecords(this, periodStartDate, periodEndDate, false);
                    }

                    string reportTitle = "";

                    if (reportParams.EmployeePayrollSheetDataTable == null
                        && reportParams.ProjectsOtherCostsSheetDataTable == null)
                    {
                        reportTitle = "Отчет по трудозатратам за период: " + periodStartDate.ToString("yyyy-MM-dd") + " - " + periodEndDate.ToString("yyyy-MM-dd");
                    }
                    else
                    {
                        reportTitle = "Отчет по затратам за период: " + periodStartDate.ToString("yyyy-MM-dd") + " - " + periodEndDate.ToString("yyyy-MM-dd");
                    }

                    var projectsOtherCostsSheetDataTable = reportParams.ProjectsOtherCostsSheetDataTable;
                    if (reportParams.GetDataFromDaykassa == true
                        && projectsOtherCostsSheetDataTable != null)
                    {
                        Daykassa dk = new Daykassa();
                        dk.GetDataFromDaykassaDB(this, periodStartDate.ToString("yyyy-MM-dd"), periodEndDate.ToString("yyyy-MM-dd"), false);
                        projectsOtherCostsSheetDataTable = dk.GetProjectsOtherCostsTransationsFromDaykassa(projectsOtherCostsSheetDataTable);
                    }

                    if (consolidatedReport == true)
                    {
                        binData = _timesheetService.GetProjectsConsolidatedHoursReportExcel(this, reportParams.UserIdentityName, periodName, reportTitle, reportParams.SaveResultsInDB, reportParams.EmployeePayrollSheetDataTable,
                            projectsOtherCostsSheetDataTable,
                            periodStartDate, periodEndDate, reportParams.DepartmentsIDs);
                    }
                    else
                    {
                        binData = _timesheetService.GetProjectsHoursReportExcel(this, reportParams.UserIdentityName, periodName, reportTitle, monthWorkHours,
                            reportParams.SaveResultsInDB,
                            reportParams.AddToReportNotInDBEmplyees,
                            reportParams.ShowEmployeeDataInReport,
                            reportParams.GroupByMonth,
                            reportParams.EmployeePayrollSheetDataTable,
                            projectsOtherCostsSheetDataTable,
                            periodStartDate, periodEndDate, reportParams.DepartmentsIDs);
                    }

                    SetStatus(100, "Отчет сформирован");
                }
                else
                {
                    SetStatus(-1, "Ошибка при получении данных из внешнего ТШ");
                }

            }
            catch (Exception e)
            {
                SetStatus(-1, "Ошибка: " + e.Message.Replace("\r", "").Replace("\n", " "));
                htmlErrorReport += "<br>" + e.Message + "<br>" + e.StackTrace + "<br>" + e.TargetSite.ToString();
            }


            return new ReportGeneratorResult() { fileId = reportParams.ID, fileBinData = binData, htmlErrorReport = htmlErrorReport };
        }

        int CalculateMonthsWorkHours(Hashtable yearWorkHours, int startMonth, int endMonth)
        {
            var result = 0;
            if (yearWorkHours == null)
                return result;

            for (int i = startMonth; i <= endMonth; i++)
            {
                if (!yearWorkHours.ContainsKey(i))
                    continue;

                result += (int)yearWorkHours[i];
            }
            return result;
        }
    }
}