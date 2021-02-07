using System.Linq;
using Core.BL.Interfaces;
using Core.Models;
using Core.Models.RBAC;
using MainApp.RBAC.Attributes;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;






namespace MainApp.Controllers
{
    public class AppPropertyController : Controller
    {
        private readonly IAppPropertyService _appPropertyService;

        public AppPropertyController(IAppPropertyService appPropertyService)
        {
            _appPropertyService = appPropertyService;
        }

        [OperationActionFilter(nameof(Operation.AppPropertiesAccess))]
        public ActionResult Index()
        {
            return View(_appPropertyService.Get(x => x.ToList()));
        }

        [OperationActionFilter(nameof(Operation.AppPropertiesAccess))]
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return StatusCode(StatusCodes.Status404NotFound);
            }
            var appProperty = _appPropertyService.GetById(id.Value);
            if (appProperty == null)
            {
                return StatusCode(StatusCodes.Status404NotFound);
            }
            return View(appProperty);
        }

        [OperationActionFilter(nameof(Operation.AppPropertiesAccess))]
        public ActionResult Create()
        {
            return View();
        }

        [OperationActionFilter(nameof(Operation.AppPropertiesAccess))]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(AppProperty appProperty)
        {
            if (ModelState.IsValid)
            {
                _appPropertyService.Add(appProperty);
                return RedirectToAction("Index");
            }

            return View(appProperty);
        }

        [OperationActionFilter(nameof(Operation.AppPropertiesAccess))]
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return StatusCode(StatusCodes.Status400BadRequest);
            }
            AppProperty appProperty = _appPropertyService.GetById((int)id);
            if (appProperty == null)
            {
                return StatusCode(StatusCodes.Status404NotFound);
            }
            return View(appProperty);
        }

        [OperationActionFilter(nameof(Operation.AppPropertiesAccess))]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(AppProperty appProperty)
        {
            if (ModelState.IsValid)
            {
                _appPropertyService.Update(appProperty);
                return RedirectToAction("Index");
            }
            return View(appProperty);
        }

        [OperationActionFilter(nameof(Operation.AppPropertiesAccess))]
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return StatusCode(StatusCodes.Status400BadRequest);
            }
            AppProperty appProperty = _appPropertyService.GetById((int)id);
            if (appProperty == null)
            {
                return StatusCode(StatusCodes.Status404NotFound);
            }
            return View(appProperty);
        }

        [OperationActionFilter(nameof(Operation.AppPropertiesAccess))]
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            AppProperty appProperty = _appPropertyService.GetById((int)id);
            _appPropertyService.Delete(appProperty.ID);
            return RedirectToAction("Index");
        }
    }
}
