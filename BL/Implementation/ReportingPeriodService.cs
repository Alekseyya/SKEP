using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Core.BL;
using Core.Validation;

using Core.BL.Interfaces;
using Core.Data;
using Core.Data.Interfaces;
using Core.Models;




namespace BL.Implementation
{
    public class ReportingPeriodService : RepositoryAwareServiceBase<ReportingPeriod, int, IReportingPeriodRepository>, IReportingPeriodService
    {
        public ReportingPeriodService(IRepositoryFactory repositoryFactory) : base(repositoryFactory)
        {
        }

        public void Validate(ReportingPeriod entity, IValidationRecipient validationRecipient)
        {
            throw new NotImplementedException();
        }

        #region IReportiongPecordService
        
        public IList<ReportingPeriod> GetAll()
        {
            return GetAll(null);
        }

        public IList<ReportingPeriod> GetAll(Expression<Func<ReportingPeriod, bool>> conditionFunc)
        {
            var repository = RepositoryFactory.GetRepository<IReportingPeriodRepository>();
            IList<ReportingPeriod> reportingPeriods;
            if (conditionFunc != null)
            {
                reportingPeriods = repository.GetAll(conditionFunc).ToList();
                return reportingPeriods;
            }
            return repository.GetAll();
        }
        #endregion
    }
}