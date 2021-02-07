

using Core.Models;

namespace Core.BL.Interfaces
{
    public interface IProjectStatusRecordEntryService : IServiceBase<ProjectStatusRecordEntry, int>
    {
        int GetCount();
    }
}
