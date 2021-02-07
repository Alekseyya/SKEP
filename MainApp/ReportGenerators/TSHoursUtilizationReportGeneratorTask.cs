using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.IO;
using System.Linq;
using Core;
using Core.BL.Interfaces;
using Core.Common;
using Core.Extensions;
using Core.Helpers;
using Core.Models;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;






namespace MainApp.ReportGenerators
{
    public class TSHoursUtilizationReportParams
    {
        public string ID { get; set; }
        public string UserIdentityName { get; set; }
        public string PeriodStart { get; set; }
        public string PeriodEnd { get; set; }
        public List<int> DepartmentsIDs { get; set; }
    }


    public class TSHoursUtilizationReportGeneratorTask : LongRunningTaskBase
    {
        private readonly IDepartmentService _departmentService;
        private readonly IEmployeeService _employeeService;
        private readonly IProjectTypeService _projectTypeService;
        private readonly IProjectService _projectService;
        private readonly ITSHoursRecordService _tsHoursRecordService;

        public TSHoursUtilizationReportGeneratorTask(IDepartmentService departmentService,
            IEmployeeService employeeService,
            IProjectTypeService projectTypeService,
            IProjectService projectService,
            ITSHoursRecordService tsHoursRecordService) : base()
        {
            _departmentService = departmentService;
            _employeeService = employeeService;
            _projectTypeService = projectTypeService;
            _projectService = projectService;
            _tsHoursRecordService = tsHoursRecordService;
        }

        public ReportGeneratorResult ProcessLongRunningAction(TSHoursUtilizationReportParams reportParams)
        {
            var htmlErrorReport = string.Empty;

            taskId = reportParams.ID;

            byte[] binData = null;

            try
            {

                SetStatus(0, "Старт формирования отчета...");

                int year = 2017;
                string periodName = "-";
                DateTime periodStart = DateTime.MinValue;
                DateTime periodEnd = DateTime.MinValue;

                string[] periodTokens = reportParams.PeriodStart.Split('|');
                string[] periodDateTokens = periodTokens[0].Split('.');

                year = Convert.ToInt32(periodDateTokens[1]);
                periodName = "-";

                if (periodDateTokens[0].Equals("*") == true)
                {
                    periodStart = new DateTime(year, 1, 1);
                    periodEnd = new DateTime(year, 12, DateTime.DaysInMonth(year, 12));
                    periodName = year.ToString();
                }
                else
                {
                    int month = Convert.ToInt32(periodDateTokens[0]);

                    periodStart = new DateTime(year, month, 1);
                    periodEnd = new DateTime(year, month, DateTime.DaysInMonth(year, month));
                    periodName = periodTokens[0];
                }


                if (!String.IsNullOrEmpty(reportParams.PeriodEnd))
                {
                    string[] periodEndTokens = reportParams.PeriodEnd.Split('|');
                    string[] periodEndDateTokens = periodEndTokens[0].Split('.');

                    int month = Convert.ToInt32(periodEndDateTokens[0]);
                    periodEnd = new DateTime(year, month, DateTime.DaysInMonth(year, month));
                    periodName = periodName + "-" + periodEndTokens[0];
                }

                DataTable dataTable = new DataTable();

                dataTable.Columns.Add("RowTitle", typeof(string)).Caption = "ФАКТ ПО ЧЧ";
                dataTable.Columns["RowTitle"].ExtendedProperties["Width"] = (double)45;

                if (reportParams.DepartmentsIDs != null)
                {
                    foreach (int depID in reportParams.DepartmentsIDs)
                    {
                        var department = _departmentService.GetById(depID);
                        dataTable.Columns.Add("Department_" + depID.ToString(), typeof(double)).Caption = department.DisplayShortTitle;
                        dataTable.Columns["Department_" + depID.ToString()].ExtendedProperties["Width"] = (double)12;
                    }
                }

                dataTable.Columns.Add("Total", typeof(double)).Caption = "ИТОГО";
                dataTable.Columns["Total"].ExtendedProperties["Width"] = (double)12;
                dataTable.Columns.Add("_ISGROUPROW_", typeof(bool)).Caption = "_ISGROUPROW_";

                int startMonth = periodStart.Month;
                int endMonth = periodEnd.Month;


                for (int month = startMonth; month <= endMonth; month++)
                {
                    DataTable dt = dataTable.Clone();

                    DateTime start = (month == startMonth) ? periodStart : new DateTime(periodStart.Year, month, 1);
                    DateTime end = (month == endMonth) ? periodEnd : new DateTime(periodEnd.Year, month, 1).LastDayOfMonth();


                    dt = GetTSHoursUtilizationReportDataTable(dt,
                        start, end,
                        reportParams.DepartmentsIDs);

                    foreach (DataRow dr in dt.Rows)
                    {
                        dataTable.Rows.Add(dr.ItemArray);
                    }
                }

                if (startMonth != endMonth)
                {
                    /*dataTable.Rows.Add("", "", "", "ИТОГО ЗА ВСЕ ПЕРИОДЫ:");
                    dataTable.Rows[dataTable.Rows.Count - 1]["_ISGROUPROW_"] = true;
                    dataTable.Rows[dataTable.Rows.Count - 1]["EmployeeMonthHours"] = reportTotalHours;*/
                }



                SetStatus(98, "Формирование файла MS Excel...");

                string reportTitle = "Отчет по утилизации за период: " + periodStart.ToString("yyyy-MM-dd") + " - " + periodEnd.ToString("yyyy-MM-dd");

                using (MemoryStream stream = new MemoryStream())
                {
                    using (SpreadsheetDocument doc = SpreadsheetDocument.Create(stream, SpreadsheetDocumentType.Workbook))
                    {
                        WorkbookPart workbookPart = ExcelHelper.CreateWorkbookPart(doc, "По типам проектов");

                        WorksheetPart rId1 = ExcelHelper.CreateWorksheetPartAndImportDataTable(workbookPart, "rId1", 1, 1, (uint)dataTable.Columns.Count,
                            reportTitle, dataTable, 3, 1);

                        doc.WorkbookPart.Workbook.Save();
                    }

                    stream.Position = 0;
                    BinaryReader b = new BinaryReader(stream);
                    binData = b.ReadBytes((int)stream.Length);


                }


                SetStatus(100, "Отчет сформирован");


            }
            catch (Exception e)
            {
                SetStatus(-1, "Ошибка: " + e.Message.Replace("\r", "").Replace("\n", " "));
                htmlErrorReport += "<br>" + e.Message + "<br>" + e.StackTrace + "<br>" + e.TargetSite.ToString();
            }


            return new ReportGeneratorResult() { fileId = reportParams.ID, fileBinData = binData, htmlErrorReport = htmlErrorReport };
        }

        private DataTable GetTSHoursUtilizationReportDataTable(DataTable dataTable,
           DateTime periodStart, DateTime periodEnd,
           List<int> departmentsIDs)
        {

            List<Employee> currentEmployeeList = _employeeService.GetCurrentEmployees(new DateTimeRange(periodStart, periodEnd)).ToList()
                .Where(e => e.Department != null)
                .OrderBy(e => e.Department.ShortName + (e.Department.DepartmentManagerID != e.ID).ToString() + e.FullName).ToList();

            var tsHoursRecordForPeriodList = _tsHoursRecordService.Get(x => x.Where(r => r.RecordDate >= periodStart && r.RecordDate <= periodEnd
                && r.Project != null && r.Project.ProjectType != null).ToList());

            int periodRowStartIndex = dataTable.Rows.Count;

            var projectTypeUtilizationList = _projectTypeService.Get(x => x.Where(pt => pt.Utilization == true).ToList());
            var projectTypeNonUtilizationList = _projectTypeService.Get(x => x.Where(pt => pt.Utilization == false).ToList());

            dataTable.Rows.Add(periodStart.ToString("MMMM").ToUpper());
            dataTable.Rows[dataTable.Rows.Count - 1]["_ISGROUPROW_"] = true;

            double hoursPeriodTotal = 0;

            if (departmentsIDs != null)
            {
                foreach (int depID in departmentsIDs)
                {
                    var employeeInDepartmentIDs = GetEmployeesByDepartmentID(depID, currentEmployeeList.ToList()).Select(e => e.ID);

                    double hoursPeriodInDepartment = Math.Round(tsHoursRecordForPeriodList.Where(r => r.EmployeeID != null && employeeInDepartmentIDs.Contains(r.EmployeeID.Value))
                        .ToList()
                        .Select(c => c.Hours ?? 0)
                        .DefaultIfEmpty()
                        .Sum(h => h), 2);

                    dataTable.Rows[periodRowStartIndex]["Department_" + depID.ToString()] = hoursPeriodInDepartment;

                    hoursPeriodTotal += hoursPeriodInDepartment;

                }
            }

            dataTable.Rows[periodRowStartIndex]["Total"] = hoursPeriodTotal;

            periodRowStartIndex = dataTable.Rows.Count - 1;
            foreach (var group in projectTypeUtilizationList.GroupBy(pt => pt.ActivityType))
            {
                dataTable.Rows.Add("-" + group.FirstOrDefault().ActivityType.GetAttributeOfType<DisplayAttribute>().Name);
                dataTable.Rows[dataTable.Rows.Count - 1]["_ISGROUPROW_"] = false;

                foreach (var projectType in group.OrderBy(pt => pt.ShortName))
                {
                    dataTable.Rows.Add("--" + projectType.Title);
                    dataTable.Rows[dataTable.Rows.Count - 1]["_ISGROUPROW_"] = false;

                }
            }

            dataTable.Rows.Add("ИТОГО Утилизировано");
            dataTable.Rows[dataTable.Rows.Count - 1]["_ISGROUPROW_"] = true;
            int utilizationRowNum = dataTable.Rows.Count - 1;

            dataTable = FillDataTableHoursForProjectTypes(dataTable, departmentsIDs, currentEmployeeList, tsHoursRecordForPeriodList,
                projectTypeUtilizationList, periodRowStartIndex);

            periodRowStartIndex = dataTable.Rows.Count - 1;
            foreach (var group in projectTypeNonUtilizationList.GroupBy(pt => pt.ActivityType))
            {
                dataTable.Rows.Add("-" + group.FirstOrDefault().ActivityType.GetAttributeOfType<DisplayAttribute>().Name);
                dataTable.Rows[dataTable.Rows.Count - 1]["_ISGROUPROW_"] = false;

                foreach (var projectType in group.OrderBy(pt => pt.ShortName))
                {
                    dataTable.Rows.Add("--" + projectType.Title);
                    dataTable.Rows[dataTable.Rows.Count - 1]["_ISGROUPROW_"] = false;

                }
            }

            dataTable.Rows.Add("ИТОГО Не улитизировано");
            dataTable.Rows[dataTable.Rows.Count - 1]["_ISGROUPROW_"] = true;
            int nonUtilizationRowNum = dataTable.Rows.Count - 1;

            dataTable = FillDataTableHoursForProjectTypes(dataTable, departmentsIDs, currentEmployeeList, tsHoursRecordForPeriodList,
                projectTypeNonUtilizationList, periodRowStartIndex);

            dataTable.Rows.Add("ИТОГО Утилизация");
            dataTable.Rows[dataTable.Rows.Count - 1]["_ISGROUPROW_"] = true;

            if (departmentsIDs != null)
            {
                foreach (int depID in departmentsIDs)
                {
                    double hoursUtilization = 0;
                    double hoursNonUtilization = 0;

                    if (dataTable.Rows[utilizationRowNum]["Department_" + depID.ToString()] != null
                            || String.IsNullOrEmpty(dataTable.Rows[utilizationRowNum]["Department_" + depID.ToString()].ToString()) == false)
                    {
                        hoursUtilization = (double)dataTable.Rows[utilizationRowNum]["Department_" + depID.ToString()];
                    }

                    if (dataTable.Rows[nonUtilizationRowNum]["Department_" + depID.ToString()] != null
                            || String.IsNullOrEmpty(dataTable.Rows[nonUtilizationRowNum]["Department_" + depID.ToString()].ToString()) == false)
                    {
                        hoursNonUtilization = (double)dataTable.Rows[nonUtilizationRowNum]["Department_" + depID.ToString()];
                    }

                    if ((hoursUtilization + hoursNonUtilization) != 0)
                    {
                        dataTable.Rows[dataTable.Rows.Count - 1]["Department_" + depID.ToString()] = Math.Round(hoursUtilization / (hoursUtilization + hoursNonUtilization), 2);
                    }
                }
            }

            double hoursUtilizationTotal = 0;
            double hoursNonUtilizationTotal = 0;

            if (dataTable.Rows[utilizationRowNum]["Total"] != null
                    || String.IsNullOrEmpty(dataTable.Rows[utilizationRowNum]["Total"].ToString()) == false)
            {
                hoursUtilizationTotal = (double)dataTable.Rows[utilizationRowNum]["Total"];
            }

            if (dataTable.Rows[nonUtilizationRowNum]["Total"] != null
                    || String.IsNullOrEmpty(dataTable.Rows[nonUtilizationRowNum]["Total"].ToString()) == false)
            {
                hoursNonUtilizationTotal = (double)dataTable.Rows[nonUtilizationRowNum]["Total"];
            }

            if ((hoursUtilizationTotal + hoursNonUtilizationTotal) != 0)
            {
                dataTable.Rows[dataTable.Rows.Count - 1]["Total"] = Math.Round(hoursUtilizationTotal / (hoursUtilizationTotal + hoursNonUtilizationTotal), 2);
            }

            dataTable.Rows.Add("");

            return dataTable;
        }

        private DataTable FillDataTableHoursForProjectTypes(DataTable dataTable, List<int> departmentsIDs,
            IList<Employee> employeeList,
            IList<TSHoursRecord> tsHoursRecordForPeriodList,
            IList<ProjectType> projectTypeList,
            int periodRowStartIndex)
        {

            if (departmentsIDs != null)
            {
                foreach (int depID in departmentsIDs)
                {
                    double hoursUtilization = 0;

                    var employeeInDepartmentIDs = GetEmployeesByDepartmentID(depID, employeeList.ToList()).Select(e => e.ID);

                    int k = 0;
                    foreach (var group in projectTypeList.GroupBy(pt => pt.ActivityType))
                    {

                        double hoursUtilizationActivityType = 0;
                        k++;
                        int hoursUtilizationActivityTypeRowNum = periodRowStartIndex + k;


                        foreach (var projectType in group.OrderBy(pt => pt.ShortName))
                        {
                            double hoursUtilizationProjectType = Math.Round(tsHoursRecordForPeriodList.Where(r =>
                                r.Project.ProjectTypeID == projectType.ID && r.EmployeeID != null && employeeInDepartmentIDs.Contains(r.EmployeeID.Value))
                                .ToList()
                                .Select(c => c.Hours ?? 0)
                                .DefaultIfEmpty()
                                .Sum(h => h), 2);

                            k++;
                            dataTable.Rows[periodRowStartIndex + k]["Department_" + depID.ToString()] = hoursUtilizationProjectType;
                            dataTable = AddDoubleValueToCell(dataTable, periodRowStartIndex + k, "Total", hoursUtilizationProjectType);

                            hoursUtilizationActivityType += hoursUtilizationProjectType;
                            hoursUtilization += hoursUtilizationProjectType;
                        }

                        dataTable.Rows[hoursUtilizationActivityTypeRowNum]["Department_" + depID.ToString()] = hoursUtilizationActivityType;
                        dataTable = AddDoubleValueToCell(dataTable, hoursUtilizationActivityTypeRowNum, "Total", hoursUtilizationActivityType);

                    }

                    k++;
                    dataTable.Rows[periodRowStartIndex + k]["Department_" + depID.ToString()] = hoursUtilization;
                    dataTable = AddDoubleValueToCell(dataTable, periodRowStartIndex + k, "Total", hoursUtilization);
                }
            }

            return dataTable;
        }

        private ICollection<Employee> GetEmployeesByDepartmentID(int departmentID, List<Employee> employeeList)
        {
            var employes = employeeList
                .Where(e => e.DepartmentID == departmentID)
                .ToList()
                .OrderBy(e => e.Department.ShortName + e.FullName).ToArray();
            foreach (var department in _departmentService.Get(d => d.Where(x => x.ParentDepartmentID == departmentID).ToList()))
            {
                var result = GetEmployeesByDepartmentID(department.ID, employeeList);
                employes = employes.Concat(result).ToArray();
            }

            return employes;
        }

        private DataTable AddDoubleValueToCell(DataTable dataTable, int rowNum, string columnName, double value)
        {
            if (dataTable.Rows[rowNum][columnName] == null || String.IsNullOrEmpty(dataTable.Rows[rowNum][columnName].ToString()) == true)
            {
                dataTable.Rows[rowNum][columnName] = value;
            }
            else
            {
                dataTable.Rows[rowNum][columnName] = value + (double)dataTable.Rows[rowNum][columnName];
            }

            return dataTable;
        }
    }
}
