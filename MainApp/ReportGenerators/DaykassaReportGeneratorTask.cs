using System;
using Core.Common;


namespace MainApp.ReportGenerators
{
    public class DaykassaReportGeneratorTask : LongRunningTaskBase
    {
        public DaykassaReportGeneratorTask()
            : base()
        {
        }


        public ReportGeneratorResult ProcessLongRunningAction(string userIdentityName, string id,
            string projectShortName,
            DateTime periodStart, DateTime periodEnd, bool getProfTransactions)
        {
            var htmlErrorReport = string.Empty;

            taskId = id;

            byte[] binData = null;
            try
            {
                SetStatus(0, "Старт формирования отчета...");


                Daykassa dk = new Daykassa();
                dk.GetDataFromDaykassaDB(this, periodStart.ToString("yyyy-MM-dd"), periodEnd.ToString("yyyy-MM-dd"), getProfTransactions);

                string reportTitle = "Отчет по операциям DK за период: " + periodStart.ToString("yyyy-MM-dd") + " - " + periodEnd.ToString("yyyy-MM-dd");

                binData = dk.GetDaykassaReportExcel(this, userIdentityName, reportTitle,
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