using Core.Data;
using Core.Data.Interfaces;
using Core.Models;
using Microsoft.EntityFrameworkCore;

namespace Data.Implementation
{
    public class EmployeeDepartmentAssignmentRepository : RepositoryBase<EmployeeDepartmentAssignment, int>, IEmployeeDepartmentAssignmentRepository
    {
        public EmployeeDepartmentAssignmentRepository(DbContext dbContext) : base(dbContext)
        {
        }

        protected override object[] GetEntityKeyValues(int id)
        {
            return new object[] { id };
        }

        protected override EmployeeDepartmentAssignment CreateEntityWithId(int id)
        {
            return new EmployeeDepartmentAssignment { ID = id };
        }

        protected override bool CompareEntityId(EmployeeDepartmentAssignment entity, int id)
        {
            return (entity.ID == id);
        }
    }
}
