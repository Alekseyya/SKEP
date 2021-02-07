using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Core.Models;


namespace MainApp.Dto
{
    public enum ParentStatus { None = 0, Parent = 1, Children = 2 }
    public class ProjectDto
    {
        public Project Project { get; set; }
        public ParentStatus ParentStatus { get; set; }
    }
}
