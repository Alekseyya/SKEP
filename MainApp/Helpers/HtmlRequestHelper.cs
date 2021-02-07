
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.TagHelpers;

namespace MainApp.Helpers
{
    public static class HtmlRequestHelper
    {
        public static string Id(this IHtmlHelper htmlHelper)
        {
            var routeValues = htmlHelper.ViewContext.RouteData.Values;
            if (routeValues.ContainsKey("id"))
                return (string)routeValues["id"];

            else if (htmlHelper.ViewContext.HttpContext.Request.Query.Keys.Contains("id"))
                return htmlHelper.ViewContext.HttpContext.Request.Query["id"];

            return string.Empty;
        }
        public static string Controller(this IHtmlHelper htmlHelper)
        {
            var routeValues = htmlHelper.ViewContext.RouteData.Values;

            if (routeValues.ContainsKey("controller"))
                return (string)routeValues["controller"];

            return string.Empty;
        }
    }
}