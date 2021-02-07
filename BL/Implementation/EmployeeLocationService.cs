using Core.BL;
using Core.BL.Interfaces;
using Core.Data;
using Core.Data.Interfaces;
using Core.Models;

namespace BL.Implementation
{
    public class EmployeeLocationService : RepositoryAwareServiceBase<EmployeeLocation, int, IEmployeeLocationRepository>, IEmployeeLocationService
    {
        public EmployeeLocationService(IRepositoryFactory repositoryFactory) : base(repositoryFactory)
        { }
    }
}
