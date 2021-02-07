using Core.Data;
using Core.Data.Interfaces;
using Core.Models;
using Microsoft.EntityFrameworkCore;

namespace Data.Implementation
{
    public class DepartmentRepository : RepositoryBase<Department, int>, IDepartmentRepository
    {
        public DepartmentRepository(DbContext dbContext) : base(dbContext)
        { }

        protected override bool CompareEntityId(Department entity, int id)
        {
            return (entity.ID == id);
        }

        protected override Department CreateEntityWithId(int id)
        {
            return new Department
            {
                ID = id
            };
        }

        protected override object[] GetEntityKeyValues(int id)
        {
            return new object[] { id };
        }
    }
}