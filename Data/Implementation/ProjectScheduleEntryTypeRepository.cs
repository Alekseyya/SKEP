using System;
using Core.Data;
using Core.Data.Interfaces;
using Core.Models;
using Microsoft.EntityFrameworkCore;




namespace Data.Implementation
{
    public class ProjectScheduleEntryTypeRepository : RepositoryBase<ProjectScheduleEntryType, int>, IProjectScheduleEntryTypeRepository
    {
        public ProjectScheduleEntryTypeRepository(DbContext dbContext) : base(dbContext) { }

        protected override bool CompareEntityId(ProjectScheduleEntryType entity, int id)
        {
            return (entity.ID == id);
        }

        protected override ProjectScheduleEntryType CreateEntityWithId(int id)
        {
            throw new NotImplementedException();
        }

        protected override object[] GetEntityKeyValues(int id)
        {
            return new object[] { id };
        }
    }
}
