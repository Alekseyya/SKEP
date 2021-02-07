using Core.BL;
using Core.BL.Interfaces;
using Core.Data;
using Core.Data.Interfaces;
using Core.Models;



namespace BL.Implementation
{
    public class OrganisationService : RepositoryAwareServiceBase<Organisation, int, IOrganisationRepository>, IOrganisationService
    {
        public OrganisationService(IRepositoryFactory repositoryFactory) : base(repositoryFactory)
        {
        }
    }
}
