using Core.BL;
using Core.BL.Interfaces;
using Core.Data;
using Core.Data.Interfaces;
using Core.Models;

namespace BL.Implementation
{
    public class EmployeePositionOfficialAssignmentService : RepositoryAwareServiceBase<EmployeePositionOfficialAssignment, int, IEmployeePositionOfficialAssignmentRepository>, IEmployeePositionOfficialAssignmentService

    {
        public EmployeePositionOfficialAssignmentService(IRepositoryFactory repositoryFactory) : base(repositoryFactory)
        {
        }
    }
}
