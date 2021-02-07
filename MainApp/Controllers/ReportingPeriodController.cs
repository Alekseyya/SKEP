using System;
using System.Globalization;
using System.Linq;
using Core.BL.Interfaces;
using Core.Models;
using Core.Models.RBAC;
using MainApp.RBAC.Attributes;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;






namespace MainApp.Controllers
{
    public class ReportingPeriodController : Controller
    {
        private readonly IReportingPeriodService _reportingPeriodService;
        private readonly IProjectService _projectService;

        public ReportingPeriodController(IReportingPeriodService reportingPeriodService, IProjectService projectService)
        {
            if (reportingPeriodService == null)
                throw new ArgumentException(nameof(reportingPeriodService));
            if (projectService == null)
                throw new ArgumentException(nameof(projectService));

            _projectService = projectService;
            _reportingPeriodService = reportingPeriodService;
        }

        private void SetViewBag(ReportingPeriod reportingPeriod)
        {
            var selectedYear = reportingPeriod?.Year ?? DateTime.Today.Year;
            var selectedMonth = reportingPeriod?.Month ?? DateTime.Today.Month;
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

            ViewBag.VacationProjectID = new SelectList(_projectService.GetAll("", "", "", ProjectStatus.All, null), "ID", "ShortName");
            ViewBag.VacationNoPaidProjectID = new SelectList(_projectService.GetAll("", "", "", ProjectStatus.All, null), "ID", "ShortName");
        }

        [HttpGet]
        [OperationActionFilter(nameof(Operation.ReportingPeriodView))]
        public ActionResult Index()
        {
            var reportingPeriods = _reportingPeriodService.GetAll().OrderBy(x => x.FullName).ToList();
            return View(reportingPeriods);
        }

        [HttpGet]
        [OperationActionFilter(nameof(Operation.ReportingPeriodCreateUpdate))]
        public ActionResult Create()
        {
            SetViewBag(null);
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [OperationActionFilter(nameof(Operation.ReportingPeriodCreateUpdate))]
        public ActionResult Create(ReportingPeriod reportingPeriod)
        {
            //1 для первого дня, т.к есть только год и месяц
            var currentDate = new DateTime(reportingPeriod.Year, reportingPeriod.Month, 1);
            var firstDayOnNextMonth = new DateTime(currentDate.Year, currentDate.AddMonths(1).Month, 1);
            DateTime firstDayPlusTwoMonths = firstDayOnNextMonth.AddMonths(2);
            DateTime lastDayNextMonth = firstDayPlusTwoMonths.AddDays(-1);
            DateTime endOfLastDayNextMonth = firstDayPlusTwoMonths.AddTicks(-1);

            if (reportingPeriod.NewTSRecordsAllowedUntilDate == new DateTime())
                //Если указан декабрь
                if (currentDate.Month == 12)
                    reportingPeriod.NewTSRecordsAllowedUntilDate = new DateTime(currentDate.AddYears(1).Year, currentDate.AddMonths(1).Month, 3);
                else
                    reportingPeriod.NewTSRecordsAllowedUntilDate = new DateTime(currentDate.Year, currentDate.AddMonths(1).Month, 3);
            else
            {
                var setValue = reportingPeriod.NewTSRecordsAllowedUntilDate;
                //Если указан не тот год или отчитаться на следующий год
                //if (DateTime.Now.Year != setValue.Year || currentDate.AddMonths(1).Year != DateTime.Now.Year)
                //    ModelState.AddModelError("NewTSRecordsAllowedUntilDate", "Вы не можете указать дату окончания в декабре на январь или указан не тот год");

                if (setValue < firstDayOnNextMonth)
                    ModelState.AddModelError("NewTSRecordsAllowedUntilDate", "Вам надо указать дату включительно " +
                                                                             "от первого числа следующего месяца");
            }

            if (reportingPeriod.TSRecordsEditApproveAllowedUntilDate <= firstDayOnNextMonth)
                ModelState.AddModelError("TSRecordsEditApproveAllowedUntilDate", "Дата полного закрытия должна быть не раньше первого числа следующего месяца.");
            if (reportingPeriod.TSRecordsEditApproveAllowedUntilDate <= reportingPeriod.NewTSRecordsAllowedUntilDate)
                ModelState.AddModelError("TSRecordsEditApproveAllowedUntilDate", "Дата полного закрытия не должна быть меньше или равна дате закрытия месяца.");


            ModelState.Clear();
            if (ModelState.IsValid)
            {
                _reportingPeriodService.Add(reportingPeriod);
                return RedirectToAction("Index");
            }

            SetViewBag(reportingPeriod);
            return View(reportingPeriod);
        }

        [HttpGet]
        [OperationActionFilter(nameof(Operation.ReportingPeriodCreateUpdate))]
        public ActionResult Edit(int? id)
        {
            if (id == null)
                return StatusCode(StatusCodes.Status400BadRequest);
            var reportingPeriod = _reportingPeriodService.GetById((int)id);
            if (reportingPeriod == null)
                return StatusCode(StatusCodes.Status404NotFound);

            SetViewBag(null);
            return View(reportingPeriod);
        }

        [HttpPost]
        [OperationActionFilter(nameof(Operation.ReportingPeriodCreateUpdate))]
        public ActionResult Edit(ReportingPeriod reportingPeriod)
        {
            //1 для первого дня, т.к есть только год и месяц
            var currentDate = new DateTime(reportingPeriod.Year, reportingPeriod.Month, 1);
            var firstDayOnNextMonth = new DateTime(currentDate.Year, currentDate.AddMonths(1).Month, 1);
            DateTime firstDayPlusTwoMonths = firstDayOnNextMonth.AddMonths(2);
            DateTime lastDayNextMonth = firstDayPlusTwoMonths.AddDays(-1);
            DateTime endOfLastDayNextMonth = firstDayPlusTwoMonths.AddTicks(-1);

            if (reportingPeriod.NewTSRecordsAllowedUntilDate == new DateTime())
                reportingPeriod.NewTSRecordsAllowedUntilDate = new DateTime(currentDate.Year, currentDate.AddMonths(1).Month, 3);
            else
            {
                var setValue = reportingPeriod.NewTSRecordsAllowedUntilDate;
                //Если указан не тот год или отчитаться на следующий год
                //if (DateTime.Now.Year != setValue.Year || currentDate.AddMonths(1).Year != DateTime.Now.Year)
                //    ModelState.AddModelError("NewTSRecordsAllowedUntilDate", "Вы не можете указать дату окончания в декабре на январь или указан не тот год");

                if (setValue < firstDayOnNextMonth)
                    ModelState.AddModelError("NewTSRecordsAllowedUntilDate", "Вам надо указать дату включительно " +
                                                                             "от первого числа следующего месяца");
            }
            if (reportingPeriod.TSRecordsEditApproveAllowedUntilDate <= firstDayOnNextMonth)
                ModelState.AddModelError("TSRecordsEditApproveAllowedUntilDate", "Дата полного закрытия должна быть не раньше первого числа следующего месяца.");
            if (reportingPeriod.TSRecordsEditApproveAllowedUntilDate <= reportingPeriod.NewTSRecordsAllowedUntilDate)
                ModelState.AddModelError("TSRecordsEditApproveAllowedUntilDate", "Дата полного закрытия не должна быть меньше или равна дате закрытия месяца.");

            if (ModelState.IsValid)
            {
                _reportingPeriodService.Update(reportingPeriod);
                return RedirectToAction("Index");
            }

            SetViewBag(reportingPeriod);
            return View(reportingPeriod);
        }

        [OperationActionFilter(nameof(Operation.ReportingPeriodView))]
        public ActionResult Details(int? id)
        {
            if (id == null)
                return StatusCode(StatusCodes.Status400BadRequest);

            var reportingPeriod = _reportingPeriodService.GetById((int)id);
            if (reportingPeriod == null)
                return StatusCode(StatusCodes.Status404NotFound);

            return View(reportingPeriod);
        }

        [OperationActionFilter(nameof(Operation.ReportingPeriodDelete))]
        public ActionResult Delete(int? id)
        {
            if (id == null)
                return StatusCode(StatusCodes.Status400BadRequest);

            var reportingPeriod = _reportingPeriodService.GetById((int)id);
            if (reportingPeriod == null)
                return StatusCode(StatusCodes.Status404NotFound);

            return View(reportingPeriod);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [OperationActionFilter(nameof(Operation.ReportingPeriodDelete))]
        public ActionResult DeleteConfirmed(int id)
        {
            _reportingPeriodService.Delete(id);
            return RedirectToAction("Index");
        }
    }
}