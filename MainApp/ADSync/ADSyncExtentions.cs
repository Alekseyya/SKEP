using System.Collections.Generic;
using Core.Helpers;


namespace MainApp.ADSync
{
    public static class ADSyncExtentions
    {
        public static void SafeAddToList<T>(this List<T> list, T value) where T : ADSyncEmployeeInfo
        {
            if (!list.Contains(value))
            {
                list.Add(value);
            }
        }

        public static RPCSHtmlReport GenerateHtmlReportEntry<T>(this List<T> list, RPCSHtmlReport htmlReport) where T : ADSyncEmployeeInfo
        {
            foreach (ADSyncEmployeeInfo item in list)
            {
                htmlReport = item.AddRowToHtmlReport(htmlReport);
            }
            return htmlReport;
        }
    }
}