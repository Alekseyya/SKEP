using System.ComponentModel.DataAnnotations;
using Core.Models.RBAC;


namespace Core.Models
{
    public enum QualifyingRoleType
    {
        [Display(Name = "ПРОИЗВ")]
        Production = 0,
        [Display(Name = "АДМ")]
        Administrative = 1
    }

    [DisplayTableName("Список УПР")]
    public class QualifyingRole
    {
        [Display(Name = "ИД")]
        public int ID { get; set; }

        [Display(Name = "Код")]
        public string ShortName { get; set; }

        [Display(Name = "Название роли")]
        public string Title { get; set; }

        [Required]
        [Display(Name = "Тип роли")]
        public QualifyingRoleType RoleType { get; set; }

        public string FullName
        {
            get
            {
                return ((ShortName != null) ? ShortName.Trim() + " - " : "") + ((Title != null) ? Title.Trim() : "");
            }
        }
    }
}
