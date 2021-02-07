using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;
using Core.Models.RBAC;


namespace Core.Models
{
    [DisplayTableName("УПР сотрудников")]
    public class EmployeeQualifyingRole
    {
        [Display(Name = "ИД")]
        public int ID { get; set; }

        [Required]
        [Display(Name = "Сотрудник")]
        public int? EmployeeID { get; set; }
        [Display(Name = "Сотрудник")]
        public virtual Employee Employee { get; set; }

        [Required]
        [Display(Name = "Роль")]
        public int? QualifyingRoleID { get; set; }
        [Display(Name = "Роль")]
        public virtual QualifyingRole QualifyingRole { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Действует с даты")]
        public DateTime? QualifyingRoleDateBegin { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Действует до даты")]
        public DateTime? QualifyingRoleDateEnd { get; set; }

        [Display(Name = "Примечание")]
        [DataType(DataType.MultilineText)]
        public string Comments { get; set; }
    }
}
