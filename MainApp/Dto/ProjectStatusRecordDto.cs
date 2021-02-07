using System;
using Core.Models;


namespace MainApp.Dto
{
    public class ProjectStatusRecordDto
    {
        public int ID { get; set; }
        public string StatusPeriodName { get; set; }
        public int? ProjectID { get; set; }
        public virtual BasicProjectDto Project { get; set; }
        public decimal? ContractReceivedMoneyAmountActual { get; set; }
        public decimal? PaidToSubcontractorsAmountActual { get; set; }
        public decimal? EmployeePayrollAmountActual { get; set; }
        public decimal? OtherCostsAmountActual { get; set; }
        public string StatusText { get; set; }
        public DateTime? Created { get; set; }
        public ProjectStatusRiskIndicatorFlag RiskIndicatorFlag { get; set; }
        public string SupervisorComments { get; set; }
        public string ProposedSolutionText { get; set; }
        public string ProblemsText { get; set; }
        public string Author { get; set; }
        public string StatusInfoText { get; set; }
        public string StatusInfoHtml { get; set; }
    }
}