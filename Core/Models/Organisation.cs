using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;
using Core.Models.RBAC;


namespace Core.Models
{
    [DisplayTableName("Организации")]
    public class Organisation
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