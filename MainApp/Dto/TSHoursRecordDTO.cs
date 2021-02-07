using System;
using Core.Models;


namespace MainApp.Dto
{
    public class TSHoursRecordDTO
    {
        public int ID { get; set; }
        public int? ProjectID { get; set; }
        public virtual BasicProjectDto Project { get; set; }
        public string Hyperlink { get; set; }
        public string ExternalSourceElementID { get; set; }
        public int? EmployeeID { get; set; }
        public virtual BasicEmployeeDto Employee { get; set; }
        public DateTime? RecordDate { get; set; }
        public double? Hours { get; set; }
        public string Description { get; set; }
        public DateTime? Created { get; set; }
        public TSRecordStatus RecordStatus { get; set; }
        public TSRecordSource RecordSource { get; set; }
        public string PMComment { get; set; }
        public string ProjectShortName { get; set; }
        public int? VersionNumber { get; set; }
    }
}