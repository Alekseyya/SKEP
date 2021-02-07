using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;
using Core.Models.RBAC;


namespace Core.Models
{
    public enum EmployeeCategoryType
    {
        [Display(Name = "Постоянный сотрудник")]
        Regular = 0,
        [Display(Name = "Временный сотрудник")]
        Temporary = 1,
        [Display(Name = "Фрилансер-почасовик")]
        FreelancerHourly = 2,
        [Display(Name = "Фрилансер-сдельщик")]
        FreelancerPiecework = 3,
        [Display(Name = "Сотрудник субподрядчика (юрлица)")]
        ExtContragentEmployee = 4,
    }

    [DisplayTableName("Категории сотрудников")]
    public class EmployeeCategory
    {
        [Display(Name = "ИД")]
        public int ID { get; set; }

        [Required]
        [Display(Name = "Сотрудник")]
        public int? EmployeeID { get; set; }
        [Display(Name = "Сотрудник")]
        public virtual Employee Employee { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Дата начала")]
        public DateTime? CategoryDateBegin { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Дата окончания")]
        public DateTime? CategoryDateEnd { get; set; }

        [Required]
        [Display(Name = "Категория")]
        public EmployeeCategoryType CategoryType { get; set; }

        [Display(Name = "% ставки")]
        [DisplayFormat(DataFormatString = "{0:0.##}", ApplyFormatInEditMode = true)]
        public decimal? EmploymentRatio { get; set; }

        [Display(Name = "Примечание")]
        [DataType(DataType.MultilineText)]
        public string Comments { get; set; }


        public static decimal[] GetEmploymentRatios() => new decimal[] { 1, 0.9m, 0.8m, 0.75m, 0.7m, 0.6m, 0.5m, 0.4m, 0.3m, 0.25m, 0.2m, 0.1m };

        public static string EmploymentRatioValueDefault => "Не установлено";

        public static Tuple<string, string>[] GetEmploymentRatioValues()
        {
            var ratios = GetEmploymentRatios();

            var values = new Tuple<string, string>[ratios.Length + 1];
            for (int i = 0; i < ratios.Length; i++)
                values[i] = new Tuple<string, string>(ratios[i].ToString(), ratios[i].ToString());
            values[values.Length - 1] = new Tuple<string, string>(string.Empty, EmploymentRatioValueDefault);
            return values;
        }
    }
}