using Core.BL;

using Core.BL.Interfaces;
using Core.Data;
using Core.Data.Interfaces;
using Core.Models;


namespace BL.Implementation
{
    public class EmployeeDepartmentAssignmentService : RepositoryAwareServiceBase<EmployeeDepartmentAssignment, int, IEmployeeDepartmentAssignmentRepository>, IEmployeeDepartmentAssignmentService
    {
        public EmployeeDepartmentAssignmentService(IRepositoryFactory repositoryFactory) : base(repositoryFactory)
        {
        }
    }
}
