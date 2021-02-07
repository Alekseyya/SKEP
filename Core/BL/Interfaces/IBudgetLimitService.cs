using System.Collections.Generic;
using Core.Models;


namespace Core.BL.Interfaces
{
    public interface IBudgetLimitService : IServiceBase<BudgetLimit,int>
    {
        Limit GetLimitData(int costSubItemtID, int departmentID, int? projectID, int year, int month);
        Limit GetLimitDataForBusinessTrip(int projectID, int year, int month);
        IEnumerable<Summary> GetLimitDataSummary(int costSubItemtID, int departmentID, int? projectID, int year);
        IEnumerable<Summary> GetLimitDataSummaryForBusinessTrip(int projectID, int year);
        ProjectBusinessTripInfo GetProjectBusinessTripInfo(int projectID);
        void UpdateWithoutVersion(BudgetLimit budgetLimit);
    }
}