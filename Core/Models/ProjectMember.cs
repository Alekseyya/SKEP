using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Core.Models.RBAC;


namespace Core.Models
{
    [DisplayTableName("Рабочие группы проектов")]
    public class ProjectMember
    {
        [Display(Name = "ИД")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ID { get; set; }

        [Display(Name = "Проект")]
        public int? ProjectID { get; set; }
        [Display(Name = "Проект")]
        public virtual Project Project { get; set; }

        [Display(Name = "Сотрудник")]
        public int? EmployeeID { get; set; }
        [Display(Name = "Сотрудник")]
        public virtual Employee Employee { get; set; }

        [Display(Name = "Роль")]
        public int? ProjectRoleID { get; set; }
        [Display(Name = "Роль")]
        public virtual ProjectRole ProjectRole { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Дата начала вступления в роль")]
        public DateTime? MembershipDateBegin { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Дата окончания участия в роли")]
        public DateTime? MembershipDateEnd { get; set; }

        [Display(Name = "Загрузка (%)")]
        public int AssignmentPercentage { get; set; }

    }
}