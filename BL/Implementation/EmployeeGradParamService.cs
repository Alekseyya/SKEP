using Core.BL;

using Core.BL.Interfaces;
using Core.Data;
using Core.Data.Interfaces;
using Core.Models;


namespace BL.Implementation
{
    public class EmployeeGradParamService : RepositoryAwareServiceBase<EmployeeGradParam, int, IEmployeeGradParamRepository>, IEmployeeGradParamService
    {
        public EmployeeGradParamService(IRepositoryFactory repositoryFactory) : base(repositoryFactory)
        {
        }
    }
}
