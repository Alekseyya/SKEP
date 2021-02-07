using Core.BL;

using Core.BL.Interfaces;
using Core.Data;
using Core.Data.Interfaces;
using Core.Models;




namespace BL.Implementation
{
    public class ProjectStatusRecordEntryService : RepositoryAwareServiceBase<ProjectStatusRecordEntry, int, IProjectStatusRecordEntryRepository>, IProjectStatusRecordEntryService
    {
        public ProjectStatusRecordEntryService(IRepositoryFactory repositoryFactory) : base(repositoryFactory)
        {
        }
        public int GetCount()
        {
            return RepositoryFactory.GetRepository<IProjectStatusRecordEntryRepository>().GetCount();
        }
    }
}
