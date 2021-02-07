//using System.Data.Entity;

using System.Collections.Generic;
using System.Linq;
using Core.Data;
using Core.Data.Interfaces;
using Core.Models;
using Core.RecordVersionHistory;
using Microsoft.EntityFrameworkCore;





namespace Data.Implementation
{
    public class ProjectStatusRecordRepository : RepositoryBase<ProjectStatusRecord, int>, IProjectStatusRecordRepository
    {
        public ProjectStatusRecordRepository(DbContext dbContext) : base(dbContext)
        { }

        protected override bool CompareEntityId(ProjectStatusRecord entity, int id)
        {
            return (entity.ID == id);
        }

        protected override ProjectStatusRecord CreateEntityWithId(int id)
        {
            return new ProjectStatusRecord
            {
                ID = id
            };
        }

        protected override object[] GetEntityKeyValues(int id)
        {
            return new object[] { id };
        }
        public ProjectStatusRecord GetVersion(int vacationRecordId, int version)
        {
            ProjectStatusRecord projectStatusRecordVersion;
            using (var filterDisabler = new FilterDisabler(DbContext, "IsVersion"))
            {
                var data = GetQueryable().Where(p => p.ItemID == vacationRecordId);
                if (version == 0)
                    data = data.Where(p => p.VersionNumber == 0 || p.VersionNumber == null);
                else
                    data = data.Where(p => p.VersionNumber == version);
                projectStatusRecordVersion = data.SingleOrDefault();
            }

            return projectStatusRecordVersion;
        }

        public IList<ProjectStatusRecord> GetVersions(int vacationRecordId)
        {
            return GetVersions(vacationRecordId, false);
        }

        public IList<ProjectStatusRecord> GetVersions(int vacationRecordId, bool withChangeInfo)
        {
            List<ProjectStatusRecord> projestStatusRecordVersion;
            using (var filterDisabler = new FilterDisabler(DbContext, "IsVersion"))
            {
                projestStatusRecordVersion = GetQueryable()
                    .Where(p => p.ItemID == vacationRecordId || p.ID == vacationRecordId)
                    .OrderByDescending(p => p.VersionNumber)
                    .ToList();
            }
            if (withChangeInfo)
            {
                for (int i = 0; i < projestStatusRecordVersion.Count - 1; i++)
                {

                    var changes = ChangedRecordsFiller.GetChangedData(projestStatusRecordVersion[i], projestStatusRecordVersion[i + 1]);
                    projestStatusRecordVersion[i].ChangedRecords = changes;
                }
            }
            return projestStatusRecordVersion;
        }
    }
}