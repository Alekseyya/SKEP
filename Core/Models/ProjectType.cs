using System.ComponentModel.DataAnnotations;
using Core.Models.RBAC;


namespace Core.Models
{
    public enum ProjectTypeTSApproveMode
    {
        [Display(Name = "По умолчанию")]
        Default = 0,
        [Display(Name = "РП")]
        PM = 1,
        [Display(Name = "КАМ")]
        CAM = 2
    }

    public enum ProjectTypeActivityType
    {
        [Display(Name = "Не установлено")]
        NotSpecified = 0,
        [Display(Name = "Коммерческий")]
        Commercial = 1,
        [Display(Name = "Некоммерческий")]
        NonCommercial = 2
    }

    [DisplayTableName("Типы проектов")]
    public class ProjectType
    {
        [Display(Name = "ИД")]
        public int ID { get; set; }

        [Required]
        [Display(Name = "Код")]
        public string ShortName { get; set; }

        [Required]
        [Display(Name = "Название")]
        public string Title { get; set; }

        [Required]
        [Display(Name = "Режим согласования трудозатрат")]
        public ProjectTypeTSApproveMode TSApproveMode { get; set; }

        [Display(Name = "Подстатья затрат на командировки")]
        public int? BusinessTripCostSubItemID { get; set; }
        public virtual CostSubItem BusinessTripCostSubItem { get; set; }

        [Required]
        [Display(Name = "Тип деятельности")]
        public ProjectTypeActivityType ActivityType { get; set; }

        [Display(Name = "Утилизация")]
        public bool Utilization { get; set; }

        [Display(Name = "Полное наименование")]
        public string FullName
        {
            get
            {
                return ((ShortName != null) ? ShortName.Trim() + " - " : "") + ((Title != null) ? Title.Trim() : "");
            }
        }
    }
}