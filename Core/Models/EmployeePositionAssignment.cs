using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;
using Core.Models.RBAC;


namespace Core.Models
{
    [DisplayTableName("Сотрудники на должностях")]
    public class EmployeePositionAssignment
    {
        [Display(Name = "ИД")]
        public int ID { get; set; }

        [Required]
        [Display(Name = "Сотрудник")]
        public int? EmployeeID { get; set; }
        [Display(Name = "Сотрудник")]
        public virtual Employee Employee { get; set; }

        [Required]
        [Display(Name = "Должность из справочника")]
        public int? EmployeePositionID { get; set; }
        [Display(Name = "Должность из справочника")]
        public virtual EmployeePosition EmployeePosition { get; set; }

        [Display(Name = "Должность")]
        public string EmployeePositionTitle { get; set; }

        [Required]
        [Display(Name = "Действует с даты")]
        public DateTime BeginDate { get; set; }

        [Display(Name = "Примечание")]
        public string Comments { get; set; }
    }
}