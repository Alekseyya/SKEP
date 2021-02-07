//using System.Data.Entity;

using Core.Data;
using Core.Data.Interfaces;
using Core.Models;
using Microsoft.EntityFrameworkCore;


namespace Data.Implementation
{
    public class ProjectMembersRepository : RepositoryBase<ProjectMember, int>, IProjectMembersRepository
    {
        public ProjectMembersRepository(DbContext dbContext):base(dbContext)
        { }

        protected override bool CompareEntityId(ProjectMember entity, int id)
        {
            return (entity.ID == id);
        }

        protected override ProjectMember CreateEntityWithId(int id)
        {
            return new ProjectMember { ID = id };
        }

        protected override object[] GetEntityKeyValues(int id)
        {
            return new object[] { id };
        }
    }
}