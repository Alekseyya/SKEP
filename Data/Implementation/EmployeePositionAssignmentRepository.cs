using Core.Data;
using Core.Data.Interfaces;
using Core.Models;
using Microsoft.EntityFrameworkCore;




namespace Data.Implementation
{
    public class EmployeePositionAssignmentRepository : RepositoryBase<EmployeePositionAssignment, int>, IEmployeePositionAssignmentRepository
    {
        public EmployeePositionAssignmentRepository(DbContext dbContext) : base(dbContext)
        {
        }

        protected override object[] GetEntityKeyValues(int id)
        {
            return new object[] { id };
        }

        protected override EmployeePositionAssignment CreateEntityWithId(int id)
        {
            return new EmployeePositionAssignment { ID = id };
        }

        protected override bool CompareEntityId(EmployeePositionAssignment entity, int id)
        {
            return (entity.ID == id);
        }
    }
}
