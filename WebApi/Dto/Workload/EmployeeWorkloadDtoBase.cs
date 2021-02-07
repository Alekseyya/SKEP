using System.Collections.Generic;

namespace WebApi.Dto.Workload
{
    public abstract class EmployeeWorkloadDtoBase<TWorkload>
    {
        public int EmployeeId { get; set; }

        public string EmployeeFullName { get; set; }

        public ICollection<TWorkload> Workloads { get; }

        protected EmployeeWorkloadDtoBase()
        {
            Workloads = new List<TWorkload>();
        }
    }
}