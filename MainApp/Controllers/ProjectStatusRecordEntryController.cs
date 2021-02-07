using System.Linq;
using System.Net;
using Core.BL.Interfaces;
using Core.Models;
using Core.Models.RBAC;
using MainApp.RBAC.Attributes;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;




using X.PagedList;

namespace MainApp.Controllers
{
    public class ProjectStatusRecordEntryController : Controller
    {
        private readonly IProjectStatusRecordEntryService _projectStatusRecordEntryService;
        private readonly IProjectStatusRecordService _projectStatusRecordService;
        private readonly IProjectScheduleEntryService _projectScheduleEntryService;
        private readonly int _pageSize = 10;

        public ProjectStatusRecordEntryController(IProjectStatusRecordEntryService projectStatusRecordEntryService, IProjectStatusRecordService projectStatusRecordService, IProjectScheduleEntryService projectScheduleEntryService)
        {
            _projectStatusRecordEntryService = projectStatusRecordEntryService;
            _projectStatusRecordService = projectStatusRecordService;
            _projectScheduleEntryService = projectScheduleEntryService;
        }
        [HttpGet]
        [OperationActionFilter(nameof(Operation.AdminFullAccess))]
        public ActionResult Index(int? page)
        {
            page = page.HasValue ? page : 1;
            var projectStatusRecordEntries = _projectStatusRecordEntryService.Get(pseList => pseList
                .Include(x => x.ProjectStatusRecord)
                .Include(x => x.ProjectScheduleEntry)
                .OrderBy(pse => pse.ProjectStatusRecord.StatusPeriodName)
                .Skip((page.Value - 1) * _pageSize)
                .Take(_pageSize)
                .ToList());
            int countItems = _projectStatusRecordEntryService.GetCount();
            var pageList = new StaticPagedList<ProjectStatusRecordEntry>(projectStatusRecordEntries, page.Value, _pageSize, countItems);

            return View(pageList);
        }

        [HttpGet]
        [OperationActionFilter(nameof(Operation.AdminFullAccess))]
        public ActionResult Create()
        {
            ViewBag.ProjectStatusRecords = new SelectList(_projectStatusRecordService.Get(x => x.ToList()), "ID", "FullName");
            ViewBag.ProjectScheduleEntries = new SelectList(_projectScheduleEntryService.Get(x => x.ToList()), "ID", "FullName");

            return View(new ProjectStatusRecordEntry());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [OperationActionFilter(nameof(Operation.AdminFullAccess))]
        public ActionResult Create(ProjectStatusRecordEntry projectStatusRecordEntry)
        {
            if (ModelState.IsValid)
            {
                _projectStatusRecordEntryService.Add(projectStatusRecordEntry);
            }
            ViewBag.ProjectStatusRecords = new SelectList(_projectStatusRecordService.Get(x => x.ToList()), "ID", "StatusPeriodName");
            ViewBag.ProjectScheduleEntries = new SelectList(_projectScheduleEntryService.Get(x => x.ToList()), "ID", "FullName");

            return View(projectStatusRecordEntry);
        }

        [OperationActionFilter(nameof(Operation.AdminFullAccess))]
        public ActionResult Edit(int? id)
        {
            if (id == null)
                return StatusCode(StatusCodes.Status400BadRequest);

            var projectStatusRecordEntry = _projectStatusRecordEntryService.GetById(id.Value);
            if (projectStatusRecordEntry == null)
                return StatusCode(StatusCodes.Status404NotFound);

            ViewBag.ProjectStatusRecords = new SelectList(_projectStatusRecordService.Get(x => x.ToList()), "ID", "FullName");
            ViewBag.ProjectScheduleEntries = new SelectList(_projectScheduleEntryService.Get(x => x.ToList()), "ID", "FullName");

            return View(projectStatusRecordEntry);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [OperationActionFilter(nameof(Operation.AdminFullAccess))]
        public ActionResult Edit(ProjectStatusRecordEntry projectStatusRecordEntry)
        {
            if (ModelState.IsValid)
            {
                _projectStatusRecordEntryService.Update(projectStatusRecordEntry);
                return RedirectToAction("Index");
            }
            return View(projectStatusRecordEntry);
        }

        [HttpGet]
        [OperationActionFilter(nameof(Operation.AdminFullAccess))]
        public ActionResult Details(int? id)
        {
            if (id == null)
                return StatusCode(StatusCodes.Status400BadRequest);

            var projectStatusRecordEntry = _projectStatusRecordEntryService.GetById(id.Value);

            if (projectStatusRecordEntry == null)
                return StatusCode(StatusCodes.Status404NotFound);
            return View(projectStatusRecordEntry);
        }

        [HttpGet]
        [OperationActionFilter(nameof(Operation.AdminFullAccess))]
        public ActionResult Delete(int? id)
        {
            if (id == null)
                return StatusCode(StatusCodes.Status400BadRequest);

            var projectStatusRecordEntry = _projectStatusRecordEntryService.GetById(id.Value);

            if (projectStatusRecordEntry == null)
                return StatusCode(StatusCodes.Status404NotFound);
            return View(projectStatusRecordEntry);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [OperationActionFilter(nameof(Operation.AdminFullAccess))]
        public ActionResult DeleteConfirmed(int? id)
        {
            if (id == null)
                return StatusCode(StatusCodes.Status400BadRequest);

            var projectStatusRecordEntry = _projectStatusRecordEntryService.GetById(id.Value);

            if (projectStatusRecordEntry == null)
                return StatusCode(StatusCodes.Status404NotFound);
            _projectStatusRecordEntryService.Delete(projectStatusRecordEntry.ID);
            return RedirectToAction("Index");
        }
    }
}
