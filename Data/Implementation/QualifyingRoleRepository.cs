using Core.Data;
using Core.Data.Interfaces;
using Core.Models;
using Microsoft.EntityFrameworkCore;


namespace Data.Implementation
{
   public class QualifyingRoleRepository : RepositoryBase<QualifyingRole, int>, IQualifyingRoleRepository
    {
        public QualifyingRoleRepository(DbContext dbContext) : base(dbContext)
        {
        }

        protected override object[] GetEntityKeyValues(int id)
        {
            return new object[] { id };
        }

        protected override QualifyingRole CreateEntityWithId(int id)
        {
            return new QualifyingRole { ID = id };
        }

        protected override bool CompareEntityId(QualifyingRole entity, int id)
        {
            return (entity.ID == id);
        }
    }
}
