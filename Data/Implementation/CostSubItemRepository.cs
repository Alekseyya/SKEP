using Core.Data;
using Core.Data.Interfaces;
using Core.Models;
using Microsoft.EntityFrameworkCore;

namespace Data.Implementation
{
    public class CostSubItemRepository: RepositoryBase<CostSubItem, int>, ICostSubItemRepository
    {
        public CostSubItemRepository(DbContext dbContext) : base(dbContext)
        {
        }

        protected override bool CompareEntityId(CostSubItem entity, int id)
        {
            return (entity.ID == id);
        }

        protected override CostSubItem CreateEntityWithId(int id)
        {
            return new CostSubItem
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
