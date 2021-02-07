
using System.Collections.Generic;
using Core.Models;


namespace Core.Data.Interfaces
{
    public interface IProjectRepository : IRepository<Project, int>
    {
        Project GetVersion(int projectId, int version);

        IList<Project> GetVersions(int projectId);

        IList<Project> GetVersions(int projectId, bool withChangeInfo);

        Project LoadReferences(Project project);
    }
}
