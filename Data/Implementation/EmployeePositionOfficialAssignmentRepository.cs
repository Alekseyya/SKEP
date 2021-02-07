using System;
using System.Collections.Generic;
using System.Text;
using Core.Data;
using Core.Data.Interfaces;
using Core.Models;
using Microsoft.EntityFrameworkCore;




namespace Data.Implementation
{
    public class EmployeePositionOfficialAssignmentRepository : RepositoryBase<EmployeePositionOfficialAssignment, int>, IEmployeePositionOfficialAssignmentRepository
    {
        public EmployeePositionOfficialAssignmentRepository(DbContext dbContext) : base(dbContext)
        {
        }

        protected override object[] GetEntityKeyValues(int id)
        {
            return new object[] { id };
        }

        protected override EmployeePositionOfficialAssignment CreateEntityWithId(int id)
        {
            return new EmployeePositionOfficialAssignment { ID = id };
        }

        protected override bool CompareEntityId(EmployeePositionOfficialAssignment entity, int id)
        {
            return (entity.ID == id);
        }
    }
}
