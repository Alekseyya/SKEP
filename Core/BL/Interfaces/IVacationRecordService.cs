using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Core.BL;
using Core.Models;


namespace Core.BL.Interfaces
{
    public interface IVacationRecordService : IServiceBase<VacationRecord, int>, IEntityValidatingService<VacationRecord>
    {
        void Delete(int vacationRecordId);
        IList<VacationRecord> Get(Func<IQueryable<VacationRecord>, IList<VacationRecord>> expression);
        IList<VacationRecord> GetAll();
        IList<VacationRecord> GetAll(Expression<Func<VacationRecord, bool>> conditionFunc);
        VacationRecord GetById(int id);
        VacationRecord GetVersion(int id, int version);
        VacationRecord GetVersion(int id, int version, bool includeRelations);
        void UpdateWithoutVersion(VacationRecord vacationRecord);
    }
}
