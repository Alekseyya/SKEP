using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;
using Core.Models;


namespace Core.BL.Interfaces
{
    public interface IProjectScheduleEntryService : IServiceBase<ProjectScheduleEntry, int>
    {
        int GetCount();
        IList<ProjectScheduleEntry> GetAll();
        IList<ProjectScheduleEntry> GetAll(Expression<Func<ProjectScheduleEntry, bool>> conditionFunc);
        IList<ProjectScheduleEntry> GetAll(Expression<Func<ProjectScheduleEntry, bool>> conditionFunc, bool withDeleted);
        IList<ProjectScheduleEntry> GetAllForUser(ApplicationUser user);
        ProjectScheduleEntry GetById(int id, bool includeRelations);
        ProjectScheduleEntry GetVersion(int id, int version);
        ProjectScheduleEntry GetVersion(int id, int version, bool includeRelations);
        ProjectScheduleEntry Update(ProjectScheduleEntry projectScheduleEntry, string currentUserName, string currentUserSID);
    }
}
