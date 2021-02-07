using System;
using System.Security.Principal;
using Core.BL.Interfaces;
using Core.Data;
using Core.Models.RBAC;


namespace BL.Implementation
{
    public class PermissionValidatorService : IPermissionValidatorService
    {
        private readonly IApplicationUserService _applicationUserService;
        //Todo Убрал штуку, возможно понадобится RepositoryAwareServiceBase для конектса
        public PermissionValidatorService(IRepositoryFactory repositoryFactory, IApplicationUserService applicationUserService)/* : base(repositoryFactory)*/
        {
            _applicationUserService = applicationUserService ?? throw new ArgumentNullException(nameof(applicationUserService));
        }

        //TODO Параметры contextUser не нужны по сути
        public bool HasAccess(IPrincipal contextUser, Operation operation)
        {
            bool result = false;
            //ApplicationUser user = UsersFactory.Instance.GetUser(contextUser);
            result = _applicationUserService.HasAccess(operation);
            return result;
        }

        public bool HasAccess(IPrincipal contextUser, OperationSet operationSet)
        {
            bool result = false;
            result = _applicationUserService.HasAccess(operationSet);
            return result;
        }

        public bool IsDepartmentManager(IPrincipal contextUser, int? departmentID)
        {
            if (departmentID == null || !departmentID.HasValue) return false;

            bool result = false;
            result = _applicationUserService.IsDepartmentManager(departmentID.Value);
            return result;
        }

        public bool IsDepartmentManagerForEmployee(IPrincipal contextUser, int? employeeID)
        {
            if (employeeID == null || !employeeID.HasValue) return false;

            bool result = false;
            result = _applicationUserService.IsDepartmentManagerForEmployee(employeeID.Value);
            return result;
        }

        public bool HasAccessToEmployeeUpdate(IPrincipal contextUser, int employeeID)
        {
            bool canUpdate = _applicationUserService.HasAccess(Operation.EmployeeCreateUpdate) ||
                             _applicationUserService.HasAccess(Operation.EmployeeADUpdate) ||
                             _applicationUserService.HasAccess(Operation.EmployeeIdentityDocsUpdate);


            bool isCurrentEmployee = _applicationUserService.IsCurrent(employeeID) && _applicationUserService.HasAccess(Operation.EmployeeSelfUpdate);

            return canUpdate || isCurrentEmployee;
        }

        public bool HaveAccessToEmployeeHRView(IPrincipal contextUser, int employeeID)
        {
            bool canView = _applicationUserService.HasAccess(Operation.EmployeeView);
            return canView;
        }
    }
}
