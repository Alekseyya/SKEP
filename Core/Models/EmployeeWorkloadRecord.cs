using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Core.Models
{
    public class EmployeeWorkloadRecord
    {
        public DateTime DateFrom { get; set; }

        public DateTime DateTo { get; set; }

        public double CurrentProjectPercents { get; set; }

        public double TotalPercents { get; set; }

        public double CurrentProjectHours { get; set; }
    }
}