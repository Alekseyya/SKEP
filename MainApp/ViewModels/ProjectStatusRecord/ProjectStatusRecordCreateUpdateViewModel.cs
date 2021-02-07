using System.Collections.Generic;
using Core.Models;


namespace MainApp.ViewModels.ProjectStatusRecord
{
    public class ProjectStatusRecordCreateUpdateViewModel
    {
        public Project Project { get; set; }
        public Core.Models.ProjectStatusRecord ProjectStatusRecord { get; set; }
        public IList<ProjectStatusRecordEntryViewModel> ProjectStatusRecordEntryList { get; set; }
    }
}
