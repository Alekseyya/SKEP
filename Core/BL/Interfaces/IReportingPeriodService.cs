using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using Core.BL;
using Core.Models;


namespace Core.BL.Interfaces
{
   public interface IReportingPeriodService : IEntityValidatingService<ReportingPeriod>, IServiceBase<ReportingPeriod, int>
    {
        IList<ReportingPeriod> GetAll();
        IList<ReportingPeriod> GetAll(Expression<Func<ReportingPeriod, bool>> conditionFunc);
    }
}
