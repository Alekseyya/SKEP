using System;
using Core.BL;

using Core.BL.Interfaces;
using Core.Data;
using Core.Data.Interfaces;
using Core.Models;




namespace BL.Implementation
{
    public class ProjectScheduleEntryTypeService : RepositoryAwareServiceBase<ProjectScheduleEntryType, int, IProjectScheduleEntryTypeRepository>, IProjectScheduleEntryTypeService
    {
        private readonly (string, string) _user;
        public ProjectScheduleEntryTypeService(IRepositoryFactory repositoryFactory, IUserService userService) : base(repositoryFactory)
        {
            _user = userService.GetUserDataForVersion();
        }

        public override ProjectScheduleEntryType Add(ProjectScheduleEntryType projectScheduleEntryType)
        {
            if (projectScheduleEntryType == null) throw new ArgumentException(nameof(projectScheduleEntryType));

            var projectScheduleTypeRepository = RepositoryFactory.GetRepository<IProjectScheduleEntryTypeRepository>();
            projectScheduleEntryType.InitBaseFields(Tuple.Create(_user.Item1, _user.Item2));
            return projectScheduleTypeRepository.Add(projectScheduleEntryType);
        }

        public int GetCount()
        {
            return RepositoryFactory.GetRepository<IProjectScheduleEntryTypeRepository>().GetCount();
        }

        public ProjectScheduleEntryType UpdateWithoutVersion(ProjectScheduleEntryType projectScheduleEntryType)
        {
            if (projectScheduleEntryType == null) throw new ArgumentNullException(nameof(projectScheduleEntryType));
            var projectScheduleTypeRepository = RepositoryFactory.GetRepository<IProjectScheduleEntryTypeRepository>();
            projectScheduleTypeRepository.Update(projectScheduleEntryType);
            return projectScheduleEntryType;
        }
    }
}
