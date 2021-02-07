using Core.BL;
using Core.BL.Interfaces;
using Core.Data;
using Core.Data.Interfaces;
using Core.Models;

namespace BL.Implementation
{
    public class EmployeeGradAssignmentService :
        RepositoryAwareServiceBase<EmployeeGradAssignment, int, IEmployeeGradAssignmentRepository>,
        IEmployeeGradAssignmentService
    {
        public EmployeeGradAssignmentService(IRepositoryFactory repositoryFactory) : base(repositoryFactory)
        {
        }
    }
}
