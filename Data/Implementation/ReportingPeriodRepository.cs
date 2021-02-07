using System;
using System.Collections.Generic;
using System.Text;
using Core.Data;
using Core.Data.Interfaces;
using Core.Models;
using Microsoft.EntityFrameworkCore;




namespace Data.Implementation
{
    public class ReportingPeriodRepository : RepositoryBase<ReportingPeriod, int>, IReportingPeriodRepository
    {
        public ReportingPeriodRepository(DbContext dbContext) : base(dbContext)
        {
        }

        #region IReportingPeriods implements



        #endregion

        #region Internal implementation
        protected override object[] GetEntityKeyValues(int id)
        {
            return new object[] { id };
        }

        protected override ReportingPeriod CreateEntityWithId(int id)
        {
            return new ReportingPeriod { ID = id };
        }

        protected override bool CompareEntityId(ReportingPeriod entity, int id)
        {
            return (entity.ID == id);
        }
        #endregion

    }
}
