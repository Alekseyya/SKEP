using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Core.BL;
using Core.BL.Interfaces;
using Core.Data;
using Core.Data.Interfaces;
using Core.Models;
using Core.Models.RBAC;


namespace BL.Implementation
{
    public class ProjectScheduleEntryService : RepositoryAwareServiceBase<ProjectScheduleEntry, int, IProjectScheduleEntryRepository>, IProjectScheduleEntryService
    {
        private readonly (string, string) _user;
        private readonly IApplicationUserService _applicationUserService;

        public ProjectScheduleEntryService(IRepositoryFactory repositoryFactory, IUserService userService, IApplicationUserService applicationUserService) : base(repositoryFactory)
        {
            _user = userService.GetUserDataForVersion();
            _applicationUserService = applicationUserService;
        }

        public override ProjectScheduleEntry Add(ProjectScheduleEntry projectScheduleEntry)
        {
            if (projectScheduleEntry == null) throw new ArgumentException(nameof(projectScheduleEntry));

            var projectScheduleRepository = RepositoryFactory.GetRepository<IProjectScheduleEntryRepository>();
            projectScheduleEntry.InitBaseFields(Tuple.Create(_user.Item1, _user.Item2));
            return projectScheduleRepository.Add(projectScheduleEntry);
        }

        public IList<ProjectScheduleEntry> GetAll()
        {
            return GetAll(null);
        }

        public IList<ProjectScheduleEntry> GetAll(Expression<Func<ProjectScheduleEntry, bool>> conditionFunc)
        {
            var projectScheduleEntries = RepositoryFactory.GetRepository<IProjectScheduleEntryRepository>().GetAll();
            if (conditionFunc != null)
                return projectScheduleEntries.AsQueryable().Where(conditionFunc).ToList();
            return projectScheduleEntries;
        }

        public IList<ProjectScheduleEntry> GetAll(Expression<Func<ProjectScheduleEntry, bool>> conditionFunc, bool withDeleted)
        {
            IList<ProjectScheduleEntry> result = null;
            if (withDeleted)
                RunWithoutDeletedFilter(() => { result = GetAll(conditionFunc).Where(x => x.IsDeleted).ToList(); });
            else
                result = GetAll(conditionFunc);
            return result;
        }

        public override ProjectScheduleEntry GetById(int id)
        {
            return GetById(id, false);
        }

        public ProjectScheduleEntry GetById(int id, bool includeRelations)
        {
            var projectScheduleRepository = RepositoryFactory.GetRepository<IProjectScheduleEntryRepository>();
            var projectScheduleEntry = projectScheduleRepository.GetById(id);

            projectScheduleEntry.Versions = projectScheduleRepository.GetVersions(projectScheduleEntry.ID, includeRelations);
            return projectScheduleEntry;
        }

        public int GetCount()
        {
            return RepositoryFactory.GetRepository<IProjectScheduleEntryRepository>().GetCount();
        }

        public ProjectScheduleEntry GetVersion(int id, int version)
        {
            return GetVersion(id, version, false);
        }

        public ProjectScheduleEntry GetVersion(int id, int version, bool includeRelations)
        {
            var projectScheduleRepository = RepositoryFactory.GetRepository<IProjectScheduleEntryRepository>();
            var projectScheduleEntry = projectScheduleRepository.GetVersion(id, version);
            projectScheduleEntry.Versions = new List<ProjectScheduleEntry>();
            return projectScheduleEntry;
        }

        public ProjectScheduleEntry Update(ProjectScheduleEntry projectScheduleEntry, string currentUserName, string currentUserSID)
        {
            if (projectScheduleEntry == null) throw new ArgumentNullException(nameof(projectScheduleEntry));
            var projectScheduleRepository = RepositoryFactory.GetRepository<IProjectScheduleEntryRepository>();

            var originalItem = projectScheduleRepository.FindNoTracking(projectScheduleEntry.ID);

            projectScheduleEntry.UpdateBaseFields(Tuple.Create(currentUserName, currentUserSID), originalItem.ID, originalItem);
            originalItem.FreeseVersion(originalItem);

            projectScheduleRepository.Add(originalItem);
            projectScheduleRepository.Update(projectScheduleEntry);
            return projectScheduleEntry;
        }

        public IList<ProjectScheduleEntry> GetAllForUser(ApplicationUser user)
        {
            int userEmployeeID = _applicationUserService.GetEmployeeID();

            if (_applicationUserService.HasAccess(Operation.ProjectView | Operation.ProjectCreateUpdate | Operation.ProjectScheduleEntryView | Operation.ProjectScheduleEntryCreateUpdate))
            {
                return GetAll().ToList();
            }
            else if (_applicationUserService.HasAccess(Operation.ProjectMyDepartmentProjectView) && _applicationUserService.HasAccess(Operation.ProjectMyProjectView))
            {
                var managedDepartments = _applicationUserService.GetUser().ManagedDepartments.Select(x=>x.ID);

                return GetAll(pse =>
                        pse.Project.EmployeePMID == userEmployeeID
                        || pse.Project.EmployeeCAMID == userEmployeeID
                        || pse.Project.EmployeePAID == userEmployeeID
                        || (pse.Project.Department != null && pse.Project.Department.DepartmentPAID == userEmployeeID)
                        || (managedDepartments != null && pse.Project.Department != null && managedDepartments.Contains(pse.Project.DepartmentID.Value))
                        || (pse.Project.ParentProject != null &&
                        (pse.Project.ParentProject.EmployeePMID == userEmployeeID
                        || pse.Project.ParentProject.EmployeeCAMID == userEmployeeID
                        || pse.Project.ParentProject.EmployeePAID == userEmployeeID
                        || (pse.Project.ParentProject.Department != null && pse.Project.ParentProject.Department.DepartmentPAID == userEmployeeID)
                        || (managedDepartments != null && pse.Project.ParentProject.Department != null && managedDepartments.Contains(pse.Project.ParentProject.DepartmentID.Value)))))
                    .ToList();
            }
            else if (_applicationUserService.HasAccess(Operation.ProjectMyDepartmentProjectView))
            {
                var managedDepartments = _applicationUserService.GetUser().ManagedDepartments.Select(x => x.ID);

                return GetAll(pse =>
                        (managedDepartments != null && pse.Project.Department != null && managedDepartments.Contains(pse.Project.DepartmentID.Value))
                        || (pse.Project.ParentProject != null &&
                        (managedDepartments != null && pse.Project.ParentProject.Department != null && managedDepartments.Contains(pse.Project.ParentProject.DepartmentID.Value))))
                    .ToList();
            }
            else if (_applicationUserService.HasAccess(Operation.ProjectMyProjectView))
            {

                return GetAll(pse =>
                        pse.Project.EmployeePMID == userEmployeeID
                        || pse.Project.EmployeeCAMID == userEmployeeID
                        || pse.Project.EmployeePAID == userEmployeeID
                        || (pse.Project.Department != null && pse.Project.Department.DepartmentPAID == userEmployeeID)
                        || (pse.Project.ParentProject != null &&
                        (pse.Project.ParentProject.EmployeePMID == userEmployeeID
                        || pse.Project.ParentProject.EmployeeCAMID == userEmployeeID
                        || pse.Project.ParentProject.EmployeePAID == userEmployeeID
                        || (pse.Project.ParentProject.Department != null && pse.Project.ParentProject.Department.DepartmentPAID == userEmployeeID))))
                        .ToList();
            }
            else
            {
                return new List<ProjectScheduleEntry>();
            }
        }
    }
}
