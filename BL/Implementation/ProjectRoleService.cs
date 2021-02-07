using Core.BL;

using Core.BL.Interfaces;
using Core.Data;
using Core.Data.Interfaces;
using Core.Models;




namespace BL.Implementation
{
    public class ProjectRoleService : RepositoryAwareServiceBase<ProjectRole, int, IProjectRolesRepository>, IProjectRoleService
    {
        public ProjectRoleService(IRepositoryFactory repositoryFactory) : base(repositoryFactory)
        {
        }
    }
}
