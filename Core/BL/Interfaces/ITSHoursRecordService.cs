using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Core.BL;
using Core.Models;


namespace Core.BL.Interfaces
{
     public interface ITSHoursRecordService : IEntityValidatingService<TSHoursRecord>, IServiceBase<TSHoursRecord, int>
    {
        void Add(TSHoursRecord tsHoursRecord, string currentUserName, string currentUserSID);
        void Add(IList<TSHoursRecord> listTSHoursRecords, string currentUserName, string currentUserSID);
        object CheckData(string date);
        void Update(TSHoursRecord tsHoursRecord, string currentUserName, string currentUserSID);
        void Update(IList<TSHoursRecord> tsHoursRecords);
        void UpdateWithoutVersion(TSHoursRecord tsHoursRecord);
        double GetEmployeeTsHoursRecordSummHoursMonthByStatus(int year, int month, int employeeId, params TSRecordStatus[] status);
        double GetEmployeeTsHoursRecordSummHoursMonthByStatus(int year, int month, int employeeId, int projectId, params TSRecordStatus[] status);
        IList<TSHoursRecord> FindHoursRecords(string searchString);
        IList<TSHoursRecord> GetAll();
        IList<TSHoursRecord> GetAll(Expression<Func<TSHoursRecord, bool>> conditionFunc);

        IList<TSHoursRecord> GetRecordsForWeek(int year, int week, int employeeId);
        /*
        IList<TSHoursRecord> GetAllLastVersion();
        IList<TSHoursRecord> GetAllLastVersion(Expression<Func<TSHoursRecord, bool>> conditionFunc);
        IList<TSHoursRecord> GetAllLastVersion(Expression<Func<TSHoursRecord, bool>> conditionFunc, 
                                               Func<IQueryable<TSHoursRecord>, IOrderedQueryable<TSHoursRecord>> orderFunc);*/
        bool AnyRecordsInUntilApproveReport(IGrouping<string, Employee> group, int year, int month);
        TSHoursRecord GetVersion(int id, int version);
        TSHoursRecord GetVersion(int id, int version, bool includeRelations);

        IList<TSHoursRecord> GetEmployeeTSHoursRecords(int employeId, DateTime dateTimeHoursStartDate, DateTime dateTimeHoursEndDate, int? projectId,
            TSRecordStatus tSRecordStatus, DateTime? modifiedDateTime);

        IList<TSHoursRecord> GetTSRecordsForApproval(int approveHoursEmployeeId,
            DateTime dateTimeHoursStartDate, DateTime dateTimeHoursEndDate, int? projectId,
            TSRecordStatus tSRecordStatus);

        IList<TSHoursRecord> GetRecordsHaveManagerComments(int employeeId, int projectId,
             Expression<Func<TSHoursRecord, bool>> conditionFunc,
             Func<IQueryable<TSHoursRecord>, IEnumerable<IGrouping<string, TSHoursRecord>>> groupFunc);
        IList<TSHoursRecord> GetRecordsHaveManagerComments(int employeeId, IList<int> projectId,
             Expression<Func<TSHoursRecord, bool>> conditionFunc,
             Func<IQueryable<TSHoursRecord>, IEnumerable<IGrouping<string, TSHoursRecord>>> groupFunc);

        void ChangeHoursRecordsStatus(IQueryable<TSHoursRecord> data, IList<int> listHoursRecordsId, TSRecordStatus tsHoursRecordStatus);
        void ChangeHoursRecordsStatus(IQueryable<TSHoursRecord> data, Expression<Func<TSHoursRecord, bool>> conditionFunc,
                                      IList<int> listHoursRecordsId, TSRecordStatus tsHoursRecordStatus);

        /// <summary>
        /// Изменение статусов в таймшите
        /// </summary>
        /// <param name="data">Коллекция с данными</param>
        /// <param name="conditionFunc">Условие входящее в where</param>
        /// <param name="orderFunc">Сортировка записей</param>
        /// <param name="listRecordsId">Список обрабатываемых Id</param>
        /// <param name="tsHoursStatus">Утанавливаемый статус</param>
        void ChangeHoursRecordsStatus(IQueryable<TSHoursRecord> data, Expression<Func<TSHoursRecord, bool>> conditionFunc,
                                      Func<IQueryable<TSHoursRecord>, IOrderedQueryable<TSHoursRecord>> orderFunc,
                                      IList<int> listHoursRecordsId, TSRecordStatus tsHoursRecordStatus);

    }
}
