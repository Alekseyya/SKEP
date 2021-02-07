using System.Collections.Generic;
using Core.Models;


namespace MainApp.ViewModels
{
    public class TSHoursRecordTSCompletenessReportDepartmentViewModel
    {
        public Department Department { get; set; }
        public List<TSHoursRecordTSCompletenessReportEmployeeViewModel> Employees { get; set; }
    }
}
