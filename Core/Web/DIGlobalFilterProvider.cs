namespace Core.Web
{
    //public class DIGlobalFilterProvider : IFilterProvider
    //{
    //    private readonly IDependencyResolver _dependencyResolver;

    //    public DIGlobalFilterProvider(IDependencyResolver dependencyResolver)
    //    {
    //        if (dependencyResolver == null)
    //            throw new ArgumentNullException(nameof(dependencyResolver));
    //        _dependencyResolver = dependencyResolver;
    //    }

    //    public IEnumerable<Filter> GetFilters(ControllerContext controllerContext, ActionDescriptor actionDescriptor)
    //    {
    //        foreach (var filter in _dependencyResolver.GetServices<IActionFilter>())
    //            yield return new Filter(filter, FilterScope.Global, null);

    //        foreach (var filter in _dependencyResolver.GetServices<IAuthorizationFilter>())
    //            yield return new Filter(filter, FilterScope.Global, null);

    //        foreach (var filter in _dependencyResolver.GetServices<IExceptionFilter>())
    //            yield return new Filter(filter, FilterScope.Global, null);

    //        foreach (var filter in _dependencyResolver.GetServices<IResultFilter>())
    //            yield return new Filter(filter, FilterScope.Global, null);

    //        foreach (var filter in _dependencyResolver.GetServices<IAuthenticationFilter>())
    //            yield return new Filter(filter, FilterScope.Global, null);
    //    }
    //}
}
