using System;
using Core.Data;




namespace Data.Implementation
{
    public class RPCSUnitOfWork : IUnitOfWork
    {
        private readonly IRPCSDbAccessor _dbAccessor;

        public RPCSUnitOfWork(IRPCSDbAccessor dbAccessor)
        {
            if (dbAccessor == null)
                throw new ArgumentNullException(nameof(dbAccessor));

            _dbAccessor = dbAccessor;
        }

        public void CommitTransaction()
        {
            _dbAccessor.CommitTransaction();
        }

        public void EnsureTransaction()
        {
            _dbAccessor.EnsureTransaction();
        }

        public void RollbackTransaction()
        {
            _dbAccessor.RollbackTransaction();
        }
    }
}