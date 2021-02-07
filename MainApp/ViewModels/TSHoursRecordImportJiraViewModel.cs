using Core.JIRA;
using Core.Models;


namespace MainApp.ViewModels
{
    public class TSHoursRecordImportJiraViewModel
    {
        public bool Selected { get; set; }
        public TSHoursRecord TSHoursRecord { get; set; }
        public string JiraIssueName { get; set; }
        public bool Imported { get; set; }
        public bool ImportedDeclinedRecord { get; set; }
        public bool ChangedRecord { get; set; }
        public bool IsProjectNotFound { get; set; }
        public string FullDescription { get; set; }
        public string JiraProjectKey { get; set; }
        public ErrorTypesJira? ErrorType { get; set; }
    }
}
