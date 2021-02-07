using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Filters;


namespace MainApp.Common
{
    public class ReturnUrlController : IActionFilter
    {
        private readonly IHttpContextAccessor _htmlContextAccessor;

        public ReturnUrlController(IHttpContextAccessor htmlContextAccessor)
        {
            _htmlContextAccessor = htmlContextAccessor ?? throw new ArgumentNullException(nameof(htmlContextAccessor));
        }

        private readonly Dictionary<string, string> _parameterDictionary = new Dictionary<string, string>();

        public void OnActionExecuted(ActionExecutedContext filterContext)
        {
            var urlCookieName = "UrlCookie" + filterContext.RouteData.Values["controller"] +
                                filterContext.RouteData.Values["action"];

            //Удаление куков если они были заполнены
            if (_htmlContextAccessor.HttpContext.Request.Cookies[urlCookieName] != null)
            {
                var cookie = new CookieOptions { Expires = DateTime.Now.AddDays(-1) };
                _htmlContextAccessor.HttpContext.Response.Cookies.Append(urlCookieName,"true", cookie);
            }

            //получение юрла без ? 
            var url = string.Empty;
            foreach (var parameter in _parameterDictionary)
            {
                if(!string.IsNullOrEmpty(parameter.Value))
                    url += parameter.Key + "=" + parameter.Value + "&";
            }
            //Без последнего символа - без &
            if (!string.IsNullOrEmpty(url))
            {
                url = url.Remove(url.Length - 1);
                var newCookie = new CookieOptions() { Expires = DateTime.Now.AddDays(1) };
                _htmlContextAccessor.HttpContext.Response.Cookies.Append(urlCookieName, "?" + url, newCookie);
            }
        }

        
        public void OnActionExecuting(ActionExecutingContext filterContext)
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