using System;
using Core.BL.Interfaces;
using Core.Models;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;



namespace MainApp.RBAC.Attributes
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class AEmployeeAnyUpdate : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            try
            {
                var permissionValidatorService = filterContext.HttpContext.RequestServices.GetService<IPermissionValidatorService>();
                int id = 0;
                if (filterContext.ActionArguments.ContainsKey("id"))
                    Int32.TryParse(filterContext.ActionArguments["id"].ToString(), out id);
                else if (filterContext.ActionArguments.ContainsKey("employee"))
                {
                    Employee val = filterContext.ActionArguments["employee"] as Employee;
                    if (val != null)
                        id = val.ID;
                }
                if (permissionValidatorService.HasAccessToEmployeeUpdate(filterContext.HttpContext.User, id))
                    return;
                else
                    filterContext.Result = NoPermissionResult.Generate();
            }
            catch
            {
                filterContext.Result = NoPermissionResult.Generate();
            }
        }
    }

}