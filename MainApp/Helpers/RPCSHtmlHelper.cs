using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace MainApp.Helpers
{
    public class RPCSHtmlHelper
    {
        public static string GetSimpleUTF8HtmlPage(string title, string htmlBody)
        {
            string htmlPage = "";

            htmlPage += "<!DOCTYPE html>";
            htmlPage += "\r\n";
            htmlPage += "<html xmlns='http://www.w3.org/1999/xhtml'>";
            htmlPage += "\r\n";
            htmlPage += "<head><meta charset='utf-8' /><title>" + title + "</title></head>";
            htmlPage += "\r\n";
            htmlPage += "<body>";
            htmlPage += "\r\n";

            htmlPage += htmlBody;

            htmlPage += "</body>";
            htmlPage += "\r\n";
            htmlPage += "</html>";
            htmlPage += "\r\n";

            return htmlPage;
        }
    }
}