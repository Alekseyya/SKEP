using System.Linq;
using Core.BL.Interfaces;
using Core.Models;
using Core.Models.RBAC;
using MainApp.RBAC.Attributes;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;




using X.PagedList;

namespace MainApp.Controllers
{
    public class ProjectScheduleEntryTypeController : Controller
    {
        private readonly IProjectScheduleEntryTypeService _projectScheduleEntryTypeService;
        private readonly IUserService _userService;
        private readonly IProjectTypeService _projectTypeService;
        private readonly IServiceService _serviceService;
        private readonly int _pageSize = 10;

        public ProjectScheduleEntryTypeController(IProjectScheduleEntryTypeService projectScheduleEntryTypeService,
            IUserService userService,
            IProjectTypeService projectTypeService, IServiceService serviceService)
        {
            _projectScheduleEntryTypeService = projectScheduleEntryTypeService;
            _userService = userService;
            _projectTypeService = projectTypeService;
            _serviceService = serviceService;
        }
        // GET: ProjectScheduleEntryType
        [HttpGet]
        [OperationActionFilter(nameof(Operation.ProjectScheduleEntryTypeView))]
        public ActionResult Index(int? page)
        {
            page = page.HasValue ? page : 1;
            var scheduleEntryTypes = _projectScheduleEntryTypeService.Get(tList => tList
                .OrderBy(t => t.WBSCode)
                .Skip((page.Value - 1) * _pageSize)
                .Take(_pageSize)
                .ToList());
            int countItems = _projectScheduleEntryTypeService.GetCount();
            var pageList = new StaticPagedList<ProjectScheduleEntryType>(scheduleEntryTypes, page.Value, _pageSize, countItems);

            return View(pageList);
        }

        [HttpGet]
        [OperationActionFilter(nameof(Operation.ProjectScheduleEntryTypeCreateUpdate))]
        public ActionResult Create()
        {
            ViewBag.ShortNameFromList = true;

            ViewBag.ProjectTypeID = new SelectList(_projectTypeService.Get(x => x.ToList().OrderBy(pt => pt.FullName).ToList()), "ID", "FullName");

            return View(new ProjectScheduleEntryType());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [OperationActionFilter(nameof(Operation.ProjectScheduleEntryTypeCreateUpdate))]
        public ActionResult Create(ProjectScheduleEntryType projectScheduleEntryType)
        {
            if (ModelState.IsValid)
            {
                _projectScheduleEntryTypeService.Add(projectScheduleEntryType);
                return RedirectToAction("Index");
            }

            ViewBag.ProjectTypeID = new SelectList(_projectTypeService.Get(x => x.ToList().OrderBy(pt => pt.FullName).ToList()), "ID", "FullName", projectScheduleEntryType.ProjectTypeID);

            return View(projectScheduleEntryType);
        }

        [HttpGet]
        [OperationActionFilter(nameof(Operation.ProjectScheduleEntryTypeCreateUpdate))]
        public ActionResult Edit(int? id)
        {
            if (!id.HasValue)
                return StatusCode(StatusCodes.Status400BadRequest);

            var projectScheduleEntryType = _projectScheduleEntryTypeService.GetById(id.Value);
            if (projectScheduleEntryType == null)
                return StatusCode(StatusCodes.Status404NotFound);

            ViewBag.ProjectTypeID = new SelectList(_projectTypeService.Get(x => x.ToList().OrderBy(pt => pt.FullName).ToList()), "ID", "FullName", projectScheduleEntryType.ProjectTypeID);

            return View(projectScheduleEntryType);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [OperationActionFilter(nameof(Operation.ProjectScheduleEntryTypeCreateUpdate))]
        public ActionResult Edit(ProjectScheduleEntryType projectScheduleEntryType)
        {
            if (ModelState.IsValid)
            {
                projectScheduleEntryType = _projectScheduleEntryTypeService.UpdateWithoutVersion(projectScheduleEntryType);
                return RedirectToAction("Index");
            }

            ViewBag.ProjectTypeID = new SelectList(_projectTypeService.Get(x => x.ToList().OrderBy(pt => pt.FullName).ToList()), "ID", "FullName", projectScheduleEntryType.ProjectTypeID);

            return View(projectScheduleEntryType);
        }

        [HttpGet]
        [OperationActionFilter(nameof(Operation.ProjectScheduleEntryTypeView))]
        public ActionResult Details(int? id)
        {
            if (!id.HasValue)
                return StatusCode(StatusCodes.Status400BadRequest);
            var projectScheduleEntryType = _projectScheduleEntryTypeService.GetById(id.Value);
            if (projectScheduleEntryType == null)
                return StatusCode(StatusCodes.Status404NotFound);

            return View(projectScheduleEntryType);
        }

        [HttpGet]
        [OperationActionFilter(nameof(Operation.ProjectScheduleEntryTypeDelete))]
        public ActionResult Delete(int? id)
        {
            if (!id.HasValue)
                return StatusCode(StatusCodes.Status400BadRequest);

            var _projectScheduleEntryType = _projectScheduleEntryTypeService.GetById(id.Value);
            if (_projectScheduleEntryType == null)
                return StatusCode(StatusCodes.Status404NotFound);

            return View(_projectScheduleEntryType);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [OperationActionFilter(nameof(Operation.ProjectScheduleEntryTypeDelete))]
        public ActionResult DeleteConfirmed(int? id)
        {
            if (!id.HasValue)
                return StatusCode(StatusCodes.Status400BadRequest);
            var projectScheduleEntryType = _projectScheduleEntryTypeService.GetById(id.Value);
            if (projectScheduleEntryType == null)
                return StatusCode(StatusCodes.Status404NotFound);
            var user = _userService.GetUserDataForVersion();
            var recycleBinInDBRelation = _serviceService.HasRecycleBinInDBRelation(projectScheduleEntryType);
            if (recycleBinInDBRelation.hasRelated == false)
            {
                var recycleToRecycleBin = _projectScheduleEntryTypeService.RecycleToRecycleBin(projectScheduleEntryType.ID, user.Item1, user.Item2);
                if (!recycleToRecycleBin.toRecycleBin)
                {
                    ViewBag.RecycleBinError =
                        "Невозможно удалить, так как на удаляемый элемент ссылаются другие элементы в системе." +
                        "Сначала необходимо удалить элементы, которые ссылаются на данный элемент. " +
                        recycleToRecycleBin.relatedClassId;
                    return View(projectScheduleEntryType);
                }
            }
            else
            {
                ViewBag.RecycleBinError =
                    "Невозможно удалить, так как на удаляемый элемент ссылаются другие элементы в системе." +
                    $"Сначала необходимо удалить элементы, которые ссылаются на данный элемент. {recycleBinInDBRelation.relatedInDBClassId}";
                return View(projectScheduleEntryType);
            }

            return RedirectToAction("Index");
        }

    }
}
