
//using System.Data.Entity;

using Core.Data;
using Core.Data.Interfaces;
using Core.Models;
using Microsoft.EntityFrameworkCore;




namespace Data.Implementation
{
    public class ExpensesRecordRepository: RepositoryBase<ExpensesRecord, int>, IExpensesRecordRepository
    {
        public ExpensesRecordRepository(DbContext dbContext) : base(dbContext)
        { }

        protected override bool CompareEntityId(ExpensesRecord entity, int id)
        {
            return (entity.ID == id);
        }

        protected override ExpensesRecord CreateEntityWithId(int id)
        {
            return new ExpensesRecord { ID = id };
        }

        protected override object[] GetEntityKeyValues(int id)
        {
            return new object[] { id };
        }
    }
}