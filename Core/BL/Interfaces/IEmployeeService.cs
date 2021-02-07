using System;
using System.Collections.Generic;
using System.Linq;
using Core.Models;


namespace Core.BL.Interfaces
{
    public interface IEmployeeService : IServiceBase<Employee, int>
    {
        IList<Employee> FindEmployees(string searchString);
        Employee FindEmployeeByFullName(string searchFullName);
        Employee GetEmployeeByLogin(string userLogin);
        void UpdateWithoutVersion(Employee employee);
        IList<Employee> GetCurrentEmployees(DateTimeRange dateRange);

        IList<Employee> GetAllEmployees();
        IList<Employee> GetAllManagedEmployees(IList<Department> listDepartments);

        string GetADEmployeeIDByEmployeeIDInDB(int id);
        string GetEmployeeTitleByADEmployeeID(string userADEmployeeID);
        string GetADEmployeeIDBySearchString(string searchString);

        IList<Employee> GetEmployeesInDepartment(int departmentId);

        IList<Employee> GetEmployeesInDepartment(int departmentId, DateTimeRange dateRange);

        IList<Employee> GetEmployeesInDepartment(int departmentId, bool includeChildDepratments);

        IList<Employee> GetEmployeesInDepartment(int departmentId, DateTimeRange dateRange, bool includeChildDepratments);

    }
}
