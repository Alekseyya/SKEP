using System;
using System.Data;
using System.IO;
using Core.Helpers;

namespace MainApp.BudgetLimitRecordsFromExcel
{
    public class BudgetHelper
    {
        public static DataTable GetDefaultBudgetExcelDataTable()
        {
            var table = new DataTable();
            table.Columns.Add("Income", typeof(string)).Caption = "Доходы";
            table.Columns.Add("CostSubItemShortName", typeof(string)).Caption = "Код";
            table.Columns.Add("Section", typeof(string)).Caption = "Статья";
            table.Columns.Add("DepartmentShortTitle", typeof(string)).Caption = "СТАТЬЯ / ПОДСТАТЬЯ / ЦФО";
            // Месяца
            table.Columns.Add("January", typeof(double)).Caption = "ЯНВ";
            table.Columns.Add("February", typeof(double)).Caption = "ФЕВ";
            table.Columns.Add("March", typeof(double)).Caption = "МАР";
            table.Columns.Add("April", typeof(double)).Caption = "АПР";
            table.Columns.Add("May", typeof(double)).Caption = "МАЙ";
            table.Columns.Add("June", typeof(double)).Caption = "ИЮН";
            table.Columns.Add("July", typeof(double)).Caption = "ИЮЛ";
            table.Columns.Add("August", typeof(double)).Caption = "АВГ";
            table.Columns.Add("September", typeof(double)).Caption = "СЕН";
            table.Columns.Add("October", typeof(double)).Caption = "ОКТ";
            table.Columns.Add("November", typeof(double)).Caption = "НОЯ";
            table.Columns.Add("December", typeof(double)).Caption = "ДЕК";
            //
            table.Columns.Add("Total", typeof(double)).Caption = "ИТОГО";
            return table;
        }

        public static DataTable ExportDataToDefaultTable(Stream dataStream)
        {
            return ExcelHelper.ExportData(GetDefaultBudgetExcelDataTable(), dataStream);
        }

        public static DataTable ExportData(DataTable table, Stream dataStream)
        {
            throw new NotImplementedException();
        }

    }
}
