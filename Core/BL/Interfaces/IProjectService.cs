using System;
using System.Collections.Generic;
using System.Linq;
using Core.BL;
using Core.Models;


namespace Core.BL.Interfaces
{
    public interface IProjectService : IEntityValidatingService<Project>, IServiceBase<Project, int>
    {
        IList<Project> GetAll(string sortField, string sortOrder, string searchQuery, ProjectStatus projectStatus, int? employeeId);
        Project GetById(int id, bool includeRelations);
        Project GetByShortName(string shortName);
        IList<ProjectReportRecord> GetReportRecords(int? projectId, bool employeeSorting);
        IList<ProjectStatusRecord> GetStatusRecords(int? projectId);
        Project GetVersion(int id, int version);
        Project GetVersion(int id, int version, bool includeRelations);
        Project LoadRelations(Project project);
        IList<ProjectRole> GetProjectRoles();
    }
}