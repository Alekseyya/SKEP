using System;
using System.Linq;
using System.Security.Principal;
using Core.BL.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Core.Data;
using Core.Models;


namespace BL.Implementation
{
   public class UsersFactoryService : IUserFactoryService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IMemoryCache _memoryCache;
        private readonly IUserService _userService;
        private readonly IDepartmentService _departmentService;
        private readonly IEmployeeService _employeeService;
        private readonly IApplicationUserService _applicationUserService;

        public UsersFactoryService(IRepositoryFactory repositoryFactory,
                                  IHttpContextAccessor httpContextAccessor,
                                  IMemoryCache memoryCache,
                                  IUserService userService,
                                  IDepartmentService departmentService,
                                  IEmployeeService employeeService,
                                  IApplicationUserService applicationUserService)
        {
            _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
            _memoryCache = memoryCache ?? throw new ArgumentNullException(nameof(memoryCache));
            _userService = userService ?? throw new ArgumentNullException(nameof(userService));
            _departmentService = departmentService ?? throw new ArgumentNullException(nameof(departmentService));
            _employeeService = employeeService ?? throw new ArgumentNullException(nameof(employeeService));
            _applicationUserService = applicationUserService ?? throw new ArgumentNullException(nameof(applicationUserService));
        }

        public ApplicationUser GetUser(IPrincipal contextUser)
        {
            //TODO - пользователя еще нет в БД - у него будем маска Empty
            ApplicationUser applicationUser;
            string cacheKey = contextUser.Identity.Name.ToLower();
            if (/*false &&*/ _memoryCache.Get(cacheKey) != null && _memoryCache.Get(cacheKey) is ApplicationUser)
            {
                applicationUser = (ApplicationUser)_memoryCache.Get(cacheKey);
            }
            else
            {
                ////TODO переделать чуть по позже, когда появятся идеи!
                //result = new ApplicationUser();
                ////TODO инициализация
                //var rpcUser =  _userService.GetCurrentUser();
                //if (rpcUser != null)
                //{
                //    result.UserId = rpcUser.ID;
                //    result.UserLogin = rpcUser.UserLogin;
                //}

                //var employeeId = _employeeService.GetCurrentEmployees().FirstOrDefault(e => e.ADLogin == result.UserLogin).ID;
                //result.ManagedDepartments = _departmentService.GetDepartmentsForManager(employeeId);

                //result.InitRoles(rpcUser);
                //if (rpcUser != null && String.IsNullOrEmpty(rpcUser.OOLogin) == false)
                //{
                //    result.OOLogin = rpcUser.OOLogin;
                //}
                ////TODO конец инициализации - куда вкихнуть?

                //applicationUser = _applicationUserService.Init(contextUser);
                //_memoryCache.Set(cacheKey, applicationUser);
            }
            return null;
        }

        public ApplicationUser GetCurrentUser()
        {
            if (_httpContextAccessor.HttpContext != null && _httpContextAccessor.HttpContext.User != null)
                return GetUser(_httpContextAccessor.HttpContext.User);
            return null;
        }
        
    }
}
