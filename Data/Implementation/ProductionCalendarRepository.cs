//using System.Data.Entity;

using Core.Data;
using Core.Data.Interfaces;
using Core.Models;
using Microsoft.EntityFrameworkCore;




namespace Data.Implementation
{
    public class ProductionCalendarRepository : RepositoryBase<ProductionCalendarRecord, int>, IProductionCalendarRepository
    {
        public ProductionCalendarRepository(DbContext dbContext):base(dbContext)
        { }

        protected override bool CompareEntityId(ProductionCalendarRecord entity, int id)
        {
            return (entity.ID == id);
        }

        protected override ProductionCalendarRecord CreateEntityWithId(int id)
        {
            return new ProductionCalendarRecord
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