using System;
using System.Security.Principal;
using Core.BL.Interfaces;
using Core.Models.RBAC;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;

namespace WebApi.RBAC.Attributes
{
    [AttributeUsage(AttributeTargets.Method, Inherited = true, AllowMultiple = true)]
    public class OperationApiActionFilter : ActionFilterAttribute
    {
        public OperationApiActionFilter(string opname)
        {
            _opname = opname;
        }

        protected string _opname { get; set; }

        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            Operation operation = null;

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
                filterContext.Result = new StatusCodeResult(StatusCodes.Status403Forbidden);
            }
            else
            {
                try
                {
                    var permissionValidatorService = filterContext.HttpContext.RequestServices.GetService<IPermissionValidatorService>();
                    IPrincipal user = filterContext.HttpContext.User;
                    if (!permissionValidatorService.HasAccess(user, operation))
                        filterContext.Result = new StatusCodeResult(StatusCodes.Status403Forbidden);
                }
                catch
                {
                    filterContext.Result = new StatusCodeResult(StatusCodes.Status403Forbidden);
                }
            }
        }
    }
}
