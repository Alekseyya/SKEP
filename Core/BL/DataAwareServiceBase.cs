using System;
using Core.Data;
using Microsoft.EntityFrameworkCore;

namespace Core.BL
{
    public abstract class DataAwareServiceBase<TDbContext, TDbAccessor>
        where TDbAccessor : IDbAccessor<TDbContext>
        where TDbContext : DbContext
    {
        private TDbAccessor _dbAccessor;

        protected TDbContext DB
        {
            get { return _dbAccessor.GetDbContext(); }
        }

        protected DataAwareServiceBase(TDbAccessor dbAccessor)
        {
            if (dbAccessor == null)
                throw new ArgumentNullException(nameof(dbAccessor));

            _dbAccessor = dbAccessor;
        }
    }
}
