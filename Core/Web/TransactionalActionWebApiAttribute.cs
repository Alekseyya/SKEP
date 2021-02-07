using System;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Core.Web
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public class TransactionalActionWebApiAttribute : ActionFilterAttribute
    {
        private bool _rollbackOnStatus;

        public TransactionalActionWebApiAttribute() : this(false)
        { }

        public TransactionalActionWebApiAttribute(bool rollbackOnStatus)
        {
            _rollbackOnStatus = rollbackOnStatus;
        }

        public override void OnActionExecuting(ActionExecutingContext actionContext)
        {
            //var resolver = GlobalConfiguration.Configuration.DependencyResolver;
            //var uow = (IUnitOfWork)resolver.GetService(typeof(IUnitOfWork));
            //uow.EnsureTransaction();
        }

        public override void OnActionExecuted(ActionExecutedContext actionExecutedContext)
        {
            //var resolver = GlobalConfiguration.Configuration.DependencyResolver;
            //var uow = (IUnitOfWork)resolver.GetService(typeof(IUnitOfWork));
            //if (actionExecutedContext.Exception != null)
            //    uow.RollbackTransaction();
            //else if (_rollbackOnStatus && (int)actionExecutedContext.ActionContext.Response.StatusCode >= 400)
            //    uow.RollbackTransaction();
            //else
            //{
            //    try
            //    {
            //        uow.CommitTransaction();
            //    }
            //    catch
            //    {
            //        uow.RollbackTransaction();
            //        throw;
            //    }
            //}
        }
    }
}
