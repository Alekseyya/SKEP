using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Core.BL;
using Core.Validation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RMX.RPCS.BL;
using Core.BL.Interfaces;
using Core.Data;
using Core.Data.Interfaces;
using Core.Extensions;
using Core.Models;

namespace BL.Implementation
{
    public class TSHoursRecordService : RepositoryAwareServiceBase<TSHoursRecord, int, ITSHoursRecordRepository>, ITSHoursRecordService
    {
        private readonly (string, string) _user;
        private readonly IUserService _userService;
        private readonly ILogger<TSHoursRecordService> _logger;

        public TSHoursRecordService(IRepositoryFactory repositoryFactory, IUserService userService, ILogger<TSHoursRecordService> logger) : base(repositoryFactory)
        {
            _userService = userService;
            _logger = logger;
            _user = _userService.GetUserDataForVersion();
        }
        public void Validate(TSHoursRecord entity, IValidationRecipient validationRecipient)
        {
            throw new NotImplementedException();
        }

        #region Implements ITSHoursRecordService

        public void Add(TSHoursRecord tsHoursRecord, string currentUserName, string currentUserSID)
        {
            if (tsHoursRecord == null)
                throw new ArgumentNullException();

            var tsHoursRecordRepository = RepositoryFactory.GetRepository<ITSHoursRecordRepository>();
            tsHoursRecord.InitBaseFields(Tuple.Create(currentUserName, currentUserSID));
            tsHoursRecordRepository.Add(tsHoursRecord);
        }

        public override TSHoursRecord Add(TSHoursRecord tsHoursRecord)
        {
            if (tsHoursRecord == null) throw new ArgumentNullException(nameof(tsHoursRecord));
            var tsHoursRecordRepository = RepositoryFactory.GetRepository<ITSHoursRecordRepository>();
            tsHoursRecord.InitBaseFields(Tuple.Create(_user.Item1, _user.Item2));
            return tsHoursRecordRepository.Add(tsHoursRecord);
        }

        public void Add(IList<TSHoursRecord> listTSHoursRecords, string currentUserName, string currentUserSID)
        {
            if (listTSHoursRecords == null) throw new ArgumentNullException(nameof(listTSHoursRecords));
            var tsHoursRecordRepository = RepositoryFactory.GetRepository<ITSHoursRecordRepository>();
            foreach (var tsHoursRecord in listTSHoursRecords)
            {
                tsHoursRecord.InitBaseFields(Tuple.Create(currentUserName, currentUserSID));
            }
            tsHoursRecordRepository.Add(listTSHoursRecords, false, 100);
        }


        public object CheckData(string date)
        {
            //Regex regex = new Regex(@"^(?:(?:31(\/|-|\.)(?:0?[13578]|1[02]))\1|(?:(?:29|30)(\/|-|\.)(?:0?[1,3-9]|1[0-2])\2))(?:(?:1[6-9]|[2-9]\d)?\d{2})$|^(?:29(\/|-|\.)0?2\3(?:(?:(?:1[6-9]|[2-9]\d)?(?:0[48]|[2468][048]|[13579][26])|(?:(?:16|[2468][048]|[3579][26])00))))$|^(?:0?[1-9]|1\d|2[0-8])(\/|-|\.)(?:(?:0?[1-9])|(?:1[0-2]))\4(?:(?:1[6-9]|[2-9]\d)?\d{2})$");
            //Regex aa = new Regex("",RegexOptions.Compiled);

            //Match match = regex.Match(date);
            //var findDate = new DateTime();
            //if (match.Success)
            //{
            //    findDate = DateTime.Parse(date).Date;
            //    return findDate;
            //}

            if (DateTime.TryParse(date, out var newDate))
                return newDate;

            return null;
        }

        public override TSHoursRecord GetById(int id)
        {
            var repository = RepositoryFactory.GetRepository<ITSHoursRecordRepository>();
            var tsHoursRecord = repository.GetById(id);
            tsHoursRecord.Versions = repository.GetVersions(tsHoursRecord.ID, true);
            return tsHoursRecord;
        }

        public IList<TSHoursRecord> GetTSRecordsForApproval(int approveHoursEmployeeId, DateTime dateTimeHoursStartDate, DateTime dateTimeHoursEndDate, int? projectId, TSRecordStatus tSRecordStatus)
        {
            DateTime nullTime = new DateTime();
            IList<TSHoursRecord> recordList = null;
            if (tSRecordStatus == TSRecordStatus.All)
            {
                //Начальная дата
                if (dateTimeHoursStartDate != nullTime && dateTimeHoursEndDate == nullTime && (projectId == null || projectId == 0))
                    recordList = Get(records => records.Where(x => x.RecordDate >= dateTimeHoursStartDate
                    && (x.Project.EmployeePMID == approveHoursEmployeeId || x.Project.EmployeeCAMID == approveHoursEmployeeId)).OrderBy(x => x.Employee.LastName).ToList()).Where(y => y.Project.ApproveHoursEmployeeID == approveHoursEmployeeId).ToList();
                //Конечная дата
                else if (dateTimeHoursEndDate != nullTime && dateTimeHoursStartDate == nullTime && (projectId == null || projectId == 0))
                    recordList = Get(records => records.Where(x =>
                            x.RecordDate <= dateTimeHoursEndDate
                            && (x.Project.EmployeePMID == approveHoursEmployeeId || x.Project.EmployeeCAMID == approveHoursEmployeeId)).OrderBy(x => x.Employee.LastName).ToList()).Where(y => y.Project.ApproveHoursEmployeeID == approveHoursEmployeeId).ToList();
                //Начальное и конечное время
                else if (dateTimeHoursStartDate != nullTime && dateTimeHoursEndDate != nullTime && (projectId == null || projectId == 0))
                    recordList = Get(records => records.Where(x =>
                            x.RecordDate >= dateTimeHoursStartDate && x.RecordDate <= dateTimeHoursEndDate
                            && (x.Project.EmployeePMID == approveHoursEmployeeId || x.Project.EmployeeCAMID == approveHoursEmployeeId)).OrderBy(x => x.Employee.LastName).ToList()).Where(y => y.Project.ApproveHoursEmployeeID == approveHoursEmployeeId).ToList();
                //Начальное время и проетк
                else if (dateTimeHoursStartDate != nullTime && dateTimeHoursEndDate == nullTime && (projectId != null && projectId != 0))
                    recordList = Get(records => records.Where(x =>
                            x.RecordDate >= dateTimeHoursStartDate && x.ProjectID == projectId
                            && (x.Project.EmployeePMID == approveHoursEmployeeId || x.Project.EmployeeCAMID == approveHoursEmployeeId)).OrderBy(x => x.Employee.LastName).ToList()).Where(y => y.Project.ApproveHoursEmployeeID == approveHoursEmployeeId).ToList();
                //Конечное время и проект
                else if (dateTimeHoursStartDate == nullTime && dateTimeHoursEndDate != nullTime && (projectId != null && projectId != 0))
                    recordList = Get(records => records.Where(x =>
                            x.RecordDate <= dateTimeHoursEndDate && x.ProjectID == projectId
                            && (x.Project.EmployeePMID == approveHoursEmployeeId || x.Project.EmployeeCAMID == approveHoursEmployeeId)).OrderBy(x => x.Employee.LastName).ToList()).Where(y => y.Project.ApproveHoursEmployeeID == approveHoursEmployeeId).ToList();
                //Начальное, конечное проект
                else if (dateTimeHoursStartDate != nullTime && dateTimeHoursEndDate != nullTime && (projectId != null && projectId != 0))
                    recordList = Get(records => records.Where(x =>
                            x.RecordDate >= dateTimeHoursStartDate && x.RecordDate <= dateTimeHoursEndDate &&
                            x.ProjectID == projectId
                            && (x.Project.EmployeePMID == approveHoursEmployeeId || x.Project.EmployeeCAMID == approveHoursEmployeeId)).OrderBy(x => x.Employee.LastName).ToList()).Where(y => y.Project.ApproveHoursEmployeeID == approveHoursEmployeeId).ToList();
                //Проект
                else if (dateTimeHoursStartDate == nullTime && dateTimeHoursEndDate == nullTime && (projectId != null && projectId != 0))
                    recordList = Get(records => records.Where(x =>
                            x.ProjectID == projectId
                            && (x.Project.EmployeePMID == approveHoursEmployeeId || x.Project.EmployeeCAMID == approveHoursEmployeeId)).OrderBy(x => x.Employee.LastName).ToList()).Where(y => y.Project.ApproveHoursEmployeeID == approveHoursEmployeeId).ToList();
                //Ничего не указано
                else
                    recordList = Get(records => records.Where(x => (x.Project.EmployeePMID == approveHoursEmployeeId || x.Project.EmployeeCAMID == approveHoursEmployeeId)).ToList().OrderBy(x => x.Employee.LastName).ToList()).Where(y => y.Project.ApproveHoursEmployeeID == approveHoursEmployeeId).ToList();

            }
            else
            {
                //Начальное время
                if (dateTimeHoursStartDate != nullTime && dateTimeHoursEndDate == nullTime && (projectId == null || projectId == 0))
                    recordList = Get(records => records.Where(x =>
                            x.RecordDate >= dateTimeHoursStartDate &&
                            x.RecordStatus == tSRecordStatus
                            && (x.Project.EmployeePMID == approveHoursEmployeeId || x.Project.EmployeeCAMID == approveHoursEmployeeId)).OrderBy(x => x.Employee.LastName).ToList()).Where(y => y.Project.ApproveHoursEmployeeID == approveHoursEmployeeId).ToList();
                //Конечно время
                else if (dateTimeHoursStartDate == nullTime && dateTimeHoursEndDate != nullTime && (projectId == null || projectId == 0))
                    recordList = Get(records => records.Where(x =>
                            x.RecordDate <= dateTimeHoursEndDate &&
                            x.RecordStatus == tSRecordStatus
                            && (x.Project.EmployeePMID == approveHoursEmployeeId || x.Project.EmployeeCAMID == approveHoursEmployeeId)).OrderBy(x => x.Employee.LastName).ToList()).Where(y => y.Project.ApproveHoursEmployeeID == approveHoursEmployeeId).ToList();
                //Начальное и конечно время
                else if (dateTimeHoursStartDate != nullTime && dateTimeHoursEndDate != nullTime && (projectId == null || projectId == 0))
                    recordList = Get(records => records.Where(x =>
                            x.RecordDate >= dateTimeHoursStartDate && x.RecordDate <= dateTimeHoursEndDate &&
                            x.RecordStatus == tSRecordStatus
                            && (x.Project.EmployeePMID == approveHoursEmployeeId || x.Project.EmployeeCAMID == approveHoursEmployeeId)).OrderBy(x => x.Employee.LastName).ToList()).Where(y => y.Project.ApproveHoursEmployeeID == approveHoursEmployeeId).ToList();
                //Начальное и проект
                else if (dateTimeHoursStartDate != nullTime && dateTimeHoursEndDate == nullTime && (projectId != null && projectId != 0))
                    recordList = Get(records => records.Where(x =>
                            x.RecordDate >= dateTimeHoursStartDate &&
                            x.ProjectID == projectId &&
                            x.RecordStatus == tSRecordStatus
                            && (x.Project.EmployeePMID == approveHoursEmployeeId || x.Project.EmployeeCAMID == approveHoursEmployeeId)).OrderBy(x => x.Employee.LastName).ToList()).Where(y => y.Project.ApproveHoursEmployeeID == approveHoursEmployeeId).ToList();
                //Конечно время и проект
                else if (dateTimeHoursStartDate == nullTime && dateTimeHoursEndDate != nullTime && (projectId != null && projectId != 0))
                    recordList = Get(records => records.Where(x =>
                            x.RecordDate <= dateTimeHoursEndDate && x.ProjectID == projectId &&
                            x.RecordStatus == tSRecordStatus
                            && (x.Project.EmployeePMID == approveHoursEmployeeId || x.Project.EmployeeCAMID == approveHoursEmployeeId)).OrderBy(x => x.Employee.LastName).ToList()).Where(y => y.Project.ApproveHoursEmployeeID == approveHoursEmployeeId).ToList();
                //Начальное, конечно проект
                else if (dateTimeHoursStartDate != nullTime && dateTimeHoursEndDate != nullTime && (projectId != null && projectId != 0))
                    recordList = Get(records => records.Where(x =>
                            x.RecordDate >= dateTimeHoursStartDate && x.RecordDate <= dateTimeHoursEndDate &&
                            x.ProjectID == projectId && x.RecordStatus == tSRecordStatus
                            && (x.Project.EmployeePMID == approveHoursEmployeeId || x.Project.EmployeeCAMID == approveHoursEmployeeId)).OrderBy(x => x.Employee.LastName).ToList()).Where(y => y.Project.ApproveHoursEmployeeID == approveHoursEmployeeId).ToList();
                //Проект
                else if (dateTimeHoursStartDate == nullTime && dateTimeHoursEndDate == nullTime && (projectId != null && projectId != 0))
                    recordList = Get(records => records.Where(x =>
                            x.ProjectID == projectId &&
                            x.RecordStatus == tSRecordStatus
                            && (x.Project.EmployeePMID == approveHoursEmployeeId || x.Project.EmployeeCAMID == approveHoursEmployeeId)).OrderBy(x => x.Employee.LastName).ToList()).Where(y => y.Project.ApproveHoursEmployeeID == approveHoursEmployeeId).ToList();
                //Статус
                else if (dateTimeHoursStartDate == nullTime && dateTimeHoursEndDate == nullTime &&
                         (projectId == null || projectId == 0))
                {
                    recordList = Get(records => records.Where(x =>
                            x.RecordStatus == tSRecordStatus
                            && (x.Project.EmployeePMID == approveHoursEmployeeId || x.Project.EmployeeCAMID == approveHoursEmployeeId))
                        .OrderBy(x => x.Employee.LastName)
                        .ToList()).Where(y => y.Project.ApproveHoursEmployeeID == approveHoursEmployeeId).ToList();
                }

            }

            return recordList;
        }

        public IList<TSHoursRecord> GetEmployeeTSHoursRecords(int employeeId, DateTime dateTimeHoursStartDate, DateTime dateTimeHoursEndDate, int? projectId,
            TSRecordStatus tsRecordStatus, DateTime? modifiedDateTime)
        {
            DateTime nullTime = new DateTime();
            var repository = RepositoryFactory.GetRepository<ITSHoursRecordRepository>().GetQueryable();
            IList<TSHoursRecord> recordList = null;
            if (tsRecordStatus == TSRecordStatus.All)
            {
                //Начальная дата
                if (dateTimeHoursStartDate != nullTime && dateTimeHoursEndDate == nullTime && (projectId == null || projectId == 0))
                    recordList = Get(records => records
                        .Include(x=>x.Project).Include(x=>x.Employee)
                        .Where(x => x.RecordDate >= dateTimeHoursStartDate
                                                 && x.EmployeeID == employeeId).OrderBy(x => x.RecordDate).ToList());
                //Конечная дата
                else if (dateTimeHoursEndDate != nullTime && dateTimeHoursStartDate == nullTime && (projectId == null || projectId == 0))
                    recordList = Get(records => records
                        .Include(x => x.Project).Include(x => x.Employee)
                        .Where(x => x.RecordDate <= dateTimeHoursEndDate
                            && x.EmployeeID == employeeId).OrderBy(x => x.RecordDate).ToList());
                //Начальное и конечное время
                else if (dateTimeHoursStartDate != nullTime && dateTimeHoursEndDate != nullTime && (projectId == null || projectId == 0))
                    recordList = Get(records => records.Include(x => x.Project).Include(x => x.Employee)
                        .Where(x => x.RecordDate >= dateTimeHoursStartDate && x.RecordDate <= dateTimeHoursEndDate
                            && x.EmployeeID == employeeId).OrderBy(x => x.RecordDate).ToList());
                //Начальное время и проетк
                else if (dateTimeHoursStartDate != nullTime && dateTimeHoursEndDate == nullTime && (projectId != null && projectId != 0))
                    recordList = Get(records => records
                        .Include(x => x.Project).Include(x => x.Employee)
                        .Where(x => x.RecordDate >= dateTimeHoursStartDate && x.ProjectID == projectId
                            && x.EmployeeID == employeeId).OrderBy(x => x.RecordDate).ToList());
                //Конечное время и проект
                else if (dateTimeHoursStartDate == nullTime && dateTimeHoursEndDate != nullTime && (projectId != null && projectId != 0))
                    recordList = Get(records => records
                        .Include(x => x.Project).Include(x => x.Employee)
                        .Where(x => x.RecordDate <= dateTimeHoursEndDate && x.ProjectID == projectId
                            && x.EmployeeID == employeeId).OrderBy(x => x.RecordDate).ToList());
                //Начальное, конечное проект
                else if (dateTimeHoursStartDate != nullTime && dateTimeHoursEndDate != nullTime && (projectId != null && projectId != 0))
                    recordList = Get(records => records
                        .Include(x => x.Project).Include(x => x.Employee)
                        .Where(x => x.RecordDate >= dateTimeHoursStartDate && x.RecordDate <= dateTimeHoursEndDate
                            && x.ProjectID == projectId && x.EmployeeID == employeeId).OrderBy(x => x.RecordDate).ToList());
                //Проект
                else if (dateTimeHoursStartDate == nullTime && dateTimeHoursEndDate == nullTime && (projectId != null && projectId != 0))
                    recordList = Get(records => records
                        .Include(x => x.Project).Include(x => x.Employee)
                        .Where(x => x.ProjectID == projectId
                            && x.EmployeeID == employeeId).OrderBy(x => x.RecordDate).ToList());
                //Ничего не указано
                else
                    recordList = Get(records => records
                        .Include(x => x.Project).Include(x => x.Employee)
                        .Where(project => project.EmployeeID == employeeId).OrderBy(x => x.RecordDate).ToList());

            }
            else
            {
                //Начальное время
                if (dateTimeHoursStartDate != nullTime && dateTimeHoursEndDate == nullTime && (projectId == null || projectId == 0))
                    recordList = Get(records => records
                        .Include(x => x.Project).Include(x => x.Employee)
                        .Where(x => x.RecordDate >= dateTimeHoursStartDate && x.EmployeeID == employeeId
                            && x.RecordStatus == tsRecordStatus).OrderBy(x => x.RecordDate).ToList());
                //Конечно время
                else if (dateTimeHoursStartDate == nullTime && dateTimeHoursEndDate != nullTime && (projectId == null || projectId == 0))
                    recordList = Get(records => records
                        .Include(x => x.Project).Include(x => x.Employee)
                        .Where(x =>
                            x.RecordDate <= dateTimeHoursEndDate && x.EmployeeID == employeeId
                            && x.RecordStatus == tsRecordStatus).OrderBy(x => x.RecordDate).ToList());
                //Начальное и конечно время
                else if (dateTimeHoursStartDate != nullTime && dateTimeHoursEndDate != nullTime && (projectId == null || projectId == 0))
                    recordList = Get(records => records
                        .Include(x => x.Project).Include(x => x.Employee)
                        .Where(x => x.RecordDate >= dateTimeHoursStartDate && x.RecordDate <= dateTimeHoursEndDate
                                                                         && x.EmployeeID == employeeId
                                                                         && x.RecordStatus == tsRecordStatus).OrderBy(x => x.RecordDate).ToList());
                //Начальное и проект
                else if (dateTimeHoursStartDate != nullTime && dateTimeHoursEndDate == nullTime && (projectId != null && projectId != 0))
                    recordList = Get(records => records
                        .Include(x => x.Project).Include(x => x.Employee)
                        .Where(x =>
                            x.RecordDate >= dateTimeHoursStartDate
                            && x.ProjectID == projectId
                            && x.EmployeeID == employeeId
                            && x.RecordStatus == tsRecordStatus).OrderBy(x => x.RecordDate).ToList());
                //Конечно время и проект
                else if (dateTimeHoursStartDate == nullTime && dateTimeHoursEndDate != nullTime && (projectId != null && projectId != 0))
                    recordList = Get(records => records
                        .Include(x => x.Project).Include(x => x.Employee)
                        .Where(x =>
                            x.RecordDate <= dateTimeHoursEndDate && x.ProjectID == projectId
                            && x.EmployeeID == employeeId
                            && x.RecordStatus == tsRecordStatus).OrderBy(x => x.RecordDate).ToList());
                //Начальное, конечно проект
                else if (dateTimeHoursStartDate != nullTime && dateTimeHoursEndDate != nullTime && (projectId != null && projectId != 0))
                    recordList = Get(records => records
                        .Include(x => x.Project).Include(x => x.Employee)
                        .Where(x =>
                            x.RecordDate >= dateTimeHoursStartDate && x.RecordDate <= dateTimeHoursEndDate
                            && x.ProjectID == projectId
                            && x.EmployeeID == employeeId && x.RecordStatus == tsRecordStatus).OrderBy(x => x.RecordDate).ToList());
                //Проект
                else if (dateTimeHoursStartDate == nullTime && dateTimeHoursEndDate == nullTime && (projectId != null && projectId != 0))
                    recordList = Get(records => records
                        .Include(x => x.Project).Include(x => x.Employee)
                        .Where(x =>
                            x.ProjectID == projectId && x.EmployeeID == employeeId
                            && x.RecordStatus == tsRecordStatus).OrderBy(x => x.RecordDate).ToList());
                //Статус
                else if (dateTimeHoursStartDate == nullTime && dateTimeHoursEndDate == nullTime && (projectId == null || projectId == 0))
                    recordList = Get(records => records
                        .Include(x => x.Project).Include(x => x.Employee)
                        .Where(x =>
                            x.RecordStatus == tsRecordStatus
                            && x.EmployeeID == employeeId).OrderBy(x => x.RecordDate).ToList());

            }

            if (modifiedDateTime != null)
            {
                var newTSHoursRecords = Get(t => t
                    .Include(x => x.Project).Include(x => x.Employee)
                    .Where(records => records.EmployeeID == employeeId && records.Modified >= modifiedDateTime).ToList());
                foreach (var tsHoursRecord in newTSHoursRecords)
                {
                    if (!recordList.Any(x => x.ID == tsHoursRecord.ID))
                        recordList.Add(tsHoursRecord);
                }
            }

            return recordList;

        }

        public void ChangeHoursRecordsStatus(IQueryable<TSHoursRecord> data, IList<int> listHoursRecordsId, TSRecordStatus tsHoursRecordStatus)
        {
            ChangeHoursRecordsStatus(data, null, listHoursRecordsId, tsHoursRecordStatus);
        }

        public void ChangeHoursRecordsStatus(IQueryable<TSHoursRecord> data, Expression<Func<TSHoursRecord, bool>> conditionFunc, IList<int> listHoursRecordsId, TSRecordStatus tsHoursRecordStatus)
        {
            ChangeHoursRecordsStatus(data, conditionFunc, null, listHoursRecordsId, tsHoursRecordStatus);
        }
        public void ChangeHoursRecordsStatus(IQueryable<TSHoursRecord> data, Expression<Func<TSHoursRecord, bool>> conditionFunc,
            Func<IQueryable<TSHoursRecord>, IOrderedQueryable<TSHoursRecord>> orderFunc, IList<int> listHoursRecordsId, TSRecordStatus tsHoursRecordStatus)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));

            if (conditionFunc != null)
                data = data.Where(conditionFunc);
            List<TSHoursRecord> tsHoursRecords;
            if (orderFunc != null)
            {
                var sortedData = orderFunc(data); //вот в этом не уверен!!
                tsHoursRecords = sortedData.ToList();
            }
            else
                tsHoursRecords = data.ToList();
            // Foreach записей и сохранение
            var tsHoursRecordRepository = RepositoryFactory.GetRepository<ITSHoursRecordRepository>();
            var originalItems = new List<TSHoursRecord>();
            foreach (var tsHoursRecord in tsHoursRecords)
            {
                if (listHoursRecordsId.Contains(tsHoursRecord.ID))
                {
                    tsHoursRecord.RecordStatus = tsHoursRecordStatus;

                    var originalItem = tsHoursRecordRepository.FindNoTracking(tsHoursRecord.ID);
                    tsHoursRecord.UpdateBaseFields(Tuple.Create(_user.Item1, _user.Item2), originalItem.ID, originalItem);
                    originalItem.FreeseVersion(originalItem);
                    originalItems.Add(originalItem);
                }
            }
            tsHoursRecordRepository.Add(originalItems, false, 50);
            tsHoursRecordRepository.Update(tsHoursRecords, 50);
        }

        private List<string> SplittingSearchQueryToQueryList(string searchString)
        {
            var searchTokensList = new List<string>();
            searchString = searchString.TrimStart().TrimEnd();
            int index = searchString.IndexOf(" ");
            var firstQuery = string.Empty;
            var secondQuery = string.Empty;
            //Если задано одно условие(например только фамилия или отчетная дата)
            if (index == -1)
            {
                firstQuery = searchString.Substring(0, searchString.Length).ToLower();
                searchTokensList.Add(firstQuery);
            }
            else
            {
                firstQuery = searchString.Substring(0, index).ToLower();
                secondQuery = searchString.Substring(index, searchString.Length - index).Trim().ToLower();
                searchTokensList.Add(firstQuery);
                searchTokensList.Add(secondQuery);
            }

            return searchTokensList;
        }

        public double GetEmployeeTsHoursRecordSummHoursMonthByStatus(int year, int month, int employeeId, params TSRecordStatus[] status)
        {
            return RepositoryFactory.GetRepository<ITSHoursRecordRepository>().GetQueryable()
                .Where(tsRecords => tsRecords.RecordDate.Value.Year == year &&
                                    tsRecords.RecordDate.Value.Month == month && tsRecords.EmployeeID == employeeId && status.Contains(tsRecords.RecordStatus))
                .Select(c => c.Hours ?? 0)
                .DefaultIfEmpty()
                .Sum(h => h);
        }

        public double GetEmployeeTsHoursRecordSummHoursMonthByStatus(int year, int month, int employeeId, int projectId,
            params TSRecordStatus[] status)
        {
            return RepositoryFactory.GetRepository<ITSHoursRecordRepository>().GetQueryable()
                .Where(tsRecords => tsRecords.RecordDate.Value.Year == year &&
                                    tsRecords.RecordDate.Value.Month == month &&
                                    tsRecords.EmployeeID == employeeId &&
                                    tsRecords.ProjectID == projectId &&
                                    status.Contains(tsRecords.RecordStatus))
                .Select(c => c.Hours ?? 0)
                .DefaultIfEmpty()
                .Sum(h => h);
        }

        public IList<TSHoursRecord> FindHoursRecords(string searchString)
        {
            var searchTokensList = SplittingSearchQueryToQueryList(searchString);

            IList<TSHoursRecord> hoursRecords = new List<TSHoursRecord>();
            //если введено две записи(например фамилия и отчетная дата)
            if (searchTokensList.Count != 0 && (searchTokensList.Count > 1 && searchTokensList.Count <= 3))
            {
                var findRecordDate = new DateTime(); //дата на начало времен по тому, что null не сделать
                var listTexts = new List<string>();
                var projectShortName = string.Empty;
                double hours = 0;
                if (searchTokensList.Count == 2)
                {
                    //пройти все значения и назначить дату если она есть иначе new DateTime()
                    foreach (var token in searchTokensList)
                    {
                        var tmpData = CheckData(token);
                        var isProject = RegexExpressions.IsMatch(token.ToUpper());

                        if (tmpData != null)
                            findRecordDate = (DateTime)tmpData;
                        else if (isProject)
                            projectShortName = token;
                        else if (double.TryParse(token.Replace(".", ","), out var newhours))
                            hours = newhours;
                        else
                            listTexts.Add(token);
                    }
                    //Если текстовых полей не вводилось
                    if (listTexts.Count == 0)
                        hoursRecords = Get(records => records.Where(x =>
                                    //Код проекта и отчетная дата
                                    (x.Project.ShortName != null && x.Project.ShortName.ToLower() == projectShortName)
                                    && (x.RecordDate != null && x.RecordDate == findRecordDate)
                                    ||
                                    //Код проекта и часы
                                    (x.Project.ShortName != null && x.Project.ShortName.ToLower() == projectShortName)
                                    && (x.Hours != null && x.Hours == hours)
                            )
                            .OrderBy(x => x.Employee.LastName)
                            //.ThenBy(x => x.Employee.FirstName)
                            //.ThenBy(x => x.Employee.MidName)
                            .ThenBy(x => x.RecordDate).ToList());

                    if (listTexts.Count == 1)
                    {
                        hoursRecords = Get(records => records.Where(x =>
                                    //Фамилия и отчетная дата
                                    ((x.Employee.LastName != null && x.Employee.LastName.ToLower().Contains(listTexts.FirstOrDefault()))
                                     && (x.RecordDate != null && x.RecordDate == (DateTime)findRecordDate))
                                    ||
                                    //Имя и отчетная дата
                                    ((x.Employee.FirstName != null && x.Employee.FirstName.ToLower().Contains(listTexts.FirstOrDefault()))
                                     && (x.RecordDate != null && x.RecordDate == findRecordDate))
                                    ||
                                    //Фамилия и код проекта
                                    (x.Employee.LastName != null && x.Employee.LastName.ToLower().Contains(listTexts.FirstOrDefault()))
                                    && (x.Project.ShortName != null && x.Project.ShortName.ToLower() == projectShortName)
                                    ||
                                    //Фамилия и часы
                                    (x.Employee.LastName != null && x.Employee.LastName.ToLower().Contains(listTexts.FirstOrDefault()))
                                    && (x.Hours != null && x.Hours == hours)

                            )
                            .OrderBy(x => x.Employee.LastName)
                            //.ThenBy(x => x.Employee.FirstName)
                            //.ThenBy(x => x.Employee.MidName)
                            .ThenBy(x => x.RecordDate).ToList());
                    }
                    //Если задано два текстовых поля(например фамилия и состав работ)
                    else if (listTexts.Count == 2)
                    {
                        //пришлось создавать новые переменные, т.к Linq почему-то не понимает если напрямую указывать listTexts[0]
                        var firstText = listTexts[0];
                        var secondText = listTexts[1];
                        hoursRecords = Get(records => records.Where(x =>
                                    //Фамилия и состав работ
                                    (x.Employee.LastName != null && x.Employee.LastName.ToLower().Contains(firstText)
                                     || x.Employee.LastName.ToLower().Contains(secondText))
                                    && (x.Description != null && x.Description.ToLower().Contains(firstText))
                                    || (x.Description != null && x.Description.ToLower().Contains(secondText)))
                            .Where(x => x.Employee.LastName.ToLower().Contains(firstText) || x.Employee.LastName.ToLower().Contains(secondText))
                            .OrderBy(x => x.Employee.LastName)
                            //.ThenBy(x => x.Employee.FirstName)
                            //.ThenBy(x => x.Employee.MidName)
                            .ThenBy(x => x.RecordDate).ToList());
                    }
                }
            }
            else if (searchTokensList.Count == 1)
            {
                var findDate = CheckData(searchString) ?? new DateTime();

                hoursRecords = Get(records => records.Where(x =>
                                        (x.Employee.FirstName != null && x.Employee.FirstName.ToLower().Contains(searchTokensList.FirstOrDefault()))
                                       || (x.Employee.LastName != null && x.Employee.LastName.ToLower().Contains(searchTokensList.FirstOrDefault()))
                                       || (x.Employee.MidName != null && x.Employee.MidName.ToLower().Contains(searchTokensList.FirstOrDefault()))
                                       || (x.Project.ShortName != null && x.Project.ShortName.ToLower().Contains(searchTokensList.FirstOrDefault()))
                                       || (x.RecordDate != null && x.RecordDate == (DateTime)findDate)
                                       //|| (hoursRecord.Hours != null && hoursRecord.Hours.Equals(Convert.ToDouble(searchTokensList.FirstOrDefault())))
                                       || (x.Description != null && x.Description.Contains(searchTokensList.FirstOrDefault()))
                                       /*|| (x.Created != null && x.Created.ToString().Contains(searchTokensList.FirstOrDefault()))*/)
                            .OrderBy(x => x.Employee.LastName)
                            //.ThenBy(x => x.Employee.FirstName)
                            //.ThenBy(x => x.Employee.MidName)
                            .ThenBy(x => x.RecordDate).ToList());
            }


            return hoursRecords;
        }

        public IList<TSHoursRecord> Get(Func<IQueryable<TSHoursRecord>, IList<TSHoursRecord>> expression)
        {
            if (expression == null) throw new ArgumentException(nameof(expression));
            var queryable = RepositoryFactory.GetRepository<ITSHoursRecordRepository>().GetQueryable();
            return expression(queryable);
        }

        public IList<TSHoursRecord> GetAll()
        {
            return GetAll(null);
        }

        public IList<TSHoursRecord> GetAll(Expression<Func<TSHoursRecord, bool>> conditionFunc)
        {
            return GetAll(conditionFunc, null);
        }

        public IList<TSHoursRecord> GetRecordsForWeek(int year, int week, int employeeId)
        {
            var startWeekDate = DateTimeExtention.FirstDateOfWeekISO8601(year, week);
            var endWeekDate = DateTimeExtention.LastDateOfWeekISO8601(year, week);

            return Get(records => records.Where(x => x.RecordDate >= startWeekDate && x.RecordDate <= endWeekDate && x.EmployeeID == employeeId).ToList());
        }


        public IList<TSHoursRecord> GetAll(Expression<Func<TSHoursRecord, bool>> conditionFunc,
            Func<IQueryable<TSHoursRecord>, IOrderedQueryable<TSHoursRecord>> orderFunc)
        {
            IList<TSHoursRecord> list = RepositoryFactory.GetRepository<ITSHoursRecordRepository>().GetAll();

            if (conditionFunc != null)
            {
                list = list.AsQueryable().Where(conditionFunc).ToList();

                if (orderFunc != null)
                    list = orderFunc(list.AsQueryable()).ToList();
            }

            return list;
        }


        //public IList<TSHoursRecord> GetAllLastVersion()
        //{
        //    var repository = RepositoryFactory.GetRepository<ITSHoursRecordRepository>();

        //    var tsHoursRecord = repository.GetAll().Select(x => GetById(x.ID).Versions.FirstOrDefault()).ToList();
        //    return tsHoursRecord;
        //}

        //public IList<TSHoursRecord> GetAllLastVersion(Expression<Func<TSHoursRecord, bool>> conditionFunc)
        //{
        //    return GetAllLastVersion(conditionFunc, null);
        //}

        //public IList<TSHoursRecord> GetAllLastVersion(Expression<Func<TSHoursRecord, bool>> conditionFunc,
        //                                              Func<IQueryable<TSHoursRecord>, IOrderedQueryable<TSHoursRecord>> orderFunc)
        //{
        //    IList<TSHoursRecord> data = new List<TSHoursRecord>();
        //    if (conditionFunc != null)
        //    {
        //        data = GetAllLastVersion().AsQueryable().Where(conditionFunc).ToList();
        //        if (orderFunc != null)
        //            data = orderFunc(data.AsQueryable()).ToList();
        //    }
        //    return data;
        //}



        public IList<TSHoursRecord> GetRecordsHaveManagerComments(int employeeId, int projectId,
            Expression<Func<TSHoursRecord, bool>> conditionFunc, Func<IQueryable<TSHoursRecord>, IEnumerable<IGrouping<string, TSHoursRecord>>> groupFunc)
        {
            var rowsEmployee = Get(records => records.Where(x => x.EmployeeID == employeeId && x.ProjectID == projectId).ToList()).AsQueryable();

            if (conditionFunc != null)
                rowsEmployee = rowsEmployee.Where(conditionFunc);
            IList<TSHoursRecord> tsHoursRecords;
            if (groupFunc != null)
            {
                var groupedData = groupFunc(rowsEmployee);
                tsHoursRecords = groupedData.SelectMany(group => group).ToList();
            }
            else
                tsHoursRecords = rowsEmployee.ToList();

            return tsHoursRecords;
        }

        public IList<TSHoursRecord> GetRecordsHaveManagerComments(int employeeId, IList<int> projectId,
             Expression<Func<TSHoursRecord, bool>> conditionFunc,
             Func<IQueryable<TSHoursRecord>, IEnumerable<IGrouping<string, TSHoursRecord>>> groupFunc)
        {
            if (employeeId <= 0) throw new ArgumentOutOfRangeException(nameof(employeeId));
            var rowsEmployee = Get(records => records.Where(x => projectId.Any(u => u == x.ProjectID) && x.EmployeeID == employeeId).ToList()).AsQueryable();

            if (conditionFunc != null)
                rowsEmployee = rowsEmployee.Where(conditionFunc);
            IList<TSHoursRecord> tsHoursRecords;
            if (groupFunc != null)
            {
                var groupedData = groupFunc(rowsEmployee).Select(x => x.FirstOrDefault());
                tsHoursRecords = groupedData.ToList();
            }
            else
                tsHoursRecords = rowsEmployee.ToList();

            return tsHoursRecords;
        }

        public void Update(TSHoursRecord tsHoursRecord, string currentUserName, string currentUserSID)
        {
            _logger.LogInformation("TSHoursRecordService: Update()");
            if (tsHoursRecord == null) throw new ArgumentNullException(nameof(tsHoursRecord));
            var tsHoursRecordRecordRepository = RepositoryFactory.GetRepository<ITSHoursRecordRepository>();

            var originalItem = tsHoursRecordRecordRepository.FindNoTracking(tsHoursRecord.ID);

            tsHoursRecord.UpdateBaseFields(Tuple.Create(currentUserName, currentUserSID), originalItem.ID, originalItem);
            originalItem.FreeseVersion(originalItem);

            _logger.LogInformation("TSHoursRecordService: Update() - Add");
            tsHoursRecordRecordRepository.Add(originalItem);
            _logger.LogInformation("TSHoursRecordService: Update() - Update");
            tsHoursRecordRecordRepository.Update(tsHoursRecord);
        }

        public void Update(IList<TSHoursRecord> tsHoursRecords)
        {
            if (tsHoursRecords == null) throw new ArgumentNullException(nameof(tsHoursRecords));
            var tsHoursRecordRecordRepository = RepositoryFactory.GetRepository<ITSHoursRecordRepository>();
            var originalItems = new List<TSHoursRecord>();
            foreach (var tsHoursRecord in tsHoursRecords)
            {
                var originalItem = tsHoursRecordRecordRepository.FindNoTracking(tsHoursRecord.ID);
                tsHoursRecord.UpdateBaseFields(Tuple.Create(_user.Item1, _user.Item2), originalItem.ID, originalItem);
                originalItem.FreeseVersion(originalItem);
                originalItems.Add(originalItem);
            }
            tsHoursRecordRecordRepository.Add(originalItems, false, 50);
            tsHoursRecordRecordRepository.Update(tsHoursRecords, 50);
        }

        public void UpdateWithoutVersion(TSHoursRecord tsHoursRecord)
        {
            if (tsHoursRecord == null)
                throw new ArgumentNullException(nameof(tsHoursRecord));

            var tsHoursRecordRecordRepository = RepositoryFactory.GetRepository<ITSHoursRecordRepository>();

            tsHoursRecordRecordRepository.Update(tsHoursRecord);
        }

        public TSHoursRecord GetVersion(int id, int version)
        {
            return GetVersion(id, version, false);
        }

        public TSHoursRecord GetVersion(int id, int version, bool includeRelations)
        {
            var tsHoursRecordRepository = RepositoryFactory.GetRepository<ITSHoursRecordRepository>();
            var tsHoursRecord = tsHoursRecordRepository.GetVersion(id, version);
            //TODO Доделать есть надо связи вставить
            tsHoursRecord.Versions = new List<TSHoursRecord>();
            return tsHoursRecord;
        }

        public bool AnyRecordsInUntilApproveReport(IGrouping<string, Employee> group, int year, int month)
        {
            foreach (var employeeInDepartment in group.ToList())
            {
                foreach (var tsHoursRecord in Get(records => records.Where(x => x.RecordDate.Value.Year == year &&
                                                                                    x.RecordDate.Value.Month == month && x.EmployeeID == employeeInDepartment.ID
                                                                                                       && x.RecordStatus == TSRecordStatus.PMApproved)
                    .ToList()).OrderBy(x => x.Project.ShortName).GroupBy(x => x.Project.ShortName).Select(y => y))
                    return true;
            }
            return false;
        }
        #endregion
    }
}