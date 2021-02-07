using System;
using System.Collections.Generic;
using System.Text;
using Core.Data;
using Core.Data.Interfaces;
using Core.Models;
using Microsoft.EntityFrameworkCore;




namespace Data.Implementation
{
    public class OrganisationRepository : RepositoryBase<Organisation, int>, IOrganisationRepository
    {
        public OrganisationRepository(DbContext dbContext) : base(dbContext)
        {
        }

        protected override object[] GetEntityKeyValues(int id)
        {
            return new object[] { id };
        }

        protected override Organisation CreateEntityWithId(int id)
        {
            return new Organisation { ID = id };
        }

        protected override bool CompareEntityId(Organisation entity, int id)
        {
            return (entity.ID == id);
        }
    }
}
