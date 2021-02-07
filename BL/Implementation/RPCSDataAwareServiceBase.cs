using Core.BL;
using Data;

namespace BL.Implementation
{
    public abstract class RPCSDataAwareServiceBase : DataAwareServiceBase<RPCSContext, IRPCSDbAccessor>
    {
        protected RPCSDataAwareServiceBase(IRPCSDbAccessor dbAccessor):base(dbAccessor)
        { }
    }
}