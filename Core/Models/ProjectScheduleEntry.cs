
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Core.Models.Attributes;
using Core.Models.RBAC;


namespace Core.Models
{
    [AllowRecycleBin]
    [DisplayTableName("Вехи проектов")]
    public class ProjectScheduleEntry : BaseModel
    {
        [Display(Name = "ИД")]
        public int ID { get; set; }

        [Required]
        [DisplayInRecycleBin(Order = 1)]
        [Display(Name = "Наименование")]
        public string Title { get; set; }

        /// <summary>
        /// Сумма с НДС, руб
        /// </summary>
        [Display(Name = "Сумма с НДС, руб")]
        [DisplayFormat(DataFormatString = "{0:#,##0.00}", ApplyFormatInEditMode = true)]
        public decimal? Amount { get; set; }

        [Display(Name = "Тип вехи")]
        public int? ProjectScheduleEntryTypeID { get; set; }
        [Display(Name = "Тип вехи")]
        public virtual ProjectScheduleEntryType ProjectScheduleEntryType { get; set; }

        /// <summary>
        /// № договора
        /// </summary>
        [Display(Name = "№ договора")]
        public string ContractNum { get; set; }

        /// <summary>
        /// № этапа договора
        /// </summary>
        [Display(Name = "№ этапа договора")]
        public string ContractStageNum { get; set; }

        [Required]
        [DisplayInRecycleBin(Order = 2)]
        [Display(Name = "Проект")]
        public int ProjectID { get; set; }
        public virtual Project Project { get; set; }

        /// <summary>
        /// Включить в статус отчет
        /// </summary>
        [Display(Name = "Включить в статус отчет")]
        public bool IncludeInProjectStatusRecord { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "План")]
        public DateTime? DueDate { get; set; }

        /// <summary>
        /// Ожидание
        /// </summary>
        [DataType(DataType.Date)]
        [Display(Name = "Ожидание")]
        public DateTime? ExpectedDueDate { get; set; }

        /// <summary>
        /// Факт
        /// </summary>
        [DataType(DataType.Date)]
        [Display(Name = "Факт")]
        public DateTime? DateCompleted { get; set; }

        [Display(Name = "Комментарий")]
        [DataType(DataType.MultilineText)]
        public string Comments { get; set; }

        /// <summary>
        /// Сдаваемый результат работ
        /// </summary>
        [Display(Name = "Сдаваемый результат работ")]
        public string WorkResult { get; set; }

        [NotMapped]
        [Display(Name = "Полное наименование")]
        public string FullName
        {
            get
            {
                if (Project == null) throw new ArgumentException(nameof(Project));
                return string.IsNullOrEmpty(Project.ShortName) ? Title : $"{Project.ShortName} - {Title}";
            }
        }

        public IEnumerable<ProjectScheduleEntry> Versions { get; set; }
    }
}
