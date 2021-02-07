using System;
using Core.Models;


namespace MainApp.Dto
{
    public class BasicDepartmentDto
    {
        public int Id { get; set; }

        public string ShortName { get; set; }

        public string Code { get; set; }

        public string ShortTitle { get; set; }

        public string Title { get; set; }

        public string FullName { get; set; }

        public int? ParentDepartmentId { get; set; }

        public int? ManagerId { get; set; }

        public BasicDepartmentDto()
        { }

        public BasicDepartmentDto(Department department)
        {
            if (department == null)
                throw new ArgumentNullException(nameof(department));

            Id = department.ID;
            ShortName = department.ShortName;
            Code = department.DisplayShortName;
            ShortTitle = department.DisplayShortTitle;
            Title = department.Title;
            FullName = department.FullName;
            ParentDepartmentId = department.ParentDepartmentID;
            ManagerId = department.DepartmentManagerID;
        }
    }
}