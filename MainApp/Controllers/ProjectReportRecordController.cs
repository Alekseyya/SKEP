using System;
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


namespace MainApp.Controllers
{
    public class ProjectReportRecordController : Controller
    {
        private readonly IProjectReportRecordService _projectReportRecordService;
        private readonly IProjectService _projectService;
        private readonly IEmployeeService _employeeService;

        public ProjectReportRecordController(IProjectReportRecordService projectReportRecordService, IProjectService projectService, IEmployeeService employeeService)
        {
            _projectReportRecordService = projectReportRecordService;
            _projectService = projectService;
            _employeeService = employeeService;
        }

        [OperationActionFilter(nameof(Operation.AdminFullAccess))]
        public ActionResult Index()
        {
            var projectReportRecords = _projectReportRecordService.Get(x => x.Include(p => p.Project).ToList());
            return View(projectReportRecords);
        }

        [OperationActionFilter(nameof(Operation.AdminFullAccess))]
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return StatusCode(StatusCodes.Status400BadRequest);
            }
            ProjectReportRecord projectReportRecord = _projectReportRecordService.GetById(id.Value);
            if (projectReportRecord == null)
            {
                return StatusCode(StatusCodes.Status404NotFound);
            }
            return View(projectReportRecord);
        }

        [OperationActionFilter(nameof(Operation.AdminFullAccess))]
        public ActionResult Create()
        {
            ViewBag.ProjectID = new SelectList(_projectService.Get(x => x.ToList()), "ID", "ShortName");
            ViewBag.EmployeeID = new SelectList(_employeeService.Get(x => x.ToList().OrderBy(p => p.FullName).ToList()), "ID", "FullName");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [OperationActionFilter(nameof(Operation.AdminFullAccess))]
        public ActionResult Create(ProjectReportRecord projectReportRecord)
        {
            if (ModelState.IsValid)
            {
                _projectReportRecordService.Add(projectReportRecord);
                return RedirectToAction("Index");
            }

            ViewBag.ProjectID = new SelectList(_projectService.Get(x => x.ToList()), "ID", "ShortName", projectReportRecord.ProjectID);
            ViewBag.EmployeeID = new SelectList(_employeeService.Get(x => x.ToList().OrderBy(p => p.FullName).ToList()), "ID", "FullName", projectReportRecord.EmployeeID);
            return View(projectReportRecord);
        }

        [OperationActionFilter(nameof(Operation.AdminFullAccess))]
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return StatusCode(StatusCodes.Status400BadRequest);
            }

            ProjectReportRecord projectReportRecord = _projectReportRecordService.GetById(id.Value);
            if (projectReportRecord == null)
            {
                return StatusCode(StatusCodes.Status404NotFound);
            }
            ViewBag.ProjectID = new SelectList(_projectService.Get(x => x.ToList()), "ID", "ShortName", projectReportRecord.ProjectID);
            ViewBag.EmployeeID = new SelectList(_employeeService.Get(x => x.ToList().OrderBy(p => p.FullName).ToList()), "ID", "FullName", projectReportRecord.EmployeeID);
            return View(projectReportRecord);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [OperationActionFilter(nameof(Operation.AdminFullAccess))]
        public ActionResult Edit(ProjectReportRecord projectReportRecord)
        {
            if (ModelState.IsValid)
            {
                _projectReportRecordService.Update(projectReportRecord);
                return RedirectToAction("Index");
            }
            ViewBag.ProjectID = new SelectList(_projectService.Get(x => x.ToList()), "ID", "ShortName", projectReportRecord.ProjectID);
            ViewBag.EmployeeID = new SelectList(_employeeService.Get(x => x.ToList().OrderBy(p => p.FullName).ToList()), "ID", "FullName", projectReportRecord.EmployeeID);
            return View(projectReportRecord);
        }

        [OperationActionFilter(nameof(Operation.AdminFullAccess))]
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return StatusCode(StatusCodes.Status400BadRequest);
            }

            ProjectReportRecord projectReportRecord = _projectReportRecordService.GetById(id.Value);
            if (projectReportRecord == null)
            {
                return StatusCode(StatusCodes.Status404NotFound);
            }
            return View(projectReportRecord);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [OperationActionFilter(nameof(Operation.AdminFullAccess))]
        public ActionResult DeleteConfirmed(int id)
        {
            ProjectReportRecord projectReportRecord = _projectReportRecordService.GetById(id);
            _projectReportRecordService.Delete(projectReportRecord.ID);
            return RedirectToAction("Index");
        }
    }
}
