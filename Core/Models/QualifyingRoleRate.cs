using System;
using System.ComponentModel.DataAnnotations;
using Core.Models.RBAC;


namespace Core.Models
{
    [DisplayTableName("Ставки УПР")]
    public class QualifyingRoleRate
    {
        [Display(Name = "ИД")]
        public int ID { get; set; }

        [Required]
        [Display(Name = "ЦФО")]
        public int? DepartmentID { get; set; }
        public virtual Department Department { get; set; }

        [Required]
        [Display(Name = "Роль")]
        public int? QualifyingRoleID { get; set; }
        [Display(Name = "Роль")]
        public virtual QualifyingRole QualifyingRole { get; set; }

        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "Дата начала действия ставки")]
        public DateTime? RateDateBegin { get; set; }

        [Required]
        [Display(Name = "Расчетная ставка/час, руб.")]
        [DisplayFormat(DataFormatString = "{0:#,##0.00}", ApplyFormatInEditMode = true)]
        public decimal? RateValue { get; set; }

        [Display(Name = "Расчетная ставка/мес, руб.")]
        [DisplayFormat(DataFormatString = "{0:#,##0.00}", ApplyFormatInEditMode = true)]
        public decimal? MonthRateValue { get; set; }

        [Display(Name = "Поправочный коэффициент ЦФО")]
        [DisplayFormat(DataFormatString = "{0:#,##0.00}", ApplyFormatInEditMode = true)]
        public decimal? FRCCorrectionFactorValue { get; set; }

        [Display(Name = "Коэффициент инфляции ЦФО")]
        [DisplayFormat(DataFormatString = "{0:#,##0.00}", ApplyFormatInEditMode = true)]
        public decimal? FRCInflationRateValue { get; set; }

        [Display(Name = "Часыплан")]
        public int? HoursPlan { get; set; }

        [Display(Name = "Фактический КОТ/мес, руб.")]
        [DisplayFormat(DataFormatString = "{0:#,##0.00}", ApplyFormatInEditMode = true)]
        public decimal? ActualAverageMonthPayrollValue { get; set; }

        [Display(Name = "Фактический КОТ/час, руб.")]
        [DisplayFormat(DataFormatString = "{0:#,##0.00}", ApplyFormatInEditMode = true)]
        public decimal? ActualAverageHourPayrollValue { get; set; }
    }
}
