using Core.BL;
using Core.BL.Interfaces;
using Core.Data;
using Core.Data.Interfaces;
using Core.Models;

namespace BL.Implementation
{
    public class EmployeePositionOfficialService : RepositoryAwareServiceBase<EmployeePositionOfficial, int, IEmployeePositionOfficialRepository>, IEmployeePositionOfficialService
    {
        public EmployeePositionOfficialService(IRepositoryFactory repositoryFactory) : base(repositoryFactory)
        { }
    }
}
