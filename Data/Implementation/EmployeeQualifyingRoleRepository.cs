using System;
using System.Collections.Generic;
using System.Text;
using Core.Data;
using Core.Data.Interfaces;
using Core.Models;
using Microsoft.EntityFrameworkCore;




namespace Data.Implementation
{
   public class EmployeeQualifyingRoleRepository : RepositoryBase<EmployeeQualifyingRole, int>, IEmployeeQualifyingRoleRepository
    {
        public EmployeeQualifyingRoleRepository(DbContext dbContext) : base(dbContext)
        {
        }

        protected override object[] GetEntityKeyValues(int id)
        {
            return new object[] { id };
        }

        protected override EmployeeQualifyingRole CreateEntityWithId(int id)
        {
            return new EmployeeQualifyingRole { ID = id };
        }

        protected override bool CompareEntityId(EmployeeQualifyingRole entity, int id)
        {
            return (entity.ID == id);
        }
    }
}
