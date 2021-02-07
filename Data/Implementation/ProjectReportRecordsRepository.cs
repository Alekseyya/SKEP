
//using System.Data.Entity;

using Core.Data;
using Core.Data.Interfaces;
using Core.Models;
using Microsoft.EntityFrameworkCore;




namespace Data.Implementation
{
    public class ProjectReportRecordsRepository : RepositoryBase<ProjectReportRecord, int>, IProjectReportRecordsRepository
    {
        public ProjectReportRecordsRepository(DbContext dbContext) : base(dbContext)
        { }

        protected override bool CompareEntityId(ProjectReportRecord entity, int id)
        {
            return (entity.ID == id);
        }

        protected override ProjectReportRecord CreateEntityWithId(int id)
        {
            return new ProjectReportRecord
            {
                ID = id
            };
        }

        protected override object[] GetEntityKeyValues(int id)
        {
            return new object[] { id };
        }
    }
}