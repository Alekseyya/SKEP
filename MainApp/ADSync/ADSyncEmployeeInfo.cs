using System;
using Core.Helpers;
using Core.Models;

namespace MainApp.ADSync
{
    public class ADSyncEmployeeInfo
    {
        public string ADLogin { get; set; }
        public string ADEmployeeID { get; set; }
        public string Email { get; set; }
        public string FullName { get; set; }
        public DateTime? EnrollmentDate { get; set; }
        

        public string ADEmployeeTitleChangedInfo { get; set; }
        public string ADEmployeeDepartmentChangedInfo { get; set; }
        public string ADEmployeeManagerChangedInfo { get; set; }

        public string ADEmployeeOrganisationTitleChangedInfo { get; set; }
        public string ADEmployeeOfficeNameChangedInfo { get; set; }
        public string ADEmployeeWorkPhoneNumberChangedInfo { get; set; }
        public string ADEmployeePublicMobilePhoneNumberChangedInfo { get; set; }
        public string ADEmployeeEmployeeLocationTitleChangedInfo { get; set; }

        public string ADSyncStatus { get; set; } 

        public ADSyncEmployeeInfo(Employee employee,
            string adEmployeeTitleChangedInfo,
            string adEmployeeDepartmentChangedInfo,
            string adEmployeeManagerChangedInfo,
            string adEmployeeOrganisationTitleChangedInfo,
            string adEmployeeOfficeNameChangedInfo,
            string adEmployeeWorkPhoneNumberChangedInfo,
            string adEmployeePublicMobilePhoneNumberChangedInfo,
            string adEmployeeEmployeeLocationTitleChangedInfo,
            string adSyncStatus)
        {
            ADLogin = employee.ADLogin;
            ADEmployeeID = employee.ADEmployeeID;
            Email = employee.Email;
            FullName = employee.FullName;
            EnrollmentDate = employee.EnrollmentDate;

            ADEmployeeTitleChangedInfo = adEmployeeTitleChangedInfo;
            ADEmployeeDepartmentChangedInfo = adEmployeeDepartmentChangedInfo;
            ADEmployeeManagerChangedInfo = adEmployeeManagerChangedInfo;

            ADEmployeeOrganisationTitleChangedInfo = adEmployeeOrganisationTitleChangedInfo;
            ADEmployeeOfficeNameChangedInfo = adEmployeeOfficeNameChangedInfo;
            ADEmployeeWorkPhoneNumberChangedInfo  = adEmployeeWorkPhoneNumberChangedInfo;
            ADEmployeePublicMobilePhoneNumberChangedInfo = adEmployeePublicMobilePhoneNumberChangedInfo;
            ADEmployeeEmployeeLocationTitleChangedInfo = adEmployeeEmployeeLocationTitleChangedInfo;

            ADSyncStatus = adSyncStatus;
        }

        public ADSyncEmployeeInfo(Employee employee,
            string adSyncStatus)
        {
            ADLogin = employee.ADLogin;
            ADEmployeeID = employee.ADEmployeeID;
            Email = employee.Email;
            FullName = employee.FullName;
            EnrollmentDate = employee.EnrollmentDate;

            ADEmployeeTitleChangedInfo = employee.EmployeePositionTitle;
            ADEmployeeDepartmentChangedInfo = (employee.Department != null) ? employee.Department.FullName : "";
            ADEmployeeManagerChangedInfo = GetEmployeeManagerFullNameForAD(employee);

            ADEmployeeOrganisationTitleChangedInfo = (employee.Organisation != null) ? employee.Organisation.Title : "";
            ADEmployeeOfficeNameChangedInfo = employee.OfficeName;
            ADEmployeeWorkPhoneNumberChangedInfo = employee.WorkPhoneNumber;
            ADEmployeePublicMobilePhoneNumberChangedInfo = employee.PublicMobilePhoneNumber;
            ADEmployeeEmployeeLocationTitleChangedInfo = (employee.EmployeeLocation != null) ? employee.EmployeeLocation.Title : "";

            ADSyncStatus = adSyncStatus;
        }

        private string GetEmployeeManagerFullNameForAD(Employee employee)
        {
            string adEmployeeManagerFullName = "";

            if (employee.Department != null
                && employee.Department.DepartmentManager != null
                && employee.ID != employee.Department.DepartmentManagerID)
            {
                adEmployeeManagerFullName = employee.Department.DepartmentManager.FullName;
            }
            else if (employee.Department != null
                && employee.Department.DepartmentManager != null
                && employee.ID == employee.Department.DepartmentManagerID
                && employee.Department.ParentDepartment != null
                && employee.Department.ParentDepartment.DepartmentManager != null
                && employee.ID != employee.Department.ParentDepartment.DepartmentManagerID)
            {
                adEmployeeManagerFullName = employee.Department.ParentDepartment.DepartmentManager.FullName;
            }

            return adEmployeeManagerFullName;
        }

        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;

            ADSyncEmployeeInfo value = obj as ADSyncEmployeeInfo;
            if (value == null)
                return false;

            if (this.ADEmployeeID == value.ADEmployeeID && this.ADLogin == value.ADLogin && this.FullName == value.FullName)
                return true;

            return false;
        }

        public override int GetHashCode()
        {
            return ADLogin.GetHashCode() + ADEmployeeID.GetHashCode() + FullName.GetHashCode();
        }

        public string GenerateHtmlReportCell(string cellValue)
        {
            string htmlReportCell = "";

            if (String.IsNullOrEmpty(cellValue) == false
                && cellValue.Contains("->") == true)
            {
                htmlReportCell += @"<td bgcolor='lightgrey' class='summaryitemmain' style='text-wrap:normal;'>";
                htmlReportCell += cellValue;
                htmlReportCell += "</td>";
                htmlReportCell += "\r\n";
            }
            else
            {
                htmlReportCell += @"<td class='summaryitemmain' style='text-wrap:normal;'>";
                htmlReportCell += cellValue;
                htmlReportCell += "</td>";
                htmlReportCell += "\r\n";
            }

            return htmlReportCell;
        }

        public RPCSHtmlReport AddRowToHtmlReport(RPCSHtmlReport htmlReport)
        {
            htmlReport.AddReportRow(FullName,
                (EnrollmentDate.HasValue == true) ? EnrollmentDate.Value.ToShortDateString() : "",
                ADLogin,
                Email,
                ADEmployeeID,
                ADEmployeeTitleChangedInfo,
                ADEmployeeDepartmentChangedInfo,
                ADEmployeeManagerChangedInfo,
                ADEmployeeOrganisationTitleChangedInfo,
                ADEmployeeOfficeNameChangedInfo,
                ADEmployeeWorkPhoneNumberChangedInfo,
                ADEmployeePublicMobilePhoneNumberChangedInfo,
                ADEmployeeEmployeeLocationTitleChangedInfo,
                ADSyncStatus);

            return htmlReport;
        }
    }

  
}