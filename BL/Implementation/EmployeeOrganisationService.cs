using Core.BL;
using Core.BL.Interfaces;
using Core.Data;
using Core.Data.Interfaces;
using Core.Models;

namespace BL.Implementation
{
    public class EmployeeOrganisationService : RepositoryAwareServiceBase<EmployeeOrganisation, int, IEmployeeOrganisationRepository>, IEmployeeOrganisationService
    {
        public EmployeeOrganisationService(IRepositoryFactory repositoryFactory) : base(repositoryFactory)
        {
        }
    }
}
