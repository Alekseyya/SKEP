using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Core.Models.Attributes;
using Core.Models.RBAC;



namespace Core.Models
{
    [AllowRecycleBin]
    [DisplayTableName("Лимиты бюджета")]
    public class BudgetLimit : BaseModel
    {
        [Display(Name = "ИД")]
        public int ID { get; set; }

        [DisplayInRecycleBin(Order = 1)]
        [Display(Name = "Подстатья")]
        public int? CostSubItemID { get; set; }
        public virtual CostSubItem CostSubItem { get; set; }

        [DisplayInRecycleBin(Order = 2)]
        [Display(Name = "Проект")]
        public int? ProjectID { get; set; }
        public virtual Project Project { get; set; }

        [Display(Name = "ЦФО")]
        public int? DepartmentID { get; set; }
        public virtual Department Department { get; set; }

        [Required]
        [DisplayInRecycleBin(Order = 3)]
        [Display(Name = "Год")]
        public int? Year { get; set; }

        [Required]
        [DisplayInRecycleBin(Order = 4)]
        [Display(Name = "Месяц")]
        public int? Month { get; set; }

        [Required]
        [DisplayInRecycleBin(Order = 5)]
        [Display(Name = "Сумма лимита на месяц")]
        [DisplayFormat(DataFormatString = "{0:#,##0.00}", ApplyFormatInEditMode = true)]
        public decimal? LimitAmount { get; set; }

        // [Required]
        [Display(Name = "Согласовано")]
        [DisplayInRecycleBin(Order = 6)]
        [DefaultValue(0)]
        [DisplayFormat(DataFormatString = "{0:#,##0.00}", ApplyFormatInEditMode = true)]
        public decimal? LimitAmountApproved { get; set; }

        // [Required]
        [Display(Name = "Израсходовано")]
        [DisplayInRecycleBin(Order = 7)]
        [DefaultValue(0)]
        [DisplayFormat(DataFormatString = "{0:#,##0.00}", ApplyFormatInEditMode = true)]
        public decimal? FundsExpendedAmount { get; set; }

        public IEnumerable<BudgetLimit> Versions { get; set; }
    }

    public class BudgetLimitSummary
    {
        public decimal? LimitAmount { get; set; }
        public decimal? LimitAmountReservedAndActuallySpent { get; set; }
        public int? FactPlanPercent => LimitAmountReservedAndActuallySpent / LimitAmount * 100 as int?;
        public decimal? LimitBalance { get; set; }
        public decimal Month { get; set; }
    }

    public class BudgetLimitYearSummaryItem
    {
        [Display(Name = "Подстатья")]
        public int? CostSubItemID { get; set; }

        [Display(Name = "ЦФО")]
        public int? DepartmentID { get; set; }

        [Display(Name = "Лимит")]
        [DisplayFormat(DataFormatString = "{0:#,##0.00}", ApplyFormatInEditMode = true)]
        public decimal? LimitAmount { get; set; }

        [Display(Name = "Согласовано")]
        [DisplayFormat(DataFormatString = "{0:#,##0.00}", ApplyFormatInEditMode = true)]
        public decimal? LimitAmountApproved { get; set; }

        [Display(Name = "Израсходовано")]
        [DisplayFormat(DataFormatString = "{0:#,##0.00}", ApplyFormatInEditMode = true)]
        public decimal? FundsExpendedAmount { get; set; }

        [Display(Name = "Год")]
        public int? Year { get; set; }

        [Display(Name = "Месяц")]
        public int Month { get; set; }
    }
}