using System;
using System.Collections.Generic;
using System.Security.Principal;
using Core.BL;
using Microsoft.AspNetCore.Http;
using Core.BL.Interfaces;
using Core.Data;
using Core.Data.Interfaces;
using Core.Models;

namespace BL.Implementation
{
    public class UserService : RepositoryAwareServiceBase<RPCSUser, int, IUserRepository>, IUserService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public UserService(IRepositoryFactory repositoryFactory, IHttpContextAccessor httpContextAccessor) : base(
            repositoryFactory)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public IList<RPCSUser> GetList()
        {
            var repository = RepositoryFactory.GetRepository<IUserRepository>();
            return repository.GetAll();
        }

        public RPCSUser GetUserByLogin(string userLogin)
        {
            RPCSUser user = null;

            try
            {
                var repository = RepositoryFactory.GetRepository<IUserRepository>();
                user = repository.GetByLogin(userLogin);
            }
            catch (Exception)
            {
                user = null;
            }

            return user;
        }

        public RPCSUser GetCurrentUser()
        {
            string currentUserLogin = GetCurrentUserLogin();
            if (currentUserLogin == null)
                return null;

            var repository = RepositoryFactory.GetRepository<IUserRepository>();
            return repository.GetByLogin(currentUserLogin);
        }

        public Employee GetEmployeeForCurrentUser()
        {
            string currentUserLogin = GetCurrentUserLogin();
            if (currentUserLogin == null)
                return null;

            var repository = RepositoryFactory.GetRepository<IEmployeeRepository>();
            return repository.GetByLogin(currentUserLogin);
        }

        public (string, string) GetUserDataForVersion()
        {
            var repository = RepositoryFactory.GetRepository<IEmployeeRepository>();

            string sid = GetCurrentUserSID();

            Employee employee = GetEmployeeForCurrentUser();
            if (employee != null)
                return (FullName: employee.FullName, sid); //sid учетной записи пользователя в AD
            string currentUserLogin = GetCurrentUserLogin();
            if (currentUserLogin != null)
                return (FullName: currentUserLogin, sid); //sid учетной записи пользователя в AD
            return ("", "");
        }

        protected string GetCurrentUserLogin()
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext == null)
                return null;
            var principal = (IPrincipal)httpContext.User;
            if (principal == null)
                return null;
            string login = principal.Identity.Name;
            if (string.IsNullOrWhiteSpace(login))
                return null;
            return login;
        }

        protected string GetCurrentUserSID()
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext == null)
                return "";
            var principal = (IPrincipal)httpContext.User;
            if (principal == null)
                return "";

            string sid = "";

            try
            {
                WindowsIdentity identity = principal.Identity as WindowsIdentity;
                if (identity != null
                    && identity.User != null
                    /*&& identity.User.AccountDomainSid != null
                    && identity.User.AccountDomainSid.Value != null*/
                    && String.IsNullOrEmpty(identity.User.Value) == false)
                {
                    sid = identity.User.Value;
                }
            }
            catch (Exception)
            {
                sid = "";
            }

            return sid;
        }
    }
}