using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Core.Models
{
    public class SubordinatesWorkload
    {
        public int EmployeeId { get; }

        public List<EmployeeProjectWorkload> SubordinateEmployeesWorkloads { get; } = new List<EmployeeProjectWorkload>();
    }
}