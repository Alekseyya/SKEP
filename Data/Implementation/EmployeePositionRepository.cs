using Core.Data;
using Core.Data.Interfaces;
using Core.Models;
using Microsoft.EntityFrameworkCore;




namespace Data.Implementation
{
    public class EmployeePositionRepository : RepositoryBase<EmployeePosition, int>, IEmployeePositionRepository
    {
        public EmployeePositionRepository(DbContext dbContext) : base(dbContext)
        {
        }

        protected override object[] GetEntityKeyValues(int id)
        {
            return new object[] { id };
        }

        protected override EmployeePosition CreateEntityWithId(int id)
        {
            return new EmployeePosition { ID = id };
        }

        protected override bool CompareEntityId(EmployeePosition entity, int id)
        {
            return (entity.ID == id);
        }
    }
}
