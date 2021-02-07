using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
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
using Microsoft.Extensions.DependencyInjection;




using X.PagedList;


namespace MainApp.Controllers
{
    public class BudgetLimitController : Controller
    {
        public const int BudgetLimitStartYear = 2015;
        private IBudgetLimitService _budgetLimitService;
        private readonly ICostSubItemService _costSubItemService;
        private readonly IProjectService _projectService;
        private readonly IDepartmentService _departmentService;
        private readonly IUserService _userService;
        private readonly IApplicationUserService _applicationUserService;
        private IServiceService _serviceService;

        public BudgetLimitController(IServiceProvider serviceProvider)
        {
            _budgetLimitService = serviceProvider.GetService<IBudgetLimitService>();
            _costSubItemService = serviceProvider.GetService<ICostSubItemService>();
            _projectService = serviceProvider.GetService<IProjectService>();
            _departmentService = serviceProvider.GetService<IDepartmentService>();
            _userService = serviceProvider.GetService<IUserService>();
            _applicationUserService = serviceProvider.GetService<IApplicationUserService>();
            _serviceService = serviceProvider.GetService<IServiceService>();
        }

        // GET: BudgetLimit
        [OperationActionFilter(nameof(Operation.FinDataView))]
        public ActionResult Index(int? costSubItemID, int? projectID, int? departmentID, int? year, int? month, int? page)
        {
            ViewBag.CurrentCostSubItemID = costSubItemID;
            ViewBag.CurrentProjectID = projectID;
            ViewBag.CurrentDepartmentID = departmentID;
            ViewBag.CurrentYear = year;
            ViewBag.CurrentMonth = month;

            var budgetLimits = _budgetLimitService.Get(x => x.Include(bl => bl.Project)
                .OrderBy(bl => bl.CostSubItem.ShortName)
                .ThenBy(bl => bl.Department.ShortName)
                .ThenBy(bl => bl.Year)
                .ThenBy(bl => bl.Month)
                .Where(bl => (bl.CostSubItemID == costSubItemID || costSubItemID == null)
                             && (bl.ProjectID == projectID || projectID == null)
                             && (bl.DepartmentID == departmentID || departmentID == null)
                             && (bl.Year == year || year == null)
                             && (bl.Month == month || month == null)).ToList());

            SetIndexViewBag(month, year);

            int pageSize = 200;
            int pageNumber = (page ?? 1);

            return View(budgetLimits.ToPagedList(pageNumber, pageSize));
        }

        // GET: BudgetLimitYearSummary
        [OperationActionFilter(nameof(Operation.FinDataView))]
        public ActionResult BudgetLimitYearSummary(int? costSubItemID, int? departmentID, int? year)
        {
            var selectedYear = year.HasValue ? year.Value : DateTime.Today.Year;
            SetIndexViewBag(null, selectedYear);
            var limits = _budgetLimitService.Get(x =>
                x.Where(i =>
                        i.CostSubItemID == costSubItemID && i.DepartmentID == departmentID && i.Year == selectedYear)
                    .ToList());
            var result = new Dictionary<int, BudgetLimitYearSummaryItem>();
            foreach (var limit in limits)
            {
                var month = limit.Month.Value;
                var summary = result.ContainsKey(month) ? result[month] : new BudgetLimitYearSummaryItem()
                {
                    LimitAmount = 0.0M,
                    FundsExpendedAmount = 0.0M,
                    LimitAmountApproved = 0.0M,
                    CostSubItemID = costSubItemID,
                    DepartmentID = departmentID,
                    Year = year,
                    Month = month,
                };
                summary.LimitAmount += limit.LimitAmount;
                summary.LimitAmountApproved += limit.LimitAmountApproved;
                summary.FundsExpendedAmount += limit.FundsExpendedAmount;
                result[month] = summary;
            }

            return View(result.Values.ToList().OrderBy(l => l.Month).ToList());
        }

        // GET: BudgetLimit/Details/5
        [OperationActionFilter(nameof(Operation.FinDataView))]
        public ActionResult Details(int? id, int? version)
        {
            if (id == null)
                return StatusCode(StatusCodes.Status400BadRequest);

            if (version != null && version.HasValue)
            {
                int ID = id.Value;
                var recordVersion = _budgetLimitService.Get(i => i.Where(x => x.ItemID == ID
                                                                              && ((x.VersionNumber == version.Value)
                                                                                  || (version.Value == 0 &&
                                                                                      x.VersionNumber == null))).ToList(), GetEntityMode.VersionAndOther).FirstOrDefault();
                if (recordVersion == null)
                    return StatusCode(StatusCodes.Status404NotFound);

                recordVersion.Versions = new List<BudgetLimit>().AsEnumerable();
                return View(recordVersion);
            }

            var record = _budgetLimitService.GetById(id.Value);
            if (record == null)
                return StatusCode(StatusCodes.Status404NotFound);

            record.Versions = _budgetLimitService.Get(x => x
                .Where(p => /*p.IsVersion == true &&*/ p.ItemID == record.ID || p.ID == record.ID)
                .OrderByDescending(p => p.VersionNumber).ToList(), GetEntityMode.VersionAndOther);

            int versionsCount = record.Versions.Count();
            for (int i = 0; i < versionsCount; i++)
            {
                if (i == versionsCount - 1)
                    continue;

                var changes = ChangedRecordsFiller.GetChangedData(record.Versions.ElementAt(i), record.Versions.ElementAt(i + 1));
                record.Versions.ElementAt(i).ChangedRecords = changes;
            }

            return View(record);
        }

        private void SetIndexViewBag(int? selectedMonth, int? selectedYear)
        {
            ViewBag.Months = new SelectList(Enumerable.Range(0, 13).Select(x =>
                new SelectListItem()
                {
                    Text = (x != 0) ? CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(x) + " (" + x + ")" : "-не выбрано-",
                    Value = (x != 0) ? x.ToString() : "",
                }), "Value", "Text", selectedMonth);

            SelectList yearsSelectList = new SelectList(Enumerable.Range(BudgetLimitStartYear, DateTime.Today.Year - BudgetLimitStartYear + 10).Select(x =>
               new SelectListItem()
               {
                   Text = (x != BudgetLimitStartYear) ? x.ToString() : "-не выбрано-",
                   Value = (x != BudgetLimitStartYear) ? x.ToString() : "",
               }), "Value", "Text", selectedYear);

            ViewBag.Years = yearsSelectList;

            ViewBag.CostSubItemID = new SelectList(_costSubItemService.Get(x => x.ToList().OrderBy(csi => csi.ShortName).ToList()), "ID", "FullName");
            ViewBag.ProjectID = new SelectList(_projectService.Get(x => x.ToList().OrderBy(p => p.ShortName).ToList()), "ID", "ShortName");
            ViewBag.DepartmentID = new SelectList(_departmentService.Get(x => x.Where(d => d.IsFinancialCentre).OrderBy(d => d.ShortName).ToList()), "ID", "FullName");
        }

        private void SetViewBag(BudgetLimit limit)
        {
            var selectedYear = limit?.Year ?? DateTime.Today.Year;
            var selectedMonth = limit?.Month ?? DateTime.Today.Month;
            var projectID = limit?.ProjectID;
            var costSubItemID = limit?.CostSubItemID;
            var departmentID = limit?.DepartmentID;

            ViewBag.Months = new SelectList(Enumerable.Range(1, 12).Select(x =>
                new SelectListItem()
                {
                    Text = CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(x) + " (" + x + ")",
                    Value = x.ToString(),
                }), "Value", "Text", selectedMonth);
            ViewBag.Years = new SelectList(Enumerable.Range(DateTime.Today.Year - 10, 20).Select(x =>
               new SelectListItem()
               {
                   Text = x.ToString(),
                   Value = x.ToString(),
               }), "Value", "Text", selectedYear);


            ViewBag.ProjectID = new SelectList(_projectService.Get(x => x.ToList().OrderBy(p => p.ShortName).ToList()), "ID", "ShortName", projectID);
            ViewBag.CostSubItemID = new SelectList(_costSubItemService.Get(x => x.ToList().OrderBy(csi => csi.ShortName).ToList()), "ID", "FullName", costSubItemID);
            ViewBag.DepartmentID = new SelectList(_departmentService.Get(x => x.Where(d => d.IsFinancialCentre).OrderBy(d => d.ShortName).ToList()), "ID", "FullName", departmentID);
        }

        // GET: BudgetLimit/Create
        [OperationActionFilter(nameof(Operation.FinDataCreateUpdate))]
        public ActionResult Create()
        {
            SetViewBag(null);
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [OperationActionFilter(nameof(Operation.FinDataCreateUpdate))]
        public ActionResult Create(BudgetLimit budgetLimit)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var limitDto = _budgetLimitService.GetLimitData(budgetLimit.CostSubItemID.Value, budgetLimit.DepartmentID.Value, budgetLimit.ProjectID, budgetLimit.Year.Value, budgetLimit.Month.Value);
                    budgetLimit.LimitAmountApproved = limitDto.LimitAmountReserved;
                    budgetLimit.FundsExpendedAmount = limitDto.LimitAmountActuallySpent;
                }
                catch (Exception ex)
                {
                    budgetLimit.LimitAmountApproved = null;
                    budgetLimit.FundsExpendedAmount = null;
                }

                _budgetLimitService.Add(budgetLimit);
                return RedirectToAction("Index");
            }

            SetViewBag(budgetLimit);
            return View(budgetLimit);
        }

        // GET: BudgetLimit/Edit/5
        [OperationActionFilter(nameof(Operation.FinDataCreateUpdate))]
        public ActionResult Edit(int? id)
        {
            if (id == null)
                return StatusCode(StatusCodes.Status400BadRequest);

            BudgetLimit budgetLimit = _budgetLimitService.GetById(id.Value);
            if (budgetLimit == null)
                return StatusCode(StatusCodes.Status404NotFound);

            if (budgetLimit.IsVersion)
                return StatusCode(StatusCodes.Status403Forbidden);
            

            SetViewBag(budgetLimit);
            return View(budgetLimit);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [OperationActionFilter(nameof(Operation.FinDataCreateUpdate))]
        public ActionResult Edit(BudgetLimit budgetLimit)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var limitDto = _budgetLimitService.GetLimitData(budgetLimit.CostSubItemID.Value, budgetLimit.DepartmentID.Value, budgetLimit.ProjectID, budgetLimit.Year.Value, budgetLimit.Month.Value);
                    if (limitDto != null)
                    {
                        budgetLimit.LimitAmountApproved = limitDto.LimitAmountReserved;
                        budgetLimit.FundsExpendedAmount = limitDto.LimitAmountActuallySpent;
                    }
                    else
                    {
                        budgetLimit.LimitAmountApproved = 0;
                        budgetLimit.FundsExpendedAmount = 0;
                    }
                }
                catch (Exception ex) { }

                _budgetLimitService.Update(budgetLimit);
                return RedirectToAction("Index");
            }

            SetViewBag(budgetLimit);
            return View(budgetLimit);
        }

        [OperationActionFilter(nameof(Operation.FinDataDelete))]
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return StatusCode(StatusCodes.Status400BadRequest);
            }
            BudgetLimit budgetLimit = _budgetLimitService.GetById(id.Value);
            if (budgetLimit == null)
            {
                return StatusCode(StatusCodes.Status404NotFound);
            }
            return View(budgetLimit);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [OperationActionFilter(nameof(Operation.FinDataDelete))]
        public ActionResult DeleteConfirmed(int id)
        {
            BudgetLimit budgetLimit = _budgetLimitService.GetById(id);
            var user = _userService.GetUserDataForVersion();
            var recycleBinInDBRelation = _serviceService.HasRecycleBinInDBRelation(budgetLimit);
            if (recycleBinInDBRelation.hasRelated == false)
            {
                var recycleToRecycleBin = _budgetLimitService.RecycleToRecycleBin(budgetLimit.ID, user.Item1, user.Item2);
                if (!recycleToRecycleBin.toRecycleBin)
                {
                    ViewBag.RecycleBinError =
                        "Невозможно удалить, так как на удаляемый элемент ссылаются другие элементы в системе." +
                        "Сначала необходимо удалить элементы, которые ссылаются на данный элемент. " +
                        recycleToRecycleBin.relatedClassId;
                    return View(budgetLimit);
                }
            }
            else
            {
                ViewBag.RecycleBinError =
                    "Невозможно удалить, так как на удаляемый элемент ссылаются другие элементы в системе." +
                    $"Сначала необходимо удалить элементы, которые ссылаются на данный элемент. {recycleBinInDBRelation.relatedInDBClassId}";
                return View(budgetLimit);
            }

            return RedirectToAction("Index");
        }

        [HttpGet]
        [OperationActionFilter(nameof(Operation.FinDataView))]
        public ActionResult ViewVersion(int? id)
        {
            BaseModel item = null;

            if (id == null)
                return StatusCode(StatusCodes.Status400BadRequest);

            item = _budgetLimitService.Get(x => x.Where(y => y.ID == id).ToList(), GetEntityMode.VersionAndOther).FirstOrDefault();

            if (item == null)
                return StatusCode(StatusCodes.Status404NotFound);

            if (!item.IsVersion)
                return StatusCode(StatusCodes.Status403Forbidden);

            // ReSharper disable once Mvc.ViewNotResolved
            return View(item);
        }

        [HttpGet]
        [OperationActionFilter(nameof(Operation.FinDataViewForMyDepartments))]
        public ActionResult BudgetLimitSummary(int? costSubItemID, int? projectID, int? departmentID, int? year, int? page)
        {
            ViewBag.CurrentCostSubItemID = costSubItemID;
            ViewBag.CurrentProjectID = projectID;
            ViewBag.CurrentDepartmentID = departmentID;
            ViewBag.CurrentYear = year;

            var budgetLimitList = _budgetLimitService.Get(x => x.Include(bl => bl.Project)
                .OrderBy(bl => bl.CostSubItem.ShortName)
                .ThenBy(bl => bl.Department.ShortName)
                .ThenBy(bl => bl.Year)
                .ThenBy(bl => bl.Month)
                .Where(bl => (bl.CostSubItemID == costSubItemID || costSubItemID == null)
                             && (bl.ProjectID == projectID || projectID == null)
                             && (bl.DepartmentID == departmentID || departmentID == null)
                             && (bl.Year == year || year == null)).ToList());

            ViewBag.Years = new SelectList(Enumerable.Range(BudgetLimitStartYear, DateTime.Today.Year - BudgetLimitStartYear + 10).Select(x =>
                new SelectListItem()
                {
                    Text = x.ToString(),
                    Value = x.ToString()
                }), "Value", "Text", year);

            ViewBag.CostSubItemID = new SelectList(_costSubItemService.Get(x => x.ToList().OrderBy(csi => csi.ShortName).ToList()), "ID", "FullName");
            ViewBag.ProjectID = new SelectList(_projectService.Get(x => x.ToList().OrderBy(p => p.ShortName).ToList()), "ID", "ShortName");

            IList<Department> departmentSelectList = null;
            if (_applicationUserService.HasAccess(Operation.FinDataView))
                departmentSelectList = _departmentService.Get(x => x.Where(d => d.IsFinancialCentre).OrderBy(d => d.ShortName).ToList());
            else if (_applicationUserService.HasAccess(Operation.FinDataViewForMyDepartments))
                departmentSelectList = _departmentService.Get(x => x.Where(d => d.IsFinancialCentre).OrderBy(d => d.ShortName).ToList())
                    .Where(x => _applicationUserService.IsDepartmentManager(x.ID) == true).ToList();

            var selectedDepartment = departmentSelectList?.Where(d => d.ID == departmentID).FirstOrDefault();

            if (selectedDepartment == null)
            {
                selectedDepartment = departmentSelectList.FirstOrDefault();
                if (selectedDepartment != null)
                {
                    return RedirectToAction("BudgetLimitSummary", new { departmentID = selectedDepartment.ID, year = year ?? DateTime.Today.Year });
                }
                else
                {
                    return StatusCode(StatusCodes.Status403Forbidden);
                }
            }

            ViewBag.DepartmentID = new SelectList(departmentSelectList.ToList(),
                    "ID", "FullName", selectedDepartment.ID);

            int pageSize = 200;
            int pageNumber = (page ?? 1);

            return View(budgetLimitList.ToPagedList(pageNumber, pageSize));
        }
    }
}
