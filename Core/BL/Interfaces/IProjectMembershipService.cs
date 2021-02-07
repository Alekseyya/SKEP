using System.Collections.Generic;
using Core.BL;
using Core.Models;


namespace Core.BL.Interfaces
{
    public interface IProjectMembershipService : IEntityValidatingService<ProjectMember>, IServiceBase<ProjectMember, int>
    {
        IList<ProjectMember> GetAllProjectMembers();
        IList<Employee> GetAllProjectManagers();
        IList<ProjectMember> GetMembersForProject(int projectId);
        IList<Employee> GetEmployeesOnProject(int projectId);
        IList<Employee> GetEmployeesOnProject(int projectId, DateTimeRange dateRange);
        IList<ProjectMember> GetActualMembersForProject(int projectId, DateTimeRange dataRange);
        IList<Employee> GetManagersForProjects(IEnumerable<int> projects);
        IList<Project> GetProjectsForEmployee(int employeeId);
        IList<Project> GetProjectsForEmployee(int employeeId, DateTimeRange dateRange);
        IList<Project> GetProjectsForEmployeeInTSMyHours(int employeeId, DateTimeRange dateRange);
        IList<Project> GetProjectsForManager(int projectManagerId);
        IList<Project> GetProjectsForManager(int projectManagerId, DateTimeRange dateRange);
        IList<ProjectMember> GetProjectMembershipForEmployees(DateTimeRange dateRange, IEnumerable<Employee> employees);
        IList<ProjectMember> GetProjectMembershipForEmployees(DateTimeRange dateRange, IEnumerable<int> employeesIds);
        IList<ProjectMember> GetProjectMembershipForEmployees(DateTimeRange dateRange, params int[] employeesIds);
    }
}