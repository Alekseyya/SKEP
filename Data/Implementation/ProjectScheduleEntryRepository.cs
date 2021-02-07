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
    public class ProjectScheduleEntryRepository : RepositoryBase<ProjectScheduleEntry, int>, IProjectScheduleEntryRepository
    {
        public ProjectScheduleEntryRepository(DbContext dbContext) : base(dbContext) { }

        protected override bool CompareEntityId(ProjectScheduleEntry entity, int id)
        {
            return (entity.ID == id);
        }

        protected override ProjectScheduleEntry CreateEntityWithId(int id)
        {
            throw new NotImplementedException();
        }

        protected override object[] GetEntityKeyValues(int id)
        {
            return new object[] { id };
        }

        public ProjectScheduleEntry GetVersion(int projectScheduleEntryId, int version)
        {
            ProjectScheduleEntry projectScheduleEntryVersion;
            using (var filterDisabler = new FilterDisabler(DbContext, "IsVersion"))
            {
                var data = GetQueryable().Where(p => p.ItemID == projectScheduleEntryId);
                if (version == 0)
                    data = data.Where(p => p.VersionNumber == 0 || p.VersionNumber == null);
                else
                    data = data.Where(p => p.VersionNumber == version);
                projectScheduleEntryVersion = data.SingleOrDefault();
            }

            return projectScheduleEntryVersion;
        }

        public IList<ProjectScheduleEntry> GetVersions(int projectScheduleEntryId)
        {
            return GetVersions(projectScheduleEntryId, false);
        }

        public IList<ProjectScheduleEntry> GetVersions(int projectScheduleEntryId, bool withChangeInfo)
        {
            List<ProjectScheduleEntry> projectScheduleEntryVersion;
            using (var filterDisabler = new FilterDisabler(DbContext, "IsVersion"))
            {
                projectScheduleEntryVersion = GetQueryable().Where(p => p.ItemID == projectScheduleEntryId || p.ID == projectScheduleEntryId)
                    .OrderByDescending(p => p.VersionNumber).ToList();
            }
            if (withChangeInfo)
            {
                for (int i = 0; i < projectScheduleEntryVersion.Count - 1; i++)
                {

                    var changes = ChangedRecordsFiller.GetChangedData(projectScheduleEntryVersion[i], projectScheduleEntryVersion[i + 1]);
                    projectScheduleEntryVersion[i].ChangedRecords = changes;
                }
            }
            return projectScheduleEntryVersion;
        }
    }
}
