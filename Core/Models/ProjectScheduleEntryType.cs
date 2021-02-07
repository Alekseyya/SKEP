using System.ComponentModel.DataAnnotations;
using Core.Models.RBAC;


namespace Core.Models
{
    [DisplayTableName("Типы вех")]
    public class ProjectScheduleEntryType : BaseModel
    {
        [Display(Name = "ИД")]
        public int ID { get; set; }

        [Required]
        [Display(Name = "Код")]
        public string ShortName { get; set; }

        [Required]
        [Display(Name = "Код СДР")]
        public string WBSCode { get; set; }

        [Required]
        [Display(Name = "Наименование")]
        public string Title { get; set; }

        [Display(Name = "Тип проекта")]
        public int? ProjectTypeID { get; set; }
        [Display(Name = "Тип проекта")]
        public virtual ProjectType ProjectType { get; set; }

        [Display(Name = "Полное наименование")]
        public string FullName
        {
            get
            {
                return ((ShortName != null) ? ShortName.Trim() + " - " : "") + ((Title != null) ? Title.Trim() : "");
            }
        }

        [Display(Name = "Полное наименование (СДР)")]
        public string WBSCodeName
        {
            get
            {
                return ((WBSCode != null) ? WBSCode.Trim() + " - " : "") + ((Title != null) ? Title.Trim() : "");
            }
        }
    }
}
