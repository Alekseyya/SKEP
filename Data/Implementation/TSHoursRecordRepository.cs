using System.Collections.Generic;
using System.Linq;
using Core.Data;
using Core.Data.Interfaces;
using Core.Models;
using Core.RecordVersionHistory;
using Microsoft.EntityFrameworkCore;

namespace Data.Implementation
{
    public class TSHoursRecordRepository : RepositoryBase<TSHoursRecord, int>, ITSHoursRecordRepository
    {
        public TSHoursRecordRepository(DbContext dbContext) : base(dbContext)
        {}

        #region ITSRecordsRepository implements
        public virtual TSHoursRecord GetVersion(int tsHoursRecordId, int version)
        {
            TSHoursRecord tsHoursRecordVersion;
            using (var filterDisabler = new FilterDisabler(DbContext, "IsVersion"))
            {
                var data = GetQueryable().Where(p => p.ItemID == tsHoursRecordId);
                if (version == 0)
                    data = data.Where(p => p.VersionNumber == 0 || p.VersionNumber == null);
                else
                    data = data.Where(p => p.VersionNumber == version);
                tsHoursRecordVersion = data.SingleOrDefault();
            }

           return tsHoursRecordVersion;
        }

        public virtual IList<TSHoursRecord> GetVersions(int tsHoursRecordId)
        {
            return GetVersions(tsHoursRecordId, false);
        }

        public virtual IList<TSHoursRecord> GetVersions(int tsHoursRecordId, bool withChangeInfo)
        {
            List<TSHoursRecord> tsHoursRecordVersion;
            using (var filterDisabler = new FilterDisabler(DbContext, "IsVersion"))
            {
                tsHoursRecordVersion = GetQueryable().Where(p => p.ItemID == tsHoursRecordId || p.ID == tsHoursRecordId)
                    .OrderByDescending(p => p.VersionNumber)
                    .ToList();
            }
            if (withChangeInfo)
            {
                for (int i = 0; i < tsHoursRecordVersion.Count - 1; i++)
                {

                    var changes = ChangedRecordsFiller.GetChangedData(tsHoursRecordVersion[i], tsHoursRecordVersion[i + 1]);
                    tsHoursRecordVersion[i].ChangedRecords = changes;
                }
            }
            return tsHoursRecordVersion;
        }

        #endregion

        #region Internal implementation
        protected override object[] GetEntityKeyValues(int id)
        {
            return new object[] { id };
        }

        protected override TSHoursRecord CreateEntityWithId(int id)
        {
            return new TSHoursRecord { ID = id };
        }

        protected override bool CompareEntityId(TSHoursRecord entity, int id)
        {
            return (entity.ID == id);
        }

        
        #endregion
    }
}