using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Core.BL;
using Core.BL.Interfaces;
using Core.Config;
using Core.Data;
using Core.Data.Interfaces;
using Core.Models;
using Microsoft.Extensions.Options;

namespace BL.Implementation
{
   public class ExpensesRecordService : RepositoryAwareServiceBase<ExpensesRecord, int, IExpensesRecordRepository>, IExpensesRecordService
    {
        private readonly BitrixConfig _bitrixConfig;
        public ExpensesRecordService(IRepositoryFactory repositoryFactory, IOptions<BitrixConfig> bitrixOptions) : base(repositoryFactory)
        {
            _bitrixConfig = bitrixOptions.Value ?? throw new ArgumentNullException(nameof(bitrixOptions));
        }
        #region Implements IExpensesRecordSevice

        public IList<ExpensesRecord> GetAll()
        {
            return GetAll(null);
        }

        public IList<ExpensesRecord> GetAll(Expression<Func<ExpensesRecord, bool>> conditionFunc)
        {
            var expensesList = RepositoryFactory.GetRepository<IExpensesRecordRepository>().GetAll();
            if (conditionFunc != null)
                expensesList = expensesList.AsQueryable().Where(conditionFunc).ToList();
            return expensesList;
        }
        public ExpensesRecord GetByBitrixNumber(string bitrixNumber)
        {
            var expensesRepository = RepositoryFactory.GetRepository<IExpensesRecordRepository>();
            return expensesRepository
                .GetAll(expRecord => String.Equals(expRecord.BitrixURegNum.Trim(), bitrixNumber.Trim(), StringComparison.CurrentCultureIgnoreCase))
                .FirstOrDefault();
        }

        public Hashtable GetExpensesRecordBitrixURLFromConfig()
        {
            var expensesRecordsId = _bitrixConfig.SyncListURLs.Split(',');
            var hashtableRecords = new Hashtable();
            foreach (var record in expensesRecordsId)
            {
                var recordId = record.Trim().Split('=')[0].Trim();
                var recordUrl = record.Trim().Split('=')[1].Trim();
                hashtableRecords.Add(recordId, recordUrl);
            }

            return hashtableRecords;
        }
        #endregion
    }
}
