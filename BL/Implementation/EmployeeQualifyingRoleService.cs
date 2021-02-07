using System;
using Core.BL;

using Core.BL.Interfaces;
using Core.Data;
using Core.Data.Interfaces;
using Core.Models;




namespace BL.Implementation
{
    public class EmployeeQualifyingRoleService : RepositoryAwareServiceBase<EmployeeQualifyingRole, int, IEmployeeQualifyingRoleRepository>, IEmployeeQualifyingRoleService
    {
        public EmployeeQualifyingRoleService(IRepositoryFactory repositoryFactory) : base(repositoryFactory)
        {
        }
    }
}
