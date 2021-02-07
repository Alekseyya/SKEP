using System;
using System.Diagnostics;
using Core.BL.Interfaces;
using Core.Models;
using MainApp.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;




namespace MainApp.Controllers
{
    public class HomeController : Controller
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IApplicationUserService _applicationUserService;

        public HomeController(IHttpContextAccessor httpContextAccessor, IApplicationUserService applicationUserService)
        {
            _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
            _applicationUserService = applicationUserService;
        }

        public ActionResult Index()
        {
            return View();
        }

        public ActionResult About()
        {
            ViewBag.Message = "Система контроля исполнения проектов";

            return View();
        }

        public ActionResult Tiles()
        {
            return View();
        }

        public ActionResult Support()
        {
            ViewBag.Message = "Техническая поддержка - ";

            return View();
        }

        public ActionResult Timesheet()
        {
            ViewBag.Message = "Справка по Timesheet - ";

            return View();
        }

        public ActionResult Error()
        {
            return View(new ErrorViewModel
            { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        //TODO Переделать!!!
        public ActionResult LoginAsAnotherUser()
        {
            #region Староя заколнение кукув

            ApplicationUser user = _applicationUserService.GetUser();

            if (user != null)
            {
                _applicationUserService.ClearOOPassword();
            }

            //HttpCookie cookie = Request.Cookies["TSWA-Last-User"];
            var cookieOptions = new CookieOptions();
            var cookieValueFromContext = Request.Cookies["TSWA-Last-User"];

            if (User.Identity.IsAuthenticated == false || string.IsNullOrEmpty(cookieValueFromContext) || StringComparer.OrdinalIgnoreCase.Equals(User.Identity.Name, cookieValueFromContext))
            {
                string name = string.Empty;

                if (User.Identity.IsAuthenticated)
                {
                    name = User.Identity.Name;
                }

                cookieOptions = new CookieOptions();
                _httpContextAccessor.HttpContext.Response.Cookies.Append("TSWA-Last-User", name, cookieOptions);

                _httpContextAccessor.HttpContext.Response.Headers.Append("Connection", "close");
                _httpContextAccessor.HttpContext.Response.StatusCode = 401; // Unauthorized;
                _httpContextAccessor.HttpContext.Response.Clear();
                //should probably do a redirect here to the unauthorized/failed login page
                //if you know how to do this, please tap it on the comments below

                //var bytes = Encoding.UTF8.GetBytes("Unauthorized. Reload the page to try again...");
                //_httpContextAccessor.HttpContext.Response.Body.Seek(_httpContextAccessor.HttpContext.Response.Body.Length, SeekOrigin.Begin);
                //_httpContextAccessor.HttpContext.Response.Body.Write(bytes, 0, bytes.Length);

                _httpContextAccessor.HttpContext.Response.WriteAsync("Для входа в систему необходимо ввести данные Вашей учетной записи. Перезагрузите страницу и попробуйте еще раз.");
                //_httpContextAccessor.HttpContext.Response.StatusCode = StatusCodes.Status200OK;


                return RedirectToAction("Index");
            }

            cookieOptions = new CookieOptions()
            {
                Expires = DateTime.Now.AddYears(-5)
            };

            _httpContextAccessor.HttpContext.Response.Cookies.Append("TSWA-Last-User", string.Empty, cookieOptions);

            return RedirectToAction("Index");

            #endregion
        }

        public ActionResult LogOut()
        {
            ViewBag.Message = "Выход из системы - ";

            return View();
        }

        public ActionResult ForceLogOut()
        {
            ApplicationUser user = _applicationUserService.GetUser();

            if (user != null)
            {
                _applicationUserService.ClearOOPassword();
            }

            //HttpCookie cookie = Request.Cookies["TSWA-Last-User"];
            var cookieOptions = new CookieOptions();
            var cookieValueFromContext = Request.Cookies["TSWA-Last-User"];

            if (User.Identity.IsAuthenticated == false || string.IsNullOrEmpty(cookieValueFromContext) || StringComparer.OrdinalIgnoreCase.Equals(User.Identity.Name, cookieValueFromContext))
            {
                string name = string.Empty;

                if (User.Identity.IsAuthenticated)
                {
                    name = User.Identity.Name;
                }

                cookieOptions = new CookieOptions();
                _httpContextAccessor.HttpContext.Response.Cookies.Append("TSWA-Last-User", name, cookieOptions);

                _httpContextAccessor.HttpContext.Response.Headers.Append("Connection", "close");
                _httpContextAccessor.HttpContext.Response.StatusCode = 401; // Unauthorized;
                _httpContextAccessor.HttpContext.Response.Clear();
                //should probably do a redirect here to the unauthorized/failed login page
                //if you know how to do this, please tap it on the comments below

                //var bytes = Encoding.UTF8.GetBytes("Unauthorized. Reload the page to try again...");
                //_httpContextAccessor.HttpContext.Response.Body.Seek(_httpContextAccessor.HttpContext.Response.Body.Length, SeekOrigin.Begin);
                //_httpContextAccessor.HttpContext.Response.Body.Write(bytes, 0, bytes.Length);

                _httpContextAccessor.HttpContext.Response.WriteAsync("Вы вышли из системы. Закройте вкладку браузера.");
                //_httpContextAccessor.HttpContext.Response.StatusCode = StatusCodes.Status200OK;


                return RedirectToAction("Index");
            }

            cookieOptions = new CookieOptions()
            {
                Expires = DateTime.Now.AddYears(-5)
            };

            _httpContextAccessor.HttpContext.Response.Cookies.Append("TSWA-Last-User", string.Empty, cookieOptions);

            return RedirectToAction("Index");
        }

    }
}