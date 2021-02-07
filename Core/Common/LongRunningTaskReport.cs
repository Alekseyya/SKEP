using System;
using System.Collections.Generic;
using System.Text;
using Core.Helpers;

namespace Core.Common
{
    public class LongRunningTaskReport
    {
        protected string _additionalNameReport { get; set; }
        protected string _reportName { get; set; }
        protected RPCSHtmlReport _htmlReport = null;

        public LongRunningTaskReport(string reportName, string additionalNameReport)
        {
            _reportName = reportName;
            _additionalNameReport = additionalNameReport;

            _htmlReport = new RPCSHtmlReport();
            _htmlReport.AddHeaderColumn("Дата и время");
            _htmlReport.AddHeaderColumn("Событие");
        }

        public string GenerateHtmlReport()
        {
            string reportTitle = _reportName + ((String.IsNullOrEmpty(_additionalNameReport) == false) ? ": " + _additionalNameReport : "");

            return _htmlReport.GetHtmlReportContent(reportTitle);
        }

        public void AddReportEvent(string eventDescription)
        {
            _htmlReport.AddReportRow(DateTime.Now.ToString(), eventDescription);
        }
    }
}
