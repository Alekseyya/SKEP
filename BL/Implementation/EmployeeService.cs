using System;
using System.Collections.Generic;
using System.DirectoryServices;
using System.DirectoryServices.AccountManagement;
using System.DirectoryServices.ActiveDirectory;
using System.Linq;
using System.Text.RegularExpressions;
using Core;
using Core.BL;
using Core.BL.Interfaces;
using Core.Data;
using Core.Data.Interfaces;
using Core.Helpers;
using Core.Models;
using Microsoft.EntityFrameworkCore;


namespace BL.Implementation
{
    public class EmployeeService : RepositoryAwareServiceBase<Employee, int, IEmployeeRepository>, IEmployeeService
    {
        private readonly IDepartmentService _departmentService;
        private readonly (string, string) _user;

        public EmployeeService(IRepositoryFactory repositoryFactory, IDepartmentService departmentService, IUserService userService) : base(repositoryFactory)
        {
            _departmentService = departmentService;
            _user = userService.GetUserDataForVersion();
        }

        public override Employee Add(Employee employee)
        {
            if (employee == null)
                throw new ArgumentNullException();

            var employeesRepository = RepositoryFactory.GetRepository<IEmployeeRepository>();
            employee.InitBaseFields(Tuple.Create(_user.Item1, _user.Item2));
            return employeesRepository.Add(employee);
        }
        public void UpdateWithoutVersion(Employee employee)
        {
            if (employee == null)
                throw new ArgumentNullException(nameof(employee));

            var employeeRepository = RepositoryFactory.GetRepository<IEmployeeRepository>();

            employeeRepository.Update(employee);
        }

        public override Employee Update(Employee employee)
        {
            if (employee == null) throw new ArgumentNullException(nameof(employee));
            var employeesRepository = RepositoryFactory.GetRepository<IEmployeeRepository>();

            var originalItem = employeesRepository.FindNoTracking(employee.ID);

            employee.UpdateBaseFields(Tuple.Create(_user.Item1, _user.Item2), originalItem.ID, originalItem);
            originalItem.FreeseVersion(originalItem.ID);


            employeesRepository.Add(originalItem);
            return employeesRepository.Update(employee);
        }

        public List<Employee> GetEmployeesInFRCDepartment(int departmentID)
        {
            var employeeRepository = RepositoryFactory.GetRepository<IEmployeeRepository>();
            var employees = employeeRepository.GetQueryable()
                .Where(e => (e.EnrollmentDate == null || e.EnrollmentDate.Value <= DateTime.Today)
                            && (e.DismissalDate == null || e.DismissalDate >= DateTime.Today))
                .Include(e => e.EmployeePosition).Where(e => e.DepartmentID == departmentID)
                .ToList()
                .OrderBy(e => e.Department.ShortName + e.FullName).ToList();

            foreach (var department in  _departmentService.Get(dp => dp.Where(x => x.ParentDepartmentID == departmentID
                                                                                    && x.IsFinancialCentre == false).ToList()))
            {
                var result = GetEmployeesInFRCDepartment(department.ID);
                employees = employees.Concat(result).ToList();
            }
            return employees;
        }

        public Employee FindEmployeeByFullName(string searchFullName)
        {
            var splitedFullName = Regex.Replace(searchFullName.Trim(), @"\s+", " ").Contains(' ') ?
                Regex.Replace(searchFullName.Trim(), @"\s+", " ").Split(' ') : null;
            if (splitedFullName == null)
                return null;

            var employeeRepository = RepositoryFactory.GetRepository<IEmployeeRepository>();
            Employee employee = null;
            try
            {
                var employeeLastName = splitedFullName[0].ToLower();
                var employeeFirstName = splitedFullName[1].ToLower();
                var employeeMidName = splitedFullName[2].ToLower();

                if (splitedFullName.Length == 4)
                {
                    employeeMidName = splitedFullName[2].ToLower() + " " + splitedFullName[3].ToLower();
                }

                employee = employeeRepository.GetAll(e => e.FirstName.ToLower() == employeeFirstName
                                                          && e.MidName.ToLower() == employeeMidName
                                                          && e.LastName.ToLower() == employeeLastName)
                    .FirstOrDefault();
            }
            catch (IndexOutOfRangeException e)
            {
                return null;
            }

            return employee;
        }

        public Employee GetEmployeeByLogin(string userLogin)
        {
            return RepositoryFactory.GetRepository<IEmployeeRepository>().GetQueryable().SingleOrDefault(e => e.ADLogin != null && e.ADLogin.ToLower() == userLogin);
        }

        public IList<Employee> GetCurrentEmployees(DateTimeRange dateRange)
        {
            var employeeRepository = RepositoryFactory.GetRepository<IEmployeeRepository>();
            var employeeCategoryRepository = RepositoryFactory.GetRepository<IEmployeeCategoryRepository>();

            var employeeCategoryList = employeeCategoryRepository.GetAll(e => (e.CategoryDateBegin == null || e.CategoryDateBegin.Value <= dateRange.End)
                                                                              && (e.CategoryDateEnd == null || e.CategoryDateEnd >= dateRange.Begin));

            var employeeByCategoryIDs = employeeCategoryList.Select(x => x.EmployeeID).Distinct().ToList();

            return employeeRepository.GetAll(e => e.IsVacancy == false)
                .Where(e => (e.EnrollmentDate == null || e.EnrollmentDate.Value <= dateRange.End || employeeByCategoryIDs.Any(x => x == e.ID))
                            && (e.DismissalDate == null || e.DismissalDate >= dateRange.Begin)).ToList();
        }
        public IList<Employee> FindEmployees(string searchString)
        {
            //TODO Разбиение стоит по пробелу, но возможно нужно по точно и другим символам
            var searchToken = searchString.Split(' ').Select(x => x.ToLower());
            var searchTokensList = new List<string>();
            foreach (var token in searchToken)
            {
                if (!string.IsNullOrEmpty(token) && !string.IsNullOrEmpty(token.Trim()))
                    searchTokensList.Add(token.Trim());
            }

            var repository = RepositoryFactory.GetRepository<IEmployeeRepository>();
            var employees = new List<Employee>();
            if (searchTokensList.Count > 1)
                employees = repository.GetAll(empl =>
                    searchTokensList.All(prop => (empl.FirstName != null && empl.FirstName.ToLower().Contains(prop))
                                                 || (empl.LastName != null && empl.LastName.ToLower().Contains(prop))
                                                 || (empl.MidName != null && empl.MidName.ToLower().Contains(prop))
                                                 || (empl.Email != null && empl.Email.ToLower().Contains(prop))
                                                 || (empl.EmployeePositionTitle != null &&
                                                     empl.EmployeePositionTitle.ToLower().Contains(prop)))).OrderBy(empl => empl.FullName).ToList();
            else//TODO Написать поиск по подразделению/отделу
                employees = repository.GetAll(empl =>
                    (empl.FirstName != null && empl.FirstName.ToLower().Contains(searchTokensList.FirstOrDefault()))
                    || (empl.LastName != null && empl.LastName.ToLower().Contains(searchTokensList.FirstOrDefault()))
                    || (empl.MidName != null && empl.MidName.ToLower().Contains(searchTokensList.FirstOrDefault()))
                    || (empl.Email != null && empl.Email.ToLower().Contains(searchTokensList.FirstOrDefault()))
                    || (empl.EmployeePositionTitle != null && empl.EmployeePositionTitle.ToLower().Contains(searchTokensList.FirstOrDefault()))
                        //|| (empl.Department != null && empl.Department.ShortName != null && 
                        //empl.Department.ShortName.ToLower().Contains(searchTokensList.FirstOrDefault())
                        ).OrderBy(empl => empl.FullName).ToList();
            return employees;
        }

        public string GetADEmployeeIDBySearchString(string searchString)
        {
            string userADEmployeeID = "";

            var employees = RepositoryFactory.GetRepository<IEmployeeRepository>().GetQueryable();

            string[] searchTokens = searchString.Split(' ');

            List<string> searchTokensList = new List<string>();
            for (int i = 0; i < searchTokens.Length; i++)
            {
                if (String.IsNullOrEmpty(searchTokens[i]) == false
                    && String.IsNullOrEmpty(searchTokens[i].Trim()) == false)
                {
                    searchTokensList.Add(searchTokens[i].Trim());
                }
            }

            List<Employee> employeeList = null;

            if (searchTokensList.Count > 1)
            {
                employeeList = employees.Where(e => searchTokensList.All(stl => e.FirstName.Contains(stl)
                                                                                || e.LastName.Contains(stl)
                                                                                || e.MidName.Contains(stl)
                                                                                || e.Email.Contains(stl))).ToList();
            }
            else
            {
                employeeList = employees.Where(e => e.FirstName.Contains(searchString.Trim())
                                                    || e.LastName.Contains(searchString.Trim())
                                                    || e.MidName.Contains(searchString.Trim())
                                                    || e.Email.Contains(searchString.Trim())).ToList();
            }

            if (employeeList != null && employeeList.Count() == 1
                                     && String.IsNullOrEmpty(employeeList.FirstOrDefault().ADEmployeeID) == false)
            {
                userADEmployeeID = employeeList.FirstOrDefault().ADEmployeeID;
            }
            else
            {
                userADEmployeeID = ADHelper.GetADEmployeeIDBySearchStringInAD(searchString);
            }

            return userADEmployeeID;
        }

        public string GetEmployeeTitleByADEmployeeID(string userADEmployeeID)
        {
            string employeeTitle = "";

            Employee employee = RepositoryFactory.GetRepository<IEmployeeRepository>().GetQueryable().FirstOrDefault(e => e.ADEmployeeID == userADEmployeeID);

            if (employee != null)
            {
                employeeTitle = employee.FullName;
            }

            return employeeTitle;
        }

        public string GetADEmployeeIDByEmployeeIDInDB(int id)
        {
            string userADEmployeeID = "";

            Employee employee = RepositoryFactory.GetRepository<IEmployeeRepository>().GetById(id);

            if (employee != null)
            {
                if (String.IsNullOrEmpty(employee.ADEmployeeID) == false)
                {
                    userADEmployeeID = employee.ADEmployeeID;
                }
                else
                {
                    userADEmployeeID = ADHelper.GetADEmployeeIDBySearchStringInAD(RPCSHelper.NormalizeAndTrimString(employee.FullName));
                }
            }

            return userADEmployeeID;
        }

        public static string GetADEmployeeIDBySearchStringInAD(string searchString)
        {
            string userADEmployeeID = "";
            string domainName = Domain.GetCurrentDomain().Name;

            using (var pc = new PrincipalContext(ContextType.Domain, domainName))
            {
                UserPrincipal userPrincipal = UserPrincipal.FindByIdentity(pc, IdentityType.Name, searchString);

                if (userPrincipal != null)
                {
                    DirectoryEntry de = (userPrincipal.GetUnderlyingObject() as DirectoryEntry);

                    if (de.Properties.Contains("extensionAttribute10") == true
                        && de.Properties["extensionAttribute10"].Value != null)
                    {
                        userADEmployeeID = de.Properties["extensionAttribute10"].Value.ToString();
                    }
                }
            }

            return userADEmployeeID;
        }

        public IList<Employee> GetAllManagedEmployees(IList<Department> listDepartments)
        {
            return Get(empl => empl.Where(e => e.Department != null && listDepartments.Contains(e.Department))
                .Include(e => e.EmployeePosition)
                .Include(e => e.Department)
                .ToList());
        }
        public IList<Employee> GetAllEmployees()
        {
            var repository = RepositoryFactory.GetRepository<IEmployeeRepository>();
            return repository.GetAll();
        }

        public IList<Employee> GetEmployeesInDepartment(int departmentId)
        {
            return GetEmployeesInDepartment(departmentId, false);
        }

        public IList<Employee> GetEmployeesInDepartment(int departmentId, DateTimeRange dateRange)
        {
            return GetEmployeesInDepartment(departmentId, dateRange, false);
        }

        public IList<Employee> GetEmployeesInDepartment(int departmentId, bool includeChildDepratments)
        {
            return GetEmployeesInDepartmentInternal(departmentId, null, includeChildDepratments);
        }

        public IList<Employee> GetEmployeesInDepartment(int departmentId, DateTimeRange dateRange, bool includeChildDepratments)
        {
            return GetEmployeesInDepartmentInternal(departmentId, dateRange, includeChildDepratments);
        }

        private IList<Employee> GetEmployeesInDepartmentInternal(int departmentId, DateTimeRange? dateRange, bool includeChildDepratment)
        {
            var employeeRepository = RepositoryFactory.GetRepository<IEmployeeRepository>();
            IList<Employee> employees;
            if (includeChildDepratment)
            {
                var departmentRepository = RepositoryFactory.GetRepository<IDepartmentRepository>();
                employees = GetEmployeesInDepartmentHierarchy(departmentRepository, employeeRepository, departmentId, dateRange);
            }
            else
                employees = GetEmployeesInOneDepartment(employeeRepository, departmentId, dateRange);
            return employees;
        }

        private IList<Employee> GetEmployeesInOneDepartment(IEmployeeRepository employeeRepository, int departmentId, DateTimeRange? dateRange)
        {
            IList<Employee> employees;
            if (dateRange.HasValue)
            {
                DateTime dateForm = dateRange.Value.Begin;
                DateTime dateTo = dateRange.Value.End;
                employees = employeeRepository.GetAll(e => e.DepartmentID.Value == departmentId
                    && (e.EnrollmentDate == null || e.EnrollmentDate.Value <= dateTo)
                    && (e.DismissalDate == null || e.DismissalDate.Value >= dateForm)
                );
            }
            else
                employees = employeeRepository.GetAll(e => e.DepartmentID.Value == departmentId);
            return employees;
        }

        private IList<Employee> GetEmployeesInDepartmentHierarchy(IDepartmentRepository departmentRepository, IEmployeeRepository employeeRepository, int departmentId, DateTimeRange? dateRange)
        {
            // TODO: кривая реализация, несколкьо запросов в БД, можно сделать одним запросов с использованием иерархических запросов / CTE
            var employees = GetEmployeesInOneDepartment(employeeRepository, departmentId, dateRange);
            var childDepartmentsIds = departmentRepository.GetQueryable().Where(d => d.ParentDepartmentID.Value == departmentId).Select(d => d.ID).ToList();
            if (childDepartmentsIds.Count > 0)
            {
                var allEmployees = new List<Employee>(employees);
                foreach (int childDepartmentId in childDepartmentsIds)
                {
                    var childDepartmentEmployees = GetEmployeesInDepartmentHierarchy(departmentRepository, employeeRepository, childDepartmentId, dateRange);
                    allEmployees.AddRange(childDepartmentEmployees);
                }
                employees = allEmployees;
            }
            return employees;
        }
    }
}