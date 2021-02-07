using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;
using Core.Models.RBAC;


namespace Core.Models
{
    [DisplayTableName("Подробности записей статус отчетов по проектам")]
    public class ProjectStatusRecordEntry
    {
        [Display(Name = "ИД")]
        public int ID { get; set; }

        [Required]
        [Display(Name = "Запись статус отчета")]
        public int? ProjectStatusRecordID { get; set; }
        public virtual ProjectStatusRecord ProjectStatusRecord { get; set; }

        [Display(Name = "Веха")]
        public int? ProjectScheduleEntryID { get; set; }
        public virtual ProjectScheduleEntry ProjectScheduleEntry { get; set; }

        [Display(Name = "Комментарий по ходу работ по вехе")]
        [DataType(DataType.MultilineText)]
        public string ProjectScheduleEntryComments { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Ожидание")]
        public DateTime? ExpectedDueDate { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Факт")]
        public DateTime? DateCompleted { get; set; }

        [Display(Name = "Комментарий")]
        [DataType(DataType.MultilineText)]
        public string Comments { get; set; }

        [NotMapped]
        [Display(Name = "Полное наименование")]
        public string FullName
        {
            get
            {
                if (ProjectStatusRecord == null) throw new ArgumentException(nameof(ProjectStatusRecord));
                return (ProjectScheduleEntry != null) ?
                    $"{ProjectStatusRecord.StatusPeriodName} ({ProjectStatusRecord.ProjectStatusBeginDate.Value:dd.MM} - {ProjectStatusRecord.ProjectStatusEndDate.Value:dd.MM}) - {ProjectScheduleEntry.FullName}" :
                    $"{ProjectStatusRecord.StatusPeriodName} ({ProjectStatusRecord.ProjectStatusBeginDate.Value:dd.MM} - {ProjectStatusRecord.ProjectStatusEndDate.Value:dd.MM})";
            }
        }
    }
}
