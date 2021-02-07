using System;

namespace MainApp.Dto
{
    public class TSAutoHoursRecordDTO
    {
        public int ID { get; set; }

        public int? EmployeeID { get; set; }
       
        public virtual BasicEmployeeDto Employee { get; set; }
       
        public DateTime? BeginDate { get; set; }
       
        public DateTime? EndDate { get; set; }
       
        public double? DayHours { get; set; }
       
        public int? ProjectID { get; set; }
        public virtual BasicProjectDto Project { get; set; }
    }
}