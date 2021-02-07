using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

//using System.Data.Entity;


namespace Core.Data
{
    public abstract class UnitOfWorkBase : IUnitOfWork, IDisposable
    {
        private bool _disposed = false;

        protected IDbContextTransaction Transaction { get; set; }

        public void EnsureTransaction()
        {
            if (_disposed)
                throw new ObjectDisposedException("Transaction");

            if (Transaction == null)
            {
                var database = GetDbContext().Database;
                Transaction = database.BeginTransaction();
            }
        }

        public void CommitTransaction()
        {
            if (_disposed)
                throw new ObjectDisposedException("Transaction");

            if (Transaction != null)
            {
                try
                {
                    Transaction.Commit();
                    DisposeTransaction();
                }
                catch
                {
                    RollbackTransaction();
                }
            }

        }

        public void RollbackTransaction()
        {
            if (_disposed)
                throw new ObjectDisposedException("Transaction");

            if (Transaction != null)
            {
                Transaction.Rollback();
                DisposeTransaction();
            }
        }

        public void Dispose()
        {
            if(!_disposed)
            {
                RollbackTransaction();
                _disposed = true;
            }
        }

        protected abstract DbContext GetDbContext();

        protected void DisposeTransaction()
        {
            if (Transaction == null)
                throw new InvalidOperationException("Транзакция не открыта");

            Transaction.Dispose();
            Transaction = null;
        }
    }
}
