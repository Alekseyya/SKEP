using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;
using Core.Models.RBAC;


namespace Core.Models
{
    [DisplayTableName("Грейды сотрудников")]
    public class EmployeeGradAssignment
    {
        [Display(Name = "ИД")]
        public int ID { get; set; }

        [Required]
        [Display(Name = "Сотрудник")]
        public int? EmployeeID { get; set; }
        [Display(Name = "Сотрудник")]
        public virtual Employee Employee { get; set; }

        [Required]
        [Display(Name = "Грейд")]
        public int? EmployeeGradID { get; set; }
        [Display(Name = "Грейд")]
        public virtual EmployeeGrad EmployeeGrad { get; set; }

        [Required]
        [Display(Name = "Действует с даты")]
        public DateTime BeginDate { get; set; }

        [Display(Name = "Примечание")]
        public string Comments { get; set; }
        
        [Display(Name = "ExternalSourceElementID")]
        public string ExternalSourceElementID { get; set; }

        [Display(Name = "ExternalSourceListID")]
        public string ExternalSourceListID { get; set; }
    }
}