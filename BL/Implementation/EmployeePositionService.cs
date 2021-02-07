using Core.BL;

using Core.BL.Interfaces;
using Core.Data;
using Core.Data.Interfaces;
using Core.Models;




namespace BL.Implementation
{
    public class EmployeePositionService : RepositoryAwareServiceBase<EmployeePosition, int, IEmployeePositionRepository>, IEmployeePositionService
    {
        public EmployeePositionService(IRepositoryFactory repositoryFactory) : base(repositoryFactory)
        { }
    }
}
