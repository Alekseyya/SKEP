namespace MainApp.ViewModels
{
    public class TSHoursRecordTSApproveHoursReportEmployeeViewModel
    {
        public int ID { get; set; }
        public string FullName { get; set; }
        public int ProjectId { get; set; }
        public string ProjectShortName { get; set; }
        public int DepartmentID { get; set; }
        public string DepartmentShortName { get; set; }
        public string DepartmentTitle { get; set; }
        public bool DepartmentIsFinancialCentre { get; set; }
        public double ApprovingHours { get; set; }
    }
}
