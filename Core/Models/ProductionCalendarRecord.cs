using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;
using Core.Models.RBAC;


namespace Core.Models
{
    [DisplayTableName("Производственный календарь")]
    public class ProductionCalendarRecord
    {
        [Display(Name = "ИД")]
        public int ID { get; set; }

        [Required]
        [Display(Name = "Год")]
        public int Year { get; set; }

        [Required]
        [Display(Name = "Месяц")]
        public int Month { get; set; }

        [Required]
        [Display(Name = "День месяца")]
        public int Day { get; set; }

        [Required]
        [Display(Name = "Количество рабочих часов в дне")]
        public int WorkingHours { get; set; }

        // TODO: Стоит сделать в сеттере установку Year, Month и Day на основе значения CalendarDate
        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "Дата календаря")]
        public DateTime CalendarDate { get; set; }

        [Display(Name = "Праздничный день")]
        public bool IsCelebratory { get; set; }

    }
}