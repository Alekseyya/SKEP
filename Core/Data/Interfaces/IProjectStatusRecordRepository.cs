using System.Collections.Generic;
using Core.Models;


namespace Core.Data.Interfaces
{
    public interface IProjectStatusRecordRepository : IRepository<ProjectStatusRecord, int>
    {
        ProjectStatusRecord GetVersion(int vacationRecordId, int version);
        IList<ProjectStatusRecord> GetVersions(int vacationRecordId);
        IList<ProjectStatusRecord> GetVersions(int vacationRecordId, bool withChangeInfo);
    }
}
