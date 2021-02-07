using Core.BL;

using Core.BL.Interfaces;
using Core.Data;
using Core.Data.Interfaces;
using Core.Models;




namespace BL.Implementation
{
   public class QualifyingRoleRateService : RepositoryAwareServiceBase<QualifyingRoleRate, int, IQualifyingRoleRateRepository>, IQualifyingRoleRateService
    {
        public QualifyingRoleRateService(IRepositoryFactory repositoryFactory) : base(repositoryFactory)
        {
        }
    }
}
