using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Core.BL;
using Core.Models;


namespace Core.BL.Interfaces
{
   public interface ITSAutoHoursRecordService : IEntityValidatingService<TSAutoHoursRecord>, IServiceBase<TSAutoHoursRecord, int>
   {
       IList<TSAutoHoursRecord> GetAll();
       IList<TSAutoHoursRecord> GetAll(Expression<Func<TSAutoHoursRecord, bool>> conditionFunc);
       IList<TSAutoHoursRecord> GetAll(Expression<Func<TSAutoHoursRecord, bool>> conditionFunc, bool withDeleted);
       TSAutoHoursRecord GetVersion(int id, int version);
       TSAutoHoursRecord GetVersion(int id, int version, bool includeRelations);
    }
}
