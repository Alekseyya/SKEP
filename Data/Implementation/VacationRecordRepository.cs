using System.Collections.Generic;
using System.Linq;
using Core.Data;
using Core.Data.Interfaces;
using Core.Models;
using Core.RecordVersionHistory;
using Microsoft.EntityFrameworkCore;

namespace Data.Implementation
{
    public class VacationRecordRepository : RepositoryBase<VacationRecord, int>, IVacationRecordRepository
    {
        public VacationRecordRepository(DbContext dbContext) : base(dbContext)
        {
        }

        #region IVacationRecords

        public VacationRecord GetVersion(int vacationRecordId, int version)
        {
            VacationRecord vacationRecordVersion;
            using (var filterDisabler = new FilterDisabler(DbContext, "IsVersion"))
            {
                var data = GetQueryable().Where(p => p.ItemID == vacationRecordId);
                if (version == 0)
                    data = data.Where(p => p.VersionNumber == 0 || p.VersionNumber == null);
                else
                    data = data.Where(p => p.VersionNumber == version);
                vacationRecordVersion = data.SingleOrDefault();
            }

            return vacationRecordVersion;
        }

        public IList<VacationRecord> GetVersions(int vacationRecordId)
        {
            return GetVersions(vacationRecordId, false);
        }

        public IList<VacationRecord> GetVersions(int vacationRecordId, bool withChangeInfo)
        {
            List<VacationRecord> vacationRecordVersion;
            using (var filterDisabler = new FilterDisabler(DbContext, "IsVersion"))
            {
                vacationRecordVersion = GetQueryable()
                    .Where(p => p.ItemID == vacationRecordId || p.ID == vacationRecordId)
                    .OrderByDescending(p => p.VersionNumber)
                    .ToList();
            }
            if (withChangeInfo)
            {
                for (int i = 0; i < vacationRecordVersion.Count - 1; i++)
                {

                    var changes = ChangedRecordsFiller.GetChangedData(vacationRecordVersion[i], vacationRecordVersion[i + 1]);
                    vacationRecordVersion[i].ChangedRecords = changes;
                }
            }
            return vacationRecordVersion;
        }

        #endregion

        protected override object[] GetEntityKeyValues(int id)
        {
            return new object[] { id };
        }

        protected override VacationRecord CreateEntityWithId(int id)
        {
            return new VacationRecord { ID = id };
        }

        protected override bool CompareEntityId(VacationRecord entity, int id)
        {
            return (entity.ID == id);
        }

        
    }
}