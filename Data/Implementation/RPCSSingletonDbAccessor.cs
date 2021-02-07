using System;
//using System.Data.Entity;
using Core.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;


namespace Data.Implementation
{
    public sealed class RPCSSingletonDbAccessor : SingletonDbAccessorBase<RPCSContext>, IRPCSDbAccessor
    {
        private IDbContextTransaction _transaction = null;

        private readonly DbContextOptions<RPCSContext> _options;

        public RPCSSingletonDbAccessor(DbContextOptions<RPCSContext> options)
        {
            if (options == null)
                throw new ArgumentNullException(nameof(options));

            _options = options;
        }

        public void EnsureTransaction()
        {
            if (_transaction == null)
            {
                var dbContext = GetDbContext();
                var database = dbContext.Database;
                _transaction = database.BeginTransaction();
            }
        }

        public void CommitTransaction()
        {
            if(_transaction != null)
            {
                _transaction.Commit();
                _transaction.Dispose();
                _transaction = null;
            }
        }

        public void RollbackTransaction()
        {
            if (_transaction != null)
            {
                _transaction.Rollback();
                _transaction.Dispose();
                _transaction = null;
            }
        }

        protected override RPCSContext CreateDbContext()
        {
            return new RPCSContext(_options);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                RollbackTransaction();
            }
            base.Dispose(disposing);
        }
    }
}