using System.ComponentModel.DataAnnotations;
using Core.Models;


namespace MainApp.ViewModels
{
    public class TSHoursRecordTSCompletenessReportEmployeeViewModel
    {
        public int ID { get; set; }
        public string LastName { get; set; }
        public string FirstName { get; set; }
        public string MidName { get; set; }
        public string EmployeePositionTitle { get; set; }
        public string EmployeeCategory { get; set; }
        public int DepartmentID { get; set; }
        public string DepartmentShortName { get; set; }
        public string DepartmentTitle { get; set; }
        public bool DepartmentIsFinancialCentre { get; set; }
        public bool IsAutonomousDepartment { get; set; }
        public int PlanHours { get; set; }
        public EmployeeCategoryType? CategoryType { get; set; }

        [DisplayFormat(DataFormatString = "{0:0.##}")]
        public double EnteredHours { get; set; }

        [DisplayFormat(DataFormatString = "{0:0.##}")]
        public double ApprovedHours { get; set; }

        [DisplayFormat(DataFormatString = "{0:0.##}")]
        public double DeclinedHours { get; set; }

        [DisplayFormat(DataFormatString = "{0:0.##}")]
        public double OverHours => ((PlanHours < ApprovedHours) ? ApprovedHours - PlanHours : 0);

        [DisplayFormat(DataFormatString = "{0:0.##}")]
        public double UnderHours => ((PlanHours > ApprovedHours) ? PlanHours - ApprovedHours : 0);

        public string FullName => ((LastName != null) ? LastName.Trim() + " " : "") + ((FirstName != null) ? FirstName.Trim() + " " : "") + ((MidName != null) ? MidName.Trim() : "");
    }
}
