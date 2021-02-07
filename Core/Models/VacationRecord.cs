
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using Core.Models.Attributes;
using Core.Models.RBAC;


namespace Core.Models
{

    public enum VacationRecordStatus
    {
        [Display(Name = "Все")]
        All = 0,
        [Display(Name = "Актуальные записи")]
        ActualVacancyRecord = 1,
    }

    public enum VacationRecordType
    {
        [Display(Name = "Все")]
        All = 0,
        [Display(Name = "Ежегодный оплачиваемый отпуск")]
        VacationPaid = 1,
        [Display(Name = "Отпуск без сохранения заработной платы")]
        VacationNoPaid = 2
    }
    public enum VacationRecordSource
    {
        [Display(Name = "Все")]
        All = 0,
        [Display(Name = "Ручной ввод")]
        UserInput = 1,
        [Display(Name = "Внешний Таймшит")]
        ExternalTS = 2
    }

    [AllowRecycleBin]
    [DisplayTableName("Записи об отпусках сотрудников")]
    public class VacationRecord : BaseModel
    {
        [Display(Name = "ИД")]
        public int ID { get; set; }

        [Required]
        [DisplayInRecycleBin(Order = 1)]
        [Display(Name = "Cотрудник")]
        public int EmployeeID { get; set; }
        [Display(Name = "Сотрудник")]
        public virtual Employee Employee { get; set; }

        [Required]
        [DisplayInRecycleBin(Order = 2)]
        [Display(Name = "Дата начала отпуска")]
        [DataType(DataType.Date)]
        public DateTime VacationBeginDate { get; set; }

        [Required]
        [DisplayInRecycleBin(Order = 4)]
        [Display(Name = "Вид отпуска")]
        public VacationRecordType VacationType { get; set; }

        [Required]
        [DisplayInRecycleBin(Order = 3)]
        [DataType(DataType.Date)]
        [Display(Name = "Дата окончания отпуска")]
        public DateTime VacationEndDate { get; set; }

        [Required]
        [DisplayInRecycleBin(Order = 5)]
        [Display(Name = "Количество дней отпуска")]
        public int VacationDays { get; set; }

        [Display(Name = "Источник")]
        public VacationRecordSource RecordSource { get; set; }

        [Display(Name = "ExternalSourceElementID")]
        public string ExternalSourceElementID { get; set; }

        [Display(Name = "ExternalSourceListID")]
        public string ExternalSourceListID { get; set; }

        [Display(Name = "Полное наименование")]
        public string FullName
        {
            get
            {
                return (Employee?.FullName + ": " + VacationBeginDate.ToString("dd'.'MM'.'yyyy", CultureInfo.InvariantCulture) + " - "
                        + VacationEndDate.ToString("dd'.'MM'.'yyyy", CultureInfo.InvariantCulture));
            }
        }
        public IEnumerable<VacationRecord> Versions { get; set; }

    }
}