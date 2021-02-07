using System;
using Microsoft.EntityFrameworkCore;

namespace Core.Data
{
    public abstract class WrappingDbAccessorBase<TDbContext> : IDbAccessor<TDbContext>
            where TDbContext : DbContext
    {
        private TDbContext _dbContext;

        public WrappingDbAccessorBase(TDbContext dbContext)
        {
            if (dbContext == null)
                throw new ArgumentNullException(nameof(dbContext));

            _dbContext = dbContext;
        }

        public TDbContext GetDbContext()
        {
            return _dbContext;
        }
    }
}
