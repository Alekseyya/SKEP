using System.Security.Principal;
using Core.Models.RBAC;

namespace Core.BL.Interfaces
{
   public interface IPermissionValidatorService
   {
       bool HasAccess(IPrincipal contextUser, Operation operation);
       bool HasAccess(IPrincipal contextUser, OperationSet operationSet);
       bool IsDepartmentManager(IPrincipal contextUser, int? departmentID);
       bool IsDepartmentManagerForEmployee(IPrincipal contextUser, int? employeeID);
       bool HasAccessToEmployeeUpdate(IPrincipal contextUser, int employeeID);
       bool HaveAccessToEmployeeHRView(IPrincipal contextUser, int employeeID);
   }
}
