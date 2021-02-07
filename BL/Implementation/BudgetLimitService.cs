using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Core.BL;
using Core.BL.Interfaces;
using Core.Data;
using Core.Data.Interfaces;
using Core.Extensions;

using Core.Models;

namespace BL.Implementation
{
    public class BudgetLimitService : RepositoryAwareServiceBase<BudgetLimit, int, IBudgetLimitRepository>, IBudgetLimitService
    {
        private readonly (string, string) _user;

        public BudgetLimitService(IRepositoryFactory repositoryFactory, IUserService userService) : base(repositoryFactory)
        {
            _user = userService.GetUserDataForVersion();
        }

        public override BudgetLimit Add(BudgetLimit budgetLimit)
        {
            if (budgetLimit == null) throw new ArgumentException(nameof(budgetLimit));

            var budgetLimitRepository = RepositoryFactory.GetRepository<IBudgetLimitRepository>();
            budgetLimit.InitBaseFields(Tuple.Create(_user.Item1, _user.Item2));
            return budgetLimitRepository.Add(budgetLimit);
        }

        public override IList<BudgetLimit> Get(Func<IQueryable<BudgetLimit>, IList<BudgetLimit>> expression)
        {
            if (expression == null) throw new ArgumentException(nameof(expression));
            var queryable = RepositoryFactory.GetRepository<IBudgetLimitRepository>().GetQueryable();
            return expression(queryable);
        }

        public Limit GetLimitDataForBusinessTrip(int projectID, int year, int month)
        {
            Limit result = null;
            var projectRepository = RepositoryFactory.GetRepository<IProjectRepository>();
            var project = projectRepository.GetById(projectID);

            if (project != null
                && project.ProjectType != null
                && project.ProjectType.BusinessTripCostSubItemID != null
                && project.DepartmentID != null)
            {
                result = GetLimitData(project.ProjectType.BusinessTripCostSubItemID.Value, project.DepartmentID.Value, null, year, month);
            }

            return result;
        }

        public Limit GetLimitData(int costSubItemtID, int departmentID, int? projectID, int year, int month)
        {
            var expression = GetBaseExpression(costSubItemtID, departmentID, projectID, year);
            Expression<Func<BudgetLimit, bool>> month_expression = item => item.Month == month;
            expression = PredicateBuilder.And(expression, month_expression);
            var queryLimits = RepositoryFactory.GetRepository<IBudgetLimitRepository>().GetQueryableAsNoTracking();
            BudgetLimit limit = queryLimits.Where(expression).FirstOrDefault();
            if (limit == null)
                return null;

            var amount = limit.LimitAmount.Value;
            var reserved = 0.00M;
            var spent = 0.00M;
            var expenses_records = GetExpensesRecords(costSubItemtID, departmentID, projectID, year, month);
            foreach (var record in expenses_records)
            {
                /*
                if (record.RecordStatus == ExpensesRecordStatus.Reserved)
                    reserved += record.Amount.Value;
                else
                    spent += record.Amount.Value;*/

                //Без НДС

                if (record.PaymentCompletedActualDate != null)
                {
                    spent += record.AmountNoVAT.Value;
                }
                else
                {
                    reserved += record.AmountReservedNoVAT.Value;
                }

            }

            return new Limit { LimitAmountReserved = reserved, LimitAmount = amount, LimitAmountActuallySpent = spent };
        }

        public IEnumerable<Summary> GetLimitDataSummaryForBusinessTrip(int projectID, int year)
        {
            IEnumerable<Summary> result = null;
            var projectRepository = RepositoryFactory.GetRepository<IProjectRepository>();
            var project = projectRepository.GetById(projectID);

            if (project != null
                && project.ProjectType != null
                && project.ProjectType.BusinessTripCostSubItemID != null
                && project.DepartmentID != null)
            {
                result = GetLimitDataSummary(project.ProjectType.BusinessTripCostSubItemID.Value, project.DepartmentID.Value, null, year);
            }

            return result;
        }

        public IEnumerable<Summary> GetLimitDataSummary(int costSubItemtID, int departmentID, int? projectID, int year)
        {
            var queryLimits = RepositoryFactory.GetRepository<IBudgetLimitRepository>().GetQueryable();
            var limits = queryLimits.Where(GetBaseExpression(costSubItemtID, departmentID, projectID, year)).ToList();
            var result = new Dictionary<int, Summary>();
            foreach (var limit in limits)
            {
                var month = limit.Month.Value;
                var item = result.ContainsKey(month) ? result[month] : new Summary { Month = month };
                item.LimitAmount += limit.LimitAmount.Value;
                result[month] = item;
            }

            var expenses_records = GetExpensesRecords(costSubItemtID, departmentID, projectID, year, null).ToList();
            foreach (var record in expenses_records)
            {
                if (record.AmountReservedApprovedActualDate != null)
                {
                    var month = record.AmountReservedApprovedActualDate.Value.Month;
                    var item = result.ContainsKey(month) ? result[month] : new Summary { Month = month };
                    if (record.PaymentCompletedActualDate != null)
                    {
                        item.LimitAmountReservedAndActuallySpent += record.AmountNoVAT.Value;
                    }
                    else
                    {
                        item.LimitAmountReservedAndActuallySpent += record.AmountReservedNoVAT.Value;
                    }
                    result[month] = item;
                }
            }

            var result_list = result.Values.ToList();
            return result_list.OrderBy(x => x.Month);
        }

        public ProjectBusinessTripInfo GetProjectBusinessTripInfo(int projectID)
        {
            ProjectBusinessTripInfo result = null;
            var projectRepository = RepositoryFactory.GetRepository<IProjectRepository>();
            var project = projectRepository.GetById(projectID);

            if (project != null
                && project.ProjectType != null
                && project.ProjectType.BusinessTripCostSubItemID != null
                && project.DepartmentID != null)
            {
                result = new ProjectBusinessTripInfo
                {
                    Id = project.ID,
                    ShortName = project.ShortName,
                    Title = project.Title,
                    FullName = project.FullName,

                    ProjectTypeId = project.ProjectTypeID,
                    ProjectTypeShortName = project.ProjectType.ShortName,

                    DepartmentId = project.DepartmentID,
                    DepartmentShortName = project.Department.ShortName,
                    DepartmentShortTitle = project.Department.ShortTitle,
                    DepartmentTitle = project.Department.Title,

                    BusinessTripCostSubItemId = project.ProjectType.BusinessTripCostSubItemID,
                    BusinessTripCostSubItemShortName = project.ProjectType.BusinessTripCostSubItem.ShortName,
                    BusinessTripCostSubItemTitle = project.ProjectType.BusinessTripCostSubItem.Title
                };
            }

            return result;
        }

        private List<ExpensesRecord> GetExpensesRecords(int costSubItemtID, int departmentID, int? projectID, int year, int? month)
        {
            Expression<Func<ExpensesRecord, bool>> expr = item =>
                item.CostSubItemID == costSubItemtID &&
                item.DepartmentID == departmentID &&
                item.AmountReservedApprovedActualDate != null &&
                item.AmountReservedApprovedActualDate.Value.Year == year;

            if (projectID.HasValue)
            {
                Expression<Func<ExpensesRecord, bool>> project_expression = item => item.ProjectID == projectID.Value;
                expr = PredicateBuilder.And(expr, project_expression);
            }

            if (month.HasValue)
            {
                Expression<Func<ExpensesRecord, bool>> project_expression = item => item.AmountReservedApprovedActualDate != null && item.AmountReservedApprovedActualDate.Value.Month == month.Value;
                expr = PredicateBuilder.And(expr, project_expression);
            }

            return RepositoryFactory.GetRepository<IExpensesRecordRepository>().GetQueryable().Where(expr).ToList();
        }

        private Expression<Func<BudgetLimit, bool>> GetBaseExpression(int costSubItemtID, int departmentID, int? projectID, int year)
        {
            Expression<Func<BudgetLimit, bool>> expr = item =>
                item.CostSubItemID == costSubItemtID &&
                item.DepartmentID == departmentID &&
                item.Year == year;

            if (projectID.HasValue)
            {
                Expression<Func<BudgetLimit, bool>> project_expression = item => item.ProjectID == projectID.Value;
                expr = PredicateBuilder.And(expr, project_expression);
            }

            return expr;
        }


        public void UpdateWithoutVersion(BudgetLimit budgetLimit)
        {
            if (budgetLimit == null)
                throw new ArgumentNullException(nameof(budgetLimit));
            var budgetLimitRepository = RepositoryFactory.GetRepository<IBudgetLimitRepository>();
            budgetLimitRepository.Update(budgetLimit);
        }

        public override BudgetLimit Update(BudgetLimit budgetLimit)
        {
            if (budgetLimit == null) throw new ArgumentNullException(nameof(budgetLimit));
            var budgetLimitRepository = RepositoryFactory.GetRepository<IBudgetLimitRepository>();

            var originalItem = budgetLimitRepository.FindNoTracking(budgetLimit.ID);

            budgetLimit.UpdateBaseFields(Tuple.Create(_user.Item1, _user.Item2), originalItem.ID, originalItem);
            originalItem.FreeseVersion(originalItem.ID);

            budgetLimitRepository.Add(originalItem);
            return budgetLimitRepository.Update(budgetLimit);
        }
    }
}