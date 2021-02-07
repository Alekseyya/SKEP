using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Data
{
    public interface IUnitOfWork
    {
        void EnsureTransaction();

        void CommitTransaction();

        void RollbackTransaction();
    }
}
