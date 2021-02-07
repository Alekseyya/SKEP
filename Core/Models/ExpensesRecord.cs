using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;
using Core.Models.RBAC;


namespace Core.Models
{
    public enum ExpensesRecordStatus
    {
        [Display(Name = "Все")]
        All = 0,
        [Display(Name = "Зарезервировано")]
        Reserved = 1,
        [Display(Name = "Фактически израсходовано")]
        ActuallySpent = 2,
    }

    public enum SourceDB
    {
        [Display(Name = "Не определено")]
        Undefined = 0,
        [Display(Name = "Битрикс24")]
        Bitrix = 1,
     }

    [DisplayTableName("Записи о расходах")]
    public class ExpensesRecord
    {
        [Display(Name = "ИД")]
        public int ID { get; set; }

        [Required]
        [Display(Name = "Дата и время расхода")]
        [DataType(DataType.DateTime)]
        public DateTime ExpensesDate { get; set; }

        [Required]
        [Display(Name = "Подстатья затрат")]
        public int CostSubItemID { get; set; }
        [Display(Name = "Подстатья затрат")]
        public virtual CostSubItem CostSubItem { get; set; }

        [Required]
        [Display(Name = "ЦФО")]
        public int DepartmentID { get; set; }
        [Display(Name = "ЦФО")]
        public virtual Department Department { get; set; }

        [Required]
        [Display(Name = "Проект")]
        public int ProjectID { get; set; }
        [Display(Name = "Проект")]
        public virtual Project Project { get; set; }

        [Required]
        [Display(Name = "Сумма расхода всего на текущий момент, руб")]
        [DisplayFormat(DataFormatString = "{0:#,##0.00}", ApplyFormatInEditMode = true)]
        public decimal? Amount { get; set; }

        [Display(Name = "Сумма расхода всего на текущий момент без НДС, руб")]
        [DisplayFormat(DataFormatString = "{0:#,##0.00}", ApplyFormatInEditMode = true)]
        public decimal? AmountNoVAT { get; set; }

        [Required]
        [Display(Name = "Уникальный номер заявки на затраты")]
        public string BitrixURegNum { get; set; }

        [Required]
        [Display(Name = "Статус")]
        public ExpensesRecordStatus RecordStatus { get; set; }

        [Display(Name = "Наименование затрат")]
        public string ExpensesRecordName { get; set; }

        [Display(Name = "SourceElementID")]
        public string SourceElementID { get; set; }

        [Display(Name = "SourceListID")]
        public string SourceListID { get; set; }

        [Display(Name = "SourceDB")]
        public SourceDB SourceDB { get; set; }

        [Display(Name = "Зарезервированная сумма, руб")]
        [DisplayFormat(DataFormatString = "{0:#,##0.00}", ApplyFormatInEditMode = true)]
        public decimal? AmountReserved { get; set; }

        [Display(Name = "Зарезервированная сумма без НДС, руб")]
        [DisplayFormat(DataFormatString = "{0:#,##0.00}", ApplyFormatInEditMode = true)]
        public decimal? AmountReservedNoVAT { get; set; }

        [Display(Name = "Дата и время утверждения зарезервированной суммы")]
        [DataType(DataType.DateTime)]
        public DateTime? AmountReservedApprovedActualDate { get; set; }

        [Display(Name = "Дата и время фактического завершения оплат")]
        [DataType(DataType.DateTime)]
        public DateTime? PaymentCompletedActualDate { get; set; }
    }
}