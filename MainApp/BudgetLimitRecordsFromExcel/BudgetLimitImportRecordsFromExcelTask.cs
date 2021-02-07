using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using Core.BL.Interfaces;
using Core.Common;
using Core.Models;


namespace MainApp.BudgetLimitRecordsFromExcel
{
    public class BudgetLimitImportRecordsFromExcelTask : LongRunningTaskBase
    {
        private readonly IBudgetLimitService _budgetLimitService;
        private readonly ICostSubItemService _costSubItemService;
        private readonly IDepartmentService _departmentService;
        private readonly IUserService _userService;

        string[] _monthCols = { "January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December" };

        public BudgetLimitImportRecordsFromExcelTask(IBudgetLimitService budgetLimitService, ICostSubItemService costSubItemService, IDepartmentService departmentService, IUserService userService)
        {
            _budgetLimitService = budgetLimitService;
            _costSubItemService = costSubItemService;
            _departmentService = departmentService;
            _userService = userService;
        }

        public BudgetLimitImportRecordsFromExcelResult ProcessLongRunningAction(string userIdentityName, string fileId, DataTable budgetLimitTable, int reportYear, bool onlyValidate)
        {
            var htmlReport = string.Empty;
            var htmlErrorReport = string.Empty;
            taskId = fileId;
            LongRunningTaskReport report = null;
            var execludedColumns = new string[] { "Income", "Total" };
            try
            {
                SetStatus(0, "Старт загрузки...");
                SetStatus(1, "Обработка файла Excel");
                report = ImportBudgetLimitRecords(budgetLimitTable, reportYear, onlyValidate, execludedColumns);
                SetStatus(100, "Загрузка завершена");
            }
            catch (Exception e)
            {
                SetStatus(-1, "Ошибка: " + e.Message);
                htmlErrorReport += "<br>" + e.Message + "<br>" + e.StackTrace + "<br>" + e.TargetSite;
            }

            try
            {
                if (report != null)
                    htmlReport = report.GenerateHtmlReport();
            }
            catch (Exception e)
            {
                SetStatus(-1, "Ошибка: " + e.Message);
                htmlErrorReport += "<br>" + e.Message + "<br>" + e.StackTrace + "<br>" + e.TargetSite.ToString();
            }

            return new BudgetLimitImportRecordsFromExcelResult()
            {
                UserInitiationgReport = userIdentityName,
                FileId = fileId,
                FileHtmlReport = new List<string>() { htmlReport, htmlErrorReport }
            };
        }

        private LongRunningTaskReport ImportBudgetLimitRecords(DataTable budgetLimitTable, int year, bool onlyValidate, string[] execludedColumns)
        {
            LongRunningTaskReport report = new LongRunningTaskReport("Отчет о загрузке данных о лимитах за месяц", "");

            Dictionary<string, int> mounthDict = GetMounth();

            double countRow = budgetLimitTable.Rows.Count;
            for (int i = 0; i < budgetLimitTable.Rows.Count; i++)
            {
                int percent = i == 0 ? 1 : (int)((i / countRow) * 100);
                SetStatus(percent, "Обработано данных");
                var row = budgetLimitTable.Rows[i];
                var section = row["Section"].ToString();
                if (!section.Trim().ToLower().Equals("цфо", StringComparison.OrdinalIgnoreCase))
                    continue;

                var costSubItemShortName = row["CostSubItemShortName"].ToString();
                var departmentShortTitle = row["DepartmentShortTitle"].ToString();
                BudgetLimit[] budgetLimitArrayFromDataTable = Deserialize(row, mounthDict, year);
                foreach (var budgetLimit in budgetLimitArrayFromDataTable)
                {
                    var results = _budgetLimitService
                        .Get(query => query
                        .Where(b => b.Month == budgetLimit.Month
                        && b.Year == budgetLimit.Year
                        && b.CostSubItem.ShortName.Equals(costSubItemShortName.Trim(), StringComparison.OrdinalIgnoreCase)
                        && b.Department.ShortTitle.Equals(departmentShortTitle.Trim(), StringComparison.OrdinalIgnoreCase))
                        .ToList());
                    if (results.Count == 0) // добавляем
                    {
                        var costSubItem = _costSubItemService.Get(query => query.Where(c => c.ShortName.Equals(costSubItemShortName.Trim(), StringComparison.OrdinalIgnoreCase)).ToList()).FirstOrDefault();
                        if (costSubItem == null)
                        {
                            if (!onlyValidate)
                                report.AddReportEvent($"Не удалось добавить лимит: {budgetLimit.LimitAmount}, на месяц: {CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(budgetLimit.Month.Value)}, не найдена подстатья: {costSubItemShortName}");
                            else
                                report.AddReportEvent($"Не найдена подстатья: {costSubItemShortName}, для лимита: {budgetLimit.LimitAmount}, на месяц: {CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(budgetLimit.Month.Value)}");
                            continue;
                        }

                        var department = _departmentService.Get(query => query.Where(d => d.ShortTitle.Equals(departmentShortTitle.Trim(), StringComparison.OrdinalIgnoreCase)).ToList()).FirstOrDefault();
                        if (department == null)
                        {
                            if (!onlyValidate)
                                report.AddReportEvent($"Не удалось добавить лимит: {budgetLimit.LimitAmount}, на месяц: {CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(budgetLimit.Month.Value)}, не найдено подразделение: {departmentShortTitle}");
                            else
                                report.AddReportEvent($"Не найдено подразделение: {departmentShortTitle}, для лимита: {budgetLimit.LimitAmount}, на месяц: {CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(budgetLimit.Month.Value)}");
                            continue;
                        }

                        budgetLimit.CostSubItemID = costSubItem.ID;
                        budgetLimit.DepartmentID = department.ID;
                        if (!onlyValidate)
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

                            _budgetLimitService.Add(budgetLimit);
                            report.AddReportEvent($"Добавлен лимит: {budgetLimit.LimitAmount}, на месяц: {CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(budgetLimit.Month.Value)}, подстатья: {costSubItemShortName}, подразделение: {departmentShortTitle}");
                        }
                        else
                            report.AddReportEvent($"Лимит еще не создан: {budgetLimit.LimitAmount}, на месяц: {CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(budgetLimit.Month.Value)}, подстатья: {costSubItemShortName}, подразделение: {departmentShortTitle}");

                    }
                    else // обновляем
                    {
                        if (!onlyValidate)
                        {
                            var budgetLimitFromDB = results.FirstOrDefault();
                            decimal? limitAmountOldValue = budgetLimitFromDB.LimitAmount;
                            if (budgetLimitFromDB.LimitAmount != budgetLimit.LimitAmount.Value)
                            {
                                budgetLimitFromDB.LimitAmount = budgetLimit.LimitAmount.Value;
                                _budgetLimitService.Update(budgetLimitFromDB);
                                report.AddReportEvent($"Обновлен лимит: {budgetLimit.LimitAmount}, (старое значение: {limitAmountOldValue}), месяц: {CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(budgetLimit.Month.Value)}, подстатья: {costSubItemShortName}, подразделение: {departmentShortTitle}");
                            }
                            else
                            {
                                report.AddReportEvent($"Не изменилось значение лимита: {budgetLimit.LimitAmount}, месяц: {CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(budgetLimit.Month.Value)}, подстатья: {costSubItemShortName}, подразделение: {departmentShortTitle}");
                            }
                        }
                        else
                            report.AddReportEvent($"Найден лимит: {results.FirstOrDefault().LimitAmount}, (новое значение: {budgetLimit.LimitAmount}), месяц: {CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(budgetLimit.Month.Value)}, подстатья: {costSubItemShortName}, подразделение: {departmentShortTitle}");
                    }
                }
            }

            return report;
        }

        private BudgetLimit[] Deserialize(DataRow row, Dictionary<string, int> monthDict, int year)
        {
            // преобразование строк в лимиты по месяцам
            var budgetLimits = new List<BudgetLimit>();
            foreach (var month in monthDict)
            {
                var limitAmount = row[month.Key];
                if (limitAmount != null && String.IsNullOrEmpty(limitAmount.ToString().Trim()) == false)
                {
                    BudgetLimit budgetLimit = null;

                    try
                    {
                        budgetLimit = new BudgetLimit()
                        {
                            Month = month.Value,
                            LimitAmount = Math.Round(Convert.ToDecimal(limitAmount), 0),
                            Year = year
                        };
                    }
                    catch (Exception)
                    {
                    }

                    budgetLimits.Add(budgetLimit);
                }
            }
            return budgetLimits.ToArray();
        }

        private Dictionary<string, int> GetMounth()
        {
            // Словарь месяцев от 1 до 12
            var mounth = new Dictionary<string, int>();
            for (int i = 0; i < _monthCols.Length; i++)
                mounth.Add(_monthCols[i], i + 1);
            return mounth;
        }

    }
}
