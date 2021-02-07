using System;
using System.Collections.Generic;
using System.Linq;
using Core.Data;
using Core.Data.Interfaces;
using Core.Models;
using Core.RecordVersionHistory;
using Microsoft.EntityFrameworkCore;




namespace Data.Implementation
{
    public class TSAutoHoursRecordRepository : RepositoryBase<TSAutoHoursRecord, int>, ITSAutoHoursRecordRepository
    {
        public TSAutoHoursRecordRepository(DbContext dbContext) : base(dbContext)
        {
        }

        #region ITSAutoHoursRecord

        public TSAutoHoursRecord GetVersion(int tsAutoHoursRecordId, int version)
        {
            TSAutoHoursRecord tsAutoHoursRecordVersion;
            using (var filterDisabler = new FilterDisabler(DbContext, "IsVersion"))
            {
                var data = GetQueryable().Where(p => p.ItemID == tsAutoHoursRecordId);
                if (version == 0)
                    data = data.Where(p => p.VersionNumber == 0 || p.VersionNumber == null);
                else
                    data = data.Where(p => p.VersionNumber == version);
                tsAutoHoursRecordVersion = data.SingleOrDefault();
            }

            return tsAutoHoursRecordVersion;
        }

        public IList<TSAutoHoursRecord> GetVersions(int tsAutoHoursRecordId)
        {
            return GetVersions(tsAutoHoursRecordId, false);
        }

        public IList<TSAutoHoursRecord> GetVersions(int tsAutoHoursRecordId, bool withChangeInfo)
        {
            List<TSAutoHoursRecord> tsAutoHoursRecordVersion;
            using (var filterDisabler = new FilterDisabler(DbContext, "IsVersion"))
            {
                tsAutoHoursRecordVersion = GetQueryable().Where(p => p.ItemID == tsAutoHoursRecordId || p.ID == tsAutoHoursRecordId)
                    .OrderByDescending(p => p.VersionNumber)
                    .ToList();
            }
            if (withChangeInfo)
            {
                for (int i = 0; i < tsAutoHoursRecordVersion.Count - 1; i++)
                {

                    var changes = ChangedRecordsFiller.GetChangedData(tsAutoHoursRecordVersion[i], tsAutoHoursRecordVersion[i + 1]);
                    tsAutoHoursRecordVersion[i].ChangedRecords = changes;
                }
            }
            return tsAutoHoursRecordVersion;
        }

        #endregion

        protected override object[] GetEntityKeyValues(int id)
        {
            return new object[] { id };
        }

        protected override TSAutoHoursRecord CreateEntityWithId(int id)
        {
            return new TSAutoHoursRecord { ID = id };
        }

        protected override bool CompareEntityId(TSAutoHoursRecord entity, int id)
        {
            return (entity.ID == id);
        }

        
    }
}