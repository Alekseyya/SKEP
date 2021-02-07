using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Core.BL;
using Core.Validation;
using Microsoft.EntityFrameworkCore;
using BL.Validation;

using Core.BL.Interfaces;
using Core.Data;
using Core.Data.Interfaces;
using Core.Extensions;
using Core.Models;




namespace BL.Implementation
{
    public class ProjectService : RepositoryAwareServiceBase<Project, int, IProjectRepository>, IProjectService
    {
        private readonly (string, string) _user;

        public ProjectService(IRepositoryFactory repositoryFactory, IUserService userService) : base(repositoryFactory)
        {
            _user = userService.GetUserDataForVersion();
        }

        public override Project GetById(int id)
        {
            return GetById(id, false);
        }

        public Project GetById(int id, bool includeRelations)
        {
            var repository = RepositoryFactory.GetRepository<IProjectRepository>();
            var project = repository.GetById(id);
            if (project != null && includeRelations)
            {
                LoadRelations(project);
                project.Versions = repository.GetVersions(project.ID, true);
            }
            return project;
        }

        public Project GetByShortName(string shortName)
        {
            var projectRepository = RepositoryFactory.GetRepository<IProjectRepository>().GetQueryable();
            var project = projectRepository.FirstOrDefault(pr => pr.ShortName.ToLower() == shortName.ToLower());
            return project;
        }

        public Project GetVersion(int id, int version)
        {
            return GetVersion(id, version, false);
        }

        public Project GetVersion(int id, int version, bool includeRelations)
        {
            var repository = RepositoryFactory.GetRepository<IProjectRepository>();
            var project = repository.GetVersion(id, version);
            if (project != null && includeRelations)
            {
                LoadRelations(project);
                project.Versions = new List<Project>();
            }
            return project;
        }

        public IList<Project> GetAll(string sortField, string sortOrder, string searchQuery, ProjectStatus projectStatus, int? employeeId)
        {
            IEnumerable<Project> projects = RepositoryFactory.GetRepository<IProjectRepository>().GetQueryable()
                .Include(p => p.EmployeePM).Include(p => p.Department).Include(p => p.ProjectType)
                .ToList();
            if (employeeId.HasValue)
                projects = projects.Where(p => p.EmployeePMID == employeeId.Value || p.EmployeeCAMID == employeeId.Value || p.EmployeePAID == employeeId.Value);

            if (!string.IsNullOrWhiteSpace(searchQuery))
            {
                var searchTokens = searchQuery.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(token => token.ToLower());

                foreach (string searchToken in searchTokens)
                {
                    // Добавить проверку на null
                    // добавить доп условие по поиску
                    projects = projects.Where(p => (p.ShortName != null && p.ShortName.ToLower().Contains(searchToken))
                                                   || p.Title.ToLower().Contains(searchToken)
                                                   || (p.ProjectType != null && p.ProjectType.ShortName.ToLower().Contains(searchToken))
                                                   || (p.Department != null && p.Department.DisplayShortTitle.ToLower().Contains(searchToken))
                                                   || (p.ProductionDepartment != null && p.ProductionDepartment.DisplayShortTitle.ToLower().Contains(searchToken))
                                                   || (p.EmployeePM != null && (
                                                           (p.EmployeePM.FirstName?.ToLower().Contains(searchToken)).NullableBoolToBool()
                                                           || (p.EmployeePM.LastName?.ToLower().Contains(searchToken)).NullableBoolToBool()
                                                           || (p.EmployeePM.MidName?.ToLower().Contains(searchToken)).NullableBoolToBool()
                                                       )
                                                       || (p.EmployeeCAM != null && (
                                                               (p.EmployeeCAM.FirstName?.ToLower().Contains(searchToken)).NullableBoolToBool()
                                                               || (p.EmployeeCAM.LastName?.ToLower().Contains(searchToken)).NullableBoolToBool()
                                                               || (p.EmployeeCAM.MidName?.ToLower().Contains(searchToken)).NullableBoolToBool())
                                                       )
                                                   ));
                }
            }
            if (projectStatus != ProjectStatus.All)
            {
                switch (projectStatus)
                {
                    case ProjectStatus.Active:
                        projects = projects.Where(p => p.Status == ProjectStatus.Active);
                        break;
                    case ProjectStatus.Closed:
                        projects = projects.Where(p => p.Status == ProjectStatus.Closed);
                        break;
                    case ProjectStatus.Planned:
                        projects = projects.Where(p => p.Status == ProjectStatus.Planned);
                        break;
                    default:
                        throw new NotSupportedException($"Значение статуса проекта {projectStatus} не обрабатывается");
                }
            }

            IOrderedEnumerable<Project> orderedProjects;
            if (string.IsNullOrWhiteSpace(sortField))
            {
                orderedProjects = projects.OrderBy(p => p.ShortName);
            }
            else
            {
                string[] propertyNames = sortField.Split(new char[] { '_' }, StringSplitOptions.RemoveEmptyEntries);
                var propertyGetterExpr = GetPropertyStringValueFunc(propertyNames[0], propertyNames.Length > 1 ? propertyNames[1] : null);
                var propertyGetterFunc = propertyGetterExpr.Compile();
                if (string.Equals(sortOrder, "desc", StringComparison.InvariantCultureIgnoreCase))
                    orderedProjects = projects.OrderByDescending(propertyGetterFunc);
                else
                    orderedProjects = projects.OrderBy(propertyGetterFunc);
            }
            orderedProjects = orderedProjects.ThenBy(p => p.ID);
            var result = orderedProjects.ToList();
            return result;
        }

        public override Project Add(Project project)
        {
            var projectRolesRepository = RepositoryFactory.GetRepository<IProjectRolesRepository>();

            var rolePM = projectRolesRepository.GetAll(pr => pr.RoleType == ProjectRoleType.PM).FirstOrDefault();
            if (rolePM == null)
                throw new InvalidOperationException("Отсутствует запись о роли ПМ");
            var roleKAM = projectRolesRepository.GetAll(pr => pr.RoleType == ProjectRoleType.CAM).FirstOrDefault();
            if (roleKAM == null)
                throw new InvalidOperationException("Отсутствует запись о роли КАМ");

            var projectRepository = RepositoryFactory.GetRepository<IProjectRepository>();
            var projectMembersRepository = RepositoryFactory.GetRepository<IProjectMembersRepository>();

            project.InitBaseFields(Tuple.Create(_user.Item1, _user.Item2));
            project = projectRepository.Add(project);

            DateTime membershipBeginDate = project.BeginDate.HasValue ? project.BeginDate.Value : DateTime.Today;
            if (project.EmployeePMID.HasValue)
            {
                var projectMemberPM = new ProjectMember
                {
                    AssignmentPercentage = 0,
                    EmployeeID = project.EmployeePMID,
                    MembershipDateBegin = membershipBeginDate,
                    ProjectID = project.ID,
                    ProjectRoleID = rolePM.ID
                };
                projectMembersRepository.Add(projectMemberPM);
            }
            if (project.EmployeeCAMID.HasValue)
            {
                var projectMemberKAM = new ProjectMember
                {
                    AssignmentPercentage = 0,
                    EmployeeID = project.EmployeeCAMID,
                    MembershipDateBegin = membershipBeginDate,
                    ProjectID = project.ID,
                    ProjectRoleID = roleKAM.ID
                };
                projectMembersRepository.Add(projectMemberKAM);
            }
            return project;
        }

        public override Project Update(Project project)
        {
            var projectRepository = RepositoryFactory.GetRepository<IProjectRepository>();
            var projectReportRecordsRepository = RepositoryFactory.GetRepository<IProjectReportRecordsRepository>();
            var projectStatusRecordRepository = RepositoryFactory.GetRepository<IProjectStatusRecordRepository>();

            var originalItem = projectRepository.FindNoTracking(project.ID);

            project.UpdateBaseFields(Tuple.Create(_user.Item1, _user.Item2), originalItem.ID, originalItem);
            originalItem.FreeseVersion(originalItem.ID);

            var projectReportRecords = projectReportRecordsRepository.GetQueryableAsNoTracking().Where(prr => prr.ProjectID == project.ID);

            project.TotalHoursActual = 0;
            foreach (var record in projectReportRecords)
            {
                if (record.Hours != null && record.EmployeeID == null) // Тут отбираем записи ProjectReportRecords, которые не привязаны к конкретному сотруднику, то есть: EmployeeID == null
                    project.TotalHoursActual += record.Hours;
            }

            var projectStatusRecords = projectStatusRecordRepository.GetQueryableAsNoTracking().Where(psr => psr.ProjectID == project.ID).OrderByDescending(psr => psr.Created);
            var projectStatusRecord = projectStatusRecords.FirstOrDefault();
            if (projectStatusRecord != null && projectStatusRecord.EmployeePayrollAmountActual != null)
                project.EmployeePayrollTotalAmountActual = projectStatusRecord.EmployeePayrollAmountActual;
            projectRepository.Add(originalItem);
            return projectRepository.Update(project);

        }

        public IList<ProjectStatusRecord> GetStatusRecords(int? projectId)
        {
            var projectStatusRecords = RepositoryFactory.GetRepository<IProjectStatusRecordRepository>()
                .GetAll(psr => psr.ProjectID == projectId, data => data.OrderByDescending(psr => psr.Created));
            return projectStatusRecords;
        }

        public IList<ProjectReportRecord> GetReportRecords(int? projectId, bool employeeSorting)
        {
            var projectReportRecords = RepositoryFactory.GetRepository<IProjectReportRecordsRepository>()
                .GetAll(prr => prr.EmployeeID == null && prr.ProjectID == projectId, data =>
                {
                    var orderedData = data.OrderBy(prr => prr.RecordSortKey);
                    if (employeeSorting)
                        orderedData = orderedData.ThenBy(prr => prr.Employee.FullName);
                    return orderedData;
                });
            return projectReportRecords;
        }

        public Project LoadRelations(Project project)
        {
            var projectRepository = RepositoryFactory.GetRepository<IProjectRepository>();
            var projectMembersRepository = RepositoryFactory.GetRepository<IProjectMembersRepository>();
            var projectReportRecordsRepository = RepositoryFactory.GetRepository<IProjectReportRecordsRepository>();
            var projectStatusRecordRepository = RepositoryFactory.GetRepository<IProjectStatusRecordRepository>();

            project.ReportRecords = projectReportRecordsRepository.GetAll(prr => prr.ProjectID == project.ID, data => data.OrderBy(prr => prr.RecordSortKey));
            project.StatusRecords = projectStatusRecordRepository.GetAll(psr => psr.ProjectID == project.ID, data => data.OrderByDescending(psr => psr.Created));
            project.ChildProjects = projectRepository.GetAll(p => p.ParentProjectID == project.ID, data => data.OrderBy(p => p.ShortName));
            project.ProjectTeam = projectMembersRepository.GetAll(pm => pm.ProjectID == project.ID, data => data.OrderByDescending(pp => pp.Employee.LastName)); // TODO: Почему Descending ?

            return project;
        }

        public IList<ProjectRole> GetProjectRoles()
        {
            var projectRolesRepository = RepositoryFactory.GetRepository<IProjectRolesRepository>();
            return projectRolesRepository.GetAll();
        }

        public void Validate(Project project, IValidationRecipient validationRecipient)
        {
            var repository = RepositoryFactory.GetRepository<IProjectRepository>();
            var validator = new ProjectValidator(project, validationRecipient, repository.GetQueryable());
            validator.Validate();
        }

        protected Expression<Func<Project, string>> GetPropertyStringValueFunc(string propertyName, string nestedPropertyName)
        {
            MethodInfo convertMethodInfo = typeof(Convert).GetMethod("ToString", new Type[] { typeof(object) });
            var paramExpr = Expression.Parameter(typeof(Project), "p");
            var propertyExpr = Expression.Property(paramExpr, propertyName);
            var nullConstExpr = Expression.Constant(null);
            var isNotNullExpr = Expression.NotEqual(propertyExpr, nullConstExpr);
            MethodCallExpression callToStringExpr;
            if (string.IsNullOrWhiteSpace(nestedPropertyName))
            {
                var convertPropertyExpr = Expression.Convert(propertyExpr, typeof(object));
                callToStringExpr = Expression.Call(convertMethodInfo, convertPropertyExpr);
            }
            else
            {
                var innerPropertyExpr = Expression.Property(propertyExpr, nestedPropertyName);
                var convertInnerPropertyExpr = Expression.Convert(innerPropertyExpr, typeof(object));
                callToStringExpr = Expression.Call(convertMethodInfo, convertInnerPropertyExpr);
            }
            var emptyStringExpr = Expression.Constant("");
            var conditionExpr = Expression.Condition(isNotNullExpr, callToStringExpr, emptyStringExpr);
            var lambdaExpr = Expression.Lambda<Func<Project, string>>(conditionExpr, paramExpr);
            return lambdaExpr;
        }
    }
}