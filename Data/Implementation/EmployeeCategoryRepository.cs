using Core.Data;
using Core.Data.Interfaces;
using Core.Models;
using Microsoft.EntityFrameworkCore;
namespace Data.Implementation
{
   public class EmployeeCategoryRepository: RepositoryBase<EmployeeCategory, int>, IEmployeeCategoryRepository
    {
        public EmployeeCategoryRepository(DbContext dbContext) : base(dbContext)
        {
        }

        protected override object[] GetEntityKeyValues(int id)
        {
            return new object[] { id };
        }

        protected override EmployeeCategory CreateEntityWithId(int id)
        {
            return new EmployeeCategory { ID = id };
        }

        protected override bool CompareEntityId(EmployeeCategory entity, int id)
        {
            return (entity.ID == id);
        }
    }
}
