using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;

namespace MainApp.Common
{
    public class ReturnUrlActionFilter : ActionFilterAttribute
    {
        private readonly Dictionary<string, string> _parameterDictionary = new Dictionary<string, string>();

        public override void OnActionExecuted(ActionExecutedContext filterContext)
        {
            var httpContextAccessor = filterContext.HttpContext.RequestServices.GetService<IHttpContextAccessor>();
            var urlCookieName = "UrlCookie" + filterContext.RouteData.Values["controller"] +
                                filterContext.RouteData.Values["action"];

            //Удаление куков если они были заполнены
            if (httpContextAccessor.HttpContext.Request.Cookies[urlCookieName] != null)
            {
                var cookieOptions = new CookieOptions();
                cookieOptions.Expires = DateTime.Now.AddDays(-1);
                filterContext.HttpContext.Response.Cookies.Append(urlCookieName, "",cookieOptions);
            }

            //получение юрла без ? 
            var url = string.Empty;
            foreach (var parameter in _parameterDictionary)
            {
                if (!string.IsNullOrEmpty(parameter.Value))
                    url += parameter.Key + "=" + parameter.Value + "&";
            }
            //Без последнего символа - без &
            if (!string.IsNullOrEmpty(url))
            {
                url = url.Remove(url.Length - 1);
                var newCookieOptions = new CookieOptions();
                newCookieOptions.Expires = DateTime.Now.AddDays(1);
                filterContext.HttpContext.Response.Cookies.Append(urlCookieName, "?" + url, newCookieOptions);
            }
        }


        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            //Заполняет словарь параметрами
            if (_parameterDictionary.Count == 0)
            {

                foreach (var parameter in filterContext.ActionArguments)
                {
                    _parameterDictionary.Add(parameter.Key, parameter.Value == null ? "" : parameter.Value.ToString());
                }
            }
            else
            {
                foreach (var parameter in filterContext.ActionArguments)
                {
                    _parameterDictionary[parameter.Key] = parameter.Value == null ? "" : parameter.Value.ToString();
                }
            }
        }
    }
}
