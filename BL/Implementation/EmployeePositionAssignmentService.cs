using Core.BL;

using Core.BL.Interfaces;
using Core.Data;
using Core.Data.Interfaces;
using Core.Models;




namespace BL.Implementation
{
    public class EmployeePositionAssignmentService : RepositoryAwareServiceBase<EmployeePositionAssignment, int, IEmployeePositionAssignmentRepository>, IEmployeePositionAssignmentService
    {
        public EmployeePositionAssignmentService(IRepositoryFactory repositoryFactory) : base(repositoryFactory)
        { }
    }
}
