using System;
using System.Collections.Generic;
using System.Globalization;
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
using Newtonsoft.Json;
using X.PagedList;

namespace MainApp.Controllers
{
    public class ExpensesRecordController : Controller
    {
        private readonly IExpensesRecordService _expensesRecordService;
        private readonly ICostSubItemService _costSubItemService;
        private readonly IDepartmentService _departmentService;
        private readonly IProjectService _projectService;
        private readonly IApplicationUserService _applicationUserService;

        public ExpensesRecordController(IExpensesRecordService expensesRecordService,
            ICostSubItemService costSubItemService,
            IDepartmentService departmentService,
            IProjectService projectService, IApplicationUserService applicationUserService)
        {
            _expensesRecordService = expensesRecordService;
            _costSubItemService = costSubItemService;
            _departmentService = departmentService;
            _projectService = projectService;
            _applicationUserService = applicationUserService;
        }

        [OperationActionFilter(nameof(Operation.FinDataViewForMyDepartments))]
        public ActionResult Index(int? costSubItemID, int? projectID, int? departmentID, int? year, int? month, string uRegNum, int? page)
        {
            ViewBag.CurrentCostSubItemID = costSubItemID;
            ViewBag.CurrentProjectID = projectID;
            ViewBag.CurrentDepartmentID = departmentID;
            ViewBag.CurrentYear = year;
            ViewBag.CurrentMonth = month;
            ViewBag.CurrentMonth = month;
            ViewBag.CurrentURegNum = uRegNum;

            var expensesRecords = _expensesRecordService.Get(expRecord => expRecord.Include(er => er.CostSubItem)
                .Include(er => er.Department).Include(er => er.Project)
                .Where(er => (er.CostSubItemID == costSubItemID || costSubItemID == null)
                             && (er.ProjectID == projectID || projectID == null)
                             && (er.DepartmentID == departmentID || departmentID == null)
                             && ((er.AmountReservedApprovedActualDate != null &&
                                  er.AmountReservedApprovedActualDate.Value.Year == year) || year == null)
                             && ((er.AmountReservedApprovedActualDate != null &&
                                  er.AmountReservedApprovedActualDate.Value.Month == month) || month == null)
                             && ((er.BitrixURegNum != null && !string.IsNullOrEmpty(uRegNum) && er.BitrixURegNum.ToLower().Contains(uRegNum.ToLower())) || String.IsNullOrEmpty(uRegNum) == true)
                )
                .OrderBy(er => er.AmountReservedApprovedActualDate).ToList());


            IList<Department> departmentSelectList = null;
            if (_applicationUserService.HasAccess(Operation.FinDataView))
                departmentSelectList = _departmentService.Get(x => x.Where(d => d.IsFinancialCentre).OrderBy(d => d.ShortName).ToList());
            else if (_applicationUserService.HasAccess(Operation.FinDataViewForMyDepartments))
                departmentSelectList = _departmentService.Get(x => x.Where(d => d.IsFinancialCentre).OrderBy(d => d.ShortName).ToList())
                                 .Where(x => _applicationUserService.IsDepartmentManager(x.ID) == true).ToList();

            var selectedDepartment = departmentSelectList?.Where(d => d.ID == departmentID).FirstOrDefault();

            if (selectedDepartment == null && _applicationUserService.HasAccess(Operation.FinDataViewForMyDepartments) && !_applicationUserService.HasAccess(Operation.FinDataView))
            {
                selectedDepartment = departmentSelectList.FirstOrDefault();
                if (selectedDepartment != null)
                {
                    return RedirectToAction("Index", new { departmentID = selectedDepartment.ID });
                }
                else
                {
                    return StatusCode(StatusCodes.Status403Forbidden);
                }
            }

            SetIndexViewBag(month, year);

            ViewBag.DepartmentID = new SelectList(departmentSelectList.ToList(), "ID", "FullName", selectedDepartment?.ID);

            int pageSize = 200;
            int pageNumber = (page ?? 1);

            return View(expensesRecords.ToPagedList(pageNumber, pageSize));
        }

        private void SetIndexViewBag(int? selectedMonth, int? selectedYear)
        {
            ViewBag.Months = new SelectList(Enumerable.Range(0, 13).Select(x =>
                new SelectListItem()
                {
                    Text = (x != 0) ? CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(x) + " (" + x + ")" : "-не выбрано-",
                    Value = (x != 0) ? x.ToString() : "",
                }), "Value", "Text", selectedMonth);

            SelectList yearsSelectList = new SelectList(Enumerable.Range(BudgetLimitController.BudgetLimitStartYear, DateTime.Today.Year - BudgetLimitController.BudgetLimitStartYear + 10).Select(x =>
               new SelectListItem()
               {
                   Text = (x != BudgetLimitController.BudgetLimitStartYear) ? x.ToString() : "-не выбрано-",
                   Value = (x != BudgetLimitController.BudgetLimitStartYear) ? x.ToString() : "",
               }), "Value", "Text", selectedYear);

            ViewBag.Years = yearsSelectList;

            ViewBag.CostSubItemID = new SelectList(_costSubItemService.Get(x => x.OrderBy(csi => csi.ShortName).ToList()), "ID", "FullName");
            ViewBag.ProjectID = new SelectList(_projectService.Get(x => x.OrderBy(p => p.ShortName).ToList()), "ID", "ShortName");
            //ViewBag.DepartmentID = new SelectList(_departmentService.Get(x => x.Where(d => d.IsFinancialCentre).OrderBy(d => d.ShortName).ToList()), "ID", "FullName");
        }

        [OperationActionFilter(nameof(Operation.FinDataView))]
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return StatusCode(StatusCodes.Status400BadRequest);
            }
            var expensesRecord = _expensesRecordService.GetById((int)id);
            if (expensesRecord == null)
            {
                return StatusCode(StatusCodes.Status404NotFound);
            }
            return View(expensesRecord);
        }

        private void SetViewBag(ExpensesRecord expensesRecord)
        {
            ViewBag.CostSubItemID = new SelectList(_costSubItemService.Get(x => x.OrderBy(csi => csi.ShortName).ToList()), "ID", "FullName", expensesRecord?.CostSubItemID);
            ViewBag.DepartmentID = new SelectList(_departmentService.Get(x => x.Where(d => d.IsFinancialCentre).OrderBy(d => d.ShortName).ToList()), "ID", "FullName", expensesRecord?.DepartmentID);
            ViewBag.ProjectID = new SelectList(_projectService.Get(x => x.OrderBy(p => p.ShortName).ToList()), "ID", "ShortName", expensesRecord?.ProjectID);
        }

        [OperationActionFilter(nameof(Operation.AdminFullAccess))]
        public ActionResult Create()
        {
            SetViewBag(null);
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [OperationActionFilter(nameof(Operation.AdminFullAccess))]
        public ActionResult Create(ExpensesRecord expensesRecord)
        {
            if (ModelState.IsValid)
            {
                _expensesRecordService.Add(expensesRecord);
                return RedirectToAction("Index");
            }

            SetViewBag(expensesRecord);
            return View(expensesRecord);
        }

        [OperationActionFilter(nameof(Operation.AdminFullAccess))]
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return StatusCode(StatusCodes.Status400BadRequest);
            }
            var expensesRecord = _expensesRecordService.GetById((int)id);
            if (expensesRecord == null)
            {
                return StatusCode(StatusCodes.Status404NotFound);
            }
            SetViewBag(expensesRecord);
            return View(expensesRecord);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [OperationActionFilter(nameof(Operation.AdminFullAccess))]
        public ActionResult Edit(ExpensesRecord expensesRecord)
        {
            if (ModelState.IsValid)
            {
                _expensesRecordService.Update(expensesRecord);
                return RedirectToAction("Index");
            }
            SetViewBag(expensesRecord);
            return View(expensesRecord);
        }


        [OperationActionFilter(nameof(Operation.AdminFullAccess))]
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return StatusCode(StatusCodes.Status400BadRequest);
            }
            var expensesRecord = _expensesRecordService.GetById((int)id);
            if (expensesRecord == null)
            {
                return StatusCode(StatusCodes.Status404NotFound);
            }
            return View(expensesRecord);
        }


        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [OperationActionFilter(nameof(Operation.AdminFullAccess))]
        public ActionResult DeleteConfirmed(int id)
        {
            ExpensesRecord expensesRecord = _expensesRecordService.GetById(id);
            _expensesRecordService.Delete(expensesRecord.ID);
            return RedirectToAction("Index");
        }


        [HttpGet]
        public string GetPartsLinksByBitrixExpenses(int id)
        {
            var expensesRecord = _expensesRecordService.GetById(id);
            var objectsForUrl = new
            {
                ExpensesRecordId = expensesRecord.SourceListID,
                SourceElementId = expensesRecord.SourceElementID,
                GeneralUrl = _expensesRecordService.GetExpensesRecordBitrixURLFromConfig()[expensesRecord.SourceListID]
            };
            return JsonConvert.SerializeObject(objectsForUrl);
        }
    }
}
