
using System.Collections.Generic;
using Core.Models;


namespace Core.Data.Interfaces
{
   public interface ITSHoursRecordRepository : IRepository<TSHoursRecord, int>
   {
       TSHoursRecord GetVersion(int tsHoursRecordId, int version);
       IList<TSHoursRecord> GetVersions(int tsHoursRecordId);
       IList<TSHoursRecord> GetVersions(int tsHoursRecordId, bool withChangeInfo);
   }
}
