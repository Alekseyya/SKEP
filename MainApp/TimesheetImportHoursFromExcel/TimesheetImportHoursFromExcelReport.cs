using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Core.Helpers;


namespace MainApp.TimesheetImportHoursFromExcel
{
    public class TimesheetImportHoursFromExcelReport
    {
        public string AdditionalNameReport { get; set; }
        public List<string> ReportLines { get; set; }

        public TimesheetImportHoursFromExcelReport()
        {
            ReportLines = new List<string>();
        }

        public string GenerateHtmlReport()
        {
            RPCSHtmlReport htmlReport = new RPCSHtmlReport();
            string reportTitle = "Отчет о загрузке трудозатрат за месяц: " + AdditionalNameReport;

            htmlReport.AddHeaderColumn("Событие");

            foreach (string line in ReportLines)
            {
                htmlReport.AddReportRow(line);
            }

            return htmlReport.GetHtmlReportContent(reportTitle);
        }
    }
}
