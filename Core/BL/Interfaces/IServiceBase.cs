using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Core.BL;

namespace Core.BL.Interfaces
{
    public interface IServiceBase<TEntry, TId>
    {
        TEntry Add(TEntry entry);
        TEntry Update(TEntry entry);
        TEntry GetById(TId id);
        TEntry GetById(TId id, params Expression<Func<TEntry, object>>[] includes);
        void Delete(TId id);
        IList<TEntry> Get(Func<IQueryable<TEntry>, IList<TEntry>> delegateExpr);
        IList<TEntry> Get(Func<IQueryable<TEntry>, IList<TEntry>> delegateExpr, GetEntityMode getTypes);
        TEntry GetByIdWithDeleteFilter(TId id);
        void RemoveRange(IList<TEntry> entries);
        void RestoreFromRecycleBin(int id);
        void DeleteRelatedEntries(int parentId);
        (bool toRecycleBin, string relatedClassId) RecycleToRecycleBin(int id, string userName, string userSID);
        (bool toRecycleBin, string recycleClassId) RecycleToRecycleBinRange(IList<TEntry> entries, string userName, string userSID);
    }
}
