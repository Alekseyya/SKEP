using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace MainApp.Dto.Workload
{
    public class EmployeeProjectWorkloadRecordDto
    {
        public BasicProjectDto Project { get; set; }

        public ICollection<int> WorkloadPercents { get; } = new List<int>();
    }
}