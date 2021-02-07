using Core.Data;
using Core.Data.Interfaces;
using Core.Models;
using Microsoft.EntityFrameworkCore;

namespace Data.Implementation
{
    public class EmployeeLocationRepository : RepositoryBase<EmployeeLocation, int>, IEmployeeLocationRepository
    {
        public EmployeeLocationRepository(DbContext dbContext) : base(dbContext)
        {
        }

        protected override object[] GetEntityKeyValues(int id)
        {
            return new object[] { id };
        }

        protected override EmployeeLocation CreateEntityWithId(int id)
        {
            return new EmployeeLocation { ID = id };
        }

        protected override bool CompareEntityId(EmployeeLocation entity, int id)
        {
            return (entity.ID == id);
        }
    }
}
