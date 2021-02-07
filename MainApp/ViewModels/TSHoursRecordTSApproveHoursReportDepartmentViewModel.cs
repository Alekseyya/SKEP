using System.Collections.Generic;
using Core.Models;


namespace MainApp.ViewModels
{
    public class TSHoursRecordTSApproveHoursReportDepartmentViewModel
    {
        public Department Department { get; set; }
        public List<TSHoursRecordTSApproveHoursReportEmployeeViewModel> Employees { get; set; }
    }
}
