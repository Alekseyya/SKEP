using System.Collections.Generic;
using Core;

namespace WebApi.Dto.Workload
{
    public class SubordinatesWorkloadDto
    {
        public int ManagerEmployeeId { get; set; }

        public ICollection<DateTimeRange> DateRanges { get; } = new List<DateTimeRange>();

        public ICollection<EmployeeProjectWorkloadDto> EmployeesWorkloads { get; } = new List<EmployeeProjectWorkloadDto>();
    }
}