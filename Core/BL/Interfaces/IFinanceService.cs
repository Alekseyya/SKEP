using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using Core.Models;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;


namespace Core.BL.Interfaces
{
    public interface IFinanceService
    {
        bool IsEmployeeEqualsDataRow(Employee employee, DataRow dataRow);
        void ClearRecord(DataTable employeePayrollSheetDataTable, int rowId, bool tempCPFile);
        void UpdateEmployeePayrollTableRecords(DataTable employeePayrollSheetDataTable,
            string currentUserLogin, Employee currentEmployee, Employee employee, EmployeePayrollRecord record,
            bool tempCPFile, int rowId = -1);
        List<EmployeePayrollRecord> GetEmployeePayrollRecordsFromDataTable(DataTable employeePayrollSheetDataTable, Employee employee);
        List<EmployeePayrollRecord> GetFullListEmployeePayrollRecordsFromDataTable(DataTable employeePayrollSheetDataTable, IEnumerable<Employee> employeeList);
        double GetEmployeePayrollOnDate(DataTable employeePayrollSheetDataTable, Employee employee, DateTime date, bool useEmployeeCategory,
            IEnumerable<EmployeeCategory> employeeCategories);
        DateTime GetEmployeePayrollLastChangeDateOnDate(DataTable employeePayrollSheetDataTable, Employee employee, DateTime date);
        double GetProjectEmployeeOvertimePayrollDeltaValueForEmployee(DataTable projectsOtherCostsSheetDataTable, DataTable employeePayrollSheetDataTable, string projectShortName,
            Employee employee,
            double employeeMonthOverHours,
            int monthWorkHours,
            DateTime periodStart, DateTime periodEnd);
        double GetProjectEmployeePerformanceBonusValueForEmployee(DataTable projectsOtherCostsSheetDataTable, string projectShortName, Employee employee, DateTime periodStart, DateTime periodEnd);

        double GetProjectOtherCostsValueForEmployee(DataTable projectsOtherCostsSheetDataTable, string projectShortName, Employee employee, DateTime periodStart, DateTime periodEnd);
        DataTable GetEmployeePayrollSheetDataTableFromOO(ApplicationUser user, bool tempCPFile);
        bool PutEmployeePayrollSheetDataTableToOO(ApplicationUser user, DataTable employeePayrollSheetDataTable, bool tempCPFile);
        string GetOODefalutCPFileUrl();
        string GetOOTempCPFileUrl();
        string GetProjectEmployeePayrollCostsCalcMethod();
        string GetQualifyingRoleRateFRCCalcParamRecordListJson();
        void SetQualifyingRoleRateFRCCalcParamRecordListJson(string value);
        string GetQualifyingRoleRateHoursPlanCalcParam();
        void SetQualifyingRoleRateHoursPlanCalcParam(string value);
        bool IsCPFileAllowHistoryColumns();
        byte[] GetEmployeePayrollChangeReport(EmployeePayrollRecord employeePayrollRecord, EmployeePayrollRecord tmpEmployeePayrollRecord, double oldGradPercent, double newGradPercent,
            string comment);

        byte[] GetDepartmentPayrollChangeReport(IEnumerable<BudgetLimit> limits, IEnumerable<EmployeePayrollRecord> employeePayrollRecords,
            IEnumerable<EmployeePayrollRecord.EmployeeReqPayrollChange> tmpEmployeePayrollRecords, Department frcDepartment, int year);
        DataTable InitFirstPageDPCReport(string deptName, int year);
        DataTable InitSecondPageDPCReport();
        DataTable InitThirdPageDPCReport(int year);

    }
}