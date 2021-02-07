

using Core.Models;

namespace Core.BL.Interfaces
{
    public interface IProjectScheduleEntryTypeService : IServiceBase<ProjectScheduleEntryType, int>
    {
        int GetCount();
        ProjectScheduleEntryType UpdateWithoutVersion(ProjectScheduleEntryType projectScheduleEntryType);
    }
}
