using Core.Data;
using Core.Data.Interfaces;
using Core.Models;
using Microsoft.EntityFrameworkCore;

namespace Data.Implementation
{
    public class EmployeeGradRepository: RepositoryBase<EmployeeGrad, int>, IEmployeeGradRepository
    {
        public EmployeeGradRepository(DbContext dbContext) : base(dbContext)
        {
        }

        protected override object[] GetEntityKeyValues(int id)
        {
            return new object[] { id };
        }

        protected override EmployeeGrad CreateEntityWithId(int id)
        {
            return new EmployeeGrad { ID = id };
        }

        protected override bool CompareEntityId(EmployeeGrad entity, int id)
        {
            return (entity.ID == id);
        }
    }
}
