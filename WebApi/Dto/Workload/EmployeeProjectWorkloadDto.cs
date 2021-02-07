using System.Collections.Generic;

namespace WebApi.Dto.Workload
{
    public class EmployeeProjectWorkloadDto
    {
        public BasicEmployeeDto Employee { get; set; }

        public ICollection<int> TotalWorkloadPercents { get; } = new List<int>();

        public ICollection<EmployeeProjectWorkloadRecordDto> ProjectWorkloads { get; } = new List<EmployeeProjectWorkloadRecordDto>();
    }
}