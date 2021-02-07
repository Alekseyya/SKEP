using System.Collections.Generic;

namespace WebApi.Dto.Workload
{
    public class EmployeeProjectWorkloadRecordDto
    {
        public BasicProjectDto Project { get; set; }

        public ICollection<int> WorkloadPercents { get; } = new List<int>();
    }
}