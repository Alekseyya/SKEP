using System;
using Core.Data;

namespace Data.Implementation
{
    public class RPCSWrappingDbAccessor : WrappingDbAccessorBase<RPCSContext>, IRPCSDbAccessor
    {
        public RPCSWrappingDbAccessor(RPCSContext dbContext) : base(dbContext)
        {
        }

        public void EnsureTransaction()
        {
            throw new NotImplementedException($"{nameof(RPCSWrappingDbAccessor)} не поддерживает транзакции");
        }

        public void CommitTransaction()
        {
            throw new NotImplementedException($"{nameof(RPCSWrappingDbAccessor)} не поддерживает транзакции");
        }

        public void RollbackTransaction()
        {
            throw new NotImplementedException($"{nameof(RPCSWrappingDbAccessor)} не поддерживает транзакции");
        }
    }
}