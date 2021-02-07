using System.Collections.Generic;
using Core.Models;


namespace Core.Data.Interfaces
{
    public interface IVacationRecordRepository : IRepository<VacationRecord, int>
    {
        VacationRecord GetVersion(int vacationRecordId, int version);
        IList<VacationRecord> GetVersions(int vacationRecordId);
        IList<VacationRecord> GetVersions(int vacationRecordId, bool withChangeInfo);
    }
}
