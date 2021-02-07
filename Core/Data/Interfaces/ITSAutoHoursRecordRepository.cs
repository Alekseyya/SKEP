using System.Collections.Generic;
using Core.Models;


namespace Core.Data.Interfaces
{
    public interface ITSAutoHoursRecordRepository : IRepository<TSAutoHoursRecord, int>
    {
        TSAutoHoursRecord GetVersion(int tsAutoHoursRecordId, int version);
        IList<TSAutoHoursRecord> GetVersions(int tsAutoHoursRecordId);
        IList<TSAutoHoursRecord> GetVersions(int tsAutoHoursRecordId, bool withChangeInfo);
    }
}