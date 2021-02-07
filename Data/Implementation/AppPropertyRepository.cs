using Core.Data;
using Core.Data.Interfaces;
using Core.Models;
using Microsoft.EntityFrameworkCore;

namespace Data.Implementation
{
    public class AppPropertyRepository : RepositoryBase<AppProperty, int>, IAppPropertyRepository
    {
        public AppPropertyRepository(DbContext dbContext) : base(dbContext)
        {
        }

        protected override object[] GetEntityKeyValues(int id)
        {
            return new object[] { id };
        }

        protected override AppProperty CreateEntityWithId(int id)
        {
            return new AppProperty { ID = id };
        }

        protected override bool CompareEntityId(AppProperty entity, int id)
        {
            return (entity.ID == id);
        }
    }
}
