using System;

namespace Core.Helpers
{
    public class RPCSHtmlReport
    {
        protected int colNum = 0;
        protected string htmlReportHeaderContent = "";
        protected string htmlReportContent = "";

        public RPCSHtmlReport()
        {

        }

        public string GetHtmlReportContent(string title)
        {
            string htmlReport = "";
            htmlReport = "<style>\r\n";
            htmlReport += " TABLE.mail { border-style:none; border-collapse:collapse; font:8pt Tahoma; width:100%; } \r\n";
            htmlReport += " TD.header { background:#d9d9d9; border:1px solid #E8EAEC; padding:2pt 10px 2pt 10px; } \r\n ";
            htmlReport += " TD.main { padding:4pt 0px 4pt 0px; } \r\n ";
            htmlReport += " TD.footer { border-width:1px; border-style:solid none none none; border-color:#9CA3AD; padding:4pt 10px 4pt 10px; } \r\n ";
            htmlReport += " DIV.title { font:10pt Arial; font:bold; padding:4pt 10px 4pt 0px; } \r\n ";
            htmlReport += " DIV.headertext { margin:5px 0px 7px 0px; } \r\n ";
            htmlReport += " DIV.error { font-weight:bold; } \r\n ";
            htmlReport += " DIV.comment { color:#9CA3AD; } \r\n";
            htmlReport += " SPAN.wfname { font:bold italic; }\r\n";
            htmlReport += " .summarycontent { margin-top:3px; margin-bottom:3px; border-bottom:none; border-top:solid 1px #000000;} \r\n ";
            htmlReport += " .summaryitemmain { font-family: tahoma; font-size:8pt; padding: 0px 10px 0px 10px; vertical-align: top;} \r\n ";
            htmlReport += " .summaryitemmain {color: #000000;} \r\n ";
            htmlReport += " .summaryitemmain { border-bottom:solid 1px #000000; border-top:none; border-left:solid 1px #000000; border-right:solid 1px #000000; padding-top:2px; padding-bottom:2px; } \r\n ";
            htmlReport += " DIV.vertext { font:8pt Arial; font:normal; } .vertablecell {width:220px; } \r\n";
            htmlReport += " .listitemcontent { margin-top:3px; margin-bottom:3px; width: 100%; border-bottom:solid 1px #9ca3ad; border-top:solid 1px #9ca3ad;} \r\n ";
            htmlReport += " .formlabel {border-right:solid 1px #e8eaec;}\r\n";
            htmlReport += " .formlabel, .formmain  { font-family: tahoma; font-size:8pt; padding: 0px 7px 0px 7px; vertical-align: top;}\r\n";
            htmlReport += " .formlabel {color: #616a76; font-weight: bold; }\r\n";
            htmlReport += " .formmain {color: #000000;}\r\n";
            htmlReport += " .formlabel, .formmain { border-bottom:solid 1px #e8eaec; padding-top:2px; padding-bottom:2px; }\r\n";
            htmlReport += " .formlabel, .formmain {background: #f8f8f9; }\r\n";
            htmlReport += "</style>";
            htmlReport += "\r\n";

            htmlReport += @"<table cellpadding='2' cellspacing='0' class='mail' DIR='none'>";
            htmlReport += "\r\n";
            htmlReport += @"<tr class='main'><td class='main' valign='top'><div><b>" + title;
            htmlReport += @"</b></div></td></tr>";
            htmlReport += "\r\n";
            htmlReport += @"</table>";
            htmlReport += "\r\n";

            htmlReport += @"<table class='summarycontent' cellspacing='0' cellpadding='0' >";
            htmlReport += "\r\n";
            htmlReport += "<tr>";
            htmlReport += htmlReportHeaderContent;
            htmlReport += "\r\n";
            htmlReport += @"</tr>";
            htmlReport += "\r\n";

            htmlReport += htmlReportContent;

            htmlReport += @"</table>";
            htmlReport += "\r\n";

            return htmlReport;
        }

        public void AddHeaderColumn(string columnTitle)
        {
            colNum++;

            htmlReportHeaderContent += @"<td class='summaryitemmain' style='text-wrap:normal;'>";
            htmlReportHeaderContent += "<b>" + columnTitle + "</b>";
            htmlReportHeaderContent += "</td>";
            htmlReportHeaderContent += "\r\n";
        }

        public void AddReportSection(string sectionTitle)
        {
            htmlReportContent += "<tr>";
            htmlReportContent += @"<td colspan='" + colNum.ToString() + "' class='summaryitemmain' style='text-wrap:normal;'>";
            htmlReportContent += "<b>" + sectionTitle + "</b>";
            htmlReportContent += "</td>";
            htmlReportContent += "\r\n";
            htmlReportContent += @"</tr>";
            htmlReportContent += "\r\n";
        }

        public void AddReportRow(params string[] values)
        {
            htmlReportContent += "<tr>";
            htmlReportContent += "\r\n";

            foreach (string val in values)
            {
                htmlReportContent += GenerateHtmlReportCell(val);
            }

            htmlReportContent += @"</tr>";
            htmlReportContent += "\r\n";

        }

        protected string GenerateHtmlReportCell(string cellValue)
        {
            string htmlReportCell = "";

            if (String.IsNullOrEmpty(cellValue) == false
                && cellValue.Contains("->") == true)
            {
                htmlReportCell += @"<td bgcolor='lightgrey' class='summaryitemmain' style='text-wrap:normal;'>";
                htmlReportCell += cellValue;
                htmlReportCell += "</td>";
                htmlReportCell += "\r\n";
            }
            else
            {
                htmlReportCell += @"<td class='summaryitemmain' style='text-wrap:normal;'>";
                htmlReportCell += cellValue;
                htmlReportCell += "</td>";
                htmlReportCell += "\r\n";
            }

            return htmlReportCell;
        }
    }
}
