using System;
using System.Collections.Generic;
using System.Linq;
using Core;
using Core.BL;
using Core.BL.Interfaces;
using Core.Models;
using Core.Models.RBAC;
using Core.RecordVersionHistory;
using MainApp.RBAC.Attributes;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;







namespace MainApp.Controllers
{
    public class DepartmentController : Controller
    {
        private readonly IDepartmentService _departmentService;
        private readonly IEmployeeService _employeeService;
        private readonly IOrganisationService _organisationService;
        private readonly IUserService _userService;
        private readonly IServiceService _serviceService;

        public DepartmentController(IDepartmentService departmentService,
            IEmployeeService employeeService,
            IOrganisationService organisationService, IUserService userService, IServiceService serviceService)
        {
            _departmentService = departmentService;
            _employeeService = employeeService;
            _organisationService = organisationService;
            _userService = userService;
            _serviceService = serviceService;
        }

        [OperationActionFilter(nameof(Operation.DepartmentListView))]
        public ActionResult Index()
        {
            return View(_departmentService.Get(x => x.Include(d => d.ParentDepartment).ToList().OrderBy(d => d.ShortName).ToList()));
        }

        [OperationActionFilter(nameof(Operation.DepartmentView))]
        public ActionResult Details(int? id, int? version)
        {
            if (id == null)
            {
                return StatusCode(StatusCodes.Status400BadRequest);
            }
            if (version != null && version.HasValue)
            {
                int departmentID = id.Value;
                Department depsVersion = _departmentService.Get(d => d.Where(x => x.ItemID == departmentID
                                                                                   && ((x.VersionNumber ==
                                                                                        version.Value)
                                                                                       || (version.Value == 0 &&
                                                                                           x.VersionNumber == null)))
                    .ToList(), GetEntityMode.VersionAndOther).FirstOrDefault();
                if (depsVersion == null)
                     return StatusCode(StatusCodes.Status404NotFound);

                depsVersion.EmployeesInDepartment = GetEmployeesInDepartment(departmentID);
                depsVersion.Versions = new List<Department>().AsEnumerable();
                return View(depsVersion);
            }
            else
            {
                Department department = _departmentService.GetById(id.Value);
                if (department == null)
                {
                     return StatusCode(StatusCodes.Status404NotFound);
                }
                department.EmployeesInDepartment = GetEmployeesInDepartment(department.ID);

                department.Versions = _departmentService.Get(x => x
                    .Where(p => /*p.IsVersion == true &&*/ p.ItemID == department.ID || p.ID == department.ID)
                    .OrderByDescending(p => p.VersionNumber).ToList(), GetEntityMode.VersionAndOther);

                int versionsCount = department.Versions.Count();
                for (int i = 0; i < versionsCount; i++)
                {

                    if (i == versionsCount - 1)
                        continue;

                    var changes = ChangedRecordsFiller.GetChangedData(department.Versions.ElementAt(i), department.Versions.ElementAt(i + 1));
                    department.Versions.ElementAt(i).ChangedRecords = changes;
                }

                return View(department);
            }
        }

        private ICollection<Employee> GetEmployeesInDepartment(int departmentID)
        {
            var employeeList = _employeeService.GetCurrentEmployees(new DateTimeRange(DateTime.Today, DateTime.Today)).ToList()
                .Where(e => e.DepartmentID == departmentID)
                .ToList()
                .OrderBy(e => e.Department.ShortName + e.FullName).ToArray();
            foreach (var department in _departmentService.Get(d => d.Where(x => x.ParentDepartmentID == departmentID).ToList()))
            {
                var result = GetEmployeesInDepartment(department.ID);
                employeeList = employeeList.Concat(result).ToArray();
            }

            return employeeList;
        }

        [OperationActionFilter(nameof(Operation.DepartmentCreateUpdate))]
        public ActionResult Create()
        {
            ViewBag.ParentDepartmentID = new SelectList(_departmentService.Get(x => x.ToList().OrderBy(d => d.ShortName).ToList()), "ID", "FullName");
            ViewBag.OrganisationID = new SelectList(_organisationService.Get(x => x.ToList().OrderBy(o => o.ShortName).ToList()), "ID", "FullName");
            ViewBag.DepartmentManagerID = new SelectList(_employeeService.Get(x => x.ToList().OrderBy(e => e.LastName).ToList()), "ID", "FullName");
            ViewBag.DepartmentManagerAssistantID = new SelectList(_employeeService.Get(x => x.ToList().OrderBy(e => e.LastName).ToList()), "ID", "FullName");
            ViewBag.DepartmentPAID = new SelectList(_employeeService.Get(x => x.ToList().OrderBy(e => e.LastName).ToList()), "ID", "FullName");

            return View();
        }

        [OperationActionFilter(nameof(Operation.DepartmentCreateUpdate))]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(Department department)
        {
            if (ModelState.IsValid)
            {
                _departmentService.Add(department);
                return RedirectToAction("Index");
            }

            ViewBag.ParentDepartmentID = new SelectList(_departmentService.Get(x => x.ToList().OrderBy(d => d.ShortName).ToList()), "ID", "FullName", department.ParentDepartmentID);
            ViewBag.OrganisationID = new SelectList(_organisationService.Get(x => x.ToList().OrderBy(o => o.ShortName).ToList()), "ID", "FullName", department.OrganisationID);
            ViewBag.DepartmentManagerID = new SelectList(_employeeService.Get(x => x.ToList().OrderBy(e => e.LastName).ToList()), "ID", "FullName", department.DepartmentManagerID);
            ViewBag.DepartmentManagerAssistantID = new SelectList(_employeeService.Get(x => x.ToList().OrderBy(e => e.LastName).ToList()), "ID", "FullName", department.DepartmentManagerAssistantID);
            ViewBag.DepartmentPAID = new SelectList(_employeeService.Get(x => x.ToList().OrderBy(e => e.LastName).ToList()), "ID", "FullName", department.DepartmentPAID);

            return View(department);
        }

        [OperationActionFilter(nameof(Operation.DepartmentCreateUpdate))]
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return StatusCode(StatusCodes.Status400BadRequest);
            }

            Department department = _departmentService.GetById(id.Value);
            if (department == null)
            {
                 return StatusCode(StatusCodes.Status404NotFound);
            }
            if (department.IsVersion)
                return StatusCode(StatusCodes.Status403Forbidden);

            ViewBag.ParentDepartmentID = new SelectList(_departmentService.Get(x => x.Where(d => d.ID != id).OrderBy(d => d.ShortName).ToList()), "ID", "FullName", department.ParentDepartmentID);
            ViewBag.OrganisationID = new SelectList(_organisationService.Get(x => x.ToList().OrderBy(o => o.ShortName).ToList()), "ID", "FullName", department.OrganisationID);
            ViewBag.DepartmentManagerID = new SelectList(_employeeService.Get(x => x.ToList().OrderBy(e => e.LastName).ToList()), "ID", "FullName", department.DepartmentManagerID);
            ViewBag.DepartmentManagerAssistantID = new SelectList(_employeeService.Get(x => x.ToList().OrderBy(e => e.LastName).ToList()), "ID", "FullName", department.DepartmentManagerAssistantID);
            ViewBag.DepartmentPAID = new SelectList(_employeeService.Get(x => x.ToList().OrderBy(e => e.LastName).ToList()), "ID", "FullName", department.DepartmentPAID);
            return View(department);
        }

        [OperationActionFilter(nameof(Operation.DepartmentCreateUpdate))]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(Department department)
        {
            if (ModelState.IsValid)
            {
                _departmentService.Update(department);
                return RedirectToAction("Index");
            }
            ViewBag.ParentDepartmentID = new SelectList(_departmentService.Get(x => x.ToList().OrderBy(d => d.ShortName).ToList()), "ID", "FullName", department.ParentDepartmentID);
            ViewBag.OrganisationID = new SelectList(_organisationService.Get(x => x.ToList().OrderBy(o => o.ShortName).ToList()), "ID", "FullName", department.OrganisationID);
            ViewBag.DepartmentManagerID = new SelectList(_employeeService.Get(x => x.ToList().OrderBy(e => e.LastName).ToList()), "ID", "FullName", department.DepartmentManagerID);
            ViewBag.DepartmentManagerAssistantID = new SelectList(_employeeService.Get(x => x.ToList().OrderBy(e => e.LastName).ToList()), "ID", "FullName", department.DepartmentManagerAssistantID);
            ViewBag.DepartmentPAID = new SelectList(_employeeService.Get(x => x.ToList().OrderBy(e => e.LastName).ToList()), "ID", "FullName", department.DepartmentPAID);
            return View(department);
        }

        [OperationActionFilter(nameof(Operation.DepartmentDelete))]
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return StatusCode(StatusCodes.Status400BadRequest);
            }
            Department department = _departmentService.GetById(id.Value);
            if (department == null)
            {
                 return StatusCode(StatusCodes.Status404NotFound);
            }
            return View(department);
        }

        [OperationActionFilter(nameof(Operation.DepartmentDelete))]
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            Department department = _departmentService.GetById(id);
            var user = _userService.GetUserDataForVersion();
            var recycleBinInDBRelation = _serviceService.HasRecycleBinInDBRelation(department);
            if (recycleBinInDBRelation.hasRelated == false)
            {
                var recycleToRecycleBin = _departmentService.RecycleToRecycleBin(department.ID, user.Item1, user.Item2);
                if (!recycleToRecycleBin.toRecycleBin)
                {
                    ViewBag.RecycleBinError =
                        "Невозможно удалить, так как на удаляемый элемент ссылаются другие элементы в системе." +
                        "Сначала необходимо удалить элементы, которые ссылаются на данный элемент. " +
                        recycleToRecycleBin.relatedClassId;
                    return View(department);
                }
            }
            else
            {
                ViewBag.RecycleBinError =
                    "Невозможно удалить, так как на удаляемый элемент ссылаются другие элементы в системе." +
                    $"Сначала необходимо удалить элементы, которые ссылаются на данный элемент. {recycleBinInDBRelation.relatedInDBClassId}";
                return View(department);
            }
            return RedirectToAction("Index");
        }

        [OperationActionFilter(nameof(Operation.DepartmentView))]
        [HttpGet]
        public ActionResult ViewVersion(int? id)
        {
            Department department = null;

            if (id == null)
            {
                return StatusCode(StatusCodes.Status400BadRequest);
            }
            department = _departmentService.Get(x => x.Where(d => d.ID == id.Value).ToList(), GetEntityMode.VersionAndOther).FirstOrDefault();

            if (department == null)
            {
                 return StatusCode(StatusCodes.Status404NotFound);
            }

            if (!department.IsVersion)
                return StatusCode(StatusCodes.Status403Forbidden);

            // ReSharper disable once Mvc.ViewNotResolved
            return View(department);

        }
    }
}
