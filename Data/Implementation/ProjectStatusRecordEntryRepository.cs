using Core.Data;
using Core.Data.Interfaces;
using Core.Models;
using Microsoft.EntityFrameworkCore;




namespace Data.Implementation
{
    public class ProjectStatusRecordEntryRepository : RepositoryBase<ProjectStatusRecordEntry, int>, IProjectStatusRecordEntryRepository
    {
        public ProjectStatusRecordEntryRepository(DbContext dbContext) : base(dbContext)
        {
        }

        protected override object[] GetEntityKeyValues(int id)
        {
            return new object[] { id };
        }

        protected override ProjectStatusRecordEntry CreateEntityWithId(int id)
        {
            return new ProjectStatusRecordEntry { ID = id };
        }

        protected override bool CompareEntityId(ProjectStatusRecordEntry entity, int id)
        {
            return (entity.ID == id);
        }
    }
}
