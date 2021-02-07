using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Core.Extensions;
using Core.Models.Attributes;
using Core.Models.RBAC;



namespace Core.Models
{
    public enum ProjectStatusRiskIndicatorFlag 
    {
        [Display(Name = "Все")]
        All = 0,
        [Display(Name="Зеленый")]
        Green = 1,
        [Display(Name="Желтый")]
        Yellow = 2,
        [Display(Name = "Красный")]
        Red = 3
    }

    [AllowRecycleBin]
    [DisplayTableName("Записи статус отчетов по проектам")]
    public class ProjectStatusRecord : BaseModel
    {
        [Display(Name = "ИД")]
        public int ID { get; set; }

        [Required]
        [DisplayInRecycleBin(Order = 2)]
        [Display(Name = "Отчетная неделя")]
        public string StatusPeriodName { get; set; }

        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "Начало периода")]
        public DateTime? ProjectStatusBeginDate { get; set; }

        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "Окончание периода")]
        public DateTime? ProjectStatusEndDate { get; set; }

        [DataType(DataType.MultilineText)]
        [Display(Name = "Комментарий руководителя")]
        public string SupervisorComments { get; set; }

        [Required]
        [DataType(DataType.MultilineText)]
        [Display(Name = "Риски/Проблемы")]
        public string ProblemsText { get; set; }

        [Required]
        [DataType(DataType.MultilineText)]
        [Display(Name = "Предлагаемые решения")]
        public string ProposedSolutionText { get; set; }

        [Required]
        [DisplayInRecycleBin(Order = 1)]
        [Display(Name = "Проект")]
        public int? ProjectID { get; set; }
        public virtual Project Project { get; set; }

        [Display(Name = "Поступило по договору (факт), руб.")]
        [DisplayFormat(DataFormatString = "{0:#,##0.00}", ApplyFormatInEditMode = true)]
        public decimal? ContractReceivedMoneyAmountActual { get; set; }

        [Display(Name = "Выплачено субподрядчикам (факт), руб.")]
        [DisplayFormat(DataFormatString = "{0:#,##0.00}", ApplyFormatInEditMode = true)]
        public decimal? PaidToSubcontractorsAmountActual { get; set; }

        [Display(Name = "Выплачено ФОТ (факт), руб.")]
        [DisplayFormat(DataFormatString = "{0:#,##0.00}", ApplyFormatInEditMode = true)]
        public decimal? EmployeePayrollAmountActual { get; set; }

        [Display(Name = "Прочие затраты, выплачено (факт), руб.")]
        [DisplayFormat(DataFormatString = "{0:#,##0.00}", ApplyFormatInEditMode = true)]
        public decimal? OtherCostsAmountActual { get; set; }

        [Required]
        [DataType(DataType.MultilineText)]
        [Display(Name = "Ход проекта")]
        public string StatusText { get; set; }

        [Required]
        [DisplayInRecycleBin(Order = 3)]
        [Display(Name = "Уровень риска")]
        public ProjectStatusRiskIndicatorFlag RiskIndicatorFlag { get; set; }

        [DataType(DataType.MultilineText)]
        [Display(Name = "Комментарий по индикатору рисков")]
        public string RiskIndicatorComments { get; set; }

        [Display(Name = "№ планируемого релиза")]
        public string PlannedReleaseInfo { get; set; }

        [Display(Name = "Внешние зависимости")]
        public string ExternalDependenciesInfo { get; set; }

        [NotMapped]
        [Display(Name = "Полное наименование")]
        public string FullName
        {
            get
            {
                if (Project == null) throw new ArgumentException(nameof(Project));
                return string.IsNullOrEmpty(Project.ShortName) ? StatusPeriodName : $"{Project.ShortName} - {StatusPeriodName}";
            }
        }

        [NotMapped]
        [Display(Name = "Описание хода проекта")]
        public string StatusInfoText
        {
            get
            {
                var value = string.Empty;
                if (!string.IsNullOrEmpty(StatusText))
                    value += ExpressionExtension.GetPropertyName(() => StatusText) + ": " + "\n" + StatusText + "\n";
                if (!string.IsNullOrEmpty(ProblemsText))
                    value += ExpressionExtension.GetPropertyName(() => ProblemsText) + ": " + "\n" + ProblemsText + "\n";
                if (!string.IsNullOrEmpty(ProposedSolutionText))
                    value += ExpressionExtension.GetPropertyName(() => ProposedSolutionText) + ": " + "\n" + ProposedSolutionText + "\n";
                return value;
            }
        }

        [NotMapped]
        [Display(Name = "Описание хода проекта")]
        public string StatusInfoHtml
        {
            get
            {
                var html = string.Empty;
                if (!string.IsNullOrEmpty(StatusText))
                    html += "<div style='font-weight: bold;'>" + ExpressionExtension.GetPropertyName(() => StatusText)
                                                               + ": </div> <div>" + StatusText.Replace("\n", "<br>") + "</div>\n";
                if (!string.IsNullOrEmpty(ProblemsText))
                    html += "<div style='font-weight: bold;'>" + ExpressionExtension.GetPropertyName(() => ProblemsText)
                                                               + ": </div> <div>" + ProblemsText.Replace("\n", "<br>") + "</div>\n";
                if (!string.IsNullOrEmpty(ProposedSolutionText))
                    html += "<div style='font-weight: bold;'>" + ExpressionExtension.GetPropertyName(() => ProposedSolutionText)
                                                               + ": </div> <div>" + ProposedSolutionText.Replace("\n", "<br>") + "</div>\n";
                return html;
            }
        }


        public IEnumerable<ProjectStatusRecord> Versions { get; set; }
    }
}