using System;
using System.Collections.Generic;
using System.Linq;
using Core;
using Core.BL;
using Core.Validation;
using Microsoft.EntityFrameworkCore;
using BL.Validation;
using Core;

using Core.BL.Interfaces;
using Core.Data;
using Core.Data.Interfaces;
using Core.Models;





namespace BL.Implementation
{
    public class ProjectMembershipService : RepositoryAwareServiceBase<ProjectMember, int, IProjectMembersRepository>, IProjectMembershipService
    {
        public ProjectMembershipService(IRepositoryFactory repositoryFactory) : base(repositoryFactory)
        { }

        public IList<ProjectMember> GetAllProjectMembers()
        {
            var projectMemberRepository = RepositoryFactory.GetRepository<IProjectMembersRepository>();
            return projectMemberRepository.GetAll();
        }

        public IList<Employee> GetAllProjectManagers()
        {
            var projectMamagerIds = RepositoryFactory.GetRepository<IProjectRepository>().GetQueryable()
                .Where(projectManagerIds => projectManagerIds.EmployeePM != null).Select(x => x.EmployeePMID).Distinct().ToList();
            var employees = RepositoryFactory.GetRepository<IEmployeeRepository>().GetQueryable()
                .Where(c => projectMamagerIds.Any(x => x == c.ID)).ToList();
            return employees;
        }

        public IList<ProjectMember> GetMembersForProject(int projectId)
        {
            var projectMemberRepository = RepositoryFactory.GetRepository<IProjectMembersRepository>();
            return projectMemberRepository.GetAll(pm => pm.ProjectID.Value == projectId);
        }

        public IList<ProjectMember> GetActualMembersForProject(int projectId, DateTimeRange dataRange)
        {
            var projectMemberRepository = RepositoryFactory.GetRepository<IProjectMembersRepository>();
            return projectMemberRepository.GetQueryable().Include(x=>x.ProjectRole).Include(x=>x.Project).Where(pm => pm.ProjectID == projectId
                                                                      && (!pm.MembershipDateBegin.HasValue || pm.MembershipDateBegin.Value <= dataRange.End.Date)
                                                                      && (!pm.MembershipDateEnd.HasValue || pm.MembershipDateEnd.Value >= dataRange.Begin.Date)).ToList();
        }

        public IList<Employee> GetEmployeesOnProject(int projectId)
        {
            var employeeRepository = RepositoryFactory.GetRepository<IEmployeeRepository>();
            var projectMemberRepository = RepositoryFactory.GetRepository<IProjectMembersRepository>();

            var employeesIds = projectMemberRepository.GetQueryable().Where(pm => pm.ProjectID == projectId && pm.EmployeeID.HasValue)
                                                .Select(pm => pm.EmployeeID.Value);
            var employees = employeeRepository.GetQueryable().Where(emp => employeesIds.Contains(emp.ID));
            return employees.ToList();
        }

        public IList<Employee> GetEmployeesOnProject(int projectId, DateTimeRange dateRange)
        {
            var employeeRepository = RepositoryFactory.GetRepository<IEmployeeRepository>();
            var projectMemberRepository = RepositoryFactory.GetRepository<IProjectMembersRepository>();

            var employeesIds = projectMemberRepository.GetQueryable().Where(pm => pm.ProjectID == projectId
                                                             && (!pm.MembershipDateBegin.HasValue || pm.MembershipDateBegin.Value <= dateRange.End.Date)
                                                             && (!pm.MembershipDateEnd.HasValue || pm.MembershipDateEnd.Value >= dateRange.Begin.Date)
                                                             && pm.EmployeeID.HasValue)
                                                .Select(pm => pm.EmployeeID.Value);
            var employees = employeeRepository.GetQueryable().Where(emp => employeesIds.Contains(emp.ID));
            return employees.ToList();
        }

        public IList<Project> GetProjectsForEmployee(int employeeId)
        {
            var projectMemberRepository = RepositoryFactory.GetRepository<IProjectMembersRepository>();
            var projectRepository = RepositoryFactory.GetRepository<IProjectRepository>();

            var projectsIds = projectMemberRepository.GetQueryable().Where(pm => pm.EmployeeID == employeeId && pm.ProjectID.HasValue)
                                                    .Select(pm => pm.ProjectID.Value);
            var projects = projectRepository.GetQueryable().Where(p => projectsIds.Contains(p.ID)
            || (p.AllowTSRecordWithoutProjectMembership == true && projectsIds.Contains(p.ID) == false));

            return projects.ToList();
        }

        public IList<Project> GetProjectsForEmployee(int employeeId, DateTimeRange dateRange)
        {
            var projectMemberRepository = RepositoryFactory.GetRepository<IProjectMembersRepository>();
            var projectRepository = RepositoryFactory.GetRepository<IProjectRepository>();

            var projectsIds = new List<int>();

            var projects = new List<Project>();

            projectsIds = projectMemberRepository.GetQueryable().Where(pm => pm.EmployeeID == employeeId
                                                                                           && (!pm.MembershipDateBegin.HasValue || pm.MembershipDateBegin.Value <= dateRange.End.Date)
                                                                                           && (!pm.MembershipDateEnd.HasValue || pm.MembershipDateEnd.Value >= dateRange.Begin.Date)
                                                                                           && pm.ProjectID.HasValue)
            .Select(pm => pm.ProjectID.Value).ToList();
            projects = projectRepository.GetQueryable().Where(p => projectsIds.Contains(p.ID)
            || (p.AllowTSRecordWithoutProjectMembership == true && projectsIds.Contains(p.ID) == false
            && (!p.BeginDate.HasValue || p.BeginDate.Value <= dateRange.End.Date)
            && (!p.EndDate.HasValue || p.EndDate.Value >= dateRange.Begin.Date))).ToList();


            return projects;
        }

        public IList<Project> GetProjectsForEmployeeInTSMyHours(int employeeId, DateTimeRange dateRange)
        {
            var projectMemberRepository = RepositoryFactory.GetRepository<IProjectMembersRepository>();
            var projectRepository = RepositoryFactory.GetRepository<IProjectRepository>();

            var projectsIds = new List<int>();

            var projects = new List<Project>();


            projectsIds = projectMemberRepository.GetQueryable().Where(pm => pm.EmployeeID == employeeId
                                                                                           && (!pm.MembershipDateBegin.HasValue || pm.MembershipDateBegin.Value <= dateRange.End.Date)
                                                                                           && (!pm.MembershipDateEnd.HasValue || pm.MembershipDateEnd.Value >= dateRange.Begin.Date)
                                                                                           && pm.ProjectID.HasValue)
                                                                        .Select(pm => pm.ProjectID.Value).ToList();

            projects = projectRepository.GetQueryable().Where(p => projectsIds.Contains(p.ID)
                                                                            && (!p.BeginDate.HasValue || p.BeginDate.Value <= dateRange.End.Date)
                                                                            && (!p.EndDate.HasValue || p.EndDate.Value >= dateRange.Begin.Date)).ToList()
                                                                           .Where(p => p.Status != ProjectStatus.Archived
                                                                           && p.Status != ProjectStatus.Cancelled
                                                                           && p.Status != ProjectStatus.Paused).ToList();

            projects.AddRange(projectRepository.GetQueryable().Where(p => p.AllowTSRecordWithoutProjectMembership
                                                                            && projectsIds.Contains(p.ID) == false
                                                                            && (!p.BeginDate.HasValue || p.BeginDate.Value <= dateRange.End.Date)
                                                                            && (!p.EndDate.HasValue || p.EndDate.Value >= dateRange.Begin.Date)).ToList()
                                                                            .Where(p => p.Status != ProjectStatus.Archived
                                                                            && p.Status != ProjectStatus.Cancelled
                                                                            && p.Status != ProjectStatus.Paused).ToList());


            return projects;
        }


        public IList<Project> GetProjectsForManager(int projectManagerId)
        {
            var projectMemberRepository = RepositoryFactory.GetRepository<IProjectMembersRepository>();
            var projectRepository = RepositoryFactory.GetRepository<IProjectRepository>();

            var projectsIds = projectMemberRepository.GetQueryable().Where(pm => pm.EmployeeID == projectManagerId
                                                                        && pm.ProjectRole != null
                                                                        && pm.ProjectRole.RoleType == ProjectRoleType.PM
                                                                        && pm.ProjectID.HasValue)
                                            .Select(pm => pm.ProjectID.Value);
            var projects = projectRepository.GetQueryable().Where(p => projectsIds.Contains(p.ID));
            return projects.ToList();
        }

        public IList<Project> GetProjectsForManager(int projectManagerId, DateTimeRange dateRange)
        {
            var projectMemberRepository = RepositoryFactory.GetRepository<IProjectMembersRepository>();
            var projectRepository = RepositoryFactory.GetRepository<IProjectRepository>();

            var projectsIds = projectMemberRepository.GetQueryable().Where(pm => pm.EmployeeID == projectManagerId
                                                                        && (!pm.MembershipDateBegin.HasValue || pm.MembershipDateBegin.Value <= dateRange.End.Date)
                                                                        && (!pm.MembershipDateEnd.HasValue || pm.MembershipDateEnd.Value >= dateRange.Begin.Date)
                                                                        && pm.ProjectRole != null
                                                                        && pm.ProjectRole.RoleType == ProjectRoleType.PM
                                                                        && pm.ProjectID.HasValue)
                                            .Select(pm => pm.ProjectID.Value);
            var projects = projectRepository.GetQueryable().Where(p => projectsIds.Contains(p.ID));
            return projects.ToList();
        }

        public IList<ProjectMember> GetProjectMembershipForEmployees(DateTimeRange dateRange, IEnumerable<Employee> employees)
        {
            if (employees == null)
                throw new ArgumentNullException(nameof(employees));

            return GetProjectMembershipForEmployees(dateRange, employees.Select(emp => emp.ID));
        }

        public IList<ProjectMember> GetProjectMembershipForEmployees(DateTimeRange dateRange, params int[] employeesIds)
        {
            if (employeesIds == null)
                throw new ArgumentNullException(nameof(employeesIds));
            if (employeesIds.Length == 0)
                throw new ArgumentException("Необходимо указать хотя бы один ID сотрудника", nameof(employeesIds));

            return GetProjectMembershipForEmployees(dateRange, (IEnumerable<int>)employeesIds);
        }

        public IList<ProjectMember> GetProjectMembershipForEmployees(DateTimeRange dateRange, IEnumerable<int> employeesIds)
        {
            if (employeesIds == null)
                throw new ArgumentNullException(nameof(employeesIds));

            var projectMemberRepository = RepositoryFactory.GetRepository<IProjectMembersRepository>();

            var projectMembers = projectMemberRepository.GetQueryable().Include(pm => pm.Employee)
                                            .Where(pm => (!pm.MembershipDateBegin.HasValue || pm.MembershipDateBegin.Value <= dateRange.End.Date)
                                                            && (!pm.MembershipDateEnd.HasValue || pm.MembershipDateEnd.Value >= dateRange.Begin.Date)
                                                            && pm.EmployeeID.HasValue
                                                            && employeesIds.Contains(pm.EmployeeID.Value));
            return projectMembers.ToList();
        }

        public void Validate(ProjectMember entity, IValidationRecipient validationRecipient)
        {
            var projectRepository = RepositoryFactory.GetRepository<IProjectRepository>();
            var validator = new ProjectMemberValidator(entity, validationRecipient, projectRepository.GetQueryable());
            validator.Validate();
        }

        public IList<Employee> GetManagersForProjects(IEnumerable<int> projects)
        {
            var projectMemberRepository = RepositoryFactory.GetRepository<IProjectMembersRepository>();
            var listPMs = projectMemberRepository.GetQueryable().Where(pm => projects.Any(id => id == pm.ProjectID))
                .Select(project => project.Project)
                .Select(emp => emp.EmployeePM).Distinct().ToList();
            return listPMs;
        }
    }
}