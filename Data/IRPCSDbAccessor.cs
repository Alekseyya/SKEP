using Core.Data;
//using System.Data.Entity;


namespace Data
{
    public interface IRPCSDbAccessor : IDbAccessor<RPCSContext>, IUnitOfWork
    {

    }
}
