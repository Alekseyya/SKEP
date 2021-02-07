using System;
using System.ComponentModel.DataAnnotations;
using Core.Models.RBAC;


namespace Core.Models
{
    [DisplayTableName("Сотрудники в подразделениях")]
    public class EmployeeDepartmentAssignment
    {
        [Display(Name = "ИД")]
        public int ID { get; set; }

        [Required]
        [Display(Name = "Сотрудник")]
        public int? EmployeeID { get; set; }
        [Display(Name = "Сотрудник")]
        public virtual Employee Employee { get; set; }

        [Required]
        [Display(Name = "Подразделение")]
        public int? DepartmentID { get; set; }
        [Display(Name = "Подразделение")]
        public virtual Department Department { get; set; }

        [Required]
        [Display(Name = "Действует с даты")]
        public DateTime BeginDate { get; set; }

        [Display(Name = "Примечание")]
        public string Comments { get; set; }
    }
}