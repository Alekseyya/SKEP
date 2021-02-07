using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Threading.Tasks;
using Core.BL.Interfaces;
using Core.Models.RBAC;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;



namespace MainApp.RBAC.Attributes
{
    [AttributeUsage(AttributeTargets.Method, Inherited = true, AllowMultiple = false)]
    public class ATSHoursRecordCreateUpdateMyHours : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            try
            {
                var permissionValidatorService = filterContext.HttpContext.RequestServices.GetService<IPermissionValidatorService>();
                var applicationUserService = filterContext.HttpContext.RequestServices.GetService<IApplicationUserService>();
                IPrincipal user = filterContext.HttpContext.User;
                if (!permissionValidatorService.HasAccess(user, Operation.TSHoursRecordCreateUpdateMyHours) || applicationUserService.GetEmployeeID() == 0)
                    filterContext.Result = NoPermissionResult.Generate();
            }
            catch
            {
                filterContext.Result = NoPermissionResult.Generate();
            }
        }

    }

    [AttributeUsage(AttributeTargets.Method, Inherited = true, AllowMultiple = false)]
    public class ATSHoursRecordPMApproveHours : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            try
            {
                var permissionValidatorService = filterContext.HttpContext.RequestServices.GetService<IPermissionValidatorService>();
                var applicationUserService = filterContext.HttpContext.RequestServices.GetService<IApplicationUserService>();
                IPrincipal user = filterContext.HttpContext.User;
                if (!permissionValidatorService.HasAccess(user, Operation.TSHoursRecordPMApproveHours) || applicationUserService.GetEmployeeID() == 0)
                    filterContext.Result = NoPermissionResult.Generate();
            }
            catch
            {
                filterContext.Result = NoPermissionResult.Generate();
            }
        }

    }
}
