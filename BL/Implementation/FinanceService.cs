using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using Core.BL.Interfaces;
using Core.Data;
using Core.Helpers;
using Core.Models;
using Data;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;

namespace BL.Implementation
{
    public class FinanceService : IFinanceService
    {
        private readonly IRPCSDbAccessor _dbAccessor;
        private readonly IApplicationUserService _applicationUserService;
        private readonly IAppPropertyService _appPropertyService;
        private readonly IOOService _ooService;

        public FinanceService(IRepositoryFactory repositoryFactory, IRPCSDbAccessor dbAccessor,
            IApplicationUserService applicationUserService, IAppPropertyService appPropertyService, IOOService ooService)
        {
            _dbAccessor = dbAccessor ?? throw new ArgumentNullException(nameof(dbAccessor));
            _applicationUserService = applicationUserService ?? throw new ArgumentNullException(nameof(applicationUserService));
            _appPropertyService = appPropertyService;
            _ooService = ooService;
        }

        public bool IsEmployeeEqualsDataRow(Employee employee, DataRow dataRow)
        {
            bool result = false;

            try
            {
                if (employee != null && dataRow != null
                    && (String.IsNullOrEmpty(dataRow["EmployeeFullName"].ToString()) == false
                    && String.IsNullOrEmpty(dataRow["EmployeeFullName"].ToString().Trim()) == false
                    && dataRow["EmployeeFullName"].ToString().Trim().ToLower().Equals(employee.FullName.Trim().ToLower()) == true)
                    || (String.IsNullOrEmpty(dataRow["ADEmployeeID"].ToString()) == false
                    && String.IsNullOrEmpty(dataRow["ADEmployeeID"].ToString().Trim()) == false
                    && String.IsNullOrEmpty(employee.ADEmployeeID) == false
                    && String.IsNullOrEmpty(employee.ADEmployeeID.Trim()) == false
                    && dataRow["ADEmployeeID"].ToString().Trim().Equals(employee.ADEmployeeID.Trim()) == true))
                {
                    result = true;
                }
            }
            catch (Exception)
            {
                result = false;
            }

            return result;
        }

        private DateTime? ConvertObjectDataValueToDateTime(object value)
        {
            if (value != null)
            {
                try
                {
                    return Convert.ToDateTime(value);
                }
                catch (Exception ex)
                {
                    return null;
                }
            }
            return null;
        }

        private double ConvertObjectDataValueToDouble(object value)
        {
            if (value != null)
            {
                try
                {
                    return Convert.ToDouble(value);
                }
                catch (Exception ex)
                {
                    return 0;
                }
            }
            return 0;
        }

        private string ConvertObjectDataValueToString(object value)
        {
            if (value != null)
            {
                try
                {
                    return value.ToString();
                }
                catch (Exception ex)
                {
                    return string.Empty;
                }
            }
            return string.Empty;
        }

        private int ConvertObjectDataValueToInt(object value)
        {
            if (value != null)
            {
                try
                {
                    return Convert.ToInt32(value);
                }
                catch (Exception ex)
                {
                    return 0;
                }
            }
            return 0;
        }

        public void ClearRecord(DataTable employeePayrollSheetDataTable, int rowId, bool tempCPFile)
        {
            var row = employeePayrollSheetDataTable.Rows[rowId];
            row["EmployeeGrad"] = 0; // Предлагаемый грейд
            row["PayrollValue"] = 0.0; // Ежемесячная выплата
            row["Param1Value"] = 0.0; // Ежеквартальная выплата
            row["Param2Value"] = 0.0; // Полугодовая выплата
            row["PayrollChangeDate"] = DateTime.Now.Date; // Планируемая дата изменения
            if (tempCPFile)
            {
                row["UserComment"] = "";
                row["UserSpecialComment"] = "";
            }
            else
            {
                row["UserComment"] = "";
            }

            row["Comments"] = ""; // Обоснование изменения
            row["AuthorFullName"] = "";
            row["URegNum"] = "";
            row["Created"] = DateTime.Now.Date; // Дата создания
            // "Указано пользователем: " + User.Identity.Name + ", " + DateTime.Now.ToString() 
            row["SourceElementID"] = "";
            row["RecordType"] = "";
            row["ADEmployeeID"] = "";
            row["EmployeeFullName"] = "";
        }

        public void UpdateEmployeePayrollTableRecords(DataTable employeePayrollSheetDataTable,
            string currentUserLogin, Employee currentEmployee, Employee employee, EmployeePayrollRecord record,
            bool tempCPFile, int rowId = -1)
        {
            if (rowId != -1)
            {
                //обновление
                var row = employeePayrollSheetDataTable.Rows[rowId];
                UpdateEmployeePayrollTableRow(row, currentUserLogin, currentEmployee, employee, record, tempCPFile, false);
            }
            else
            {
                var row = employeePayrollSheetDataTable.NewRow();
                //добавление
                UpdateEmployeePayrollTableRow(row, currentUserLogin, currentEmployee, employee, record, tempCPFile, true);
                employeePayrollSheetDataTable.Rows.Add(row);
            }
        }

        private void UpdateEmployeePayrollTableRow(DataRow row, string currentUserLogin, Employee currentEmployee, Employee employee, EmployeePayrollRecord record, bool tempCPFile, bool isNew)
        {
            row["EmployeeGrad"] = record.EmployeeGrad.Value; // Предлагаемый грейд
            row["PayrollValue"] = record.PayrollValue.Value; // Ежемесячная выплата
            row["Param1Value"] = record.PayrollQuarterValue.HasValue ? record.PayrollQuarterValue.Value : 0; // Ежеквартальная выплата
            row["Param2Value"] = record.PayrollHalfYearValue.HasValue ? record.PayrollHalfYearValue.Value : 0; // Полугодовая выплата
            row["PayrollChangeDate"] = record.PayrollChangeDate.HasValue ? record.PayrollChangeDate.Value : DateTime.MinValue.Date; // Планируемая дата изменения
            if (tempCPFile)
            {
                row["UserComment"] = record.UserComment;
                row["UserSpecialComment"] = record.UserSpecialComment;
            }
            else
            {
                row["UserComment"] = "";
            }
            row["RecordResult"] = EmployeePayrollRecordResultHelper.GetDisplayShortNameFor(record.RecordResult);

            row["Comments"] = "Указано пользователем: " + currentUserLogin + ", " + DateTime.Now.ToString(); // Обоснование изменения
            row["AuthorFullName"] = currentEmployee.FullName;
            row["URegNum"] = record.URegNum;
            row["Created"] = record.Created.HasValue ? record.Created.Value.Date : DateTime.Now.Date; // Дата создания
            // "Указано пользователем: " + User.Identity.Name + ", " + DateTime.Now.ToString() 
            if (isNew)
            {
                row["SourceElementID"] = record.SourceElementID;
                row["RecordType"] = EmployeePayrollRecordTypeHelper.GetDisplayShortNameFor(record.RecordType);
            }
            if (employee != null)
            {
                row["ADEmployeeID"] = employee.ADEmployeeID;
                row["EmployeeFullName"] = employee.FullName;
            }
        }

        private EmployeePayrollRecord DataRowToRecord(DataTable data, Employee employee, int rowId)
        {
            double employeePayrollValue = 0;
            DateTime payrollChangeDate = Convert.ToDateTime(data.Rows[rowId]["PayrollChangeDate"]).Date;
            employeePayrollValue = Convert.ToDouble(data.Rows[rowId]["PayrollValue"]);

            var record = new EmployeePayrollRecord
            {
                ID = 0,
                EmployeeID = employee.ID,
                PayrollChangeDate = payrollChangeDate,
                PayrollValue = employeePayrollValue,

                PayrollQuarterValue = 0,
                PayrollHalfYearValue = 0,

                PaymentMethod = "",
                AdditionallyInfo = "",
            };

            if (data.Columns.Contains("Param1Value"))
                record.PayrollQuarterValue = ConvertObjectDataValueToDouble(data.Rows[rowId]["Param1Value"]);

            if (data.Columns.Contains("Param2Value"))
                record.PayrollHalfYearValue = ConvertObjectDataValueToDouble(data.Rows[rowId]["Param2Value"]);

            if (data.Columns.Contains("Comments"))
                record.Comments = ConvertObjectDataValueToString(data.Rows[rowId]["Comments"]);

            if (data.Columns.Contains("SourceElementID"))
                record.SourceElementID = ConvertObjectDataValueToString(data.Rows[rowId]["SourceElementID"]);

            if (data.Columns.Contains("URegNum"))
                record.URegNum = ConvertObjectDataValueToString(data.Rows[rowId]["URegNum"]);

            if (data.Columns.Contains("EmployeeGrad"))
                record.EmployeeGrad = ConvertObjectDataValueToInt(data.Rows[rowId]["EmployeeGrad"]);

            if (data.Columns.Contains("UserComment"))
                record.UserComment = ConvertObjectDataValueToString(data.Rows[rowId]["UserComment"]);

            if (data.Columns.Contains("UserSpecialComment"))
                record.UserSpecialComment = ConvertObjectDataValueToString(data.Rows[rowId]["UserSpecialComment"]);

            if (data.Columns.Contains("AuthorFullName"))
                record.AuthorFullName = ConvertObjectDataValueToString(data.Rows[rowId]["AuthorFullName"]);

            if (data.Columns.Contains("RecordType"))
            {
                string recType = ConvertObjectDataValueToString(data.Rows[rowId]["RecordType"]);
                record.RecordType = EmployeePayrollRecordTypeHelper.GetByDisplayShortName(recType);
            }
            if (data.Columns.Contains("Created"))
                record.Created = ConvertObjectDataValueToDateTime(data.Rows[rowId]["Created"]);

            if (data.Columns.Contains("RecordResult"))
                record.RecordResult = EmployeePayrollRecordResultHelper.GetByDisplayShortName(ConvertObjectDataValueToString(data.Rows[rowId]["RecordResult"]));

            if (data.Columns.Contains("PaymentMethod") == true && data.Rows[rowId]["PaymentMethod"] != null)
                record.PaymentMethod = data.Rows[rowId]["PaymentMethod"].ToString();

            if (data.Columns.Contains("AdditionallyInfo") == true && data.Rows[rowId]["AdditionallyInfo"] != null)
                record.AdditionallyInfo = data.Rows[rowId]["AdditionallyInfo"].ToString();

            return record;
        }

        public List<EmployeePayrollRecord> GetEmployeePayrollRecordsFromDataTable(DataTable employeePayrollSheetDataTable, Employee employee)
        {
            // double employeePayrollValue = 0;
            // DateTime employeePayrollLastChangeDate = DateTime.MinValue;

            var records = new List<EmployeePayrollRecord>();

            int rowIndex = 1;
            for (int k = 0; k < employeePayrollSheetDataTable.Rows.Count; k++)
            {
                if (IsEmployeeEqualsDataRow(employee, employeePayrollSheetDataTable.Rows[k]) == true)
                {
                    try
                    {
                        //DateTime payrollChangeDate = Convert.ToDateTime(employeePayrollSheetDataTable.Rows[k]["PayrollChangeDate"]).Date;
                        //employeePayrollValue = Convert.ToDouble(employeePayrollSheetDataTable.Rows[k]["PayrollValue"]);

                        //var record = new EmployeePayrollRecord
                        //{
                        //    ID = rowIndex,
                        //    EmployeeID = employee.ID,
                        //    PayrollChangeDate = payrollChangeDate,
                        //    PayrollValue = employeePayrollValue,

                        //    // сделать проверку на null
                        //    PayrollQuarterValue = 0,
                        //    PayrollHalfYearValue = 0,
                        //    // PayrollYearValue = 0,

                        //    //PaymentMethodProbation = "",
                        //    PaymentMethod = "",
                        //    AdditionallyInfo = "",
                        //};

                        //if(employeePayrollSheetDataTable.Columns.Contains("Param1Value"))
                        //{
                        //    record.PayrollQuarterValue = ConvertObjectDataValueToDouble(employeePayrollSheetDataTable.Rows[k]["Param1Value"]);
                        //}

                        //if (employeePayrollSheetDataTable.Columns.Contains("Param2Value"))
                        //{
                        //    record.PayrollHalfYearValue = ConvertObjectDataValueToDouble(employeePayrollSheetDataTable.Rows[k]["Param2Value"]);
                        //}

                        ///*
                        //if (employeePayrollSheetDataTable.Columns.Contains("Param3Value"))
                        //{
                        //    record.PayrollYearValue = ConvertObjectDataValueToDouble(employeePayrollSheetDataTable.Rows[k]["Param3Value"]);
                        //}
                        //*/
                        ////
                        //if (employeePayrollSheetDataTable.Columns.Contains("Comments"))
                        //    record.Comments = ConvertObjectDataValueToString(employeePayrollSheetDataTable.Rows[k]["Comments"]);
                        //if (employeePayrollSheetDataTable.Columns.Contains("SourceElementID"))
                        //    record.SourceElementID = ConvertObjectDataValueToString(employeePayrollSheetDataTable.Rows[k]["SourceElementID"]);
                        //if (employeePayrollSheetDataTable.Columns.Contains("URegNum"))
                        //    record.URegNum = ConvertObjectDataValueToString(employeePayrollSheetDataTable.Rows[k]["URegNum"]);
                        //if (employeePayrollSheetDataTable.Columns.Contains("EmployeeGrad"))
                        //    record.EmployeeGrad = ConvertObjectDataValueToInt(employeePayrollSheetDataTable.Rows[k]["EmployeeGrad"]);
                        //if (employeePayrollSheetDataTable.Columns.Contains("UserComment"))
                        //    record.UserComment = ConvertObjectDataValueToString(employeePayrollSheetDataTable.Rows[k]["UserComment"]);
                        //if (employeePayrollSheetDataTable.Columns.Contains("UserSpecialComment"))
                        //    record.UserSpecialComment = ConvertObjectDataValueToString(employeePayrollSheetDataTable.Rows[k]["UserSpecialComment"]);
                        //if (employeePayrollSheetDataTable.Columns.Contains("AuthorFullName"))
                        //    record.AuthorFullName = ConvertObjectDataValueToString(employeePayrollSheetDataTable.Rows[k]["AuthorFullName"]);
                        //if (employeePayrollSheetDataTable.Columns.Contains("RecordType"))
                        //{
                        //    string recType = ConvertObjectDataValueToString(employeePayrollSheetDataTable.Rows[k]["RecordType"]);
                        //    record.RecordType = EmployeePayrollRecordTypeHelper.GetByDisplayShortName(recType);
                        //}
                        //if (employeePayrollSheetDataTable.Columns.Contains("Created"))
                        //{
                        //    record.Created = ConvertObjectDataValueToDateTime(employeePayrollSheetDataTable.Rows[k]["Created"]);
                        //}
                        //if (employeePayrollSheetDataTable.Columns.Contains("RecordResult"))
                        //    record.RecordResult = EmployeePayrollRecordResultHelper.GetByDisplayShortName(ConvertObjectDataValueToString(employeePayrollSheetDataTable.Rows[k]["RecordResult"]));
                        ////
                        ///*if(employeePayrollSheetDataTable.Columns.Contains("PaymentMethodProbation") == true
                        //    && employeePayrollSheetDataTable.Rows[k]["PaymentMethodProbation"] != null)
                        //{
                        //    record.PaymentMethodProbation = employeePayrollSheetDataTable.Rows[k]["PaymentMethodProbation"].ToString();
                        //}*/

                        //if (employeePayrollSheetDataTable.Columns.Contains("PaymentMethod") == true
                        //    && employeePayrollSheetDataTable.Rows[k]["PaymentMethod"] != null)
                        //{
                        //    record.PaymentMethod = employeePayrollSheetDataTable.Rows[k]["PaymentMethod"].ToString();
                        //}

                        //if (employeePayrollSheetDataTable.Columns.Contains("AdditionallyInfo") == true
                        //    && employeePayrollSheetDataTable.Rows[k]["AdditionallyInfo"] != null)
                        //{
                        //    record.AdditionallyInfo = employeePayrollSheetDataTable.Rows[k]["AdditionallyInfo"].ToString();
                        //}
                        var record = DataRowToRecord(employeePayrollSheetDataTable, employee, k);
                        record.ID = rowIndex;
                        records.Add(record);
                    }
                    catch (Exception)
                    {

                    }
                }

                rowIndex++;
            }

            return records;
        }

        public List<EmployeePayrollRecord> GetFullListEmployeePayrollRecordsFromDataTable(DataTable employeePayrollSheetDataTable, IEnumerable<Employee> employeeList)
        {
            double employeePayrollValue = 0;
            DateTime employeePayrollLastChangeDate = DateTime.MinValue;

            List<EmployeePayrollRecord> records = new List<EmployeePayrollRecord>();

            int rowIndex = 1;
            for (int k = 0; k < employeePayrollSheetDataTable.Rows.Count; k++)
            {
                DataRow dataRow = employeePayrollSheetDataTable.Rows[k];
                Employee employee = null;

                if (String.IsNullOrEmpty(dataRow["ADEmployeeID"].ToString()) == false
                    && String.IsNullOrEmpty(dataRow["ADEmployeeID"].ToString().Trim()) == false)
                {
                    employee = employeeList.Where(e => e.ADEmployeeID == dataRow["ADEmployeeID"].ToString().Trim()).FirstOrDefault();
                }

                if (employee == null
                    && String.IsNullOrEmpty(dataRow["EmployeeFullName"].ToString()) == false
                    && String.IsNullOrEmpty(dataRow["EmployeeFullName"].ToString().Trim()) == false)
                {
                    employee = employeeList.Where(e => e.FullName.ToLower().Trim() == dataRow["EmployeeFullName"].ToString().ToLower().Trim()).FirstOrDefault();
                }

                if (employee != null)
                {
                    try
                    {
                        DateTime payrollChangeDate = Convert.ToDateTime(employeePayrollSheetDataTable.Rows[k]["PayrollChangeDate"]).Date;
                        employeePayrollValue = Convert.ToDouble(employeePayrollSheetDataTable.Rows[k]["PayrollValue"]);

                        var record = DataRowToRecord(employeePayrollSheetDataTable, employee, k);
                        record.ID = rowIndex++;
                        record.Employee = employee;
                        records.Add(record);
                        // records.Add(new EmployeePayrollRecord { ID = rowIndex++, EmployeeID = employee.ID, PayrollChangeDate = payrollChangeDate, PayrollValue = employeePayrollValue });

                    }
                    catch (Exception)
                    {

                    }
                }
            }

            return records;
        }

        public double GetEmployeePayrollOnDate(DataTable employeePayrollSheetDataTable, Employee employee, DateTime date, bool useEmployeeCategory,
            IEnumerable<EmployeeCategory> employeeCategories)
        {
            double employeePayrollValue = 0;
            DateTime employeePayrollLastChangeDate = DateTime.MinValue;

            for (int k = 0; k < employeePayrollSheetDataTable.Rows.Count; k++)
            {
                if (IsEmployeeEqualsDataRow(employee, employeePayrollSheetDataTable.Rows[k]) == true)
                {
                    try
                    {
                        DateTime payrollChangeDate = Convert.ToDateTime(employeePayrollSheetDataTable.Rows[k]["PayrollChangeDate"]).Date;

                        if (payrollChangeDate.Date > employeePayrollLastChangeDate.Date
                            && payrollChangeDate.Date <= date.Date)
                        {
                            employeePayrollValue = Convert.ToDouble(employeePayrollSheetDataTable.Rows[k]["PayrollValue"]);
                            employeePayrollLastChangeDate = payrollChangeDate;
                        }
                    }
                    catch (Exception)
                    {

                    }
                }
            }

            if (useEmployeeCategory == true)
            {
                bool dontCalcPayrollOnDate = true;

                if (employeeCategories != null)
                {
                    foreach (EmployeeCategory category in employeeCategories)
                    {
                        try
                        {
                            if (category.CategoryType == EmployeeCategoryType.Regular
                                || category.CategoryType == EmployeeCategoryType.Temporary)
                            {
                                if (category.CategoryDateBegin != null
                                    && category.CategoryDateBegin <= date
                                    && (category.CategoryDateEnd == null
                                    || category.CategoryDateEnd >= date))
                                {
                                    dontCalcPayrollOnDate = false;
                                    break;
                                }
                            }
                        }
                        catch (Exception)
                        {

                        }
                    }
                }

                if (dontCalcPayrollOnDate == true)
                {
                    employeePayrollValue = 0;
                }

            }

            return employeePayrollValue;
        }

        public DateTime GetEmployeePayrollLastChangeDateOnDate(DataTable employeePayrollSheetDataTable, Employee employee, DateTime date)
        {
            DateTime employeePayrollLastChangeDate = DateTime.MinValue;

            for (int k = 0; k < employeePayrollSheetDataTable.Rows.Count; k++)
            {
                if (IsEmployeeEqualsDataRow(employee, employeePayrollSheetDataTable.Rows[k]) == true)
                {
                    try
                    {
                        DateTime payrollChangeDate = Convert.ToDateTime(employeePayrollSheetDataTable.Rows[k]["PayrollChangeDate"]).Date;

                        if (payrollChangeDate.Date > employeePayrollLastChangeDate.Date
                            && payrollChangeDate.Date <= date.Date)
                        {
                            employeePayrollLastChangeDate = payrollChangeDate;
                        }
                    }
                    catch (Exception)
                    {

                    }
                }
            }

            return employeePayrollLastChangeDate;
        }

        public double GetProjectEmployeeOvertimePayrollDeltaValueForEmployee(DataTable projectsOtherCostsSheetDataTable,
           DataTable employeePayrollSheetDataTable,
           string projectShortName,
           Employee employee,
           double employeeMonthOverHours,
           int monthWorkHours,
           DateTime periodStart, DateTime periodEnd)
        {
            double projectEmployeeOvertimePayrollValueDeltaForEmployee = 0;
            double projectEmployeeOvertimePayrollValueForEmployee = 0;
            double employeePayrollValue = GetEmployeePayrollOnDate(employeePayrollSheetDataTable, employee, periodEnd, false, null);

            double employeeHourCost = 0;

            if (employeePayrollValue != 0)
            {
                if (employeePayrollValue > 0)
                {
                    employeeHourCost = employeePayrollValue / monthWorkHours;
                }
                else
                {
                    employeeHourCost = (-employeePayrollValue);
                }
            }

            for (int k = 0; k < projectsOtherCostsSheetDataTable.Rows.Count; k++)
            {
                try
                {
                    if (projectsOtherCostsSheetDataTable.Rows[k]["RecordDate"] != null
                        && String.IsNullOrEmpty(projectsOtherCostsSheetDataTable.Rows[k]["RecordDate"].ToString()) == false
                        && projectsOtherCostsSheetDataTable.Rows[k]["ProjectShortName"] != null
                        && String.IsNullOrEmpty(projectsOtherCostsSheetDataTable.Rows[k]["ProjectShortName"].ToString()) == false
                        && projectsOtherCostsSheetDataTable.Rows[k]["ProjectShortName"].ToString().Equals(projectShortName) == true
                        && IsEmployeeEqualsDataRow(employee, projectsOtherCostsSheetDataTable.Rows[k]) == true)
                    {
                        DateTime recordDate = Convert.ToDateTime(projectsOtherCostsSheetDataTable.Rows[k]["RecordDate"]).Date;

                        if (recordDate <= periodEnd
                            && recordDate >= periodStart
                            && projectsOtherCostsSheetDataTable.Rows[k]["OvertimePayrollValue"] != null
                            && String.IsNullOrEmpty(projectsOtherCostsSheetDataTable.Rows[k]["OvertimePayrollValue"].ToString()) == false
                            && projectsOtherCostsSheetDataTable.Rows[k]["OvertimePayrollRate"] != null
                            && String.IsNullOrEmpty(projectsOtherCostsSheetDataTable.Rows[k]["OvertimePayrollRate"].ToString()) == false)
                        {
                            double overtimePayrollValue = Convert.ToDouble(projectsOtherCostsSheetDataTable.Rows[k]["OvertimePayrollValue"]);

                            projectEmployeeOvertimePayrollValueForEmployee += overtimePayrollValue;
                        }
                    }
                }
                catch (Exception)
                {

                }
            }

            if (projectEmployeeOvertimePayrollValueForEmployee > employeeMonthOverHours * employeeHourCost)
            {
                for (int k = 0; k < projectsOtherCostsSheetDataTable.Rows.Count; k++)
                {
                    try
                    {
                        if (projectsOtherCostsSheetDataTable.Rows[k]["RecordDate"] != null
                            && String.IsNullOrEmpty(projectsOtherCostsSheetDataTable.Rows[k]["RecordDate"].ToString()) == false
                            && projectsOtherCostsSheetDataTable.Rows[k]["ProjectShortName"] != null
                            && String.IsNullOrEmpty(projectsOtherCostsSheetDataTable.Rows[k]["ProjectShortName"].ToString()) == false
                            && projectsOtherCostsSheetDataTable.Rows[k]["ProjectShortName"].ToString().Equals(projectShortName) == true
                            && IsEmployeeEqualsDataRow(employee, projectsOtherCostsSheetDataTable.Rows[k]) == true)
                        {
                            DateTime recordDate = Convert.ToDateTime(projectsOtherCostsSheetDataTable.Rows[k]["RecordDate"]).Date;

                            if (recordDate <= periodEnd
                                && recordDate >= periodStart
                                && projectsOtherCostsSheetDataTable.Rows[k]["OvertimePayrollValue"] != null
                                && String.IsNullOrEmpty(projectsOtherCostsSheetDataTable.Rows[k]["OvertimePayrollValue"].ToString()) == false
                                && projectsOtherCostsSheetDataTable.Rows[k]["OvertimePayrollRate"] != null
                                && String.IsNullOrEmpty(projectsOtherCostsSheetDataTable.Rows[k]["OvertimePayrollRate"].ToString()) == false)
                            {
                                double overtimePayrollValue = Convert.ToDouble(projectsOtherCostsSheetDataTable.Rows[k]["OvertimePayrollValue"]);
                                double overtimePayrollRate = Convert.ToDouble(projectsOtherCostsSheetDataTable.Rows[k]["OvertimePayrollRate"]);

                                if (overtimePayrollRate > 1)
                                {
                                    projectEmployeeOvertimePayrollValueDeltaForEmployee += (overtimePayrollRate - 1) * overtimePayrollValue / overtimePayrollRate;
                                }
                            }
                        }
                    }
                    catch (Exception)
                    {

                    }
                }
            }

            return projectEmployeeOvertimePayrollValueDeltaForEmployee;
        }

        public double GetProjectEmployeePerformanceBonusValueForEmployee(DataTable projectsOtherCostsSheetDataTable, string projectShortName,
            Employee employee,
            DateTime periodStart, DateTime periodEnd)
        {
            double projectEmployeePerformanceBonusValue = 0;

            if (String.IsNullOrEmpty(projectShortName) == false)
            {
                DataRow[] rowsByProject = projectsOtherCostsSheetDataTable.Select("ProjectShortName = '" + projectShortName + "'");

                for (int k = 0; k < rowsByProject.Length; k++)
                {
                    try
                    {
                        if (rowsByProject[k]["RecordDate"] != null
                            && String.IsNullOrEmpty(rowsByProject[k]["RecordDate"].ToString()) == false
                            && (employee == null
                            || (employee != null
                            && IsEmployeeEqualsDataRow(employee, rowsByProject[k]) == true)))
                        {
                            DateTime recordDate = Convert.ToDateTime(rowsByProject[k]["RecordDate"]).Date;

                            if (recordDate <= periodEnd
                                && recordDate >= periodStart
                                && rowsByProject[k]["PerformanceBonusValue"] != null
                                && String.IsNullOrEmpty(rowsByProject[k]["PerformanceBonusValue"].ToString()) == false)
                            {
                                projectEmployeePerformanceBonusValue += Convert.ToDouble(rowsByProject[k]["PerformanceBonusValue"]);
                            }
                        }
                    }
                    catch (Exception)
                    {

                    }
                }
            }

            return projectEmployeePerformanceBonusValue;
        }

        public double GetProjectOtherCostsValueForEmployee(DataTable projectsOtherCostsSheetDataTable, string projectShortName,
            Employee employee,
            DateTime periodStart, DateTime periodEnd)
        {
            double projectOtherCostsValue = 0;

            if (String.IsNullOrEmpty(projectShortName) == false)
            {
                DataRow[] rowsByProject = projectsOtherCostsSheetDataTable.Select("ProjectShortName = '" + projectShortName + "'");

                for (int k = 0; k < rowsByProject.Length; k++)
                {
                    try
                    {
                        if (rowsByProject[k]["RecordDate"] != null
                            && String.IsNullOrEmpty(rowsByProject[k]["RecordDate"].ToString()) == false
                            && (employee == null
                            || (employee != null
                            && IsEmployeeEqualsDataRow(employee, rowsByProject[k]) == true)))
                        {
                            DateTime recordDate = Convert.ToDateTime(rowsByProject[k]["RecordDate"]).Date;

                            if (recordDate <= periodEnd
                                && recordDate >= periodStart
                                && rowsByProject[k]["OtherCostsValue"] != null
                                && String.IsNullOrEmpty(rowsByProject[k]["OtherCostsValue"].ToString()) == false)
                            {
                                projectOtherCostsValue += Convert.ToDouble(rowsByProject[k]["OtherCostsValue"]);
                            }
                        }
                    }
                    catch (Exception)
                    {

                    }
                }
            }

            return projectOtherCostsValue;
        }

        public DataTable GetEmployeePayrollSheetDataTableFromOO(ApplicationUser user, bool tempCPFile)
        {
            DataTable employeePayrollSheetDataTable = new DataTable();
            employeePayrollSheetDataTable.Columns.Add("ADEmployeeID", typeof(string)).Caption = "EmployeeID";
            employeePayrollSheetDataTable.Columns["ADEmployeeID"].ExtendedProperties["Width"] = (double)18;
            employeePayrollSheetDataTable.Columns.Add("EmployeeFullName", typeof(string)).Caption = "ФИО";
            employeePayrollSheetDataTable.Columns["EmployeeFullName"].ExtendedProperties["Width"] = (double)41;
            employeePayrollSheetDataTable.Columns.Add("PayrollChangeDate", typeof(DateTime)).Caption = "Дата";
            employeePayrollSheetDataTable.Columns["PayrollChangeDate"].ExtendedProperties["Width"] = (double)12;
            employeePayrollSheetDataTable.Columns.Add("PayrollValue", typeof(double)).Caption = "КОТ";
            employeePayrollSheetDataTable.Columns["PayrollValue"].ExtendedProperties["Width"] = (double)12;
            employeePayrollSheetDataTable.Columns.Add("Comments", typeof(string)).Caption = "Комментарий";
            employeePayrollSheetDataTable.Columns["Comments"].ExtendedProperties["Width"] = (double)60;
            employeePayrollSheetDataTable.Columns.Add("Param1StartDate", typeof(DateTime)).Caption = "Дата отсчета";
            employeePayrollSheetDataTable.Columns["Param1StartDate"].ExtendedProperties["Width"] = (double)12;
            employeePayrollSheetDataTable.Columns.Add("Param1", typeof(string)).Caption = "Пар. 1";
            employeePayrollSheetDataTable.Columns["Param1"].ExtendedProperties["Width"] = (double)12;
            employeePayrollSheetDataTable.Columns.Add("Param1Value", typeof(double)).Caption = "Сумма";
            employeePayrollSheetDataTable.Columns["Param1Value"].ExtendedProperties["Width"] = (double)12;
            employeePayrollSheetDataTable.Columns.Add("Param2StartDate", typeof(DateTime)).Caption = "Дата отсчета";
            employeePayrollSheetDataTable.Columns["Param2StartDate"].ExtendedProperties["Width"] = (double)12;
            employeePayrollSheetDataTable.Columns.Add("Param2", typeof(string)).Caption = "Пар. 2";
            employeePayrollSheetDataTable.Columns["Param2"].ExtendedProperties["Width"] = (double)12;
            employeePayrollSheetDataTable.Columns.Add("Param2Value", typeof(double)).Caption = "Сумма";
            employeePayrollSheetDataTable.Columns["Param2Value"].ExtendedProperties["Width"] = (double)12;

            if (tempCPFile == false)
            {
                employeePayrollSheetDataTable.Columns.Add("PaymentMethod", typeof(string)).Caption = "Способ выплат";
                employeePayrollSheetDataTable.Columns["PaymentMethod"].ExtendedProperties["Width"] = (double)20;
                employeePayrollSheetDataTable.Columns.Add("AdditionallyInfo", typeof(string)).Caption = "Доп. отметки";
                employeePayrollSheetDataTable.Columns["AdditionallyInfo"].ExtendedProperties["Width"] = (double)20;
            }
            else
            {
                employeePayrollSheetDataTable.Columns.Add("PaymentMethodProbation", typeof(string)).Caption = "Способ выплат ИС";
                employeePayrollSheetDataTable.Columns["PaymentMethodProbation"].ExtendedProperties["Width"] = (double)20;
                employeePayrollSheetDataTable.Columns.Add("PaymentMethod", typeof(string)).Caption = "Способ выплат";
                employeePayrollSheetDataTable.Columns["PaymentMethod"].ExtendedProperties["Width"] = (double)20;
                employeePayrollSheetDataTable.Columns.Add("AdditionallyInfo", typeof(string)).Caption = "Доп. отметки";
                employeePayrollSheetDataTable.Columns["AdditionallyInfo"].ExtendedProperties["Width"] = (double)20;
            }

            if (IsCPFileAllowHistoryColumns() == true)
            {
                employeePayrollSheetDataTable.Columns.Add("SourceElementID", typeof(string)).Caption = "SourceElementID";
                employeePayrollSheetDataTable.Columns["SourceElementID"].ExtendedProperties["Width"] = (double)12;

                employeePayrollSheetDataTable.Columns.Add("URegNum", typeof(string)).Caption = "Ун. номер заявки";
                employeePayrollSheetDataTable.Columns["URegNum"].ExtendedProperties["Width"] = (double)12;

                employeePayrollSheetDataTable.Columns.Add("EmployeeGrad", typeof(int)).Caption = "Грейд";
                employeePayrollSheetDataTable.Columns["EmployeeGrad"].ExtendedProperties["Width"] = (double)8;

                employeePayrollSheetDataTable.Columns.Add("UserComment", typeof(string)).Caption = "Обоснование/Комментарий";
                employeePayrollSheetDataTable.Columns["UserComment"].ExtendedProperties["Width"] = (double)60;

                employeePayrollSheetDataTable.Columns.Add("UserSpecialComment", typeof(string)).Caption = "Особое мнение";
                employeePayrollSheetDataTable.Columns["UserSpecialComment"].ExtendedProperties["Width"] = (double)60;

                employeePayrollSheetDataTable.Columns.Add("RecordType", typeof(string)).Caption = "Тип записи";
                employeePayrollSheetDataTable.Columns["RecordType"].ExtendedProperties["Width"] = (double)20;

                employeePayrollSheetDataTable.Columns.Add("AuthorFullName", typeof(string)).Caption = "ФИО автора записи";
                employeePayrollSheetDataTable.Columns["AuthorFullName"].ExtendedProperties["Width"] = (double)12;

                employeePayrollSheetDataTable.Columns.Add("Created", typeof(DateTime)).Caption = "Создано";
                employeePayrollSheetDataTable.Columns["Created"].ExtendedProperties["Width"] = (double)12;

                employeePayrollSheetDataTable.Columns.Add("RecordResult", typeof(string)).Caption = "Результат";
                employeePayrollSheetDataTable.Columns["RecordResult"].ExtendedProperties["Width"] = (double)12;
            }

            string docServerLogin = _applicationUserService.GetOOLogin();
            string docServerPassword = _applicationUserService.GetOOPassword();

            Stream employeePayrollSheetStream = null;

            if (String.IsNullOrEmpty(docServerLogin) == false
                && String.IsNullOrEmpty(docServerPassword) == false)
            {
                string employeePayrollSheetFileUrl = null;

                if (tempCPFile == false)
                {
                    employeePayrollSheetFileUrl = GetOODefalutCPFileUrl();
                }
                else
                {
                    employeePayrollSheetFileUrl = GetOOTempCPFileUrl();
                }

                if (String.IsNullOrEmpty(employeePayrollSheetFileUrl) == false)
                {
                    byte[] employeePayrollSheetFileBinData = _ooService.DownloadFile(employeePayrollSheetFileUrl, docServerLogin, docServerPassword);

                    if (employeePayrollSheetFileBinData != null)
                    {
                        employeePayrollSheetStream = new MemoryStream(employeePayrollSheetFileBinData);
                    }
                }
            }

            if (employeePayrollSheetStream != null)
            {
                employeePayrollSheetDataTable = ExcelHelper.ExportData(employeePayrollSheetDataTable, employeePayrollSheetStream);
            }

            return employeePayrollSheetDataTable;
        }


        public bool PutEmployeePayrollSheetDataTableToOO(ApplicationUser user, DataTable employeePayrollSheetDataTable, bool tempCPFile)
        {
            bool result = false;
            byte[] employeePayrollSheetFileBinData = null;

            string docServerLogin = _applicationUserService.GetOOLogin();
            string docServerPassword = _applicationUserService.GetOOPassword();

            using (MemoryStream stream = new MemoryStream())
            {
                using (SpreadsheetDocument doc = SpreadsheetDocument.Create(stream, SpreadsheetDocumentType.Workbook))
                {
                    WorkbookPart workbookPart = ExcelHelper.CreateWorkbookPart(doc, "КОТ");

                    WorksheetPart rId1 = ExcelHelper.CreateWorksheetPartAndImportDataTable(workbookPart, "rId1", 1, 1, (uint)employeePayrollSheetDataTable.Columns.Count,
                        null, employeePayrollSheetDataTable, 1, 1);

                    doc.WorkbookPart.Workbook.Save();
                }

                stream.Position = 0;
                BinaryReader b = new BinaryReader(stream);
                employeePayrollSheetFileBinData = b.ReadBytes((int)stream.Length);
            }

            if (String.IsNullOrEmpty(docServerLogin) == false
                && String.IsNullOrEmpty(docServerPassword) == false)
            {
                string employeePayrollSheetFileUrl = null;

                if (tempCPFile == false)
                {
                    employeePayrollSheetFileUrl = GetOODefalutCPFileUrl();
                }
                else
                {
                    employeePayrollSheetFileUrl = GetOOTempCPFileUrl();
                }

                if (String.IsNullOrEmpty(employeePayrollSheetFileUrl) == false)
                {
                    result = _ooService.UploadFileVersion(employeePayrollSheetFileUrl, docServerLogin, docServerPassword, employeePayrollSheetFileBinData);
                    // result = true;
                }
            }

            return result;
        }

        private string GetAppSetting(string name)
        {
            string result = "";

            //RPCSContext db = new RPCSContext();

            //AppProperty appProperty = db.AppProperties.Where(ap => ap.Name == name).FirstOrDefault();

            //if (appProperty != null)
            //    result = appProperty.Value;

            //if (!String.IsNullOrEmpty(result))
            //    return result;

            //if (ConfigurationManager.AppSettings.AllKeys.Contains(name))
            //    result = ConfigurationManager.AppSettings[name];

            return result;
        }

        private void SetAppSetting(string name, string value)
        {
            //RPCSContext db = new RPCSContext();

            //AppProperty appProperty = db.AppProperties.Where(ap => ap.Name == name).FirstOrDefault();

            //if (appProperty != null)
            //{
            //    appProperty.Value = value;
            //    db.Entry(appProperty).State = EntityState.Modified;
            //    db.SaveChanges();
            //}
            //else
            //{
            //    appProperty = new AppProperty
            //    {
            //        Name = name,
            //        Value = value
            //    };
            //    db.AppProperties.Add(appProperty);
            //    db.SaveChanges();
            //}

        }

        public string GetOODefalutCPFileUrl()
        {
            return GetAppSetting("OODefalutCPFileUrl");
        }

        public string GetOOTempCPFileUrl()
        {
            return GetAppSetting("OOTempCPFileUrl");
        }

        public string GetProjectEmployeePayrollCostsCalcMethod()
        {
            return GetAppSetting("ProjectEmployeePayrollCostsCalcMethod");
        }

        public string GetQualifyingRoleRateFRCCalcParamRecordListJson()
        {
            return GetAppSetting("QualifyingRoleRateFRCCalcParamRecordListJson");
        }

        public void SetQualifyingRoleRateFRCCalcParamRecordListJson(string value)
        {
            SetAppSetting("QualifyingRoleRateFRCCalcParamRecordListJson", value);
        }

        public string GetQualifyingRoleRateHoursPlanCalcParam()
        {
            return GetAppSetting("QualifyingRoleRateHoursPlanCalcParam");
        }

        public void SetQualifyingRoleRateHoursPlanCalcParam(string value)
        {
            SetAppSetting("QualifyingRoleRateHoursPlanCalcParam", value);
        }

        public bool IsCPFileAllowHistoryColumns()
        {
            bool result = false;

            try
            {
                result = Convert.ToBoolean(GetAppSetting("OOCPFileAllowHistoryColumns"));
            }
            catch (Exception)
            {
                result = false;
            }

            return result;
        }

        public byte[] GetEmployeePayrollChangeReport(EmployeePayrollRecord employeePayrollRecord, EmployeePayrollRecord tmpEmployeePayrollRecord, double oldGradPercent, double newGradPercent, string comment)
        {
            var dataTable = new DataTable();

            dataTable.Columns.Add("DepartmentName", typeof(string)).Caption = "ЦФО";
            dataTable.Columns["DepartmentName"].ExtendedProperties["Width"] = (double)7;

            dataTable.Columns.Add("FullName", typeof(string)).Caption = "ФИО";
            dataTable.Columns["FullName"].ExtendedProperties["Width"] = (double)24;

            dataTable.Columns.Add("EmployeePosition", typeof(string)).Caption = "ДОЛЖНОСТЬ";
            dataTable.Columns["EmployeePosition"].ExtendedProperties["Width"] = (double)18;

            {
                // dataTable.Columns.Add("PayrollValue", typeof(ExcelCell)).Caption = "ТЕКУЩИЙ";
                dataTable.Columns.Add("PayrollValue", typeof(double)).Caption = "ТЕКУЩИЙ";
                dataTable.Columns["PayrollValue"].ExtendedProperties["Width"] = (double)11;
                dataTable.Columns["PayrollValue"].ExtendedProperties["SubCaption1"] = "ЕЖЕМЕСЯЧНЫЙ ОКЛАД СОТРУДНИКА";
                dataTable.Columns["PayrollValue"].ExtendedProperties["SubCaptionColumnSpan1"] = 2;

                dataTable.Columns.Add("PayrollValue_new", typeof(double)).Caption = "НОВЫЙ";
                dataTable.Columns["PayrollValue_new"].ExtendedProperties["Width"] = (double)11;
                dataTable.Columns["PayrollValue_new"].ExtendedProperties["SubCaption1"] = "";
            }

            {
                // dataTable.Columns.Add("PayrollValue_increase", typeof(ExcelCell)).Caption = "РУБ.";
                dataTable.Columns.Add("PayrollValue_increase", typeof(double)).Caption = "РУБ.";
                dataTable.Columns["PayrollValue_increase"].ExtendedProperties["Width"] = (double)11;
                dataTable.Columns["PayrollValue_increase"].ExtendedProperties["SubCaption1"] = "ПОВЫШЕНИЕ ЕЖЕМЕСЯЧНОГО ОКЛАДА";
                dataTable.Columns["PayrollValue_increase"].ExtendedProperties["SubCaptionColumnSpan1"] = 2;

                dataTable.Columns.Add("PayrollValue_increase_percent", typeof(double)).Caption = "%.";
                dataTable.Columns["PayrollValue_increase_percent"].ExtendedProperties["Width"] = (double)11;
                dataTable.Columns["PayrollValue_increase_percent"].ExtendedProperties["SubCaption1"] = "";
            }

            {
                // dataTable.Columns.Add("EmployeeGrad", typeof(ExcelCell)).Caption = "ТЕКУЩИЙ";
                dataTable.Columns.Add("EmployeeGrad", typeof(int)).Caption = "ТЕКУЩИЙ";
                dataTable.Columns["EmployeeGrad"].ExtendedProperties["Width"] = (double)11;
                dataTable.Columns["EmployeeGrad"].ExtendedProperties["SubCaption1"] = "ГРЕЙД СОТРУДНИКА";
                dataTable.Columns["EmployeeGrad"].ExtendedProperties["SubCaptionColumnSpan1"] = 4;

                dataTable.Columns.Add("EmployeeGrad_percent", typeof(double)).Caption = "% от годовой ЗП";
                dataTable.Columns["EmployeeGrad_percent"].ExtendedProperties["Width"] = (double)14;
                dataTable.Columns["EmployeeGrad_percent"].ExtendedProperties["SubCaption1"] = "";

                dataTable.Columns.Add("EmployeeGrad_new", typeof(int)).Caption = "НОВЫЙ";
                dataTable.Columns["EmployeeGrad_new"].ExtendedProperties["Width"] = (double)11;
                dataTable.Columns["EmployeeGrad_new"].ExtendedProperties["SubCaption1"] = "";

                dataTable.Columns.Add("EmployeeGrad_new_percent", typeof(double)).Caption = "% от годовой ЗП";
                dataTable.Columns["EmployeeGrad_new_percent"].ExtendedProperties["Width"] = (double)14;
                dataTable.Columns["EmployeeGrad_new_percent"].ExtendedProperties["SubCaption1"] = "";
            }

            {
                // dataTable.Columns.Add("EmployeeGrad", typeof(ExcelCell)).Caption = "ТЕКУЩИЙ";
                dataTable.Columns.Add("PayrollYearValue", typeof(double)).Caption = "ТЕКУЩАЯ";
                dataTable.Columns["PayrollYearValue"].ExtendedProperties["Width"] = (double)14;
                dataTable.Columns["PayrollYearValue"].ExtendedProperties["SubCaption1"] = "ГОДОВАЯ ЗП СОТРУДНИКА";
                dataTable.Columns["PayrollYearValue"].ExtendedProperties["SubCaptionColumnSpan1"] = 3;

                dataTable.Columns.Add("PayrollYearValue_new", typeof(double)).Caption = "НОВАЯ";
                dataTable.Columns["PayrollYearValue_new"].ExtendedProperties["Width"] = (double)14;
                dataTable.Columns["PayrollYearValue_new"].ExtendedProperties["SubCaption1"] = "";

                dataTable.Columns.Add("PayrollYearValue_increase", typeof(double)).Caption = "ПОВЫШЕНИЕ";
                dataTable.Columns["PayrollYearValue_increase"].ExtendedProperties["Width"] = (double)14;
                dataTable.Columns["PayrollYearValue_increase"].ExtendedProperties["SubCaption1"] = "";
            }

            {
                dataTable.Columns.Add("MaxBenifitYearValue", typeof(double)).Caption = "ТЕКУЩАЯ";
                dataTable.Columns["MaxBenifitYearValue"].ExtendedProperties["Width"] = (double)11;
                dataTable.Columns["MaxBenifitYearValue"].ExtendedProperties["SubCaption1"] = "МАКСИМАЛЬНО ВОЗМОЖНАЯ ГОДОВАЯ ПРЕМИЯ";
                dataTable.Columns["MaxBenifitYearValue"].ExtendedProperties["SubCaptionColumnSpan1"] = 3;

                dataTable.Columns.Add("MaxBenifitYearValue_new", typeof(double)).Caption = "НОВАЯ";
                dataTable.Columns["MaxBenifitYearValue_new"].ExtendedProperties["Width"] = (double)14;
                dataTable.Columns["MaxBenifitYearValue_new"].ExtendedProperties["SubCaption1"] = "";

                dataTable.Columns.Add("MaxBenifitYearValue_increase", typeof(double)).Caption = "ПОВЫШЕНИЕ";
                dataTable.Columns["MaxBenifitYearValue_increase"].ExtendedProperties["Width"] = (double)14;
                dataTable.Columns["MaxBenifitYearValue_increase"].ExtendedProperties["SubCaption1"] = "";
            }

            {
                dataTable.Columns.Add("SummaryPayrollYearValue", typeof(double)).Caption = "ТЕКУЩАЯ";
                dataTable.Columns["SummaryPayrollYearValue"].ExtendedProperties["Width"] = (double)11;
                dataTable.Columns["SummaryPayrollYearValue"].ExtendedProperties["SubCaption1"] = "СУММАРНАЯ ЗП СОТРУДНИКА ЗА ГОД";
                dataTable.Columns["SummaryPayrollYearValue"].ExtendedProperties["SubCaptionColumnSpan1"] = 4;

                dataTable.Columns.Add("SummaryPayrollYearValue_new", typeof(double)).Caption = "НОВАЯ";
                dataTable.Columns["SummaryPayrollYearValue_new"].ExtendedProperties["Width"] = (double)14;
                dataTable.Columns["SummaryPayrollYearValue_new"].ExtendedProperties["SubCaption1"] = "";

                dataTable.Columns.Add("SummaryPayrollYearValue_increase", typeof(double)).Caption = "ПОВЫШЕНИЕ";
                dataTable.Columns["SummaryPayrollYearValue_increase"].ExtendedProperties["Width"] = (double)14;
                dataTable.Columns["SummaryPayrollYearValue_increase"].ExtendedProperties["SubCaption1"] = "";

                dataTable.Columns.Add("SummaryPayrollYearValue_percent", typeof(double)).Caption = "%";
                dataTable.Columns["SummaryPayrollYearValue_percent"].ExtendedProperties["Width"] = (double)11;
                dataTable.Columns["SummaryPayrollYearValue_percent"].ExtendedProperties["SubCaption1"] = "";
            }

            dataTable.Columns.Add("PayrollChangeDate", typeof(DateTime)).Caption = "ДАТА ПОВЫШЕНИЯ";
            dataTable.Columns["PayrollChangeDate"].ExtendedProperties["Width"] = (double)14;

            dataTable.Columns.Add("UserComment", typeof(string)).Caption = "ОБОСНОВАНИЕ ПОВЫШЕНИЯ";
            dataTable.Columns["UserComment"].ExtendedProperties["Width"] = (double)30;

            dataTable.Columns.Add("IncreaseBudget", typeof(double)).Caption = "УВЕЛИЧЕНИЕ ФАКТИЧЕСКОГО БЮДЖЕТА ФОТ <ГОД>".Replace("<ГОД>", tmpEmployeePayrollRecord.PayrollChangeDate?.Year.ToString());
            dataTable.Columns["IncreaseBudget"].ExtendedProperties["Width"] = (double)14;

            dataTable.Columns.Add("IncreaseBenifit", typeof(double)).Caption = "УВЕЛИЧЕНИЕ ФАКТИЧЕСКОЙ МАКСИМАЛЬНО ВОЗМОЖНОЙ ГОДОВОЙ ПРЕМИИ <ГОД>".Replace("<ГОД>", tmpEmployeePayrollRecord.PayrollChangeDate?.Year.ToString());
            dataTable.Columns["IncreaseBenifit"].ExtendedProperties["Width"] = (double)14;

            // добавляем запись
            if (employeePayrollRecord != null)
            {
                var row = dataTable.NewRow();
                row["DepartmentName"] = employeePayrollRecord.Employee.Department?.ShortTitle;
                row["FullName"] = employeePayrollRecord.Employee.FullName;
                row["EmployeePosition"] = employeePayrollRecord.Employee.EmployeePositionTitle;
                {
                    row["PayrollValue"] = employeePayrollRecord.PayrollValue;
                    row["PayrollValue_new"] = tmpEmployeePayrollRecord.PayrollValue;
                }
                double? payrollValue_increase = tmpEmployeePayrollRecord.PayrollValue - employeePayrollRecord.PayrollValue;
                {
                    row["PayrollValue_increase"] = payrollValue_increase;
                    row["PayrollValue_increase_percent"] = payrollValue_increase * 100 / employeePayrollRecord.PayrollValue;
                }
                {
                    row["EmployeeGrad"] = employeePayrollRecord.EmployeeGrad;
                    row["EmployeeGrad_percent"] = oldGradPercent;
                    row["EmployeeGrad_new"] = tmpEmployeePayrollRecord.EmployeeGrad;
                    row["EmployeeGrad_new_percent"] = newGradPercent;
                }
                double? payrollYearValue = employeePayrollRecord.PayrollValue * 12;
                double? payrollYearValue_new = tmpEmployeePayrollRecord.PayrollValue * 12;
                {
                    row["PayrollYearValue"] = payrollYearValue;
                    row["PayrollYearValue_new"] = payrollYearValue_new;
                    row["PayrollYearValue_increase"] = payrollYearValue_new - payrollYearValue;
                }
                double? maxBenifitYearValue = employeePayrollRecord.PayrollValue * 12 * oldGradPercent / 100;
                double? maxBenifitYearValue_new = tmpEmployeePayrollRecord.PayrollValue * 12 * newGradPercent / 100;
                {
                    row["MaxBenifitYearValue"] = maxBenifitYearValue;
                    row["MaxBenifitYearValue_new"] = maxBenifitYearValue_new;
                    row["MaxBenifitYearValue_increase"] = maxBenifitYearValue_new - maxBenifitYearValue;
                }
                {
                    double? summaryPayrollYearValue = payrollYearValue + maxBenifitYearValue;
                    double? summaryPayrollYearValue_new = payrollYearValue_new + maxBenifitYearValue_new;
                    double? summaryPayrollYearValue_increase = summaryPayrollYearValue_new - summaryPayrollYearValue;
                    row["SummaryPayrollYearValue"] = summaryPayrollYearValue;
                    row["SummaryPayrollYearValue_new"] = summaryPayrollYearValue_new;
                    row["SummaryPayrollYearValue_increase"] = summaryPayrollYearValue_increase;
                    row["SummaryPayrollYearValue_percent"] = summaryPayrollYearValue_increase * 100 / summaryPayrollYearValue;
                }
                DateTime payrollChangeDate = tmpEmployeePayrollRecord.PayrollChangeDate.Value;
                row["PayrollChangeDate"] = payrollChangeDate.Date;
                row["UserComment"] = comment;
                row["IncreaseBudget"] = (12 - payrollChangeDate.Month + 1) * payrollValue_increase;
                row["IncreaseBenifit"] = (12 - payrollChangeDate.Month + 1) * ((tmpEmployeePayrollRecord.PayrollValue * newGradPercent) - (employeePayrollRecord.PayrollValue * oldGradPercent));
                dataTable.Rows.Add(row);
            }
            else
                dataTable.Rows.Add();

            byte[] binData = null;
            using (var stream = new MemoryStream())
            {
                using (var doc = SpreadsheetDocument.Create(stream, SpreadsheetDocumentType.Workbook))
                {
                    WorkbookPart workbookPart = ExcelHelper.CreateWorkbookPart(doc, "Заявка на повышение");

                    WorksheetPart rId1 = ExcelHelper.CreateWorksheetPartAndImportDataTable(workbookPart, "rId1", 1, 1, (uint)dataTable.Columns.Count,
                        "ЗАЯВКА НА ПОВЫШЕНИЕ ЗАРАБОТНОЙ ПЛАТЫ ИЛИ ГРЕЙДА", dataTable, 3, 1);

                    doc.WorkbookPart.Workbook.Save();
                }

                stream.Position = 0;
                var b = new BinaryReader(stream);
                binData = b.ReadBytes((int)stream.Length);
            }

            return binData;
        }

        public byte[] GetDepartmentPayrollChangeReport(IEnumerable<BudgetLimit> limits, IEnumerable<EmployeePayrollRecord> employeePayrollRecords, IEnumerable<EmployeePayrollRecord.EmployeeReqPayrollChange> tmpEmployeePayrollRecords, Department frcDepartment, int year)
        {
            var firstPage = InitFirstPageDPCReport(frcDepartment.ShortTitle, year);
            var months = Enumerable.Range(1, 12);
            // limits = limits.OrderBy(l => l.Month).AsEnumerable();

            var firstRow = new object[14];
            var secondRow = new object[14];
            var thirdRow = new object[14];
            firstRow[0] = "План";
            secondRow[0] = "Факт + Прогноз";
            thirdRow[0] = "";
            double sumPlan = 0;
            double sumFact = 0;
            double maxPlanFromMonth = 0;
            double maxFactFromMonth = 0;
            foreach (var month in months)
            {
                var limit = limits.FirstOrDefault(l => l.Month == month);
                var planValue = (double)(limit != null ? (limit.LimitAmount.HasValue ? limit.LimitAmount.Value : 0) : 0);
                maxPlanFromMonth = maxPlanFromMonth < planValue ? planValue : maxPlanFromMonth;
                firstRow[month] = planValue;

                double factValue = tmpEmployeePayrollRecords.Where(r => r.Record.PayrollChangeDate.Value.Month == month).Sum(r => r.EmployeePayrollChange);
                secondRow[month] = factValue;
                maxFactFromMonth = maxFactFromMonth < factValue ? factValue : maxFactFromMonth;

                double percent = planValue > 0 ? (factValue / planValue) * 100 : 0;
                thirdRow[month] = percent;

                sumPlan += planValue;
                sumFact += factValue;
            }
            firstRow[13] = sumPlan;
            secondRow[13] = sumFact;
            double total = sumPlan > 0 ? (sumFact / sumPlan) * 100 : 0;
            thirdRow[13] = total;

            firstPage.Rows.Add(firstRow);
            firstPage.Rows.Add(secondRow);
            firstPage.Rows.Add(thirdRow);

            var secondPage = InitSecondPageDPCReport();
            secondPage.Rows.Add("Максимальный рост ежемесячного ФОТ ЦФО за #".Replace("#", year.ToString()),
                maxPlanFromMonth,
                maxFactFromMonth,
                (maxPlanFromMonth > 0 ? (maxFactFromMonth / maxPlanFromMonth) * 100 : 0),
                (maxPlanFromMonth - maxFactFromMonth));

            secondPage.Rows.Add("Влияние повышений на годовой ФОТ ЦФО за #".Replace("#", year.ToString()),
                sumPlan,
                sumFact,
                (sumPlan > 0 ? (sumFact / sumPlan) * 100 : 0),
                sumPlan - sumFact);


            var thirdPage = InitThirdPageDPCReport(year);

            foreach (var tmpRecord in tmpEmployeePayrollRecords)
            {
                var record = employeePayrollRecords.FirstOrDefault(r => r.EmployeeID == tmpRecord.Record.EmployeeID);
                if (record != null /*&& record.PayrollValue.HasValue*/)
                {
                    var row = thirdPage.NewRow();
                    // double? increase = tmpRecord.Record.PayrollValue - record.PayrollValue;
                    row["ListEmployee"] = tmpRecord.Record.Employee.FullName;
                    row["Increase"] = tmpRecord.EmployeePayrollChange;
                    row["Month"] = tmpRecord.Record.PayrollChangeDate.Value.Date;
                    row["Influence"] = tmpRecord.EmployeePayrollChange * (12 - tmpRecord.Record.PayrollChangeDate.Value.Month + 1);
                    row["NumRequest"] = tmpRecord.Record.URegNum;
                    row["StatusRequest"] = tmpRecord.GetStatusDisplayName();
                    var dismissalDate = tmpRecord.Record.Employee.DismissalDate;
                    string statusEmployee = tmpRecord.Department.ID == frcDepartment.ID ? string.Empty : "Перешёл в другое ЦФО";
                    if (string.IsNullOrEmpty(statusEmployee) || dismissalDate.HasValue)
                        statusEmployee = dismissalDate.HasValue ? "Уволен " + dismissalDate.Value.ToString("dd.MM.yyyy") : "Работает";
                    row["StatusEmployee"] = statusEmployee;
                    row["Department"] = tmpRecord.Department.ID == frcDepartment.ID ? frcDepartment.ShortTitle : tmpRecord.Department.ShortTitle;
                    thirdPage.Rows.Add(row);
                }
            }

            byte[] binData = null;
            using (var stream = new MemoryStream())
            {
                using (var doc = SpreadsheetDocument.Create(stream, SpreadsheetDocumentType.Workbook))
                {
                    WorkbookPart workbookPart = ExcelHelper.CreateWorkbookPart(doc, "Отчёт");

                    WorksheetPart rId1 = ExcelHelper.CreateWorksheetPartAndImportDataTable(workbookPart, "rId1", 1, 1, "Расчет изменения ФОТ подразделения", new List<ExcelDataTable>() {
                        new ExcelDataTable(firstPage, 3, 1),
                        new ExcelDataTable(secondPage, (UInt32)firstPage.Rows.Count + 6U, 1),
                        new ExcelDataTable(thirdPage, (UInt32)firstPage.Rows.Count + (UInt32)secondPage.Rows.Count + 8U, 1),
                    });

                    //WorksheetPart rId1 = ExcelHelper.CreateWorksheetPartAndImportDataTable(workbookPart, "rId1", 1, 1, (uint)thirdPage.Columns.Count,
                    //    "Расчет изменения ФОТ подразделения", thirdPage, 3, 1);

                    doc.WorkbookPart.Workbook.Save();
                }

                stream.Position = 0;
                var b = new BinaryReader(stream);
                binData = b.ReadBytes((int)stream.Length);
            }

            return binData;
        }

        public DataTable InitFirstPageDPCReport(string deptName, int year)
        {
            var dt = new DataTable();
            dt.Columns.Add("DepartmentName", typeof(string)).Caption = deptName;
            dt.Columns["DepartmentName"].ExtendedProperties["Width"] = (double)7;

            dt.Columns.Add("January", typeof(double)).Caption = "янв." + year;
            dt.Columns["January"].ExtendedProperties["Width"] = (double)14;
            dt.Columns["January"].ExtendedProperties["SubCaption1"] = "БЮДЖЕТ ПОВЫШЕНИЙ";
            dt.Columns["January"].ExtendedProperties["SubCaptionColumnSpan1"] = 12;

            dt.Columns.Add("February", typeof(double)).Caption = "фев." + year;
            dt.Columns["February"].ExtendedProperties["Width"] = (double)14;
            dt.Columns["February"].ExtendedProperties["SubCaption1"] = "";

            dt.Columns.Add("March", typeof(double)).Caption = "мар." + year;
            dt.Columns["March"].ExtendedProperties["Width"] = (double)14;
            dt.Columns["March"].ExtendedProperties["SubCaption1"] = "";

            dt.Columns.Add("April", typeof(double)).Caption = "апр." + year;
            dt.Columns["April"].ExtendedProperties["Width"] = (double)14;
            dt.Columns["April"].ExtendedProperties["SubCaption1"] = "";

            dt.Columns.Add("May", typeof(double)).Caption = "май." + year;
            dt.Columns["May"].ExtendedProperties["Width"] = (double)14;
            dt.Columns["May"].ExtendedProperties["SubCaption1"] = "";

            dt.Columns.Add("June", typeof(double)).Caption = "июн." + year;
            dt.Columns["June"].ExtendedProperties["Width"] = (double)14;
            dt.Columns["June"].ExtendedProperties["SubCaption1"] = "";

            dt.Columns.Add("July", typeof(double)).Caption = "июл." + year;
            dt.Columns["July"].ExtendedProperties["Width"] = (double)14;
            dt.Columns["July"].ExtendedProperties["SubCaption1"] = "";

            dt.Columns.Add("August", typeof(double)).Caption = "авг." + year;
            dt.Columns["August"].ExtendedProperties["Width"] = (double)14;
            dt.Columns["August"].ExtendedProperties["SubCaption1"] = "";

            dt.Columns.Add("September", typeof(double)).Caption = "сен." + year;
            dt.Columns["September"].ExtendedProperties["Width"] = (double)14;
            dt.Columns["September"].ExtendedProperties["SubCaption1"] = "";

            dt.Columns.Add("October", typeof(double)).Caption = "окт." + year;
            dt.Columns["October"].ExtendedProperties["Width"] = (double)14;
            dt.Columns["October"].ExtendedProperties["SubCaption1"] = "";

            dt.Columns.Add("November", typeof(double)).Caption = "ноя." + year;
            dt.Columns["November"].ExtendedProperties["Width"] = (double)14;
            dt.Columns["November"].ExtendedProperties["SubCaption1"] = "";

            dt.Columns.Add("December", typeof(double)).Caption = "дек." + year;
            dt.Columns["December"].ExtendedProperties["Width"] = (double)14;
            dt.Columns["December"].ExtendedProperties["SubCaption1"] = "";

            dt.Columns.Add("Result", typeof(double)).Caption = "ИТОГО";
            dt.Columns["Result"].ExtendedProperties["Width"] = (double)14;
            dt.Columns["Result"].ExtendedProperties["SubCaption1"] = "";


            return dt;
        }

        public DataTable InitSecondPageDPCReport()
        {
            var dt = new DataTable();

            dt.Columns.Add("PlanIncrease", typeof(string)).Caption = "План повышения";
            dt.Columns["PlanIncrease"].ExtendedProperties["Width"] = (double)20;

            dt.Columns.Add("Plan", typeof(double)).Caption = "ПЛАН";
            dt.Columns["Plan"].ExtendedProperties["Width"] = (double)14;

            dt.Columns.Add("Fact", typeof(double)).Caption = "ФАКТ";
            dt.Columns["Fact"].ExtendedProperties["Width"] = (double)14;

            dt.Columns.Add("CurrentBudget", typeof(double)).Caption = "БЮДЖЕТ ВЫБРАН НА, %";
            dt.Columns["CurrentBudget"].ExtendedProperties["Width"] = (double)14;

            dt.Columns.Add("BalanceBudget", typeof(double)).Caption = "ОСТАТОК БЮДЖЕТА";
            dt.Columns["BalanceBudget"].ExtendedProperties["Width"] = (double)14;

            return dt;
        }

        public DataTable InitThirdPageDPCReport(int year)
        {
            var dt = new DataTable();

            dt.Columns.Add("ListEmployee", typeof(string)).Caption = "СПИСОК ПОВЫШЕНИЙ В ПОДРАЗДЕЛЕНИИ ЗА # ГОД".Replace("#", year.ToString());
            dt.Columns["ListEmployee"].ExtendedProperties["Width"] = (double)20;

            dt.Columns.Add("Increase", typeof(double)).Caption = "ПОВЫШЕНИЕ";
            dt.Columns["Increase"].ExtendedProperties["Width"] = (double)14;

            dt.Columns.Add("Month", typeof(DateTime)).Caption = "МЕСЯЦ";
            dt.Columns["Month"].ExtendedProperties["Width"] = (double)14;

            dt.Columns.Add("Influence", typeof(double)).Caption = "ВЛИЯНИЕ НА ГОДОВОЙ ФОТ";
            dt.Columns["Influence"].ExtendedProperties["Width"] = (double)14;

            dt.Columns.Add("NumRequest", typeof(string)).Caption = "№ ЗАЯВКИ";
            dt.Columns["NumRequest"].ExtendedProperties["Width"] = (double)14;

            dt.Columns.Add("StatusRequest", typeof(string)).Caption = "СТАТУС ЗАЯВКИ";
            dt.Columns["StatusRequest"].ExtendedProperties["Width"] = (double)20;

            dt.Columns.Add("StatusEmployee", typeof(string)).Caption = "СТАТУС СОТРУДНИКА";
            dt.Columns["StatusEmployee"].ExtendedProperties["Width"] = (double)14;

            dt.Columns.Add("Department", typeof(string)).Caption = "ЦФО";
            dt.Columns["Department"].ExtendedProperties["Width"] = (double)14;

            return dt;
        }

    }
}
