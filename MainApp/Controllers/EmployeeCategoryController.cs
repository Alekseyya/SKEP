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
    public class EmployeeCategoryController : Controller
    {
        private readonly IEmployeeCategoryService _employeeCategoryService;
        private readonly IEmployeeService _employeeService;

        public EmployeeCategoryController(IEmployeeCategoryService employeeCategoryService, IEmployeeService employeeService)
        {
            _employeeCategoryService = employeeCategoryService;
            _employeeService = employeeService;
        }

        [OperationActionFilter(nameof(Operation.EmployeeCategoryView))]
        public ActionResult Index()
        {
            var employeeCategories = _employeeCategoryService.Get(epls => epls.Include(e => e.Employee).ToList());
            return View(employeeCategories);
        }

        [OperationActionFilter(nameof(Operation.EmployeeCategoryView))]
        public ActionResult Details(int? id)
        {
            if (id == null)
                return StatusCode(StatusCodes.Status400BadRequest);
            EmployeeCategory employeeCategory = _employeeCategoryService.GetById((int)id);
            if (employeeCategory == null)
                return StatusCode(StatusCodes.Status404NotFound);
            return View(employeeCategory);
        }

        [OperationActionFilter(nameof(Operation.EmployeeCategoryCreateUpdate))]
        public ActionResult Create(int? employeeid)
        {
            ViewBag.EmployeeID = new SelectList(_employeeService.Get(x => x.ToList()), "ID", "FullName");
            ViewBag.EmploymentRatio = 1;
            return View();
        }

        [OperationActionFilter(nameof(Operation.EmployeeCategoryCreateUpdate))]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(EmployeeCategory employeeCategory)
        {
            if (ModelState.IsValid)
            {
                var employee = _employeeService.GetById((int)employeeCategory.EmployeeID);

                // Получаем все категории в отсортированном по убыванию порядке по полю закрытия
                var categoryList = _employeeCategoryService.Get(empls => empls.Where(x =>
                        x.EmployeeID == employeeCategory.EmployeeID
                        && x.CategoryDateBegin >= employee.EnrollmentDate).OrderByDescending(x => x.CategoryDateEnd)
                    .ToList());
                // Проверяем на зыкрытость категории
                bool isOpenCategoryNotExists = (categoryList.Where(x => x.CategoryDateEnd == null).Count() == 0);

                if (isOpenCategoryNotExists)
                {
                    //Проверка на корректный ввод данных

                    if (((employee.EnrollmentDate == null) && (employee.DismissalDate == null))
                        || ((employee.EnrollmentDate == null) && (employee.DismissalDate != null)))
                    {
                        ModelState.AddModelError("Comments", "Нельзя создать категорию.");
                    }
                    else if ((employeeCategory.CategoryType == EmployeeCategoryType.Regular || employeeCategory.CategoryType == EmployeeCategoryType.Temporary)
                             && !employeeCategory.EmploymentRatio.HasValue)
                    {
                        ModelState.AddModelError("EmploymentRatio", "Для выбранной категории необходимо указать % ставки.");
                    }
                    else if ((employeeCategory.CategoryType == EmployeeCategoryType.FreelancerHourly || employeeCategory.CategoryType == EmployeeCategoryType.FreelancerPiecework
                                                                                                     || employeeCategory.CategoryType == EmployeeCategoryType.ExtContragentEmployee) && employeeCategory.EmploymentRatio.HasValue)
                    {
                        ModelState.AddModelError("EmploymentRatio", "Для выбранной категории % ставки не указывается, укажите пустое значение % ставки.");
                    }
                    else if (employeeCategory.CategoryDateBegin == null)
                    {
                        ModelState.AddModelError("CategoryDateBegin", "Это поле должно быть заполнено.");
                    }
                    else if (employee.EnrollmentDate > employeeCategory.CategoryDateBegin)
                    {
                        ModelState.AddModelError("CategoryDateBegin", "В это время сотрудник не работал.");
                    }
                    else if (employeeCategory.CategoryDateBegin >= employeeCategory.CategoryDateEnd)
                    {
                        ModelState.AddModelError("CategoryDateEnd", "Дата окончания не может быть меньше или равна дате назначения.");
                    }
                    else if (employee.DismissalDate < employeeCategory.CategoryDateEnd)
                    {
                        ModelState.AddModelError("CategoryDateEnd", "В это время сотрудник был уволен.");
                    }
                    else
                    {
                        DateTime endDate;

                        if (categoryList.Count == 0)
                        {
                            endDate = employee.EnrollmentDate.Value;
                        }
                        else
                        {
                            endDate = categoryList.FirstOrDefault().CategoryDateEnd.Value.AddDays(1);
                        }

                        if (employeeCategory.CategoryDateBegin == endDate)
                        {
                            employeeCategory.CategoryDateBegin = endDate;
                        }
                        else
                        {
                            ModelState.AddModelError("CategoryDateBegin", "Допустимая дата " + endDate.ToShortDateString());
                        }
                    }
                }
                else
                {
                    ModelState.AddModelError("CategoryDateEnd", "Необходимо закрыть категорию для этого сотрудника.");
                }
            }

            if (ModelState.IsValid)
            {
                _employeeCategoryService.Add(employeeCategory);

                string returnUrl = Url.Action("Details", "Employee", new { id = employeeCategory.EmployeeID + "#employeecategory" }).Replace("%23", "#");
                return new RedirectResult(returnUrl);
            }
            // ViewBag.EmploymentRatio = employeeCategory.EmploymentRatio.HasValue ? (double)employeeCategory.EmploymentRatio.Value : 1;
            ViewBag.EmploymentRatio = employeeCategory.EmploymentRatio.HasValue ? ((double)employeeCategory.EmploymentRatio.Value).ToString() : string.Empty;
            return View(employeeCategory);

        }

        [OperationActionFilter(nameof(Operation.EmployeeCategoryCreateUpdate))]
        public ActionResult Edit(int? id)
        {
            if (id == null)
                return StatusCode(StatusCodes.Status400BadRequest);
            EmployeeCategory employeeCategory = _employeeCategoryService.GetById((int)id);
            if (employeeCategory == null)
                return StatusCode(StatusCodes.Status404NotFound);
            // ViewBag.EmploymentRatio = employeeCategory.EmploymentRatio.HasValue ? (double)employeeCategory.EmploymentRatio.Value : 1;
            ViewBag.EmploymentRatio = employeeCategory.EmploymentRatio.HasValue ? ((double)employeeCategory.EmploymentRatio.Value).ToString() : string.Empty;
            return View(employeeCategory);
        }

        [OperationActionFilter(nameof(Operation.EmployeeCategoryCreateUpdate))]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(EmployeeCategory employeeCategory)
        {

            if (ModelState.IsValid)
            {
                if (employeeCategory.CategoryDateBegin == null)
                {
                    ModelState.AddModelError("CategoryDateBegin", "Это поле должно быть заполнено.");
                }
                else if ((employeeCategory.CategoryType == EmployeeCategoryType.Regular || employeeCategory.CategoryType == EmployeeCategoryType.Temporary)
                         && !employeeCategory.EmploymentRatio.HasValue)
                {
                    ModelState.AddModelError("EmploymentRatio", "Для выбранной категории необходимо указать % ставки.");
                }
                else if ((employeeCategory.CategoryType == EmployeeCategoryType.FreelancerHourly || employeeCategory.CategoryType == EmployeeCategoryType.FreelancerPiecework
                                                                                                 || employeeCategory.CategoryType == EmployeeCategoryType.ExtContragentEmployee) && employeeCategory.EmploymentRatio.HasValue)
                {
                    ModelState.AddModelError("EmploymentRatio", "Для выбранной категории % ставки не указывается, укажите пустое значение % ставки.");
                }

                else
                {
                    var employee = _employeeService.GetById((int)employeeCategory.EmployeeID);

                    // Получаем все категории с данным EmployeeId
                    var categoryList = _employeeCategoryService.Get(empls => empls.Where(x =>
                            x.EmployeeID == employeeCategory.EmployeeID &&
                            x.CategoryDateBegin >= employee.EnrollmentDate).OrderBy(x => x.CategoryDateEnd)
                        .AsNoTracking()
                        .ToList());
                    // Получаем ещё неизмененный объект
                    var notChangeCategory = categoryList.Where(x => x.ID == employeeCategory.ID).FirstOrDefault();
                    // Смотрим есть ли незакрытые категории
                    var isOpenCategorieNotExis = (categoryList.Where(x => x.CategoryDateEnd == null).Count() == 0);
                    // Получаем первый объект из выборки 
                    EmployeeCategory firstCategory = (!isOpenCategorieNotExis) ? categoryList.FirstOrDefault() : categoryList.LastOrDefault();

                    if (((employee.EnrollmentDate == null) && (employee.DismissalDate == null)) ||
                        ((employee.EnrollmentDate == null) && (employee.DismissalDate != null))
                        || firstCategory == null || notChangeCategory == null)
                    {
                        ModelState.AddModelError("Comments", "Нельзя редактировать категорию.");
                    }
                    else
                    {
                        // Если первый объект из thisCategoryInDB равен notChangeCategory, значит мы работает с последним объектом из категории сотрудника
                        if (firstCategory.ID == notChangeCategory.ID)
                        {

                            if (employee.EnrollmentDate > employeeCategory.CategoryDateBegin)
                            {
                                ModelState.AddModelError("CategoryDateBegin", "В это время сотрудник не работал.");
                            }
                            else if (employeeCategory.CategoryDateBegin >= employeeCategory.CategoryDateEnd)
                            {
                                ModelState.AddModelError("CategoryDateEnd", "Дата окончания не может быть меньше или равна дате назначения.");
                            }
                            else if (employee.DismissalDate < employeeCategory.CategoryDateEnd)
                            {
                                ModelState.AddModelError("CategoryDateEnd", "В это время сотрудник был уволен.");
                            }
                            else if (employeeCategory.CategoryDateBegin != notChangeCategory.CategoryDateBegin)
                            {
                                ModelState.AddModelError("CategoryDateBegin", "Допустимая дата " + notChangeCategory.CategoryDateBegin.Value.ToShortDateString());
                            }
                        }
                        else
                        {

                            if ((notChangeCategory.CategoryDateBegin != employeeCategory.CategoryDateBegin) ||
                                (notChangeCategory.CategoryDateEnd != employeeCategory.CategoryDateEnd))
                            {
                                ModelState.AddModelError("CategoryDateBegin", "Для данной категории поля с датой менять нельзя.");
                            }

                        }
                    }
                }
            }

            if (ModelState.IsValid)
            {
                _employeeCategoryService.Update(employeeCategory);

                string returnUrl = Url.Action("Details", "Employee", new { id = employeeCategory.EmployeeID + "#employeecategory" }).Replace("%23", "#");
                return new RedirectResult(returnUrl);
            }

            ViewBag.EmployeeID = new SelectList(_employeeService.Get(x => x.ToList()), "ID", "FullName", employeeCategory.EmployeeID);
            ViewBag.EmploymentRatio = employeeCategory.EmploymentRatio.HasValue ? ((double)employeeCategory.EmploymentRatio.Value).ToString() : string.Empty;
            return View(employeeCategory);
        }

        [OperationActionFilter(nameof(Operation.EmployeeCategoryDelete))]
        public ActionResult Delete(int? id)
        {
            if (id == null)
                return StatusCode(StatusCodes.Status400BadRequest);
            EmployeeCategory employeeCategory = _employeeCategoryService.GetById((int)id);
            if (employeeCategory == null)
                return StatusCode(StatusCodes.Status404NotFound);
            return View(employeeCategory);
        }

        [OperationActionFilter(nameof(Operation.EmployeeCategoryDelete))]
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            EmployeeCategory employeeCategory = _employeeCategoryService.GetById(id);
            int employeeID = employeeCategory.EmployeeID.Value;
            if (employeeCategory != null)
            {
                _employeeCategoryService.Delete(employeeCategory.ID);
            }
            string returnUrl = Url.Action("Details", "Employee", new { id = employeeID + "#employeecategory" }).Replace("%23", "#");
            // return RedirectToAction("Index");
            return new RedirectResult(returnUrl);
        }

    }
}
