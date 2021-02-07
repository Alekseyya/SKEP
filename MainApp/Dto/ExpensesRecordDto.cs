using System;
using Core.Models;


namespace MainApp.Dto
{
    public class ExpensesRecordDto
    {
        public int ID { get; set; }
        public DateTime ExpensesDate { get; set; }
        public int CostSubItemID { get; set; }
        public virtual CostSubItemDto CostSubItem { get; set; }
        public int DepartmentID { get; set; }
        public virtual BasicDepartmentDto Department { get; set; }
        public int ProjectID { get; set; }
        public virtual BasicProjectDto Project { get; set; }
        public decimal? Amount { get; set; }
        public string BitrixURegNum { get; set; }
        public ExpensesRecordStatus RecordStatus { get; set; }
        public string ExpensesRecordName { get; set; }
        public string SourceElementID { get; set; }
        public SourceDB SourceDB { get; set; }
    }
}