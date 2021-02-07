using Core.BL;
using Core.BL.Interfaces;
using Core.Data;
using Core.Data.Interfaces;
using Core.Models;

namespace BL.Implementation
{
    public class EmployeeGradService : RepositoryAwareServiceBase<EmployeeGrad, int, IEmployeeGradRepository>, IEmployeeGradService
    {
        public EmployeeGradService(IRepositoryFactory repositoryFactory) : base(repositoryFactory)
        {}
    }
}
