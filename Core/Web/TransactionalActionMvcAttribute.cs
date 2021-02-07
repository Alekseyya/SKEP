using System;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Core.Web
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public class TransactionalActionMvcAttribute : ActionFilterAttribute
    {
        private bool _rollbackOnStatus;

        public TransactionalActionMvcAttribute() : this(false)
        { }

        public TransactionalActionMvcAttribute(bool rollbackOnStatus)
        {
            _rollbackOnStatus = rollbackOnStatus;
        }

        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            //if (filterContext.IsChildAction)
            //    return;

            //var resolver = DependencyResolver.Current;
            //var uow = resolver.GetService<IUnitOfWork>();
            //uow.EnsureTransaction();
        }

        public override void OnResultExecuted(ResultExecutedContext filterContext)
        {
            //if (filterContext.IsChildAction)
            //    return;

            //var resolver = DependencyResolver.Current;
            //var uow = resolver.GetService<IUnitOfWork>();
            //if (filterContext.Exception != null)
            //    uow.RollbackTransaction();
            //else if (_rollbackOnStatus && filterContext.Result is HttpStatusCodeResult && (filterContext.Result as HttpStatusCodeResult).StatusCode >= 400)
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
