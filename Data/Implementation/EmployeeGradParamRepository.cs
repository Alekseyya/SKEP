using Core.Data;
using Core.Data.Interfaces;
using Core.Models;
using Microsoft.EntityFrameworkCore;

namespace Data.Implementation
{
    public class EmployeeGradParamRepository : RepositoryBase<EmployeeGradParam, int>, IEmployeeGradParamRepository
    {
        public EmployeeGradParamRepository(DbContext dbContext) : base(dbContext)
        { }

        protected override bool CompareEntityId(EmployeeGradParam entity, int id)
        {
            return (entity.ID == id);
        }

        protected override EmployeeGradParam CreateEntityWithId(int id)
        {
            return new EmployeeGradParam
            {
                ID = id
            };
        }

        protected override object[] GetEntityKeyValues(int id)
        {
            return new object[] { id };
        }
    }
}
