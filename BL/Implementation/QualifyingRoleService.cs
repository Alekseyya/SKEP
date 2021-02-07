using Core.BL;

using Core.BL.Interfaces;
using Core.Data;
using Core.Data.Interfaces;
using Core.Models;




namespace BL.Implementation
{
    public class QualifyingRoleService : RepositoryAwareServiceBase<QualifyingRole, int, IQualifyingRoleRepository>, IQualifyingRoleService
    {
        public QualifyingRoleService(IRepositoryFactory repositoryFactory) : base(repositoryFactory)
        {
        }
    }
}
