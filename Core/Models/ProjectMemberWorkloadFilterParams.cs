using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace Core.Models
{
    public enum ProjectMemberWorkloadFilterType
    {
        ByDates,
        ByLoad
    }

    public enum ProjectMemberWorkloadLoadType
    {
        LessThan50,
        MoreThan50
    }

    public class ProjectMemberWorkloadFilterParams
    {
        [Required]
        [Display(Name = "Проект")]
        public int ProjectID { get; set; }

        [Display(Name = "Участник проекта")]
        public int MemberEmployeeID { get; set; }

        [Required]
        public ProjectMemberWorkloadFilterType FilterType { get; set; }

        public DateTime? DateFrom { get; set; }

        public DateTime? DateTo { get; set; }

        public ProjectMemberWorkloadLoadType LoadType { get; set; }
    }
    
}