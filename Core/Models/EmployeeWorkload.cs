using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Core.Models
{
    public class EmployeeWorkload
    {
        public Employee Employee { get; set; }

        public List<EmployeeWorkloadRecord> WorkloadRecords { get; }

        public EmployeeWorkload()
        {
            WorkloadRecords = new List<EmployeeWorkloadRecord>();
        }
    }
}