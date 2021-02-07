using System;
using System.Collections.Generic;
using System.Linq;
using Core.Data;
using Core.Data.Interfaces;
using Core.Models;
using Core.RecordVersionHistory;
//using System.Data.Entity;
using Microsoft.EntityFrameworkCore;


namespace Data.Implementation
{
    public class ProjectRepository : RepositoryBase<Project, int>, IProjectRepository
    {
        public ProjectRepository(DbContext dbContext):base(dbContext)
        { }

        #region IProjectRepository implementation

        public virtual Project GetVersion(int projectId, int version)
        {
            Project projectVersion;
            using (var filterDisabler = new FilterDisabler(DbContext, "IsVersion"))
            {
                var data = GetQueryable().Where(p => p.ItemID == projectId);
                if (version == 0)
                    data = data.Where(p => p.VersionNumber == 0 || p.VersionNumber == null);
                else
                    data = data.Where(p => p.VersionNumber == version);
                projectVersion = data.SingleOrDefault();
            }
            return projectVersion;
        }

        public virtual IList<Project> GetVersions(int projectId)
        {
            return GetVersions(projectId, false);
        }

        public virtual IList<Project> GetVersions(int projectId, bool withChangeInfo)
        {
            List<Project> projectVersions;
            using (var filterDisabler = new FilterDisabler(DbContext, "IsVersion"))
            {
                projectVersions = GetQueryable().Where(p => p.ItemID == projectId || p.ID == projectId)
                    .OrderByDescending(p => p.VersionNumber)
                    .ToList();
            }
            if(withChangeInfo)
            {
                for (int i = 0; i < projectVersions.Count - 1; i++)
                {

                    var changes = ChangedRecordsFiller.GetChangedData(projectVersions[i], projectVersions[i + 1]);
                    projectVersions[i].ChangedRecords = changes;
                }
            }
            return projectVersions;
        }

        public Project LoadReferences(Project project)
        {
            // TODO: Выяснить, почему связи в Project IEnumerable а не ICollection и реализовать через Entry(project).Collection(p => p.ReportRecords).Load();
            throw new NotImplementedException();
        }

        #endregion

        #region Internal implementation

        protected override bool CompareEntityId(Project entity, int id)
        {
            return (entity.ID == id);
        }

        protected override Project CreateEntityWithId(int id)
        {
            return new Project { ID = id };
        }

        protected override object[] GetEntityKeyValues(int id)
        {
            return new object[] { id };
        }

        #endregion
    }
}