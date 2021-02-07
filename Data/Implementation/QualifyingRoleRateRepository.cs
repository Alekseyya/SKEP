using System;
using System.Collections.Generic;
using System.Text;
using Core.Data;
using Core.Data.Interfaces;
using Core.Models;
using Microsoft.EntityFrameworkCore;




namespace Data.Implementation
{
    public class QualifyingRoleRateRepository : RepositoryBase<QualifyingRoleRate, int>, IQualifyingRoleRateRepository
    {
        public QualifyingRoleRateRepository(DbContext dbContext) : base(dbContext)
        {
        }

        protected override object[] GetEntityKeyValues(int id)
        {
            return new object[] { id };
        }

        protected override QualifyingRoleRate CreateEntityWithId(int id)
        {
            return new QualifyingRoleRate { ID = id };
        }

        protected override bool CompareEntityId(QualifyingRoleRate entity, int id)
        {
            return (entity.ID == id);
        }
    }
}
