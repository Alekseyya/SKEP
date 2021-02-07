using System.Collections.Generic;
using Core.Helpers;


namespace MainApp.TimesheetProcessing
{
    public class TimesheetProcessingReport
    {
        public string AdditionalNameReport { get; set; }
        public List<string> ReportLines { get; set; }

        public TimesheetProcessingReport()
        {
            ReportLines = new List<string>();
        }

        public string GenerateHtmlReport()
        {
            RPCSHtmlReport htmlReport = new RPCSHtmlReport();
            string reportTitle = "Отчет об обработке данных Timesheet: " + AdditionalNameReport;

            htmlReport.AddHeaderColumn("Событие");

            foreach (string line in ReportLines)
            {
                htmlReport.AddReportRow(line);
            }

            return htmlReport.GetHtmlReportContent(reportTitle);
        }
    }
}