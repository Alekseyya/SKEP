using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace MainApp.Dto.Workload
{
    public class EmployeeProjectWorkloadDto
    {
        public BasicEmployeeDto Employee { get; set; }

        public ICollection<int> TotalWorkloadPercents { get; } = new List<int>();

        public ICollection<EmployeeProjectWorkloadRecordDto> ProjectWorkloads { get; } = new List<EmployeeProjectWorkloadRecordDto>();
    }
}