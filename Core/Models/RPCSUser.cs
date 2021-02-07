using System.ComponentModel.DataAnnotations;
using Core.Models.RBAC;


namespace Core.Models
{
    [DisplayTableName("Пользователи")]
    public class RPCSUser
    {
        [Display(Name = "ИД")]
        public int ID { get; set; }

        [Required]
        [Display(Name = "Логин пользователя в AD")]
        public string UserLogin { get; set; }

        [Required]
        [Display(Name = "Адм системы")]
        public bool IsAdmin { get; set; }

        [Required]
        [Display(Name = "Адм AD")]
        public bool IsAdAdmin { get; set; }

        [Required]
        [Display(Name = "Отправлять email")]
        public bool AllowSendEmailNotifications { get; set; }

        [Required]
        [Display(Name = "HR")]
        public bool IsHR { get; set; }

        [Required]
        [Display(Name = "РП КАМ АдмПр")]
        public bool IsPM { get; set; }

        [Required]
        [Display(Name = "Адм БД Проекты")]
        public bool IsPMOAdmin { get; set; }

        [Required]
        [Display(Name = "Рук PMO")]
        public bool IsPMOChief { get; set; }

        [Required]
        [Display(Name = "Рук подр")]
        public bool IsDepartmentManager { get; set; }

        [Required]
        [Display(Name = "Рук компании")]
        public bool IsDirector { get; set; }

        [Required]
        [Display(Name = "ФиБ")]
        public bool IsFin { get; set; }

        [Required]
        [Display(Name = "Адм КОТ")]
        public bool IsPayrollAdmin { get; set; }

        [Required]
        [Display(Name = "Чтение КОТ подр")]
        public bool IsDepartmentPayrollRead { get; set; }

        [Required]
        [Display(Name = "Чтение КОТ")]
        public bool IsPayrollFullRead { get; set; }

        [Required]
        [Display(Name = "Адм данных")]
        public bool IsDataAdmin { get; set; }

        [Required]
        [Display(Name = "Адм ТШ")]
        public bool IsTSAdmin { get; set; }

        [Required]
        [Display(Name = "Адм пасп данных")]
        public bool IsIDDocsAdmin { get; set; }

        [Required]
        [Display(Name = "Сотрудник")]
        public bool IsEmployee { get; set; }

        [Required]
        [Display(Name = "API")]
        public bool IsApiAccess { get; set; }

        [Display(Name = "OOLogin")]
        public string OOLogin { get; set; }
    }
}