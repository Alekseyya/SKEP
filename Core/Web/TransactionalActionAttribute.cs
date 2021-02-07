using System;

namespace Core.Web
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
    public class TransactionalActionAttribute : Attribute
    {
    }
}
