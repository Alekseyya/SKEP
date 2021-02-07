using Core.Data;
using Core.Data.Interfaces;
using Core.Models;
using Microsoft.EntityFrameworkCore;

namespace Data.Implementation
{
    public class BudgetLimitRepository : RepositoryBase<BudgetLimit, int>, IBudgetLimitRepository
    {
        public BudgetLimitRepository(DbContext dbContext) : base(dbContext)
        { }

        protected override bool CompareEntityId(BudgetLimit entity, int id)
        {
            return (entity.ID == id);
        }

        protected override BudgetLimit CreateEntityWithId(int id)
        {
            return new BudgetLimit { ID = id };
        }

        protected override object[] GetEntityKeyValues(int id)
        {
            return new object[] { id };
        }
    }
}
