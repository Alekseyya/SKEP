using System.ComponentModel.DataAnnotations;
using Core.Models.RBAC;


namespace Core.Models
{
    [DisplayTableName("Должности")]
    public class EmployeePosition
    {
        [Display(Name = "ИД")]
        public int ID { get; set; }

        [Required]
        [Display(Name = "Код")]
        public string ShortName { get; set; }

        [Required]
        [Display(Name = "Наименование")]
        public string Title { get; set; }

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