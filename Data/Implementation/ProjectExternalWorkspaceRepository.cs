using Core.Data;
using Core.Data.Interfaces;
using Core.Models;
using Microsoft.EntityFrameworkCore;




namespace Data.Implementation
{
    public class ProjectExternalWorkspaceRepository : RepositoryBase<ProjectExternalWorkspace, int>, IProjectExternalWorkspaceRepository
    {
        public ProjectExternalWorkspaceRepository(DbContext dbContext) : base(dbContext)
        {
        }

        protected override object[] GetEntityKeyValues(int id)
        {
            return new object[] { id };
        }

        protected override ProjectExternalWorkspace CreateEntityWithId(int id)
        {
            return new ProjectExternalWorkspace
            {
                ID = id
            };
        }

        protected override bool CompareEntityId(ProjectExternalWorkspace entity, int id)
        {
            return (entity.ID == id);
        }
    }
}
