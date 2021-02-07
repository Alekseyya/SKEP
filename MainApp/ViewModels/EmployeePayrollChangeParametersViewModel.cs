namespace MainApp.ViewModels
{
    public class EmployeePayrollChangeParametersViewModel
    {
        public int? BitrixUserID { get; set; }
        public int? BitrixReqPayrollChangeID { get; set; }
        public int? RecordType { get; set; }
        public int? ActionModeForm { get; set; }
        public string BitrixUserLogin { get; set; }

        public bool? ForceEdit { get; set; }
        public bool? DisableReject { get; set; }
    }
}
