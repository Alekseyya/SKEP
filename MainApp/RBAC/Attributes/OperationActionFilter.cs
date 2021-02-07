using System;
using System.Security.Principal;
using Core.BL.Interfaces;
using Core.Models.RBAC;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;



namespace MainApp.RBAC.Attributes
{

    [AttributeUsage(AttributeTargets.Method, Inherited = true, AllowMultiple = true)]
    public class OperationActionFilter : ActionFilterAttribute
    {
        public OperationActionFilter(string opname)
        {
            _opname = opname;
        }

        protected string _opname { get; set; }

        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            Operation operation = null;

            var permissionValidatorService = filterContext.HttpContext.RequestServices.GetService<IPermissionValidatorService>();
            try
            {
                operation = typeof(Operation).GetField(_opname).GetValue(null) as Operation;
            }
            catch (Exception)
            {
                operation = null;
            }

            if (operation == null)
            {
                filterContext.Result = NoPermissionResult.Generate();
            }
            else
            {
                try
                {
                    IPrincipal user = filterContext.HttpContext.User;
                    if (!permissionValidatorService.HasAccess(user, operation))
                        filterContext.Result = NoPermissionResult.Generate();
                }
                catch
                {
                    filterContext.Result = NoPermissionResult.Generate();
                }
            }
        }
    }
}

