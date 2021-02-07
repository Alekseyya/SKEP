using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Core.Models
{
    public class EmployeeProjectWorkload
    {
        public Employee Employee { get; set; }

        public List<EmployeeProjectWorkloadRecord> WorkloadRecords { get; }

        public EmployeeProjectWorkload()
        {
            WorkloadRecords = new List<EmployeeProjectWorkloadRecord>();
        }
    }
}