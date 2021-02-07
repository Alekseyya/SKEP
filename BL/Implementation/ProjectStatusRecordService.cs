using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using Core.BL;
using Core.BL.Interfaces;
using Core.Validation;
using Microsoft.EntityFrameworkCore;

using Core.BL.Interfaces;
using Core.Data;
using Core.Data.Interfaces;
using Core.Models;
using Core.Models.RBAC;


namespace BL.Implementation
{
   public class ProjectStatusRecordService : RepositoryAwareServiceBase<ProjectStatusRecord, int, IProjectStatusRecordRepository>, IProjectStatusRecordService
    {
        private readonly (string, string) _user;
        private readonly IApplicationUserService _applicationUserService;

        public ProjectStatusRecordService(IRepositoryFactory repositoryFactory, IUserService userService, IApplicationUserService applicationUserService) : base(repositoryFactory)
        {
            _user = userService.GetUserDataForVersion();
            _applicationUserService = applicationUserService;
        }

        public void Validate(ProjectStatusRecord entity, IValidationRecipient validationRecipient)
        {
            throw new NotImplementedException();
        }
        
        public override ProjectStatusRecord Add(ProjectStatusRecord projectStatusRecord)
        {
            if (projectStatusRecord == null) throw new ArgumentNullException(nameof(projectStatusRecord));

            var projectStatusRepository = RepositoryFactory.GetRepository<IProjectStatusRecordRepository>();
            projectStatusRecord.InitBaseFields(Tuple.Create(_user.Item1, _user.Item2));
            return projectStatusRepository.Add(projectStatusRecord);
        }

        public int GetCount()
        {
            var projectStatusRecordRepository = RepositoryFactory.GetRepository<IProjectStatusRecordRepository>();
            return projectStatusRecordRepository.GetQueryable().GroupBy(x => x.ProjectID).Count();
        }

        public IList<ProjectStatusRecord> GetAll()
        {
            return GetAll(null);
        }

        public IList<ProjectStatusRecord> GetAll(Expression<Func<ProjectStatusRecord, bool>> conditionFunc)
        {
            var projectStatusRecordsList = RepositoryFactory.GetRepository<IProjectStatusRecordRepository>().GetAll();
            if (conditionFunc != null)
                projectStatusRecordsList = projectStatusRecordsList.AsQueryable().Where(conditionFunc).ToList();
            return projectStatusRecordsList;
        }

        public IList<ProjectStatusRecord> GetAll(Expression<Func<ProjectStatusRecord, bool>> conditionFunc, bool withDeleted)
        {
            IList<ProjectStatusRecord> result = null;
            if (withDeleted && conditionFunc != null)
                RunWithoutDeletedFilter(() => { result = GetAll(conditionFunc).Where(x => x.IsDeleted).ToList(); });
            else if (withDeleted && conditionFunc == null)
                RunWithoutDeletedFilter(() => { result = GetAll(null).Where(x => x.IsDeleted).ToList(); });
            else if (withDeleted == false && conditionFunc != null)
                result = GetAll(conditionFunc);
            return result;
        }

        public IList<ProjectStatusRecord> GetAll(Expression<Func<ProjectStatusRecord, bool>> conditionFunc, Func<IQueryable<ProjectStatusRecord>, IQueryable<ProjectStatusRecord>> includesFunc)
        {
            var projectStatusRecordRepository = RepositoryFactory.GetRepository<IProjectStatusRecordRepository>();
            var projectStatusRecordsList = new List<ProjectStatusRecord>();
            if (conditionFunc != null && includesFunc == null)
                projectStatusRecordsList = projectStatusRecordRepository.GetAll(conditionFunc).ToList();
            else if (conditionFunc != null && includesFunc != null)
                projectStatusRecordsList = projectStatusRecordRepository.GetAll(conditionFunc, null, includesFunc).ToList();
            else if (conditionFunc == null && includesFunc != null)
                projectStatusRecordsList = projectStatusRecordRepository.GetAll(null, null, includesFunc).ToList();
            return projectStatusRecordsList;
        }

        public override ProjectStatusRecord GetById(int id)
        {
            var projectStatusRepository = RepositoryFactory.GetRepository<IProjectStatusRecordRepository>();
            var projestStatusRecord = projectStatusRepository.GetById(id);
            projestStatusRecord.Versions = projectStatusRepository.GetVersions(projestStatusRecord.ID, true);
            return projestStatusRecord;
        }

        public ProjectStatusRecord GetVersion(int id, int version)
        {
            var projectStatusRecordRepository = RepositoryFactory.GetRepository<IProjectStatusRecordRepository>();
            var projectStatusRecord = projectStatusRecordRepository.GetVersion(id, version);
            projectStatusRecord.Versions = new List<ProjectStatusRecord>();
            return projectStatusRecord;
        }

        public override ProjectStatusRecord Update(ProjectStatusRecord projectStatusRecord)
        {
            if (projectStatusRecord == null) throw new ArgumentNullException(nameof(projectStatusRecord));
            var projectStatusRepository = RepositoryFactory.GetRepository<IProjectStatusRecordRepository>();
            var originalItem = projectStatusRepository.FindNoTracking(projectStatusRecord.ID);

            projectStatusRecord.UpdateBaseFields(Tuple.Create(_user.Item1, _user.Item2), originalItem.ID, originalItem);
            originalItem.FreeseVersion(originalItem.ID);

            projectStatusRepository.Add(originalItem);
            return projectStatusRepository.Update(projectStatusRecord);
        }

        //Реалиовать общий метод GetAllForUser , см. ProjectExternalWorkspaceService, ProjectScheduleEntryService, ProjectStatusRecordService
        public IList<ProjectStatusRecord> GetAllForUserOrderByCreatedDesc(ApplicationUser user)
        {
            int userEmployeeID = _applicationUserService.GetEmployeeID();

            if (_applicationUserService.HasAccess(Operation.ProjectView | Operation.ProjectCreateUpdate | Operation.ProjectStatusRecordView | Operation.ProjectStatusRecordCreateUpdate))
            {
                return Get(x=>x.Include(p=>p.Project).ToList()).OrderByDescending(pr => pr.Created).ToList();
            }
            else if (_applicationUserService.HasAccess(Operation.ProjectMyDepartmentProjectView) && _applicationUserService.HasAccess(Operation.ProjectMyProjectView))
            {
                var managedDepartments = _applicationUserService.GetUser().ManagedDepartments.Select(x=>x.ID);

                return Get(x=>x.Include(p=>p.Project).Where(psr =>
                    psr.Project.EmployeePMID == userEmployeeID
                    || psr.Project.EmployeeCAMID == userEmployeeID
                    || psr.Project.EmployeePAID == userEmployeeID
                    || (psr.Project.Department != null && psr.Project.Department.DepartmentPAID == userEmployeeID)
                    || (managedDepartments != null && psr.Project.Department != null && managedDepartments.Contains(psr.Project.DepartmentID.Value))
                    || (psr.Project.ParentProject != null &&
                        (psr.Project.ParentProject.EmployeePMID == userEmployeeID
                         || psr.Project.ParentProject.EmployeeCAMID == userEmployeeID
                         || psr.Project.ParentProject.EmployeePAID == userEmployeeID
                         || (psr.Project.ParentProject.Department != null && psr.Project.ParentProject.Department.DepartmentPAID == userEmployeeID)
                         || (managedDepartments != null && psr.Project.ParentProject.Department != null
                                                        && managedDepartments.Contains(psr.Project.ParentProject.DepartmentID.Value))))).ToList())
                    .OrderByDescending(pr => pr.Created).ToList();
            }
            else if (_applicationUserService.HasAccess(Operation.ProjectMyDepartmentProjectView))
            {
                var managedDepartments = _applicationUserService.GetUser().ManagedDepartments.Select(x => x.ID);

                return Get(x=>x.Include(p=>p.Project).Where(psr =>
                    (managedDepartments != null && psr.Project.Department != null && managedDepartments.Contains(psr.Project.DepartmentID.Value))
                    || (psr.Project.ParentProject != null &&
                        managedDepartments != null && psr.Project.ParentProject.Department != null &&
                        managedDepartments.Contains(psr.Project.ParentProject.DepartmentID.Value))).ToList()).OrderByDescending(pr => pr.Created).ToList();
            }
            else if (_applicationUserService.HasAccess(Operation.ProjectMyProjectView))
            {
                return Get(x=>x.Include(p=>p.Project).Where(psr =>
                    psr.Project.EmployeePMID == userEmployeeID
                    || psr.Project.EmployeeCAMID == userEmployeeID
                    || psr.Project.EmployeePAID == userEmployeeID
                    || (psr.Project.Department != null && psr.Project.Department.DepartmentPAID == userEmployeeID)
                    || (psr.Project.ParentProject != null &&
                        (psr.Project.ParentProject.EmployeePMID == userEmployeeID
                         || psr.Project.ParentProject.EmployeeCAMID == userEmployeeID
                         || psr.Project.ParentProject.EmployeePAID == userEmployeeID
                         || (psr.Project.ParentProject.Department != null && psr.Project.ParentProject.Department.DepartmentPAID == userEmployeeID)))).ToList()).OrderByDescending(psr => psr.Created).ToList();
            }
            else
            {
                return new List<ProjectStatusRecord>();
            }
        }
    }
}
