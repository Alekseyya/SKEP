using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Core.Models.RBAC;


namespace Core.Models
{
    [DisplayTableName("Параметры грейдов")]
    public class EmployeeGradParam
    {
        [Display(Name = "ИД")]
        public int ID { get; set; }

        [Required]
        [Display(Name = "Грейд")]
        public int? EmployeeGradID { get; set; }
        [Display(Name = "Грейд")]
        public virtual EmployeeGrad EmployeeGrad { get; set; }

        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "Дата начала действия")]
        public DateTime BeginDate { get; set; }

        [Required]
        [Display(Name = "Тип УПР")]
        public QualifyingRoleType RoleType { get; set; }

        [Required]
        [Display(Name = "% выплат от годовой зп")]
        [DisplayFormat(DataFormatString = "{0:0.##}", ApplyFormatInEditMode = true)]
        public decimal? EmployeeYearPayrollRatio { get; set; }

        [Display(Name = "Примечание")]
        [DataType(DataType.MultilineText)]
        public string Comments { get; set; }

        [NotMapped]
        [Display(Name = "Полное наименование")]
        public string FullName
        {
            get
            {
                return $"{EmployeeGrad.FullName} - {BeginDate.ToShortDateString()}";
            }
        }
    }
}
