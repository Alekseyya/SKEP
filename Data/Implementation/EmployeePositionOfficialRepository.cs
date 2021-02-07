using Core.Data;
using Core.Data.Interfaces;
using Core.Models;
using Microsoft.EntityFrameworkCore;




namespace Data.Implementation
{
    public class EmployeePositionOfficialRepository : RepositoryBase<EmployeePositionOfficial, int>, IEmployeePositionOfficialRepository
    {
        public EmployeePositionOfficialRepository(DbContext dbContext) : base(dbContext)
        {
        }

        protected override object[] GetEntityKeyValues(int id)
        {
            return new object[] { id };
        }

        protected override EmployeePositionOfficial CreateEntityWithId(int id)
        {
            return new EmployeePositionOfficial { ID = id };
        }

        protected override bool CompareEntityId(EmployeePositionOfficial entity, int id)
        {
            return (entity.ID == id);
        }
    }
}
