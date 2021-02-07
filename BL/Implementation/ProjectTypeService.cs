using Core.BL;
using Core.BL.Interfaces;
using Core.Data;
using Core.Data.Interfaces;
using Core.Models;



namespace BL.Implementation
{
    public class ProjectTypeService : RepositoryAwareServiceBase<ProjectType, int, IProjectTypeRepository>, IProjectTypeService
    {
        public ProjectTypeService(IRepositoryFactory repositoryFactory) : base(repositoryFactory)
        {
        }
    }
}
