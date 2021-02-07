using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MainApp.Dto
{
    public class EmployeeLocationDto
    {
        public int Id { get; set; }
        public string ShortName { get; set; }
        public string Title { get; set; }

        public string FullName
        {
            get { return ((ShortName != null) ? ShortName.Trim() + "-" : "") + ((Title != null) ? Title.Trim() : ""); }
        }
    }
}
