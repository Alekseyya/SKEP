using System.ComponentModel.DataAnnotations;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;
using Core.Models.RBAC;


namespace Core.Models
{
    public enum ProjectRoleType
    {
        [Display(Name = "Общая")]
        Common = 0,
        [Display(Name = "РП")]
        PM = 1,
        [Display(Name = "КАМ")]
        CAM = 2,
        [Display(Name = "ТРП")]
        TPM = 3,
        [Display(Name = "Аналитик")]
        Analyst = 4
    }

    [DisplayTableName("Проектные роли")]
    public class ProjectRole
    {
        [Key]
        [Display(Name = "ИД")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ID { get; set; }

        [Display(Name = "Код")]
        public string ShortName { get; set; }

        [Display(Name = "Название роли")]
        public string Title { get; set; }

        [Required]
        [Display(Name = "Тип роли")]
        public ProjectRoleType RoleType { get; set; }

        public string FullName
        {
            get
            {
                return ((ShortName != null) ? ShortName.Trim() + " - " : "") + ((Title != null) ? Title.Trim() : "");
            }
        }

    }
}