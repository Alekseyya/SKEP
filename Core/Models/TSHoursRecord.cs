using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Core.Models.Attributes;
using Core.Models.RBAC;
using Toolbelt.ComponentModel.DataAnnotations.Schema;

namespace Core.Models
{
    public enum TSRecordStatus
    { // Используется в Views\TSHoursRecord\MyHours.cshtml, если правим, то править и там
        [Display(Name = "Все")]
        All = 0,
        [Display(Name = "Черновик")]
        Editing = 1,
        [Display(Name = "На согласовании")]
        Approving = 2,
        [Display(Name = "Согласовано РП")]
        PMApproved = 3,
        [Display(Name = "Согласовано Руководителем подразделения")]
        HDApproved = 4,
        [Display(Name = "Отклонено")]
        Declined = 5,
        [Display(Name = "Отклонено/Редактируется")]
        DeclinedEditing = 6,
        [Display(Name = "Архивировано")]
        Archived = 7
    }

    public enum TSRecordSource
    {
        [Display(Name = "Все")]
        All = 0,
        [Display(Name = "Ручной ввод")]
        UserInput = 1,
        [Display(Name = "Автозагрузка")]
        AutoPercentAssign = 2,
        [Display(Name = "Внешний Таймшит")]
        ExternalTS = 3,
        [Display(Name = "JIRA")]
        JIRA = 4,
        [Display(Name = "Отпуск")]
        Vacantion = 5,
        [Display(Name = "Импорт из Excel за месяц")]
        ExcelImportByMonth = 6,
        [Display(Name = "Импорт из Excel по дням")]
        ExcelImportByDay = 7,
    }

    [AllowRecycleBin]
    [DisplayTableName("Записи трудозатрат ТШ")]
    public class TSHoursRecord : BaseModel
    {
        [Display(Name = "ИД")]
        public int ID { get; set; }

        [Required]
        [Display(Name = "Проект")]
        public int? ProjectID { get; set; }
        [Display(Name = "Проект")]
        public virtual Project Project { get; set; }

        [Required]
        [Display(Name = "Сотрудник")]
        public int? EmployeeID { get; set; }
        [Display(Name = "Сотрудник")]
        public virtual Employee Employee { get; set; }

        [Required]
        [DisplayInRecycleBin(Order = 1)]
        [DataType(DataType.Date)]
        [Display(Name = "Отчетная дата")]
        public DateTime? RecordDate { get; set; }

        [Required]
        [DisplayInRecycleBin(Order = 2)]
        [Display(Name = "Количество часов")]
        [DisplayFormat(DataFormatString = "{0:0.##}", ApplyFormatInEditMode = true)]
        public double? Hours { get; set; }

        [Required]
        [DisplayInRecycleBin(Order = 3)]
        [DataType(DataType.MultilineText)]
        [Display(Name = "Состав работ")]
        public string Description { get; set; }

        [Display(Name = "Родительская запись автозагрузки")]
        public int? ParentTSAutoHoursRecordID { get; set; }
        [Display(Name = "Родительская запись автозагрузки")]
        public virtual TSAutoHoursRecord ParentTSAutoHoursRecord { get; set; }

        [Display(Name = "Родительская запись отпуска")]
        public int? ParentVacationRecordID { get; set; }
        [Display(Name = "Родительская запись отпуска")]
        public virtual VacationRecord ParentVacationRecord { get; set; }

        [Display(Name = "Статус")]
        public TSRecordStatus RecordStatus { get; set; }

        [Display(Name = "Источник")]
        public TSRecordSource RecordSource { get; set; }

        [Index]
        [MaxLength(64)]
        [Display(Name = "ExternalSourceElementID")]
        public string ExternalSourceElementID { get; set; }

        [Index]
        [MaxLength(64)]
        [Display(Name = "ExternalSourceListID")]
        public string ExternalSourceListID { get; set; }

        [Display(Name = "Комментарий РП")]
        public string PMComment { get; set; }

        public IEnumerable<TSHoursRecord> Versions { get; set; }
    }
}