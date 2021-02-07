using System;
using System.ComponentModel.DataAnnotations;
using Core.Models.RBAC;


namespace Core.Models
{
    [DisplayTableName("Записи отчетов по проектам")]
    public class ProjectReportRecord
    {
        [Display(Name = "ИД")]
        public int ID { get; set; }

        [Required]
        [Display(Name = "Отчетный месяц")]
        public string ReportPeriodName { get; set; }

        [Display(Name = "Проект")]
        public int? ProjectID { get; set; }
        public virtual Project Project { get; set; }

        [Display(Name = "Сотрудник")]
        public int? EmployeeID { get; set; }
        [Display(Name = "Сотрудник")]
        public virtual Employee Employee { get; set; }

        [DataType(DataType.DateTime)]
        [Display(Name = "Дата расчета")]
        public DateTime? CalcDate { get; set; }

        [Display(Name = "Примечание")]
        public string Comments { get; set; }

        [Display(Name = "Трудозатраты (ч)")]
        public double? Hours { get; set; }

        [Display(Name = "Количество участников")]
        public int? EmployeeCount { get; set; }

        [Display(Name = "ФОТ, руб")]
        [DisplayFormat(DataFormatString = "{0:#,##0.00}", ApplyFormatInEditMode = true)]
        public decimal? EmployeePayroll { get; set; }

        [Display(Name = "СУ, руб")]
        [DisplayFormat(DataFormatString = "{0:#,##0.00}", ApplyFormatInEditMode = true)]
        public decimal? EmployeeOvertimePayroll { get; set; }

        [Display(Name = "Perf Bonus, руб")]
        [DisplayFormat(DataFormatString = "{0:#,##0.00}", ApplyFormatInEditMode = true)]
        public decimal? EmployeePerformanceBonus { get; set; }

        [Display(Name = "Заявки на затраты, руб")]
        [DisplayFormat(DataFormatString = "{0:#,##0.00}", ApplyFormatInEditMode = true)]
        public decimal? OtherCosts { get; set; }

        [Display(Name = "Итого, руб")]
        [DisplayFormat(DataFormatString = "{0:#,##0.00}", ApplyFormatInEditMode = true)]
        public decimal? TotalCosts { get; set; }

        public string RecordSortKey
        {
            get
            {
                return ((ReportPeriodName != null && ReportPeriodName.Contains(".") == true) ? (ReportPeriodName.Split('.')[1] + "_" + ReportPeriodName.Split('.')[0]) : ReportPeriodName);
            }
        }
    }
}