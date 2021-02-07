using System.Collections.Generic;
using Core.Helpers;


namespace MainApp.ADSync
{
    public class AdSyncReport
    {
        public List<ADSyncEmployeeInfo> NewUsers { get; set; }

        public List<ADSyncEmployeeInfo> UpdatedUsers { get; set; }

        public List<ADSyncEmployeeInfo> NotFoundInAD { get; set; }

        public AdSyncReport()
        {
            NewUsers = new List<ADSyncEmployeeInfo>();
            UpdatedUsers = new List<ADSyncEmployeeInfo>();
            NotFoundInAD = new List<ADSyncEmployeeInfo>();
        }

        public string GenerateHtmlReport()
        {
            RPCSHtmlReport htmlReport = new RPCSHtmlReport();
            string reportTitle = "Отчет о синхронизации с Active Directory";

            htmlReport.AddHeaderColumn("ФИО");
            htmlReport.AddHeaderColumn("Принят");
            htmlReport.AddHeaderColumn("ADLogin");
            htmlReport.AddHeaderColumn("E-mail");
            htmlReport.AddHeaderColumn("EmployeeID");
            htmlReport.AddHeaderColumn("Должность (title)");
            htmlReport.AddHeaderColumn("Подразделение (department)");
            htmlReport.AddHeaderColumn("Руководитель (manager)");

            htmlReport.AddHeaderColumn("Организация");
            htmlReport.AddHeaderColumn("Офис (№ кабинета)");
            htmlReport.AddHeaderColumn("Рабочий тел.");
            htmlReport.AddHeaderColumn("Моб. (общедоступный)");
            htmlReport.AddHeaderColumn("Терр. расп.");

            htmlReport.AddHeaderColumn("Статус синхронизации ");
            
            htmlReport.AddReportSection("Обновлены данные в на основе AD для сотрудников: ");
            htmlReport = NewUsers.GenerateHtmlReportEntry(htmlReport);

            htmlReport.AddReportSection("Обновлены данные в AD для учетных записей сотрудников: ");
            htmlReport = UpdatedUsers.GenerateHtmlReportEntry(htmlReport);

            htmlReport.AddReportSection("Не найдены в AD учетные записи для сотрудников: ");
            htmlReport = NotFoundInAD.GenerateHtmlReportEntry(htmlReport);

            return htmlReport.GetHtmlReportContent(reportTitle);
        }
    }
}