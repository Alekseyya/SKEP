using System;
using Core.Data;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Core.Web
{
    public class TransactionalActionFilterMvc : IActionFilter
    {
        private readonly IUnitOfWork _unitOfWork;

        public TransactionalActionFilterMvc(IUnitOfWork unitOfWork)
        {
            if (unitOfWork == null)
                throw new ArgumentNullException(nameof(unitOfWork));

            _unitOfWork = unitOfWork;
        }

        public void OnActionExecuted(ActionExecutedContext filterContext)
        {
            //var attribute = GetTransactionalActionAttribute(filterContext.ActionDescriptor);
            //if (attribute != null)
            //{
            //    if (filterContext.Exception == null) // TODO: Возможно, стоит проверять на тип ответа и если это 4xx или 5xx то тоже делать rollback
            //        _unitOfWork.CommitTransaction();
            //    else
            //        _unitOfWork.RollbackTransaction();
            //}
        }

        public void OnActionExecuting(ActionExecutingContext filterContext)
        {
            //var attribute = GetTransactionalActionAttribute(filterContext.ActionDescriptor);
            //if (attribute != null)
            //{
            //    _unitOfWork.EnsureTransaction();
            //}
        }

        //public TransactionalActionAttribute GetTransactionalActionAttribute(ActionDescriptor actionDescriptor)
        //{
        //    TransactionalActionAttribute attribute = null;

        //    attribute = actionDescriptor.GetCustomAttributes(typeof(TransactionalActionAttribute), false)
        //        .Cast<TransactionalActionAttribute>()
        //        .SingleOrDefault();
        //    if (attribute != null)
        //        return attribute;

        //    attribute = actionDescriptor.ControllerDescriptor.GetCustomAttributes(typeof(TransactionalActionAttribute), false)
        //        .Cast<TransactionalActionAttribute>()
        //        .SingleOrDefault();

        //    return attribute;
        //}
    }
}
