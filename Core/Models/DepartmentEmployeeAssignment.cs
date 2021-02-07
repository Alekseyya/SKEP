using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ComponentModel.DataAnnotations;
using Core.Models.RBAC;


namespace Core.Models
{
    [DisplayTableName("Назначения сотрудников руководителями подразделений")]
    public class DepartmentManagerAssignment
    {
        [Display(Name = "ИД")]
        public int ID { get; set; }

        [Required]
        [Display(Name = "Руководитель подразделения")]
        public int? DepartmentManagerID { get; set; }
        [Display(Name = "Руководитель подразделения")]
        public virtual Employee DepartmentManager { get; set; }

        [Required]
        [Display(Name = "Подразделение")]
        public int? DepartmentID { get; set; }
        [Display(Name = "Подразделение")]
        public virtual Department Department { get; set; }

        [Required]
        [Display(Name = "Дата назначения")]
        public DateTime BeginDate { get; set; }

        [Display(Name = "Примечание")]
        public string Comments { get; set; }


    }
}