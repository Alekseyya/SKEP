using Core.Data;
using Core.Data.Interfaces;
using Core.Models;
using Microsoft.EntityFrameworkCore;

namespace Data.Implementation
{
    public class CostItemRepository : RepositoryBase<CostItem, int>, ICostItemRepository
    {
        public CostItemRepository(DbContext dbContext) : base(dbContext)
        {
        }

        protected override object[] GetEntityKeyValues(int id)
        {
            return new object[] { id };
        }

        protected override CostItem CreateEntityWithId(int id)
        {
            return new CostItem
            {
                ID = id
            };
        }

        protected override bool CompareEntityId(CostItem entity, int id)
        {
            return (entity.ID == id);
        }
    }
}
