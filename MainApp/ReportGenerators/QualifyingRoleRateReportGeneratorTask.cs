using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.IO;
using System.Linq;
using Core;
using Core.BL.Interfaces;
using Core.Common;
using Core.Helpers;
using Core.Models;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using MainApp.Finance;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;





namespace MainApp.ReportGenerators
{
    public class QualifyingRoleRateReportGeneratorTask : LongRunningTaskBase
    {
        private readonly IQualifyingRoleService _qualifyingRoleService;
        private readonly IEmployeeCategoryService _employeeCategoryService;
        private readonly IQualifyingRoleRateService _qualifyingRoleRateService;
        private readonly IEmployeeQualifyingRoleService _employeeQualifyingRoleService;
        private readonly IEmployeeService _employeeService;
        private readonly IDepartmentService _departmentService;
        private readonly IFinanceService _financeService;

        public QualifyingRoleRateReportGeneratorTask(IQualifyingRoleService qualifyingRoleService,
            IEmployeeCategoryService employeeCategoryService,
            IQualifyingRoleRateService qualifyingRoleRateService,
            IEmployeeQualifyingRoleService employeeQualifyingRoleService,
            IEmployeeService employeeService,
            IDepartmentService departmentService, IFinanceService financeService)
            : base()
        {
            _qualifyingRoleService = qualifyingRoleService;
            _employeeCategoryService = employeeCategoryService;
            _qualifyingRoleRateService = qualifyingRoleRateService;
            _employeeQualifyingRoleService = employeeQualifyingRoleService;
            _employeeService = employeeService;
            _departmentService = departmentService;
            _financeService = financeService;
        }

        public ReportGeneratorResult ProcessLongRunningAction(string userIdentityName, string id,
            string reportDate,
            bool? reportRecalcQualifyingRoleRates,
            int? reportHoursPlan,
            List<QualifyingRoleRateFRCCalcParamRecord> qualifyingRoleRateFRCCalcParamRecordList,
            List<int> departmentsIDs,
            DataTable employeePayrollSheetDataTable)
        {
            var htmlErrorReport = string.Empty;


            taskId = id;

            byte[] binData = null;

            try
            {
                SetStatus(0, "Старт формирования отчета...");


                if (reportRecalcQualifyingRoleRates == null)
                {
                    reportRecalcQualifyingRoleRates = false;
                }



                DateTime rDate = DateTime.MinValue;

                try
                {
                    rDate = Convert.ToDateTime(reportDate);
                }
                catch (Exception)
                {
                    rDate = DateTime.MinValue;
                }

                DataTable dataTableEmployee = new DataTable();

                dataTableEmployee.Columns.Add("FCDepartmentShortName", typeof(string)).Caption = "ЦФО";
                dataTableEmployee.Columns["FCDepartmentShortName"].ExtendedProperties["Width"] = (double) 9;
                dataTableEmployee.Columns.Add("DepartmentShortName", typeof(int)).Caption = "Код";
                dataTableEmployee.Columns["DepartmentShortName"].ExtendedProperties["Width"] = (double) 6;
                dataTableEmployee.Columns.Add("Department", typeof(string)).Caption = "Подразделение";
                dataTableEmployee.Columns["Department"].ExtendedProperties["Width"] = (double) 60;
                dataTableEmployee.Columns.Add("DepartmentEmployeeCount", typeof(int)).Caption = "Кол-во";
                dataTableEmployee.Columns["DepartmentEmployeeCount"].ExtendedProperties["Width"] = (double) 10;
                dataTableEmployee.Columns.Add("Position", typeof(string)).Caption = "Позиция";
                dataTableEmployee.Columns["Position"].ExtendedProperties["Width"] = (double) 45;
                dataTableEmployee.Columns.Add("EmployeeTitle", typeof(string)).Caption = "Фамилия Имя Отчество";
                dataTableEmployee.Columns["EmployeeTitle"].ExtendedProperties["Width"] = (double) 39;
                dataTableEmployee.Columns.Add("EmployeePayroll", typeof(int)).Caption = "КОТ";
                dataTableEmployee.Columns["EmployeePayroll"].ExtendedProperties["Width"] = (double) 9;
                dataTableEmployee.Columns.Add("EmployeeGrad", typeof(int)).Caption = "Грейд";
                dataTableEmployee.Columns["EmployeeGrad"].ExtendedProperties["Width"] = (double) 8;
                dataTableEmployee.Columns.Add("EmployeeCategoryTitle", typeof(string)).Caption = "Категория";
                dataTableEmployee.Columns["EmployeeCategoryTitle"].ExtendedProperties["Width"] = (double) 39;
                dataTableEmployee.Columns.Add("EmployeeQualifyingRoleTitle", typeof(string)).Caption = "УПР";
                dataTableEmployee.Columns["EmployeeQualifyingRoleTitle"].ExtendedProperties["Width"] = (double) 39;
                dataTableEmployee.Columns.Add("EmployeeQualifyingRoleType", typeof(string)).Caption = "Тип";
                dataTableEmployee.Columns["EmployeeQualifyingRoleType"].ExtendedProperties["Width"] = (double) 9;
                dataTableEmployee.Columns.Add("EmployeeQualifyingRoleRate", typeof(double)).Caption =
                    "Ставка УПР/час, руб";
                dataTableEmployee.Columns["EmployeeQualifyingRoleRate"].ExtendedProperties["Width"] = (double) 9;
                dataTableEmployee.Columns.Add("EmployeeEnrollmentDate", typeof(DateTime)).Caption = "Принят";
                dataTableEmployee.Columns["EmployeeEnrollmentDate"].ExtendedProperties["Width"] = (double) 12;
                dataTableEmployee.Columns.Add("_ISGROUPROW_", typeof(bool)).Caption = "_ISGROUPROW_";

                if (reportRecalcQualifyingRoleRates == true)
                {
                    _financeService.SetQualifyingRoleRateHoursPlanCalcParam(reportHoursPlan.Value.ToString());

                    string qualifyingRoleRateFRCCalcParamRecordListJson =
                        JsonConvert.SerializeObject(qualifyingRoleRateFRCCalcParamRecordList);

                    _financeService.SetQualifyingRoleRateFRCCalcParamRecordListJson(
                        qualifyingRoleRateFRCCalcParamRecordListJson);

                    List<QualifyingRole> productionQualifyingRoleList = _qualifyingRoleService.Get(x =>
                        x.Where(qr => qr.RoleType == QualifyingRoleType.Production).OrderBy(qr => qr.ShortName)
                            .ToList()).ToList();

                    int j = 0;
                    foreach (QualifyingRoleRateFRCCalcParamRecord qualifyingRoleRateFRCCalcParamRecord in
                        qualifyingRoleRateFRCCalcParamRecordList)
                    {
                        SetStatus(j * 50 / qualifyingRoleRateFRCCalcParamRecordList.Count,
                            "Перерасчет ставок УПР для ЦФО - шаг " + (j + 1).ToString() + " из " +
                            qualifyingRoleRateFRCCalcParamRecordList.Count);
                        List<Employee> employeeInDepartmentList =
                            GetEmployeesInFRCDepartment(qualifyingRoleRateFRCCalcParamRecord.DepartmentID);

                        foreach (QualifyingRole qualifyingRole in productionQualifyingRoleList)
                        {
                            int employeeCountInDepartment = 0;
                            double employeePayrollSum = 0;
                            foreach (Employee employeeItem in employeeInDepartmentList)
                            {
                                EmployeeCategory employeeCategory = _employeeCategoryService.Get(x => x
                                    .Where(ec => ec.EmployeeID == employeeItem.ID)
                                    .OrderBy(ec => ec.CategoryDateEnd).Where(ec => ec.CategoryDateBegin != null
                                                                                   && ec.CategoryDateBegin <= rDate
                                                                                   && (ec.CategoryDateEnd == null ||
                                                                                       ec.CategoryDateEnd >= rDate))
                                    .ToList()).FirstOrDefault();

                                if (employeeCategory != null
                                    && (employeeCategory.CategoryType == EmployeeCategoryType.Regular ||
                                        employeeCategory.CategoryType == EmployeeCategoryType.Temporary))
                                {
                                    EmployeeQualifyingRole employeeQualifyingRole = _employeeQualifyingRoleService.Get(
                                        x =>
                                            x.Include(eqr => eqr.QualifyingRole)
                                                .Where(eqr => eqr.EmployeeID == employeeItem.ID)
                                                .OrderBy(eqr => eqr.QualifyingRoleDateEnd).Where(eqr =>
                                                    eqr.QualifyingRoleDateBegin != null
                                                    && eqr.QualifyingRoleDateBegin <= rDate
                                                    && (eqr.QualifyingRoleDateEnd == null ||
                                                        eqr.QualifyingRoleDateEnd >= rDate)).ToList()).FirstOrDefault();

                                    if (employeeQualifyingRole != null
                                        && employeeQualifyingRole.QualifyingRole.ID == qualifyingRole.ID)
                                    {

                                        double employeePayrollValue =
                                            _financeService.GetEmployeePayrollOnDate(employeePayrollSheetDataTable,
                                                employeeItem, rDate, false, null);

                                        if (employeePayrollValue > 0)
                                        {
                                            employeePayrollSum += employeePayrollValue;
                                            employeeCountInDepartment++;
                                        }
                                        else if (employeePayrollValue < 0)
                                        {
                                            employeePayrollSum += (-employeePayrollValue) * reportHoursPlan.Value;
                                            employeeCountInDepartment++;
                                        }
                                    }
                                }
                            }

                            if (employeeCountInDepartment != 0)
                            {
                                decimal actualAverageMonthPayrollValue =
                                    Convert.ToDecimal(employeePayrollSum / employeeCountInDepartment);
                                decimal actualAverageHourPayrollValue =
                                    Convert.ToDecimal(actualAverageMonthPayrollValue / reportHoursPlan.Value);
                                decimal monthRateValue =
                                    actualAverageMonthPayrollValue * Convert.ToDecimal(
                                        qualifyingRoleRateFRCCalcParamRecord.FRCCorrectionFactor *
                                        qualifyingRoleRateFRCCalcParamRecord.FRCInflationRate);
                                decimal rateValue = actualAverageHourPayrollValue *
                                                    Convert.ToDecimal(
                                                        qualifyingRoleRateFRCCalcParamRecord.FRCCorrectionFactor *
                                                        qualifyingRoleRateFRCCalcParamRecord.FRCInflationRate);

                                QualifyingRoleRate qualifyingRoleRate = _qualifyingRoleRateService.Get(x => x
                                    .Where(qrr =>
                                        qrr.QualifyingRoleID == qualifyingRole.ID
                                        && qrr.DepartmentID == qualifyingRoleRateFRCCalcParamRecord.DepartmentID
                                        && qrr.RateDateBegin == rDate).ToList()).FirstOrDefault();

                                if (qualifyingRoleRate != null)
                                {
                                    qualifyingRoleRate.HoursPlan = reportHoursPlan;
                                    qualifyingRoleRate.ActualAverageMonthPayrollValue = actualAverageMonthPayrollValue;
                                    qualifyingRoleRate.ActualAverageHourPayrollValue = actualAverageHourPayrollValue;
                                    qualifyingRoleRate.MonthRateValue = monthRateValue;
                                    qualifyingRoleRate.RateValue = rateValue;
                                    qualifyingRoleRate.FRCCorrectionFactorValue =
                                        Convert.ToDecimal(qualifyingRoleRateFRCCalcParamRecord.FRCCorrectionFactor);
                                    qualifyingRoleRate.FRCInflationRateValue =
                                        Convert.ToDecimal(qualifyingRoleRateFRCCalcParamRecord.FRCInflationRate);

                                    _qualifyingRoleRateService.Update(qualifyingRoleRate);
                                }
                                else
                                {
                                    qualifyingRoleRate = new QualifyingRoleRate();
                                    qualifyingRoleRate.QualifyingRoleID = qualifyingRole.ID;
                                    qualifyingRoleRate.DepartmentID = qualifyingRoleRateFRCCalcParamRecord.DepartmentID;
                                    qualifyingRoleRate.RateDateBegin = rDate;
                                    qualifyingRoleRate.HoursPlan = reportHoursPlan;
                                    qualifyingRoleRate.ActualAverageMonthPayrollValue = actualAverageMonthPayrollValue;
                                    qualifyingRoleRate.ActualAverageHourPayrollValue = actualAverageHourPayrollValue;
                                    qualifyingRoleRate.MonthRateValue = monthRateValue;
                                    qualifyingRoleRate.RateValue = rateValue;
                                    qualifyingRoleRate.FRCCorrectionFactorValue =
                                        Convert.ToDecimal(qualifyingRoleRateFRCCalcParamRecord.FRCCorrectionFactor);
                                    qualifyingRoleRate.FRCInflationRateValue =
                                        Convert.ToDecimal(qualifyingRoleRateFRCCalcParamRecord.FRCInflationRate);

                                    _qualifyingRoleRateService.Add(qualifyingRoleRate);
                                }
                            }
                        }

                        j++;
                    }
                }

                List<Employee> currentEmployeeList = _employeeService.GetCurrentEmployees(new DateTimeRange(rDate, rDate)).ToList()
                    .Where(e => e.Department != null)
                    .ToList().OrderBy(e => e.Department.ShortName + e.FullName).ToList();

                if (departmentsIDs == null)
                {
                    currentEmployeeList = currentEmployeeList.Where(e => e.Department != null).ToList();
                }
                else
                {
                    currentEmployeeList = currentEmployeeList.Where(e => e.Department != null && departmentsIDs.Contains(e.Department.ID)).ToList();
                }

                int k = 0;
                var employeeCount = currentEmployeeList.Count();

                foreach (var group in currentEmployeeList.GroupBy(e => e.Department.ShortName))
                {
                    String frcDepartmentShortName = "";

                    Department frcDepartment = group.FirstOrDefault().Department;

                    while (frcDepartment.ParentDepartment != null && frcDepartment.IsFinancialCentre == false)
                    {
                        frcDepartment = frcDepartment.ParentDepartment;
                    }

                    if (frcDepartment.IsFinancialCentre == true)
                    {
                        frcDepartmentShortName = frcDepartment.DisplayShortTitle;
                    }

                    int departmentShortNameIntValue = 0;

                    try
                    {
                        departmentShortNameIntValue = Convert.ToInt32(group.Key.Replace("-", ""));
                    }
                    catch (Exception)
                    {
                        departmentShortNameIntValue = 0;
                    }

                    dataTableEmployee.Rows.Add(frcDepartmentShortName, departmentShortNameIntValue, "", group.Count(),
                        "", "");

                    int departmentRowIndex = dataTableEmployee.Rows.Count - 1;

                    dataTableEmployee.Rows[departmentRowIndex]["_ISGROUPROW_"] = true;

                    foreach (var employeeItem in group)
                    {
                        String employeeItemFullName = "";

                        if (String.IsNullOrEmpty(employeeItem.LastName) == false)
                        {
                            employeeItemFullName = employeeItem.LastName;
                        }

                        if (String.IsNullOrEmpty(employeeItem.FirstName) == false)
                        {
                            employeeItemFullName += " " + employeeItem.FirstName;
                        }

                        if (String.IsNullOrEmpty(employeeItem.MidName) == false)
                        {
                            employeeItemFullName += " " + employeeItem.MidName;
                        }

                        employeeItemFullName = employeeItemFullName.Trim();

                        SetStatus(55 + 20 * k / employeeCount, "УПР для: " + employeeItemFullName);

                        dataTableEmployee.Rows.Add(frcDepartmentShortName,
                            departmentShortNameIntValue,
                            ((group.First().Department != null) ? group.First().Department.Title : ""),
                            null,
                            ((employeeItem.EmployeePositionTitle != null) ? employeeItem.EmployeePositionTitle : ""),
                            employeeItemFullName);

                        if (employeeItem.EmployeeGradID != null && employeeItem.EmployeeGrad != null)
                        {
                            int employeeGradShortNameIntValue = 0;

                            try
                            {
                                employeeGradShortNameIntValue = Convert.ToInt32(employeeItem.EmployeeGrad.ShortName);
                            }
                            catch (Exception)
                            {
                                employeeGradShortNameIntValue = 0;
                            }

                            if (employeeGradShortNameIntValue != 0)
                            {
                                dataTableEmployee.Rows[dataTableEmployee.Rows.Count - 1]["EmployeeGrad"] =
                                    employeeGradShortNameIntValue;
                            }
                        }

                        string employeeCategoryTitle = "";

                        EmployeeCategory employeeCategory = _employeeCategoryService.Get(x => x
                                .Where(ec => ec.EmployeeID == employeeItem.ID).OrderBy(ec => ec.CategoryDateEnd).Where(
                                    ec =>
                                        ec.CategoryDateBegin != null
                                        && ec.CategoryDateBegin <= rDate
                                        && (ec.CategoryDateEnd == null || ec.CategoryDateEnd >= rDate)).ToList())
                            .FirstOrDefault();

                        if (employeeCategory != null)
                        {
                            employeeCategoryTitle = ((DisplayAttribute) (employeeCategory.CategoryType.GetType()
                                .GetMember(employeeCategory.CategoryType.ToString()).First()
                                .GetCustomAttributes(true)[0])).Name;
                        }

                        if (String.IsNullOrEmpty(employeeCategoryTitle) == false)
                        {
                            dataTableEmployee.Rows[dataTableEmployee.Rows.Count - 1]["EmployeeCategoryTitle"] =
                                employeeCategoryTitle;
                        }

                        string employeeQualifyingRoleTitle = "";
                        string employeeQualifyingRoleType = "";

                        EmployeeQualifyingRole employeeQualifyingRole = _employeeQualifyingRoleService.Get(x => x
                            .Where(eqr => eqr.EmployeeID == employeeItem.ID).OrderBy(eqr => eqr.QualifyingRoleDateEnd)
                            .Where(eqr => eqr.QualifyingRoleDateBegin != null
                                          && eqr.QualifyingRoleDateBegin <= rDate
                                          && (eqr.QualifyingRoleDateEnd == null || eqr.QualifyingRoleDateEnd >= rDate))
                            .ToList()).FirstOrDefault();

                        if (employeeQualifyingRole != null)
                        {
                            employeeQualifyingRoleTitle = employeeQualifyingRole.QualifyingRole.FullName;
                            employeeQualifyingRoleType = ((DisplayAttribute) (employeeQualifyingRole.QualifyingRole
                                .RoleType.GetType().GetMember(employeeQualifyingRole.QualifyingRole.RoleType.ToString())
                                .First().GetCustomAttributes(true)[0])).Name;
                        }

                        if (String.IsNullOrEmpty(employeeQualifyingRoleTitle) == false)
                        {
                            dataTableEmployee.Rows[dataTableEmployee.Rows.Count - 1]["EmployeeQualifyingRoleTitle"] =
                                employeeQualifyingRoleTitle;
                            dataTableEmployee.Rows[dataTableEmployee.Rows.Count - 1]["EmployeeQualifyingRoleType"] =
                                employeeQualifyingRoleType;
                        }

                        if (employeeQualifyingRole != null)
                        {
                            QualifyingRoleRate qualifyingRoleRate = _qualifyingRoleRateService.Get(x => x.Where(qrr =>
                                    qrr.QualifyingRoleID == employeeQualifyingRole.QualifyingRole.ID
                                    && qrr.DepartmentID == frcDepartment.ID
                                    && qrr.RateDateBegin != null && qrr.RateDateBegin <= rDate)
                                .OrderByDescending(qrr => qrr.RateDateBegin).ToList()).FirstOrDefault();

                            if (qualifyingRoleRate != null && qualifyingRoleRate.RateValue != null)
                            {
                                dataTableEmployee.Rows[dataTableEmployee.Rows.Count - 1]["EmployeeQualifyingRoleRate"] =
                                    Convert.ToDouble(qualifyingRoleRate.RateValue);
                            }
                        }

                        if (employeeItem.EnrollmentDate != null && employeeItem.EnrollmentDate.HasValue == true)
                        {
                            dataTableEmployee.Rows[dataTableEmployee.Rows.Count - 1]["EmployeeEnrollmentDate"] =
                                employeeItem.EnrollmentDate.Value;
                        }

                        double employeePayrollValue =
                            _financeService.GetEmployeePayrollOnDate(employeePayrollSheetDataTable, employeeItem, rDate,
                                false, null);

                        if (employeePayrollValue != 0)
                        {
                            dataTableEmployee.Rows[dataTableEmployee.Rows.Count - 1]["EmployeePayroll"] =
                                (int) employeePayrollValue;
                        }

                        k++;
                    }
                }

                SetStatus(80, "Формирование таблицы ставок УПР по ЦФО...");

                DataTable dataTableQualifyingRoleRate = new DataTable();

                dataTableQualifyingRoleRate.Columns.Add("FCDepartmentShortName", typeof(string)).Caption = "ЦФО";
                dataTableQualifyingRoleRate.Columns["FCDepartmentShortName"].ExtendedProperties["Width"] = (double) 9;
                dataTableQualifyingRoleRate.Columns.Add("DepartmentShortName", typeof(string)).Caption = "Код";
                dataTableQualifyingRoleRate.Columns["DepartmentShortName"].ExtendedProperties["Width"] = (double) 6;
                dataTableQualifyingRoleRate.Columns.Add("Department", typeof(string)).Caption = "Подразделение";
                dataTableQualifyingRoleRate.Columns["Department"].ExtendedProperties["Width"] = (double) 60;
                dataTableQualifyingRoleRate.Columns.Add("QualifyingRoleTitle", typeof(string)).Caption = "УПР";
                dataTableQualifyingRoleRate.Columns["QualifyingRoleTitle"].ExtendedProperties["Width"] = (double) 39;
                dataTableQualifyingRoleRate.Columns.Add("QualifyingRoleRateDateBegin", typeof(DateTime)).Caption =
                    "Действует с";
                dataTableQualifyingRoleRate.Columns["QualifyingRoleRateDateBegin"].ExtendedProperties["Width"] =
                    (double) 12;
                dataTableQualifyingRoleRate.Columns.Add("QualifyingRoleHoursPlan", typeof(int)).Caption = "Часыплан";
                dataTableQualifyingRoleRate.Columns["QualifyingRoleHoursPlan"].ExtendedProperties["Width"] =
                    (double) 12;
                dataTableQualifyingRoleRate.Columns.Add("QualifyingRoleActualAverageMonthPayroll", typeof(double))
                    .Caption = "Факт КОТ/мес, руб.";
                dataTableQualifyingRoleRate.Columns["QualifyingRoleActualAverageMonthPayroll"]
                    .ExtendedProperties["Width"] = (double) 12;
                dataTableQualifyingRoleRate.Columns.Add("QualifyingRoleActualAverageHourPayrol", typeof(double)).Caption
                    = "Факт КОТ/час, руб.";
                dataTableQualifyingRoleRate.Columns["QualifyingRoleActualAverageHourPayrol"]
                    .ExtendedProperties["Width"] = (double) 12;
                dataTableQualifyingRoleRate.Columns.Add("QualifyingRoleFRCCorrectionFactor", typeof(double)).Caption =
                    "ПК-тцфо";
                dataTableQualifyingRoleRate.Columns["QualifyingRoleFRCCorrectionFactor"].ExtendedProperties["Width"] =
                    (double) 12;
                dataTableQualifyingRoleRate.Columns.Add("QualifyingRoleFRCInflationRate", typeof(double)).Caption =
                    "ИК-тцфо";
                dataTableQualifyingRoleRate.Columns["QualifyingRoleFRCInflationRate"].ExtendedProperties["Width"] =
                    (double) 12;
                dataTableQualifyingRoleRate.Columns.Add("QualifyingRoleMonthRate", typeof(double)).Caption =
                    "Ставка УПР/мес, руб";
                dataTableQualifyingRoleRate.Columns["QualifyingRoleMonthRate"].ExtendedProperties["Width"] =
                    (double) 12;
                dataTableQualifyingRoleRate.Columns.Add("QualifyingRoleRate", typeof(double)).Caption =
                    "Ставка УПР/час, руб";
                dataTableQualifyingRoleRate.Columns["QualifyingRoleRate"].ExtendedProperties["Width"] = (double) 12;

                List<QualifyingRoleRate> qualifyingRoleRateList = null;

                try
                {
                    qualifyingRoleRateList = _qualifyingRoleRateService.Get(x => x.Include(qrr => qrr.Department)
                        .Include(qrr => qrr.QualifyingRole).ToList()
                        .OrderByDescending(qrr => qrr.RateDateBegin)
                        .Where(qrr => qrr.RateDateBegin != null && qrr.RateDateBegin <= rDate)
                        .ToList().GroupBy(qrr => qrr.RateDateBegin).FirstOrDefault().ToList()
                        .OrderBy(qrr => qrr.Department.ShortName).ToList()).ToList();
                }
                catch (Exception)
                {
                    qualifyingRoleRateList = null;
                }


                if (qualifyingRoleRateList != null)
                {
                    foreach (QualifyingRoleRate qualifyingRoleRateItem in qualifyingRoleRateList)
                    {
                        try
                        {
                            dataTableQualifyingRoleRate.Rows.Add(qualifyingRoleRateItem.Department.DisplayShortTitle,
                                qualifyingRoleRateItem.Department.ShortName,
                                qualifyingRoleRateItem.Department.Title,
                                qualifyingRoleRateItem.QualifyingRole.FullName,
                                qualifyingRoleRateItem.RateDateBegin,
                                qualifyingRoleRateItem.HoursPlan,
                                qualifyingRoleRateItem.ActualAverageMonthPayrollValue,
                                qualifyingRoleRateItem.ActualAverageHourPayrollValue,
                                qualifyingRoleRateItem.FRCCorrectionFactorValue,
                                qualifyingRoleRateItem.FRCInflationRateValue,
                                qualifyingRoleRateItem.MonthRateValue,
                                qualifyingRoleRateItem.RateValue);
                        }
                        catch (Exception)
                        {
                        }
                    }
                }

                SetStatus(95, "Формирование файла MS Excel...");

                using (MemoryStream stream = new MemoryStream())
                {
                    using (SpreadsheetDocument doc =
                        SpreadsheetDocument.Create(stream, SpreadsheetDocumentType.Workbook))
                    {
                        string sheetsName = "УПР|Ставки УПР";

                        WorkbookPart workbookPart = ExcelHelper.CreateWorkbookPart(doc, sheetsName);

                        int partIndex = 1;

                        ExcelHelper.CreateWorksheetPartAndImportDataTable(workbookPart, "rId" + partIndex.ToString(), 1,
                            1, (uint) dataTableEmployee.Columns.Count,
                            null, dataTableEmployee, 1, 1);

                        partIndex++;

                        ExcelHelper.CreateWorksheetPartAndImportDataTable(workbookPart, "rId" + partIndex.ToString(), 1,
                            1, (uint) dataTableEmployee.Columns.Count,
                            null, dataTableQualifyingRoleRate, 1, 1);

                        doc.WorkbookPart.Workbook.Save();
                    }

                    stream.Position = 0;
                    BinaryReader b = new BinaryReader(stream);
                    binData = b.ReadBytes((int) stream.Length);
                }

                SetStatus(100, "Отчет сформирован");


                return new ReportGeneratorResult() {fileId = id, fileBinData = binData};
            }
            catch (Exception e)
            {
                SetStatus(-1, "Ошибка: " + e.Message.Replace("\r", "").Replace("\n", " "));
                htmlErrorReport += "<br>" + e.Message + "<br>" + e.StackTrace + "<br>" + e.TargetSite.ToString();
            }

            return new ReportGeneratorResult() {fileId = id, fileBinData = binData, htmlErrorReport = htmlErrorReport};
        }

        private List<Employee> GetEmployeesInFRCDepartment(int departmentID)
        {
            var employes = _employeeService.Get(empl => empl.Where(e =>
                    (e.EnrollmentDate == null || e.EnrollmentDate.Value <= DateTime.Today)
                    && (e.DismissalDate == null || e.DismissalDate >= DateTime.Today)).Include(e => e.EmployeePosition)
                .Where(e => e.DepartmentID == departmentID)
                .ToList()
                .OrderBy(e => e.Department.ShortName + e.FullName).ToList()).ToArray();

            foreach (var department in _departmentService.Get(d => d.Where(x => x.ParentDepartmentID == departmentID && x.IsFinancialCentre == false).ToList()))
            {
                var result = GetEmployeesInFRCDepartment(department.ID);
                employes = employes.Concat(result).ToArray();
            }

            return employes.ToList();
        }
    }
}
