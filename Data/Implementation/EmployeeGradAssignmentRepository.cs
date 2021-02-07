using Core.Data;
using Core.Data.Interfaces;
using Core.Models;
using Microsoft.EntityFrameworkCore;

namespace Data.Implementation
{
    public class EmployeeGradAssignmentRepository : RepositoryBase<EmployeeGradAssignment, int>, IEmployeeGradAssignmentRepository
    {
        public EmployeeGradAssignmentRepository(DbContext dbContext) : base(dbContext)
        {
        }

        protected override object[] GetEntityKeyValues(int id)
        {
            return new object[] { id };
        }

        protected override EmployeeGradAssignment CreateEntityWithId(int id)
        {
            return new EmployeeGradAssignment { ID = id };
        }

        protected override bool CompareEntityId(EmployeeGradAssignment entity, int id)
        {
            return (entity.ID == id);
        }
    }
}
