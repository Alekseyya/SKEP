using System;
using System.Linq;
using Core.Data;
using Core.Data.Interfaces;
using Core.Models;
using Microsoft.EntityFrameworkCore;

namespace Data.Implementation
{
    public class UserRepository : RepositoryBase<RPCSUser, int>, IUserRepository
    {
        public UserRepository(DbContext dbContext) : base(dbContext)
        { }

        public RPCSUser GetByLogin(string login)
        {
            if (string.IsNullOrWhiteSpace(login))
                throw new ArgumentException("Логин не может быть null или пустой строкой", nameof(login));

            login = login.Trim().ToLower();
            return GetQueryable().Where(e => e.UserLogin != null && e.UserLogin.ToLower() == login).SingleOrDefault(); ;
        }

        protected override bool CompareEntityId(RPCSUser entity, int id)
        {
            return (entity.ID == id);
        }

        protected override RPCSUser CreateEntityWithId(int id)
        {
            return new RPCSUser { ID = id };
        }

        protected override object[] GetEntityKeyValues(int id)
        {
            return new object[] { id };
        }
    }
}
