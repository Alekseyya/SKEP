using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using Core.BL;
using Core.Models;


namespace Core.BL.Interfaces
{
    public interface IProjectStatusRecordService : IEntityValidatingService<ProjectStatusRecord>, IServiceBase<ProjectStatusRecord, int>
    {
        int GetCount();
        IList<ProjectStatusRecord> GetAll();
        IList<ProjectStatusRecord> GetAll(Expression<Func<ProjectStatusRecord, bool>> conditionFunc);
        IList<ProjectStatusRecord> GetAll(Expression<Func<ProjectStatusRecord, bool>> conditionFunc, bool withDeleted);
        IList<ProjectStatusRecord> GetAll(Expression<Func<ProjectStatusRecord, bool>> conditionFunc, Func<IQueryable<ProjectStatusRecord>, IQueryable<ProjectStatusRecord>> includesFunc);
        IList<ProjectStatusRecord> GetAllForUserOrderByCreatedDesc(ApplicationUser user);
        ProjectStatusRecord GetVersion(int id, int version);
    }
}
