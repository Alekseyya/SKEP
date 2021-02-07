using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Core.Models
{
    public class EmployeeProjectWorkloadRecord
    {
        public Project Project { get; set; }

        public List<EmployeeWorkloadRecord> WorkloadRecords { get; }

        public EmployeeProjectWorkloadRecord()
        {
            WorkloadRecords = new List<EmployeeWorkloadRecord>();
        }
    }
}