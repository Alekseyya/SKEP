using System;
using System.Collections.Generic;
using Core.Models;
using Core.Models.RBAC;

namespace Core.BL.Interfaces
{
    public interface IApplicationUserService
    {
        bool IsDepartmentManager(int departmentId);
        bool IsCurrent(int employeeId);
        bool IsDepartmentManagerForEmployee(int employeeId);
        bool IsAuthenticated();
        bool IsMyProject(Project project);
        int GetEmployeeID();
        string GetOOPassword();
        string GetOOLogin();
        //ApplicationUser Init();
        ApplicationUser GetUser();
        ApplicationUser GetCurrentUser();
        bool HasAccess(Operation operation);
        bool HasAccess(OperationSet operationSet);
        Tuple<string, string> GetUserDataForVersion();
        IList<Project> GetMyProjects();
        //string GetOOPassword();
        void SetOOPassword(string ooPassword);
        void ClearOOPassword();
        bool CheckUserHasOwnOOLogin();
    }
}
