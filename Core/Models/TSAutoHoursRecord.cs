
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using Core.Models.Attributes;
using Core.Models.RBAC;


namespace Core.Models
{
    [AllowRecycleBin]
    [DisplayTableName("Записи автозагрузки ТШ")]
    public class TSAutoHoursRecord : BaseModel
    {
        [Display(Name = "ИД")]
        public int ID { get; set; }

        [Required]
        [DisplayInRecycleBin(Order = 1)]
        [Display(Name = "Сотрудник")]
        public int? EmployeeID { get; set; }
        [Display(Name = "Сотрудник")]
        public virtual Employee Employee { get; set; }

        [Required]
        [DisplayInRecycleBin(Order = 2)]
        [DataType(DataType.Date)]
        [Display(Name = "Дата начала действия")]
        public DateTime? BeginDate { get; set; }

        [Required]
        [DisplayInRecycleBin(Order = 3)]
        [DataType(DataType.Date)]
        [Display(Name = "Дата окончания действия")]
        public DateTime? EndDate { get; set; }

        [Required]
        [DisplayInRecycleBin(Order = 5)]
        [Display(Name = "Количество часов")]
        [DisplayFormat(DataFormatString = "{0:0.0}", ApplyFormatInEditMode = true)]
        public double? DayHours { get; set; }

        [Required]
        [DisplayInRecycleBin(Order = 4)]
        [Display(Name = "Проект")]
        public int? ProjectID { get; set; }
        [Display(Name = "Проект")]
        public virtual Project Project { get; set; }

        [Display(Name = "Полное наименование")]
        public string FullName
        {
            get
            {
                return (Employee.FullName + ": " + BeginDate?.ToString("dd'.'MM'.'yyyy", CultureInfo.InvariantCulture)
                        + " - " + EndDate?.ToString("dd'.'MM'.'yyyy", CultureInfo.InvariantCulture) + " "
                        + Project.ShortName + " (" + DayHours.ToString() + ")");
            }
        }

        public IEnumerable<TSAutoHoursRecord> Versions { get; set; }
    }
}