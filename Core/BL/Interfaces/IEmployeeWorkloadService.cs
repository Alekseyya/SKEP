

using Core.Models;

namespace Core.BL.Interfaces
{
    public enum WorkloadPeriod
    {
        Week,
        Month
    }

    public interface IEmployeeWorkloadService
    {
        ProjectWorkload GetProjectWorkload(int projectId, DateTimeRange dateRange);

        DepartmentWorkload GetDepartmentWorkload(int departmentId, DateTimeRange dateRange, WorkloadPeriod workloadPeriodStep);

        SubordinatesWorkload GetPMSubordinatesWorkload(int projectManagerId, DateTimeRange dateRange, WorkloadPeriod workloadPeriodStep);
    }
}