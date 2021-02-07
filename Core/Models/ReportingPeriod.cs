using System;
using System.ComponentModel.DataAnnotations;
using Core.Models.RBAC;


namespace Core.Models
{
    [DisplayTableName("Отчетные периоды")]
    public class ReportingPeriod
    {
        [Display(Name = "ИД")]
        public int ID { get; set; }

        [Required]
        [Display(Name = "Год")]
        public int Year { get; set; }

        [Required]
        [Display(Name = "Месяц")]
        public int Month { get; set; }

        [Display(Name = "Дата закрытия месяца")]
        [DataType(DataType.Date)]
        public DateTime NewTSRecordsAllowedUntilDate { get; set; }

        [Required]
        [Display(Name = "Проект оплачиваемого отпуска")]
        public int VacationProjectID { get; set; }

        [Display(Name = "Проект оплачиваемого отпуска")]
        public virtual Project VacationProject { get; set; }

        [Required]
        [Display(Name = "Проект неоплачиваемого отпуска")]
        public int VacationNoPaidProjectID { get; set; }

        [Display(Name = "Проект неоплачиваемого отпуска")]
        public virtual Project VacationNoPaidProject { get; set; }

        [Display(Name = "Дата полного закрытия месяца")]
        [DataType(DataType.Date)]
        public DateTime TSRecordsEditApproveAllowedUntilDate { get; set; }

        [Display(Name = "Примечание")]
        public string Comments { get; set; }

        [Display(Name = "Полное наименование")]
        public string FullName
        {
            get
            {

                return ((Year != 0 ? Year.ToString() + "." : "") + (Month != 0 ? new DateTime(Year, Month, 1).Month.ToString().PadLeft(2, '0') : ""));
            }
        }

    }
}