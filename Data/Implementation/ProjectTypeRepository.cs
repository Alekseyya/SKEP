using System;
using System.Collections.Generic;
using System.Text;
using Core.Data;
using Core.Data.Interfaces;
using Core.Models;
using Microsoft.EntityFrameworkCore;




namespace Data.Implementation
{
    public class ProjectTypeRepository : RepositoryBase<ProjectType, int>, IProjectTypeRepository
    {
        public ProjectTypeRepository(DbContext dbContext) : base(dbContext)
        {
        }

        protected override object[] GetEntityKeyValues(int id)
        {
            return new object[] { id };
        }

        protected override ProjectType CreateEntityWithId(int id)
        {
            return new ProjectType { ID = id };
        }

        protected override bool CompareEntityId(ProjectType entity, int id)
        {
            return (entity.ID == id);
        }
    }
}
