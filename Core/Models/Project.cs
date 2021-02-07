using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Core.Models.Attributes;
using Core.Models.RBAC;
using FluentValidation;



namespace Core.Models
{
    public enum ProjectStatus
    {
        [Display(Name = "Все")]
        All = 0,
        [Display(Name = "Действующий")]
        Active = 1,
        [Display(Name = "Закрыт")]
        Closed = 2,
        [Display(Name = "Инициирован")]
        Planned = 3,
        [Display(Name = "Приостановлен")]
        Paused = 4,
        [Display(Name = "Отменен")]
        Cancelled = 5,
        [Display(Name = "Архив")]
        Archived = 6,
    }

    [AllowRecycleBin]
    [DisplayTableName("Проекты")]
    public class Project : BaseModel
    {
        [Display(Name = "ИД")]
        public int ID { get; set; }

        [Required]
        [Display(Name = "Код")]
        public string ShortName { get; set; }

        [Display(Name = "Автоматический импорт данных по трудозатратам из Jira")]
        public bool AutoImportTSRecordFromJIRA { get; set; }

        [Display(Name = "Запрещен ручной ввод трудозатрат на проект")]
        public bool DisallowUserCreateTSRecord { get; set; }

        [Required]
        [Display(Name = "Полное наименование")]
        public string Title { get; set; }

        [Display(Name = "Тип проекта")]
        public int? ProjectTypeID { get; set; }
        [Display(Name = "Тип проекта")]
        public virtual ProjectType ProjectType { get; set; }
        
        [Display(Name = "Согласующий трудозатраты")]
        public int? ApproveHoursEmployeeID
        {
            get
            {
                if (EmployeeCAMID != null)
                    return ProjectType?.TSApproveMode == ProjectTypeTSApproveMode.Default || ProjectType?.TSApproveMode == ProjectTypeTSApproveMode.PM ? EmployeePMID : EmployeeCAMID;
                return EmployeePMID;
            }
        }

        [Display(Name = "Согласующий трудозатраты")]
        public Employee ApproveHoursEmployee
        {
            get
            {
                if (EmployeeCAM != null)
                    return ProjectType?.TSApproveMode == ProjectTypeTSApproveMode.Default || ProjectType?.TSApproveMode == ProjectTypeTSApproveMode.PM ? EmployeePM : EmployeeCAM;
                return EmployeePM;
            }
        }

        [Display(Name = "Доступно списание трудозатрат без участия в РГ")]
        public bool AllowTSRecordWithoutProjectMembership { get; set; }

        [Display(Name = "Доступно списание трудозатрат только в рабочие дни")]
        public bool AllowTSRecordOnlyWorkingDays { get; set; }

        [Display(Name = "Заказчик")]
        public string CustomerTitle { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Открыт")]
        public DateTime? BeginDate { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Закрыт")]
        public DateTime? EndDate { get; set; }

        [Display(Name = "КАМ")]
        public int? EmployeeCAMID { get; set; }
        [Display(Name = "КАМ")]
        public virtual Employee EmployeeCAM { get; set; }

        [Display(Name = "РП")]
        public int? EmployeePMID { get; set; }
        [Display(Name = "РП")]
        public virtual Employee EmployeePM { get; set; }

        [Display(Name = "Версия шаблона (фин. показатели)")]
        public string CalcDocTemplateVersion { get; set; }

        [Display(Name = "Дата и время загрузки (фин. показатели)")]
        [DataType(DataType.DateTime)]
        public DateTime? CalcDocUploaded { get; set; }

        [Display(Name = "Кем загружено (фин. показатели)")]
        public string CalcDocUploadedBy { get; set; }

        [Display(Name = "Версия шаблона (ПУП)")]
        public string CalcDocTemplateVersionPMP { get; set; }

        [Display(Name = "Дата и время загрузки (ПУП)")]
        [DataType(DataType.DateTime)]
        public DateTime? CalcDocUploadedPMP { get; set; }

        [Display(Name = "Кем загружено (ПУП)")]
        public string CalcDocUploadedByPMP { get; set; }

        [Display(Name = "Администратор")]
        public int? EmployeePAID { get; set; }
        [Display(Name = "Администратор")]
        public virtual Employee EmployeePA { get; set; }

        [Display(Name = "Исполнитель (юр. лицо)")]
        public int? OrganisationID { get; set; }
        public virtual Organisation Organisation { get; set; }

        [Display(Name = "Исполнитель (подразделение, отв. за реализацию проекта)")]
        public int? ProductionDepartmentID { get; set; }
        public virtual Department ProductionDepartment { get; set; }

        [Display(Name = "ЦФО")]
        public int? DepartmentID { get; set; }
        public virtual Department Department { get; set; }

        [Display(Name = "Сумма по договору (поступления за работы), руб.")]
        [DisplayFormat(DataFormatString = "{0:#,##0.00}", ApplyFormatInEditMode = true)]
        public decimal? ContractAmount { get; set; }

        [Display(Name = "Сумма поступлений на оборудование (для перепродажи), руб.")]
        [DisplayFormat(DataFormatString = "{0:#,##0.00}", ApplyFormatInEditMode = true)]
        public decimal? ContractEquipmentResaleAmount { get; set; }

        [Display(Name = "Затраты на оборудование для перепродажи, руб.")]
        [DisplayFormat(DataFormatString = "{0:#,##0.00}", ApplyFormatInEditMode = true)]
        public decimal? EquipmentCostsForResale { get; set; }

        [Display(Name = "Затраты на субподрядчиков (фин. показатели), руб.")]
        [DisplayFormat(DataFormatString = "{0:#,##0.00}", ApplyFormatInEditMode = true)]
        public decimal? SubcontractorsAmountBudget { get; set; }

        [Display(Name = "Затраты на субподрядчиков (ПУП), руб.")]
        [DisplayFormat(DataFormatString = "{0:#,##0.00}", ApplyFormatInEditMode = true)]
        public decimal? SubcontractorsAmountBudgetPMP { get; set; }

        [Display(Name = "Сумма компании, руб.")]
        [DisplayFormat(DataFormatString = "{0:#,##0.00}", ApplyFormatInEditMode = true)]
        public decimal? OrganisationAmountBudget { get; set; }

        [Display(Name = "Стоимость работ (фин. показатели), Ч-Ч")]
        [DisplayFormat(DataFormatString = "{0:#,##0}", ApplyFormatInEditMode = true)]
        public decimal? EmployeeHoursBudget { get; set; }

        [Display(Name = "Стоимость работ (ПУП), Ч-Ч")]
        [DisplayFormat(DataFormatString = "{0:#,##0}", ApplyFormatInEditMode = true)]
        public decimal? EmployeeHoursBudgetPMP { get; set; }

        [Display(Name = "ФОТ (фин. показатели), руб.")]
        [DisplayFormat(DataFormatString = "{0:#,##0.00}", ApplyFormatInEditMode = true)]
        public decimal? EmployeePayrollBudget { get; set; }

        [Display(Name = "ФОТ (ПУП), руб.")]
        [DisplayFormat(DataFormatString = "{0:#,##0.00}", ApplyFormatInEditMode = true)]
        public decimal? EmployeePayrollBudgetPMP { get; set; }

        [Display(Name = "Прочие затраты (фин. показатели), руб.")]
        [DisplayFormat(DataFormatString = "{0:#,##0.00}", ApplyFormatInEditMode = true)]
        public decimal? OtherCostsBudget { get; set; }

        [Display(Name = "Прочие затраты (ПУП), руб.")]
        [DisplayFormat(DataFormatString = "{0:#,##0.00}", ApplyFormatInEditMode = true)]
        public decimal? OtherCostsBudgetPMP { get; set; }

        [Display(Name = "Примечание")]
        [DataType(DataType.MultilineText)]
        public string Comments { get; set; }

        [Display(Name = "URL на папку документов проекта")]
        public string ProjectDocsURL { get; set; }

        [Display(Name = "Родительский проект")]
        public int? ParentProjectID { get; set; }
        public virtual Project ParentProject { get; set; }

        [Display(Name = "Трудозатраты по проекту")]
        public double? TotalHoursActual { get; set; }

        [Display(Name = "Наработано ФОТ (факт), руб.")]
        [DisplayFormat(DataFormatString = "{0:#,##0.00}", ApplyFormatInEditMode = true)]
        public decimal? EmployeePayrollTotalAmountActual { get; set; }

        [Display(Name = "Архивный")]
        public bool IsArchived { get; set; }
        [Display(Name = "Отменен")]
        public bool IsCancelled { get; set; }
        [Display(Name = "Приостановлен")]
        public bool IsPaused { get; set; }

        [Display(Name = "Полное наименование")]
        public string FullName
        {
            get
            {
                return ((ShortName != null) ? ShortName.Trim() + " - " : "") + ((Title != null) ? Title.Trim() : "");
            }
        }

        [Display(Name = "Статус")]
        public ProjectStatus Status
        {
            get
            {
                if (IsArchived) { return ProjectStatus.Archived; }
                if (IsCancelled) { return ProjectStatus.Cancelled; }
                if (IsPaused) { return ProjectStatus.Paused; }

                if (((BeginDate == null
                    || BeginDate.HasValue == false)
                    && (EndDate == null
                    || EndDate.HasValue == false))
                    || (BeginDate != null && BeginDate.HasValue == true && BeginDate.Value > DateTime.Today))
                {
                    return ProjectStatus.Planned;
                }
                else
                {
                    return (((EndDate == null || EndDate.HasValue == false) || EndDate.Value > DateTime.Today) ? ProjectStatus.Active : ProjectStatus.Closed);
                }
            }
        }

        public IEnumerable<ProjectReportRecord> ReportRecords { get; set; }

        public IEnumerable<ProjectStatusRecord> StatusRecords { get; set; }

        public IEnumerable<Project> ChildProjects { get; set; }

        public IEnumerable<ProjectMember> ProjectTeam { get; set; }

        public IEnumerable<ExpensesRecord> ExpensesRecords { get; set; }
        public IEnumerable<ProjectExternalWorkspace> ProjectExternalWorkspace { get; set; }

        public IEnumerable<Project> Versions { get; set; }
    }

    public class ProjectFluentValidator : AbstractValidator<Project>
    {
        public ProjectFluentValidator()
        {
            RuleFor(x => x.CustomerTitle).Matches("^[a-zа-яА-ЯA-Z0-9 .'\"«»]*$").WithMessage("Поле не должно иметь спец. символов");
        }
    }
}