using System;
using System.Linq;
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
    public class EmployeeQualifyingRoleController : Controller
    {
        private readonly IEmployeeQualifyingRoleService _employeeQualifyingRoleService;
        private readonly IEmployeeService _employeeService;
        private readonly IQualifyingRoleService _qualifyingRoleService;

        public EmployeeQualifyingRoleController(IEmployeeQualifyingRoleService employeeQualifyingRoleService, IEmployeeService employeeService, IQualifyingRoleService qualifyingRoleService)
        {
            _employeeQualifyingRoleService = employeeQualifyingRoleService;
            _employeeService = employeeService;
            _qualifyingRoleService = qualifyingRoleService;
        }

        [OperationActionFilter(nameof(Operation.EmployeeQualifyingRoleView))]
        public ActionResult Index()
        {
            var employeeQualifyingRoleAssignments = _employeeQualifyingRoleService.Get(eQ => eQ.Include(e => e.Employee).Include(e => e.QualifyingRole).ToList());
            return View(employeeQualifyingRoleAssignments);
        }

        [OperationActionFilter(nameof(Operation.EmployeeQualifyingRoleView))]
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return StatusCode(StatusCodes.Status400BadRequest);
            }
            EmployeeQualifyingRole employeeQualifyingRoleAssignment = _employeeQualifyingRoleService.GetById((int)id);
            if (employeeQualifyingRoleAssignment == null)
            {
                return StatusCode(StatusCodes.Status404NotFound);
            }
            return View(employeeQualifyingRoleAssignment);
        }

        [OperationActionFilter(nameof(Operation.EmployeeQualifyingRoleCreateUpdate))]
        public ActionResult Create(int? employeeid)
        {
            ViewBag.EmployeeID = new SelectList(_employeeService.Get(x => x.ToList()), "ID", "FullName");
            ViewBag.QualifyingRoleID = new SelectList(_qualifyingRoleService.Get(x => x.ToList()), "ID", "FullName");
            return View();
        }

        [OperationActionFilter(nameof(Operation.EmployeeQualifyingRoleCreateUpdate))]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(EmployeeQualifyingRole employeeQualifyingRoleAssignment)
        {
            if (ModelState.IsValid)
            {
                // Получаем сотрудника
                var employee = _employeeService.GetById((int)employeeQualifyingRoleAssignment.EmployeeID);

                // Получаем все УПР в отсортированном по убыванию порядке по полю закрытия
                var qualifyingRoleList = _employeeQualifyingRoleService.Get(e => e
                    .Where(x => x.EmployeeID == employeeQualifyingRoleAssignment.EmployeeID &&
                                x.QualifyingRoleDateBegin >= employee.EnrollmentDate).OrderByDescending(x => x.QualifyingRoleDateEnd).ToList());
                // Проверяем на зыкрытость УПР
                bool isOpenQualifyingRoleNotExists = (qualifyingRoleList.Where(x => x.QualifyingRoleDateEnd == null).Count() == 0);

                if (isOpenQualifyingRoleNotExists)
                {
                    //Проверка на корректный ввод данных

                    if (((employee.EnrollmentDate == null) && (employee.DismissalDate == null))
                        || ((employee.EnrollmentDate == null) && (employee.DismissalDate != null)))
                    {
                        ModelState.AddModelError("Comments", "Нельзя создать УПР.");
                    }
                    else if (employeeQualifyingRoleAssignment.QualifyingRoleDateBegin == null)
                    {
                        ModelState.AddModelError("QualifyingRoleDateBegin", "Это поле должно быть заполнено.");
                    }
                    else if (employee.EnrollmentDate > employeeQualifyingRoleAssignment.QualifyingRoleDateBegin)
                    {
                        ModelState.AddModelError("QualifyingRoleDateBegin", "В это время сотрудник не работал.");
                    }
                    else if (employeeQualifyingRoleAssignment.QualifyingRoleDateBegin >= employeeQualifyingRoleAssignment.QualifyingRoleDateEnd)
                    {
                        ModelState.AddModelError("QualifyingRoleDateEnd", "Дата окончания не может быть меньше или равна дате назначения.");
                    }
                    else if (employee.DismissalDate < employeeQualifyingRoleAssignment.QualifyingRoleDateEnd)
                    {
                        ModelState.AddModelError("QualifyingRoleDateEnd", "В это время сотрудник был уволен.");
                    }
                    else
                    {
                        DateTime endDate;

                        if (qualifyingRoleList.Count == 0)
                        {
                            endDate = employee.EnrollmentDate.Value;
                        }
                        else
                        {
                            endDate = qualifyingRoleList.FirstOrDefault().QualifyingRoleDateEnd.Value.AddDays(1);
                        }

                        if (employeeQualifyingRoleAssignment.QualifyingRoleDateBegin == endDate)
                        {
                            employeeQualifyingRoleAssignment.QualifyingRoleDateBegin = endDate;
                        }
                        else
                        {
                            ModelState.AddModelError("QualifyingRoleDateBegin", "Допустимая дата " + endDate.ToShortDateString());
                        }
                    }
                }
                else
                {
                    ModelState.AddModelError("QualifyingRoleDateEnd", "Необходимо закрыть УПР для этого сотрудника.");
                }
            }

            if (ModelState.IsValid)
            {
                _employeeQualifyingRoleService.Add(employeeQualifyingRoleAssignment);
                string returnUrl = Url.Action("Details", "Employee", new { id = employeeQualifyingRoleAssignment.EmployeeID + "#employeequalifyingrole" }).Replace("%23", "#");
                return new RedirectResult(returnUrl);
            }

            ViewBag.EmployeeID = new SelectList(_employeeService.Get(x => x.ToList()), "ID", "FullName", employeeQualifyingRoleAssignment.EmployeeID);
            ViewBag.QualifyingRoleID = new SelectList(_qualifyingRoleService.Get(x => x.ToList()), "ID", "FullName", employeeQualifyingRoleAssignment.QualifyingRoleID);
            return View(employeeQualifyingRoleAssignment);
        }

        [OperationActionFilter(nameof(Operation.EmployeeQualifyingRoleCreateUpdate))]
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return StatusCode(StatusCodes.Status400BadRequest);
            }
            EmployeeQualifyingRole employeeQualifyingRoleAssignment = _employeeQualifyingRoleService.GetById((int)id);
            if (employeeQualifyingRoleAssignment == null)
            {
                return StatusCode(StatusCodes.Status404NotFound);
            }
            ViewBag.EmployeeID = new SelectList(_employeeService.Get(x => x.ToList()), "ID", "FullName", employeeQualifyingRoleAssignment.EmployeeID);
            ViewBag.QualifyingRoleID = new SelectList(_qualifyingRoleService.Get(x => x.ToList()), "ID", "FullName", employeeQualifyingRoleAssignment.QualifyingRoleID);
            return View(employeeQualifyingRoleAssignment);
        }

        [OperationActionFilter(nameof(Operation.EmployeeQualifyingRoleCreateUpdate))]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(EmployeeQualifyingRole employeeQualifyingRoleAssignment)
        {
            if (ModelState.IsValid)
            {
                if (employeeQualifyingRoleAssignment.QualifyingRoleDateBegin == null)
                {
                    ModelState.AddModelError("QualifyingRoleDateBegin", "Это поле должно быть заполнено.");
                }
                else
                {
                    // Получаем сотрудника
                    var employee = _employeeService.GetById((int)employeeQualifyingRoleAssignment.EmployeeID);

                    // Получаем все УПР с данным EmployeeId
                    var qualifyingRoleList = _employeeQualifyingRoleService.Get(e => e
                        .Where(x => x.EmployeeID == employeeQualifyingRoleAssignment.EmployeeID &&
                                    x.QualifyingRoleDateBegin >= employee.EnrollmentDate)
                        .OrderBy(x => x.QualifyingRoleDateEnd).AsNoTracking().ToList());
                    // Получаем ещё неизмененный объект
                    var notChangeQualifyingRole = qualifyingRoleList.Where(x => x.ID == employeeQualifyingRoleAssignment.ID).FirstOrDefault();
                    // Смотрим есть ли незакрытые УПР
                    var isOpenCategorieNotExis = (qualifyingRoleList.Where(x => x.QualifyingRoleDateEnd == null).Count() == 0);
                    // Получаем первый объект из выборки 
                    EmployeeQualifyingRole firstQualifyingRole = (!isOpenCategorieNotExis) ? qualifyingRoleList.FirstOrDefault() : qualifyingRoleList.LastOrDefault();

                    if (((employee.EnrollmentDate == null) && (employee.DismissalDate == null)) ||
                        ((employee.EnrollmentDate == null) && (employee.DismissalDate != null))
                        || firstQualifyingRole == null || notChangeQualifyingRole == null)
                    {
                        ModelState.AddModelError("Comments", "Нельзя редактировать УПР.");
                    }
                    else
                    {
                        // Если первый объект из thisQualifyingRoleInDB равен notChangeQualifyingRole, значит мы работает с последним объектом из категории сотрудника
                        if (firstQualifyingRole.ID == notChangeQualifyingRole.ID)
                        {

                            if (employee.EnrollmentDate > employeeQualifyingRoleAssignment.QualifyingRoleDateBegin)
                            {
                                ModelState.AddModelError("QualifyingRoleDateBegin", "В это время сотрудник не работал.");
                            }
                            else if (employeeQualifyingRoleAssignment.QualifyingRoleDateBegin >= employeeQualifyingRoleAssignment.QualifyingRoleDateEnd)
                            {
                                ModelState.AddModelError("QualifyingRoleDateEnd", "Дата окончания не может быть меньше или равна дате назначения.");
                            }
                            else if (employee.DismissalDate < employeeQualifyingRoleAssignment.QualifyingRoleDateEnd)
                            {
                                ModelState.AddModelError("QualifyingRoleDateEnd", "В это время сотрудник был уволен.");
                            }
                            else if (employeeQualifyingRoleAssignment.QualifyingRoleDateBegin != notChangeQualifyingRole.QualifyingRoleDateBegin)
                            {
                                ModelState.AddModelError("QualifyingRoleDateBegin", "Допустимая дата " + notChangeQualifyingRole.QualifyingRoleDateBegin.Value.ToShortDateString());
                            }
                        }
                        else
                        {

                            if ((notChangeQualifyingRole.QualifyingRoleDateBegin != employeeQualifyingRoleAssignment.QualifyingRoleDateBegin) ||
                                (notChangeQualifyingRole.QualifyingRoleDateEnd != employeeQualifyingRoleAssignment.QualifyingRoleDateEnd))
                            {
                                ModelState.AddModelError("QualifyingRoleDateBegin", "Для данной УПР поля с датой менять нельзя.");
                            }

                        }
                    }
                }
            }

            if (ModelState.IsValid)
            {
                _employeeQualifyingRoleService.Update(employeeQualifyingRoleAssignment);
                string returnUrl = Url.Action("Details", "Employee", new { id = employeeQualifyingRoleAssignment.EmployeeID + "#employeequalifyingrole" }).Replace("%23", "#");
                return new RedirectResult(returnUrl);
            }
            ViewBag.EmployeeID = new SelectList(_employeeService.Get(x => x.ToList()), "ID", "FullName", employeeQualifyingRoleAssignment.EmployeeID);
            ViewBag.QualifyingRoleID = new SelectList(_qualifyingRoleService.Get(x => x.ToList()), "ID", "FullName", employeeQualifyingRoleAssignment.QualifyingRoleID);
            return View(employeeQualifyingRoleAssignment);
        }

        [OperationActionFilter(nameof(Operation.EmployeeQualifyingRoleDelete))]
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return StatusCode(StatusCodes.Status400BadRequest);
            }
            EmployeeQualifyingRole employeeQualifyingRoleAssignment = _employeeQualifyingRoleService.GetById((int)id);
            if (employeeQualifyingRoleAssignment == null)
            {
                return StatusCode(StatusCodes.Status404NotFound);
            }
            return View(employeeQualifyingRoleAssignment);
        }

        [OperationActionFilter(nameof(Operation.EmployeeQualifyingRoleDelete))]
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            EmployeeQualifyingRole employeeQualifyingRoleAssignment = _employeeQualifyingRoleService.GetById((int)id);
            if (employeeQualifyingRoleAssignment != null)
            {
                _employeeQualifyingRoleService.Delete(id);
            }
            return RedirectToAction("Index");
        }
    }
}
