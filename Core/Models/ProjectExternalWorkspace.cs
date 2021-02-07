using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;
using Core.Models.RBAC;


namespace Core.Models
{
    public enum ExternalWorkspaceType
    {
        [Display(Name = "Все")]
        All = 0,
        [Display(Name = "JIRA")]
        JIRA = 1,
        [Display(Name = "MS Project")]
        MSProject = 2,
    }

    [DisplayTableName("Внешние рабочие области")]
    public class ProjectExternalWorkspace
    {
        [Display(Name = "ИД")]
        public int ID { get; set; }

        [Display(Name = "Тип рабочей области")]
        public ExternalWorkspaceType WorkspaceType { get; set; }

        [Required]
        [Display(Name = "Код проекта Jira")]
        public string ExternalWorkspaceProjectShortName { get; set; }

        [Required]
        [Display(Name = "Проект")]
        public int? ProjectID { get; set; }
        [Display(Name = "Проект")]
        public virtual Project Project { get; set; }

        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "Дата начала действия")]
        public DateTime? ExternalWorkspaceDateBegin { get; set; }

        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "Дата окончания действия")]
        public DateTime? ExternalWorkspaceDateEnd { get; set; }
    }
}
