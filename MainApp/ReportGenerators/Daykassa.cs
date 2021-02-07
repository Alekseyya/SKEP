using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections;
using System.Configuration;
using System.Data;
using System.Globalization;
using System.IO;
using Core.Common;
using Core.Config;
using Core.Helpers;
using Microsoft.Extensions.DependencyInjection;




namespace MainApp.ReportGenerators
{
    public class Daykassa
    {
        public ArrayList EXP_TRANSACTIONS = new ArrayList();
        public ArrayList PROF_TRANSACTIONS = new ArrayList();
        private static IServiceProvider _serviceProvider;
        private static DaykassaConfig _daykassaConfig;

        public Daykassa()
        {
        }

        public static void Configure(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _daykassaConfig = serviceProvider.GetRequiredService<DaykassaConfig>();

        }
        static private string GetConnectionString()
        {
            string dataSource = string.Format("(DESCRIPTION=(ADDRESS_LIST=(ADDRESS=(PROTOCOL=TCP)(HOST={0})(PORT={1})))(CONNECT_DATA=(SERVER=DEDICATED)(SERVICE_NAME={2})))",
                    ConfigurationManager.AppSettings["DKOraDBHost"].ToString(),
                    ConfigurationManager.AppSettings["DKOraDBPort"].ToString(),
                    ConfigurationManager.AppSettings["DKOraDBSN"].ToString());

            string userID = ConfigurationManager.AppSettings["DKOraDBUserID"].ToString();

            string password = ConfigurationManager.AppSettings["DKOraDBPassword"].ToString();

            // To avoid storing the connection string in your code, 
            // you can retrieve it from a configuration file. 
            return "Data Source=" + dataSource + ";" +
                   "User ID=" + userID + ";Password=" + password;
        }

        public void GetDataFromDaykassaDB(LongRunningTaskBase task, string date0, string date1, bool getProfTransactions)
        {
            string connectionString = GetConnectionString();
            try
            {
                using (OracleConnection connection = new OracleConnection())
                {
                    connection.ConnectionString = connectionString;
                    connection.Open();
                    try
                    {
                        task.SetStatus(50, "State: " + connection.State);

                        task.SetStatus(50, "Старт получения записей о транзакциях в Daykassa...");
                        createTRANSACTIONS(task, connection, date0, date1, getProfTransactions);

                        task.SetStatus(55, "Прочитано из БД Daykassa записей о транзакциях:" + EXP_TRANSACTIONS.Count);
                    }
                    catch (Exception e)
                    {
                        task.SetStatus(-1, "Ошибка: " + e.Message);
                    }

                    connection.Close();
                }
            }
            catch (Exception e)
            {
                task.SetStatus(-1, "Ошибка: " + e.Message);
            }
        }

        protected void createEXPTRANSACTIONS(LongRunningTaskBase task, OracleConnection conn, String date0, String date1)
        {
            using (OracleCommand command = conn.CreateCommand())
            {

                string sql = "SELECT TRANSACTIONS.EXEC_DATE, TRANSACTIONS.DESCR, T_DETAILS.NAME, T_DETAILS.AMOUNT, T_DETAILS.CUR, PROJECTS.SHORT_NAME, TRANSACTIONS.BITRIX_NUMBER,\n" +
                    " TRANSACTIONS.EXPENSE_TYPE_CODE, EXPENSE_TYPES.SHORT_NAME,\n" +
                    " FROMBOOK.SHORT_NAME, FROMBOOK.BOOK_ID,\n" +
                    " UNITS.UNIT_INDEX, UNITS.SHORT_NAME,\n" +
                    " T_TYPES.NAME,\n" +
                    " FROMBOOK.BOOK_INDEX, FROMBOOK.GROUP_ID,\n" +
                    " TOBOOK.SHORT_NAME, TOBOOK.BOOK_ID, TOBOOK.BOOK_INDEX, TOBOOK.GROUP_ID,\n" +
                    " TRANSACTIONS.ORG\n" +
                    " FROM DK_PROD.TRANSACTIONS\n" +
                    " LEFT OUTER JOIN DK_PROD.T_DETAILS ON TRANSACTIONS.T_ID = T_DETAILS.T_ID\n" +
                    " LEFT OUTER JOIN DK_PROD.PROJECTS ON TRANSACTIONS.PROJECT_ID = PROJECTS.PROJECT_ID\n" +
                    " LEFT OUTER JOIN DK_PROD.BOOKS FROMBOOK ON TRANSACTIONS.FROM_BOOK = FROMBOOK.BOOK_ID\n" +
                    " LEFT OUTER JOIN DK_PROD.BOOKS TOBOOK ON TRANSACTIONS.TO_BOOK = TOBOOK.BOOK_ID\n" +
                    " LEFT OUTER JOIN DK_PROD.T_TYPES ON TRANSACTIONS.T_TYPE = T_TYPES.T_TYPE\n" +
                    " LEFT OUTER JOIN DK_PROD.EXPENSE_TYPES ON TRANSACTIONS.EXPENSE_TYPE_CODE = EXPENSE_TYPES.CODE\n" +
                    " LEFT OUTER JOIN DK_PROD.UNITS ON TRANSACTIONS.UNIT_ID = UNITS.UNIT_ID\n" +
                    " WHERE TRANSACTIONS.EXEC_DATE between to_date('" + date0 + "', 'YYYY-MM-DD') AND to_date('" + date1 + "', 'YYYY-MM-DD')\n" +
                    " AND TRANSACTIONS.PROJECT_ID is not null\n" +
                    " AND TRANSACTIONS.T_TYPE IN (2, 3, 4, 5, 6, 7, 8, 12, 13, 14, 15, 16)\n" +
                    " AND TRANSACTIONS.FROM_BOOK is not null\n" +
                    " ORDER BY TRANSACTIONS.EXEC_DATE";

                command.CommandText = sql;

                using (OracleDataReader reader = command.ExecuteReader())
                {

                    try
                    {
                        int recordsCount = 0;
                        while (reader.Read())
                        {

                            TRANSACTIONSRecord tr = new TRANSACTIONSRecord();
                            EXP_TRANSACTIONS.Add(tr);
                            tr.EXEC_DATE = reader.GetOracleValue(0).ToString();
                            try
                            {
                                int i = tr.EXEC_DATE.IndexOf(' ');
                                if (i > 0)
                                {
                                    tr.EXEC_DATE = tr.EXEC_DATE.Substring(0, i);
                                }
                            }
                            catch (Exception ex)
                            {
                                tr.EXEC_DATE = "";
                            }

                            tr.DESCR = reader.GetOracleValue(1).ToString();
                            tr.T_DETAILS_NAME = reader.GetOracleValue(2).ToString();

                            try
                            {
                                tr.T_DETAILS_AMOUNT = (double)(reader.GetDouble(3));
                            }
                            catch (Exception ex)
                            {
                                tr.T_DETAILS_AMOUNT = 0;
                            }
                            tr.T_DETAILS_CUR = reader.GetOracleValue(4).ToString();
                            tr.PROJECT_SHORT_NAME = reader.GetOracleValue(5).ToString();

                            try
                            {
                                tr.BITRIX_NUMBER = reader.GetOracleValue(6).ToString();

                                tr.EXPENSE_TYPE_CODE = reader.GetOracleValue(7).ToString();
                                tr.EXPENSE_TYPE_SHORT_NAME = reader.GetOracleValue(8).ToString();

                                tr.FROMBOOK_SHORT_NAME = reader.GetOracleValue(9).ToString();
                                tr.FROMBOOK_ID = reader.GetOracleValue(10).ToString();

                                tr.UNIT_INDEX = reader.GetOracleValue(11).ToString();
                                tr.UNIT_SHORT_NAME = reader.GetOracleValue(12).ToString();

                                tr.T_TYPE_NAME = reader.GetOracleValue(13).ToString();

                                tr.FROMBOOK_INDEX = reader.GetOracleValue(14).ToString();
                                tr.FROMBOOK_GROUP_ID = reader.GetOracleValue(15).ToString();

                                tr.TOBOOK_SHORT_NAME = reader.GetOracleValue(16).ToString();
                                tr.TOBOOK_ID = reader.GetOracleValue(17).ToString();
                                tr.TOBOOK_INDEX = reader.GetOracleValue(18).ToString();
                                tr.TOBOOK_GROUP_ID = reader.GetOracleValue(19).ToString();

                                tr.ORG = reader.GetOracleValue(20).ToString();
                            }
                            catch (Exception)
                            {

                            }

                            recordsCount++;
                            task.SetStatus(50 + 2 * recordsCount / 500, "Прочитано записей о транзакциях: " + recordsCount.ToString());
                        }
                    }
                    catch (Exception e)
                    {
                        task.SetStatus(-1, "Ошибка: " + e.Message);
                    }

                    reader.Close();
                }
            }
        }

        protected void createPROFTRANSACTIONS(LongRunningTaskBase task, OracleConnection conn, String date0, String date1)
        {
            using (OracleCommand command = conn.CreateCommand())
            {

                string sql = "SELECT TRANSACTIONS.EXEC_DATE, TRANSACTIONS.DESCR, T_DETAILS.NAME, T_DETAILS.AMOUNT, T_DETAILS.CUR, PROJECTS.SHORT_NAME, TRANSACTIONS.BITRIX_NUMBER,\n" +
                    " TRANSACTIONS.EXPENSE_TYPE_CODE, EXPENSE_TYPES.SHORT_NAME,\n" +
                    " FROMBOOK.SHORT_NAME, FROMBOOK.BOOK_ID,\n" +
                    " UNITS.UNIT_INDEX, UNITS.SHORT_NAME,\n" +
                    " T_TYPES.NAME,\n" +
                    " FROMBOOK.BOOK_INDEX, FROMBOOK.GROUP_ID,\n" +
                    " TOBOOK.SHORT_NAME, TOBOOK.BOOK_ID, TOBOOK.BOOK_INDEX, TOBOOK.GROUP_ID,\n" +
                    " TRANSACTIONS.ORG\n" +
                    " FROM DK_PROD.TRANSACTIONS\n" +
                    " LEFT OUTER JOIN DK_PROD.T_DETAILS ON TRANSACTIONS.T_ID = T_DETAILS.T_ID\n" +
                    " LEFT OUTER JOIN DK_PROD.PROJECTS ON TRANSACTIONS.PROJECT_ID = PROJECTS.PROJECT_ID\n" +
                    " LEFT OUTER JOIN DK_PROD.BOOKS FROMBOOK ON TRANSACTIONS.FROM_BOOK = FROMBOOK.BOOK_ID\n" +
                    " LEFT OUTER JOIN DK_PROD.BOOKS TOBOOK ON TRANSACTIONS.TO_BOOK = TOBOOK.BOOK_ID\n" +
                    " LEFT OUTER JOIN DK_PROD.T_TYPES ON TRANSACTIONS.T_TYPE = T_TYPES.T_TYPE\n" +
                    " LEFT OUTER JOIN DK_PROD.EXPENSE_TYPES ON TRANSACTIONS.EXPENSE_TYPE_CODE = EXPENSE_TYPES.CODE\n" +
                    " LEFT OUTER JOIN DK_PROD.UNITS ON TRANSACTIONS.UNIT_ID = UNITS.UNIT_ID\n" +
                    " WHERE TRANSACTIONS.EXEC_DATE between to_date('" + date0 + "', 'YYYY-MM-DD') AND to_date('" + date1 + "', 'YYYY-MM-DD')\n" +
                    //" AND TRANSACTIONS.PROJECT_ID is not null\n" +
                    " AND TRANSACTIONS.T_TYPE IN (1, 10, 11, 17, 18)\n" +
                    //" AND TRANSACTIONS.FROM_BOOK is not null\n" +
                    " ORDER BY TRANSACTIONS.EXEC_DATE";

                command.CommandText = sql;

                using (OracleDataReader reader = command.ExecuteReader())
                {

                    try
                    {
                        int recordsCount = 0;
                        while (reader.Read())
                        {

                            TRANSACTIONSRecord tr = new TRANSACTIONSRecord();
                            PROF_TRANSACTIONS.Add(tr);
                            tr.EXEC_DATE = reader.GetOracleValue(0).ToString();
                            try
                            {
                                int i = tr.EXEC_DATE.IndexOf(' ');
                                if (i > 0)
                                {
                                    tr.EXEC_DATE = tr.EXEC_DATE.Substring(0, i);
                                }
                            }
                            catch (Exception ex)
                            {
                                tr.EXEC_DATE = "";
                            }

                            tr.DESCR = reader.GetOracleValue(1).ToString();
                            tr.T_DETAILS_NAME = reader.GetOracleValue(2).ToString();

                            try
                            {
                                tr.T_DETAILS_AMOUNT = (double)(reader.GetDouble(3));
                            }
                            catch (Exception ex)
                            {
                                tr.T_DETAILS_AMOUNT = 0;
                            }
                            tr.T_DETAILS_CUR = reader.GetOracleValue(4).ToString();
                            tr.PROJECT_SHORT_NAME = reader.GetOracleValue(5).ToString();

                            try
                            {
                                tr.BITRIX_NUMBER = reader.GetOracleValue(6).ToString();

                                tr.EXPENSE_TYPE_CODE = reader.GetOracleValue(7).ToString();
                                tr.EXPENSE_TYPE_SHORT_NAME = reader.GetOracleValue(8).ToString();

                                tr.FROMBOOK_SHORT_NAME = reader.GetOracleValue(9).ToString();
                                tr.FROMBOOK_ID = reader.GetOracleValue(10).ToString();

                                tr.UNIT_INDEX = reader.GetOracleValue(11).ToString();
                                tr.UNIT_SHORT_NAME = reader.GetOracleValue(12).ToString();

                                tr.T_TYPE_NAME = reader.GetOracleValue(13).ToString();

                                tr.FROMBOOK_INDEX = reader.GetOracleValue(14).ToString();
                                tr.FROMBOOK_GROUP_ID = reader.GetOracleValue(15).ToString();

                                tr.TOBOOK_SHORT_NAME = reader.GetOracleValue(16).ToString();
                                tr.TOBOOK_ID = reader.GetOracleValue(17).ToString();
                                tr.TOBOOK_INDEX = reader.GetOracleValue(18).ToString();
                                tr.TOBOOK_GROUP_ID = reader.GetOracleValue(19).ToString();

                                tr.ORG = reader.GetOracleValue(20).ToString();
                            }
                            catch (Exception)
                            {

                            }

                            recordsCount++;
                            task.SetStatus(53 + 2 * recordsCount / 500, "Прочитано записей о транзакциях: " + recordsCount.ToString());
                        }
                    }
                    catch (Exception e)
                    {
                        task.SetStatus(-1, "Ошибка: " + e.Message);
                    }

                    reader.Close();
                }
            }
        }

        public void createTRANSACTIONS(LongRunningTaskBase task, OracleConnection conn, String date0, String date1, bool getProfTransactions)
        {

            createEXPTRANSACTIONS(task, conn, date0, date1);
            if (getProfTransactions)
            {
                createPROFTRANSACTIONS(task, conn, date0, date1);
            }

        }

        public DataTable GetProjectsOtherCostsTransationsFromDaykassa(DataTable projectsOtherCostsSheetDataTable)
        {
            for (int k = 0; k < EXP_TRANSACTIONS.Count; k++)
            {
                TRANSACTIONSRecord tr = EXP_TRANSACTIONS[k] as TRANSACTIONSRecord;

                try
                {
                    string[] recDateFormats = { "MM/dd/yyyy" };
                    DateTime recDate = DateTime.ParseExact(tr.EXEC_DATE, recDateFormats, new CultureInfo("en-US"), DateTimeStyles.None);

                    projectsOtherCostsSheetDataTable.Rows.Add("", "",
                        recDate,
                        tr.PROJECT_SHORT_NAME,
                        null,
                        null,
                        null,
                        tr.T_DETAILS_AMOUNT,
                        tr.T_DETAILS_NAME);
                }
                catch (Exception)
                {

                }
            }

            return projectsOtherCostsSheetDataTable;
        }

        protected static DataTable GetDKReportDataTableFromTransactions(ArrayList TRANSACTIONS, string projectShortName)
        {
            DataTable dataTable = new DataTable();

            dataTable.Columns.Add("ExecDate", typeof(DateTime)).Caption = "Дата выполнения";
            dataTable.Columns["ExecDate"].ExtendedProperties["Width"] = (double)14;
            dataTable.Columns.Add("BitrixNumber", typeof(string)).Caption = "№ заявки в Битриксе";
            dataTable.Columns["BitrixNumber"].ExtendedProperties["Width"] = (double)14;
            dataTable.Columns.Add("UnitIndex", typeof(string)).Caption = "Код ЦФО";
            dataTable.Columns["UnitIndex"].ExtendedProperties["Width"] = (double)9;
            dataTable.Columns.Add("UnitShortName", typeof(string)).Caption = "ЦФО";
            dataTable.Columns["UnitShortName"].ExtendedProperties["Width"] = (double)9;
            dataTable.Columns.Add("ExpenseTypeCode", typeof(string)).Caption = "Код статьи";
            dataTable.Columns["ExpenseTypeCode"].ExtendedProperties["Width"] = (double)9;
            dataTable.Columns.Add("ExpenseTypeShortName", typeof(string)).Caption = "Статья затрат";
            dataTable.Columns["ExpenseTypeShortName"].ExtendedProperties["Width"] = (double)37;
            dataTable.Columns.Add("TTypeName", typeof(string)).Caption = "Назначение";
            dataTable.Columns["TTypeName"].ExtendedProperties["Width"] = (double)18;
            dataTable.Columns.Add("FromBook", typeof(string)).Caption = "Касса откуда";
            dataTable.Columns["FromBook"].ExtendedProperties["Width"] = (double)12;
            dataTable.Columns.Add("ToBook", typeof(string)).Caption = "Касса куда";
            dataTable.Columns["ToBook"].ExtendedProperties["Width"] = (double)12;
            dataTable.Columns.Add("Org", typeof(string)).Caption = "Контрагент";
            dataTable.Columns["Org"].ExtendedProperties["Width"] = (double)12;
            dataTable.Columns.Add("ProjectShortName", typeof(string)).Caption = "Проект";
            dataTable.Columns["ProjectShortName"].ExtendedProperties["Width"] = (double)20;
            dataTable.Columns.Add("Amount", typeof(double)).Caption = "Сумма";
            dataTable.Columns["Amount"].ExtendedProperties["Width"] = (double)12;
            dataTable.Columns.Add("AmountCurrency", typeof(string)).Caption = "Валюта";
            dataTable.Columns["AmountCurrency"].ExtendedProperties["Width"] = (double)8;
            dataTable.Columns.Add("TDetailsName", typeof(string)).Caption = "Описание";
            dataTable.Columns["TDetailsName"].ExtendedProperties["Width"] = (double)80;
            dataTable.Columns.Add("TDescription", typeof(string)).Caption = "Комментарий";
            dataTable.Columns["TDescription"].ExtendedProperties["Width"] = (double)80;

            for (int k = 0; k < TRANSACTIONS.Count; k++)
            {
                TRANSACTIONSRecord tr = TRANSACTIONS[k] as TRANSACTIONSRecord;

                string[] recDateFormats = { "MM/dd/yyyy" };
                DateTime recDate = DateTime.ParseExact(tr.EXEC_DATE, recDateFormats, new CultureInfo("en-US"), DateTimeStyles.None);

                string fromBook = "";

                if (String.IsNullOrEmpty(tr.FROMBOOK_GROUP_ID) == false
                    && tr.FROMBOOK_GROUP_ID.ToLower().Trim().Equals("null") == false)
                {
                    fromBook += tr.FROMBOOK_GROUP_ID;
                }

                if (String.IsNullOrEmpty(tr.FROMBOOK_INDEX) == false
                   && tr.FROMBOOK_INDEX.ToLower().Trim().Equals("null") == false)
                {
                    if (String.IsNullOrEmpty(fromBook) == false)
                    {
                        fromBook += ".";
                    }
                    fromBook += tr.FROMBOOK_INDEX;
                }

                if (String.IsNullOrEmpty(tr.FROMBOOK_SHORT_NAME) == false
                    && tr.FROMBOOK_SHORT_NAME.ToLower().Trim().Equals("null") == false)
                {
                    fromBook += " " + tr.FROMBOOK_SHORT_NAME;
                }

                string toBook = "";

                if (String.IsNullOrEmpty(tr.TOBOOK_GROUP_ID) == false
                   && tr.TOBOOK_GROUP_ID.ToLower().Trim().Equals("null") == false)
                {
                    toBook += tr.TOBOOK_GROUP_ID;
                }

                if (String.IsNullOrEmpty(tr.TOBOOK_INDEX) == false
                   && tr.TOBOOK_INDEX.ToLower().Trim().Equals("null") == false)
                {
                    if (String.IsNullOrEmpty(toBook) == false)
                    {
                        toBook += ".";
                    }
                    toBook += tr.TOBOOK_INDEX;
                }

                if (String.IsNullOrEmpty(tr.TOBOOK_SHORT_NAME) == false
                    && tr.TOBOOK_SHORT_NAME.ToLower().Trim().Equals("null") == false)
                {
                    toBook += " " + tr.TOBOOK_SHORT_NAME;
                }

                if (String.IsNullOrEmpty(projectShortName) == true
                    || (String.IsNullOrEmpty(tr.PROJECT_SHORT_NAME) == false && projectShortName.Equals(tr.PROJECT_SHORT_NAME) == true))
                {
                    dataTable.Rows.Add(recDate.Date,
                        GetDKStringValueForReport(tr.BITRIX_NUMBER),
                        GetDKStringValueForReport(tr.UNIT_INDEX),
                        GetDKStringValueForReport(tr.UNIT_SHORT_NAME),
                        GetDKStringValueForReport(tr.EXPENSE_TYPE_CODE),
                        GetDKStringValueForReport(tr.EXPENSE_TYPE_SHORT_NAME),
                        GetDKStringValueForReport(tr.T_TYPE_NAME),
                        fromBook,
                        toBook,
                        GetDKStringValueForReport(tr.ORG),
                        GetDKStringValueForReport(tr.PROJECT_SHORT_NAME),
                        tr.T_DETAILS_AMOUNT,
                        GetDKStringValueForReport(tr.T_DETAILS_CUR),
                        GetDKStringValueForReport(tr.T_DETAILS_NAME),
                        GetDKStringValueForReport(tr.DESCR));
                }

            }

            return dataTable;
        }

        protected static string GetDKStringValueForReport(string dkStringValue)
        {
            if (String.IsNullOrEmpty(dkStringValue) == true
                || dkStringValue.ToLower().Trim().Equals("null") == true)
            {
                return "";
            }
            else
            {
                return dkStringValue;
            }
        }

        public byte[] GetDaykassaReportExcel(LongRunningTaskBase task, string userIdentityName, string reportTitle,
            string projectShortName,
            DateTime periodStart, DateTime periodEnd)
        {
            byte[] binData = null;

            task.SetStatus(60, "Обработка данных");

            DataTable expDataTable = GetDKReportDataTableFromTransactions(EXP_TRANSACTIONS, projectShortName);
            DataTable profDataTable = GetDKReportDataTableFromTransactions(PROF_TRANSACTIONS, projectShortName);

            task.SetStatus(98, "Формирование файла MS Excel...");

            using (MemoryStream stream = new MemoryStream())
            {
                using (SpreadsheetDocument doc = SpreadsheetDocument.Create(stream, SpreadsheetDocumentType.Workbook))
                {
                    string sheetsName = "Расходы|Поступления";

                    WorkbookPart workbookPart = ExcelHelper.CreateWorkbookPart(doc, sheetsName);

                    int partIndex = 1;

                    ExcelHelper.CreateWorksheetPartAndImportDataTable(workbookPart, "rId" + partIndex.ToString(), 1, 1, (uint)expDataTable.Columns.Count,
                        reportTitle, expDataTable, 3, 1);

                    partIndex++;

                    ExcelHelper.CreateWorksheetPartAndImportDataTable(workbookPart, "rId" + partIndex.ToString(), 1, 1, (uint)profDataTable.Columns.Count,
                        reportTitle, profDataTable, 3, 1);


                    doc.WorkbookPart.Workbook.Save();
                }

                stream.Position = 0;
                BinaryReader b = new BinaryReader(stream);
                binData = b.ReadBytes((int)stream.Length);
            }

            task.SetStatus(100, "Формирование файла MS Excel завершено");

            return binData;
        }
    }


    class TRANSACTIONSRecord
    {
        public String EXEC_DATE = "";
        public String DESCR = "";
        public String T_DETAILS_NAME = "";
        public double T_DETAILS_AMOUNT = 0;
        public String T_DETAILS_CUR = "";
        public String PROJECT_SHORT_NAME = "";
        public String BITRIX_NUMBER = "";
        public String EXPENSE_TYPE_CODE = "";
        public String EXPENSE_TYPE_SHORT_NAME = "";
        public String FROMBOOK_SHORT_NAME = "";
        public String FROMBOOK_ID = "";
        public String UNIT_INDEX = "";
        public String UNIT_SHORT_NAME = "";
        public String T_TYPE_NAME = "";
        public String FROMBOOK_INDEX = "";
        public String FROMBOOK_GROUP_ID = "";
        public String TOBOOK_SHORT_NAME = "";
        public String TOBOOK_ID = "";
        public String TOBOOK_INDEX = "";
        public String TOBOOK_GROUP_ID = "";
        public String ORG = "";
    }
}