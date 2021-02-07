using System;
using System.DirectoryServices.ActiveDirectory;
using System.Linq;
using Core.BL.Interfaces;
using Core.Helpers;
using Core.Models;
using Core.Models.RBAC;
using Core.Web;
using MainApp.RBAC.Attributes;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Caching.Memory;



using X.PagedList;


namespace MainApp.Controllers
{
    public class RPCSUserController : Controller
    {
        private IUserService _userService;
        private readonly IMemoryCache _memoryCache;

        public RPCSUserController(IUserService userService, IMemoryCache memoryCache)
        {
            _userService = userService ?? throw new ArgumentNullException(nameof(userService));
            _memoryCache = memoryCache ?? throw new ArgumentNullException(nameof(memoryCache));
        }

        [OperationActionFilter(nameof(Operation.RPCSUserView))]
        public ActionResult Index(string searchString = null, int? page = null)
        {
            int pageSize = 10;
            int pageNumber = !page.HasValue ? 1 : page.Value;

            string domainNetbiosName = "";

            try
            {
                domainNetbiosName = ADHelper.GetDomainNetbiosName(Domain.GetCurrentDomain());
            }
            catch (Exception)
            {

            }

            // пока получаем всех пользователей из-за специфики работы контрола RPCSAutocompleteSearchControl - в будущем сделать по нормальному!!!
            var users = _userService.GetList().AsEnumerable();
            if (!String.IsNullOrEmpty(searchString))
            {
                searchString = searchString.Trim();
                var filteredUsers = users.Where(u => u.UserLogin.Contains(searchString));
                // если по логину не наши, ищем по имени
                if (filteredUsers.Count() == 0)
                {
                    var adUsers = ADHelper.GetUserLoginsByName(searchString);  //Не хардкодим имя домена!!! .Select(au => @"ad\" + au);

                    if (adUsers != null)
                    {
                        var adUsersWithDomainName = adUsers.Select(au => domainNetbiosName + "\\" + au);
                        filteredUsers = users.Where(u => adUsersWithDomainName.Any(au => au.Equals(u.UserLogin, StringComparison.OrdinalIgnoreCase)));
                    }
                }
                users = filteredUsers;
            }
            users = users.OrderBy(u => u.UserLogin);
            ViewBag.SearchUsers = new SelectList(users, "ID", "UserLogin");
            ViewBag.SearchString = searchString;

            // users = users.Skip(pageSize * (pageNumber - 1)).Take(pageSize).ToList();
            return View(users.ToPagedList(pageNumber, pageSize));
        }

        [OperationActionFilter(nameof(Operation.RPCSUserView))]
        public ActionResult Details(int? id)
        {
            if (id == null)
                return StatusCode(StatusCodes.Status400BadRequest);

            var rpcsUser = _userService.GetById(id.Value);
            if (rpcsUser == null)
                 return StatusCode(StatusCodes.Status404NotFound);

            ViewBag.UserLogin = rpcsUser.UserLogin;
            return View(rpcsUser);
        }

        [OperationActionFilter(nameof(Operation.RPCSUserCreateUpdate))]
        public ActionResult Create()
        {
            return View();
        }

        [OperationActionFilter(nameof(Operation.RPCSUserCreateUpdate))]
        [HttpPost]
        [ValidateAntiForgeryToken]
        [TransactionalActionMvc]
        public ActionResult Create(RPCSUser rpcsUser)
        {
            if (ModelState.IsValid)
            {
                _userService.Add(rpcsUser);
                return RedirectToAction("Index");
            }

            return View(rpcsUser);
        }

        [OperationActionFilter(nameof(Operation.RPCSUserCreateUpdate))]
        public ActionResult Edit(int? id)
        {
            if (id == null)
                return StatusCode(StatusCodes.Status400BadRequest);

            var rpcsUser = _userService.GetById(id.Value);
            if (rpcsUser == null)
                 return StatusCode(StatusCodes.Status404NotFound);

            return View(rpcsUser);
        }

        [OperationActionFilter(nameof(Operation.RPCSUserCreateUpdate))]
        [HttpPost]
        [ValidateAntiForgeryToken]
        [TransactionalActionMvc]
        public ActionResult Edit(RPCSUser rpcsUser)
        {
            if (ModelState.IsValid)
            {
                _userService.Update(rpcsUser);

                string cacheKey = rpcsUser.UserLogin.ToLower();
                if (_memoryCache.Get(cacheKey) is ApplicationUser)
                    _memoryCache.Remove(cacheKey);

                return RedirectToAction("Index");
            }
            return View(rpcsUser);
        }

        [OperationActionFilter(nameof(Operation.RPCSUserDelete))]
        public ActionResult Delete(int? id)
        {
            if (id == null)
                return StatusCode(StatusCodes.Status400BadRequest);

            var rpcsUser = _userService.GetById(id.Value);
            if (rpcsUser == null)
                 return StatusCode(StatusCodes.Status404NotFound);

            return View(rpcsUser);
        }

        [OperationActionFilter(nameof(Operation.RPCSUserDelete))]
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [TransactionalActionMvc]
        public ActionResult DeleteConfirmed(int id)
        {
            _userService.Delete(id);
            return RedirectToAction("Index");
        }
    }
}
