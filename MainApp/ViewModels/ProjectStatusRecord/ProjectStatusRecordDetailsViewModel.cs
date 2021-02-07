using System.Collections.Generic;
using Core.Models;


namespace MainApp.ViewModels.ProjectStatusRecord
{
    public class ProjectStatusRecordDetailsViewModel
    {

        public Core.Models.ProjectStatusRecord ProjectStatusRecord { get; set; }
        public IList<ProjectMember> ProjectMembers { get; set; }
        public IList<ProjectScheduleEntry> ProjectScheduleEntryList { get; set; }
    }
}
