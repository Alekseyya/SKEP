using System;
using Core.BL.Interfaces;
using Core.Common;


namespace MainApp.ReportGenerators
{
    public class ProjectsHoursForPMReportGeneratorTask : LongRunningTaskBase
    {
        private readonly ITimesheetService _timesheetService;

        public ProjectsHoursForPMReportGeneratorTask(ITimesheetService timesheetService)
            : base()
        {
            _timesheetService = timesheetService;
        }


        public ReportGeneratorResult ProcessLongRunningAction(string userIdentityName, string id, DateTime periodStart, DateTime periodEnd,
            string projectShortName)
        {
            var htmlErrorReport = string.Empty;

            taskId = id;

            byte[] binData = null;

            try
            {

                SetStatus(0, "Старт формирования отчета...");

                //Timesheet ts = new Timesheet();

                string reportTitle = "";

                reportTitle = "Отчет по трудозатратам за период: " + periodStart.ToString("yyyy-MM-dd") + " - " + periodEnd.ToString("yyyy-MM-dd") + ", " + projectShortName;

                binData = _timesheetService.GetProjectsHoursForPMReportExcel(this, userIdentityName, reportTitle,
                    projectShortName,
                    periodStart, periodEnd);

                SetStatus(100, "Отчет сформирован");

            }
            catch (Exception e)
            {
                SetStatus(-1, "Ошибка: " + e.Message.Replace("\r", "").Replace("\n", " "));
                htmlErrorReport += "<br>" + e.Message + "<br>" + e.StackTrace + "<br>" + e.TargetSite.ToString();
            }

            return new ReportGeneratorResult() { fileId = id, fileBinData = binData, htmlErrorReport = htmlErrorReport };
        }

    }
}