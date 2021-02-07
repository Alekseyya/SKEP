using System;
using System.Collections.Generic;
using System.Linq;
using Core.Models;


namespace Core.BL.Interfaces
{
    public interface IDepartmentService : IServiceBase<Department, int>
    {
        IList<Department> GetChildDepartments(int departmentId, bool includeChildDepratments);
        Department GetDepartmentForManager(int managerEmployeeId);
        IList<Department> GetDepartmentsForManager(int employeeId);
    }
}
