using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using Core.Models;


namespace Core.BL.Interfaces
{
   public interface IExpensesRecordService : IServiceBase<ExpensesRecord, int>
    {
        IList<ExpensesRecord> GetAll();
        IList<ExpensesRecord> GetAll(Expression<Func<ExpensesRecord, bool>> conditionFunc);
        ExpensesRecord GetByBitrixNumber(string bitrixNumber);
        Hashtable GetExpensesRecordBitrixURLFromConfig();
    }
}
