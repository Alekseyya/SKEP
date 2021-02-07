using System;
using System.Collections.Generic;
using System.Linq;
using Core;
using Core.BL.Interfaces;
using Core.Config;
using Core.Data;
using Core.Models;
using Core.Models.RBAC;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace BL.Implementation
{
    public class ApplicationUserService : IApplicationUserService
    {
        private readonly IEmployeeService _employeeService;
        private readonly IUserService _userService;
        private readonly IDepartmentService _departmentService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IMemoryCache _memoryCache;
        private readonly IProjectService _projectService;
        private readonly OnlyOfficeConfig _onlyOfficeOptions;
        private ApplicationUser _applicationUser;


        public ApplicationUserService(IRepositoryFactory repositoryFactory,
                                      IEmployeeService employeeService,
                                      IUserService userService,
                                      IDepartmentService departmentService,
                                      IHttpContextAccessor httpContextAccessor,
                                      IMemoryCache memoryCache,
                                      IProjectService projectService,
                                      IOptions<OnlyOfficeConfig> onlyOfficeOptions)
        {
            _employeeService = employeeService ?? throw new ArgumentNullException(nameof(employeeService));
            _userService = userService ?? throw new ArgumentNullException(nameof(userService));
            _departmentService = departmentService ?? throw new ArgumentNullException(nameof(departmentService));
            _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
            _memoryCache = memoryCache ?? throw new ArgumentNullException(nameof(memoryCache));
            _projectService = projectService ?? throw new ArgumentNullException(nameof(projectService));
            _onlyOfficeOptions = onlyOfficeOptions.Value;


            //TODO переделать чуть по позже, когда появятся идеи!
            _applicationUser = new ApplicationUser();
            if (httpContextAccessor.HttpContext != null)
            {
                string cacheKey = _httpContextAccessor.HttpContext.User.Identity.Name?.ToLower();
                if (!string.IsNullOrEmpty(cacheKey) && _memoryCache.Get(cacheKey) != null && _memoryCache.Get(cacheKey) is ApplicationUser)
                {
                    _applicationUser = (ApplicationUser)_memoryCache.Get(cacheKey);
                }
                else
                {
                    _applicationUser = Init();
                    _memoryCache.Set(cacheKey, _applicationUser);
                }
            }
               
        }

        public Tuple<string, string> GetUserDataForVersion()
        {
            Tuple<string, string> result;

            var employeeData = _employeeService.GetCurrentEmployees(new DateTimeRange(DateTime.Today, DateTime.Today))
                .FirstOrDefault(x => x.ADLogin.Equals(_applicationUser.UserLogin, StringComparison.InvariantCultureIgnoreCase));
            if (employeeData != null)
            {
                result = new Tuple<string, string>(employeeData.FullName, employeeData.ADEmployeeID ?? "");
            }
            else
            {
                result = new Tuple<string, string>(_applicationUser.UserLogin, "");
            }
            return result;
        }


        public bool IsCurrent(int employeeId)
        {
            if (employeeId <= 0)
                return false;
            //придумать куда лучше ткнуть работу с Employee
            var employee = _employeeService.GetCurrentEmployees(new DateTimeRange(DateTime.Today, DateTime.Today)).FirstOrDefault(emp => emp != null && emp.ID  == employeeId);
            if (_applicationUser.UserLogin != null && employee != null && 
                _applicationUser.UserLogin.Equals(employee.ADLogin, StringComparison.InvariantCultureIgnoreCase))
                return true;
            return false;
        }


        public bool IsMyProject(Project project)
        {
            if (HasAccess(Operation.ProjectMyProjectView) == true
                && ((project.EmployeeCAM != null && _applicationUser.UserLogin.Equals(project.EmployeeCAM.ADLogin, StringComparison.InvariantCultureIgnoreCase))
                    || (project.EmployeePM != null && _applicationUser.UserLogin.Equals(project.EmployeePM.ADLogin, StringComparison.InvariantCultureIgnoreCase))
                    || (project.EmployeePA != null && _applicationUser.UserLogin.Equals(project.EmployeePA.ADLogin, StringComparison.InvariantCultureIgnoreCase))))
            {
                return true;
            }
            else if (HasAccess(Operation.ProjectMyDepartmentProjectView) == true
                     && ((project.Department != null && IsDepartmentManager(project.Department.ID) == true)
                         || (project.EmployeeCAM != null && IsDepartmentManagerForEmployee(project.EmployeeCAM.ID))
                         || (project.EmployeePM != null && IsDepartmentManagerForEmployee(project.EmployeePM.ID))
                         || (project.EmployeePA != null && IsDepartmentManagerForEmployee(project.EmployeePA.ID))))
            {
                return true;
            }

            return false;
        }

        public string GetOOPassword()
        {
            return _applicationUser.OOPassword;
        }


        public void SetOOPassword(string ooPassword)
        {
            if (String.IsNullOrEmpty(ooPassword) == false)
            {
               _applicationUser.OOPassword = ooPassword;
            }
        }

        public void ClearOOPassword()
        {
           _applicationUser.OOPassword = "";
        }

        private bool IsHasOwnOOLogin()
        {
            return !string.IsNullOrEmpty(_applicationUser.OOLogin);
        }

        public bool IsDepartmentManager(int departmentId)
        {
            if (_applicationUser.ManagedDepartments.Select(x=>x.ID).Contains(departmentId))
                return true;
            return false;
        }
        public string GetOOLogin()
        {
            string ooLogin = "";

            if (!string.IsNullOrEmpty(_applicationUser.OOLogin))
            {
                ooLogin = _applicationUser.OOLogin;
            }
            else
            {
                try
                {
                    if (!string.IsNullOrEmpty(_applicationUser.Role.CurrentOOLoginConfigPropertyName)
                        && _onlyOfficeOptions[_applicationUser.Role.CurrentOOLoginConfigPropertyName] != null)
                    {
                        ooLogin = (string)_onlyOfficeOptions[_applicationUser.Role.CurrentOOLoginConfigPropertyName];
                    }
                }
                catch (Exception)
                {
                    ooLogin = "";
                }
            }

            return ooLogin;
        }

        public bool IsDepartmentManagerForEmployee(int employeeId)
        {
            var emplDepId = _employeeService.GetCurrentEmployees(new DateTimeRange(DateTime.Today, DateTime.Today))
                .Where(e => e.ID == employeeId).Select(x => x.DepartmentID).FirstOrDefault();
            if (emplDepId != null && _applicationUser.ManagedDepartments.Select(x=>x.ID).Contains(emplDepId.Value))
                return true;
            return false;
        }

        public int GetEmployeeID()
        {
            int employeeId = 0;

            try
            {
                var employee = _employeeService.Get(x =>
                    x.AsNoTracking().Where(e => _applicationUser.UserLogin.Equals(e.ADLogin, StringComparison.InvariantCultureIgnoreCase)).ToList()).FirstOrDefault();

                if (employee != null)
                {
                    employeeId = employee.ID;
                }
            }
            catch (Exception)
            {

            }

            return employeeId;
        }


        private ApplicationUser Init()
        {
            //TODO инициализация
            var rpcUser = _userService.GetCurrentUser();
            if (rpcUser != null)
            {
                _applicationUser.UserId = rpcUser.ID;
                _applicationUser.UserLogin = rpcUser.UserLogin;
            }
            //Todo если это первый заход пользователя
            else
            {
                rpcUser = AddFirstUser();
                _applicationUser.UserId = rpcUser.ID;
                _applicationUser.UserLogin = rpcUser.UserLogin;
            }

            //TODO переделать логику!!!
            var employee = _employeeService.GetAllEmployees().FirstOrDefault(e => e.ADLogin == _applicationUser.UserLogin);
            if (employee == null)
            {
                //Todo сначала заводится RPCSUser а Employee(т.е сотрудник) уже создается менеджерами, но опять же надо проверить!!!
                //Создание пользователя
                //var newEmployee = new Employee() { ADLogin = _applicationUser.UserLogin, Created = DateTime.Now, Modified = DateTime.Now, Author = _applicationUser.UserLogin };
                //_employeeService.Add(newEmployee);
                //employee = _employeeService.GetCurrentEmployees().FirstOrDefault(e => e.ADLogin == _applicationUser.UserLogin);
            }

            if (employee != null)
                _applicationUser.ManagedDepartments = _departmentService.GetDepartmentsForManager(employee.ID);
            else
                _applicationUser.ManagedDepartments = new List<Department>();

            _applicationUser.InitRoles(rpcUser);

            if (rpcUser != null && String.IsNullOrEmpty(rpcUser.OOLogin) == false)
            {
                _applicationUser.OOLogin = rpcUser.OOLogin;
            }
            
            return _applicationUser;
        }

        private RPCSUser AddFirstUser()
        {
            var rpcsUser = new RPCSUser() { UserLogin = _httpContextAccessor.HttpContext.User.Identity.Name.ToLower(), IsAdmin = true};
            _userService.Add(rpcsUser);
            return rpcsUser;
        }

        public bool IsAuthenticated()
        {
            bool result = false;
            result = (!string.IsNullOrEmpty(GetOOPassword()));
            return result;
        }

        /// <summary>
        /// Проверяем - имеет ли пользователь право выпольнить соответствующую операцию
        /// </summary>
        /// <param name="operation"></param>
        /// <returns></returns>
        public bool HasAccess(Operation operation)
        {
            bool result = false;
            result = _applicationUser.Role.Operations.Contains(operation);

            if (result == true && _applicationUser.Role.PayrollAccessOperations.Contains(operation))
            {
                if (IsHasOwnOOLogin() == false)
                {
                    result = false;
                }
            }
            return result;
        }
        
        public bool HasAccess(OperationSet operationSet)
        {
            bool result = false;

            result = _applicationUser.Role.Operations.Contains(operationSet);

            if (result == true
                && _applicationUser.Role.PayrollAccessOperations.Contains(operationSet))
            {
                if (IsHasOwnOOLogin() == false)
                {
                    result = false;
                }
            }

            return result;
        }

        public IList<Project> GetMyProjects()
        {
            IList<Project> projects = null;

            if (HasAccess(Operation.ProjectsHoursReportView) == true)
            {
                projects = _projectService.Get(pr => pr.Include(p => p.EmployeePM)
                    .Include(p => p.EmployeeCAM).Include(p => p.EmployeePA).ToList());

            }
            else
            {
                projects = _projectService.Get(pr => pr.Include(p => p.EmployeePM).Include(p => p.EmployeeCAM)
                    .Where(p => p.EmployeePM.ADLogin.Equals(_applicationUser.UserLogin, StringComparison.InvariantCultureIgnoreCase)
                                || p.EmployeeCAM.ADLogin.Equals(_applicationUser.UserLogin, StringComparison.InvariantCultureIgnoreCase)
                                || p.EmployeePA.ADLogin.Equals(_applicationUser.UserLogin, StringComparison.InvariantCultureIgnoreCase)).ToList());
            }

            return projects;
        }


        public ApplicationUser GetUser()
        {
            return _applicationUser;
        }

        public ApplicationUser GetCurrentUser()
        {
            if (_httpContextAccessor.HttpContext != null && _httpContextAccessor.HttpContext.User != null)
                return GetUser();
            return null;
        }

        public bool CheckUserHasOwnOOLogin()
        {
            bool result = false;
            try
            {
                result = IsHasOwnOOLogin();
            }
            catch (Exception)
            {
                result = false;
            }
            return result;
        }

    }
}
