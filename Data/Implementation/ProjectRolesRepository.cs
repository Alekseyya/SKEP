//using System.Data.Entity;

using Core.Data;
using Core.Data.Interfaces;
using Core.Models;
using Microsoft.EntityFrameworkCore;





namespace Data.Implementation
{
    public class ProjectRolesRepository : RepositoryBase<ProjectRole, int>, IProjectRolesRepository
    {
        public ProjectRolesRepository(DbContext dbContext) : base(dbContext)
        { }

        protected override bool CompareEntityId(ProjectRole entity, int id)
        {
            return (entity.ID == id);
        }

        protected override ProjectRole CreateEntityWithId(int id)
        {
            return new ProjectRole
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