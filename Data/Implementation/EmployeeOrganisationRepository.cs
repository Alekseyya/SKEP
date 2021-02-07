using Core.Data;
using Core.Data.Interfaces;
using Core.Models;
using Microsoft.EntityFrameworkCore;


namespace Data.Implementation
{
    public class EmployeeOrganisationRepository : RepositoryBase<EmployeeOrganisation, int>, IEmployeeOrganisationRepository
    {
        public EmployeeOrganisationRepository(DbContext dbContext) : base(dbContext) { }

        protected override object[] GetEntityKeyValues(int id)
        {
            return new object[] { id };
        }

        protected override EmployeeOrganisation CreateEntityWithId(int id)
        {
            return new EmployeeOrganisation { ID = id };
        }

        protected override bool CompareEntityId(EmployeeOrganisation entity, int id)
        {
            return (entity.ID == id);
        }
    }
}
