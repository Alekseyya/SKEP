using System;
using System.Linq;
using Core.Data;
using Core.Data.Interfaces;
using Core.Models;
//using System.Data.Entity;
using Microsoft.EntityFrameworkCore;


namespace Data.Implementation
{
    public class EmployeeRepository : RepositoryBase<Employee, int>, IEmployeeRepository
    {
        public EmployeeRepository(DbContext dbContext) : base(dbContext)
        { }

        public Employee GetByLogin(string login)
        {
            if (string.IsNullOrWhiteSpace(login))
                throw new ArgumentException("Логин не может быть null или пустой строкой", nameof(login));

            login = login.Trim().ToLower();
            return GetQueryable().Where(e => e.ADLogin != null && e.ADLogin.ToLower() == login).AsNoTracking().SingleOrDefault();
        }

        protected override bool CompareEntityId(Employee entity, int id)
        {
            return (entity.ID == id);
        }

        protected override Employee CreateEntityWithId(int id)
        {
            return new Employee { ID = id };
        }

        protected override object[] GetEntityKeyValues(int id)
        {
            return new object[] { id };
        }
    }
}