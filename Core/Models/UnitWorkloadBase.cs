using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Core.Models
{
    public abstract class UnitWorkloadBase
    {
        public DateTime DateFrom { get; set; }

        public DateTime DateTo { get; set; }

        public double TotalHours { get; set; }

        public List<EmployeeWorkload> EmployeeWorkloads { get; }

        public UnitWorkloadBase()
        {
            EmployeeWorkloads = new List<EmployeeWorkload>();
        }
    }
}