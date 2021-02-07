using System;
using System.Collections.Generic;
using Core;

namespace WebApi.Dto.Workload
{
    public class DepartmentWorkloadDto
    {
        public int DepartmentId { get; set; }

        public DateTime PeriodStartDate { get; set; }

        public DateTime PeriodEndDate { get; set; }

        public ICollection<DateTimeRange> DateRanges { get; }

        public ICollection<CommonEmployeeWorkloadDto> EmployeesWorkloads { get; }

        public DepartmentWorkloadDto()
        {
            DateRanges = new List<DateTimeRange>();
            EmployeesWorkloads = new List<CommonEmployeeWorkloadDto>();
        }
    }
}