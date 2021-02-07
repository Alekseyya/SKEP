using System;


namespace MainApp.Dto
{
    public class ProjectReportRecordDto
    {
        public int ID { get; set; }
        public string ReportPeriodName { get; set; }

        public int? ProjectID { get; set; }
        public virtual BasicProjectDto Project { get; set; }

        public int? EmployeeID { get; set; }
        public virtual BasicEmployeeDto Employee { get; set; }

        public DateTime? CalcDate { get; set; }

        public string Comments { get; set; }

        public double? Hours { get; set; }

        public int? EmployeeCount { get; set; }

        public decimal? EmployeePayroll { get; set; }

        public decimal? EmployeeOvertimePayroll { get; set; }

        public decimal? EmployeePerformanceBonus { get; set; }

        public decimal? OtherCosts { get; set; }

        public decimal? TotalCosts { get; set; }

        public string RecordSortKey
        {
            get
            {
                return ((ReportPeriodName != null && ReportPeriodName.Contains(".") == true) ? (ReportPeriodName.Split('.')[1] + "_" + ReportPeriodName.Split('.')[0]) : ReportPeriodName);
            }
        }

    }
}