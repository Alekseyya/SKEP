using System;
using System.Collections.Generic;
using Core;

namespace WebApi.Dto.Workload
{
    public class ProjectWorkloadDto
    {
        public int ProjectId { get; set; }

        public DateTime? ProjectStartDate { get; set; }

        public DateTime? ProjectEndDate { get; set; }

        public int? TotalPossibleHours { get; set; }

        public DateTime PeriodStartDate { get; set; }

        public DateTime PeriodEndDate { get; set; }

        public int PeriodPossibleHours { get; set; }

        public ICollection<DateTimeRange> DateRanges { get; }

        public ICollection<ProjectEmployeeWorkloadDto> EmployeesWorkloads { get; }

        public ProjectWorkloadDto()
        {
            DateRanges = new List<DateTimeRange>();
            EmployeesWorkloads = new List<ProjectEmployeeWorkloadDto>();
        }
    }
}