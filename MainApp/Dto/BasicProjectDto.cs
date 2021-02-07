using System;
using Core.Models;


namespace MainApp.Dto
{
    public class BasicProjectDto
    {
        public int Id { get; set; }

        public string ShortName { get; set; }

        public string Title { get; set; }

        public string FullName { get; set; }
        public int? ApproveHoursEmployeeId { get; set; }
        public string ApproveHoursEmployeeName { get; set; }

        public int? ProjectTypeId { get; set; }

        public string CustomerTitle { get; set; }

        public DateTime? BeginDate { get; set; }

        public DateTime? EndDate { get; set; }

        public int? EmployeeKAMId { get; set; }

        public string EmployeeKAMName { get; set; }

        public int? EmployeePMId { get; set; }

        public string EmployeePMName { get; set; }

        public int? EmployeePAId { get; set; }

        public string EmployeePAName { get; set; }

        public int? OrganizationId { get; set; }

        public int? ExecutorDepartmentId { get; set; }

        public string Comments { get; set; }

        public int? ParentProjectId { get; set; }

        public ProjectStatus Status { get; set; }

        public BasicProjectDto()
        { }

        public BasicProjectDto(Project project)
        {
            if (project == null)
                throw new ArgumentNullException(nameof(project));

            Id = project.ID;
            ShortName = project.ShortName;
            Title = project.Title;
            FullName = project.FullName;
            ProjectTypeId = project.ProjectTypeID;
            CustomerTitle = project.CustomerTitle;
            BeginDate = project.BeginDate;
            EndDate = project.EndDate;
            EmployeeKAMId = project.EmployeeCAMID;
            EmployeeKAMName = project.EmployeeCAM?.FullName;
            EmployeePMId = project.EmployeePMID;
            EmployeePMName = project.EmployeePM?.FullName;
            EmployeePAId = project.EmployeePAID;
            EmployeePAName = project.EmployeePA?.FullName;
            OrganizationId = project.OrganisationID;
            ExecutorDepartmentId = project.DepartmentID;
            Comments = project.Comments;
            ParentProjectId = project.ParentProjectID;
            Status = project.Status;
        }
    }
}