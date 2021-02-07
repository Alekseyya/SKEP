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
    public class TSAutoHoursRecordService : RepositoryAwareServiceBase<TSAutoHoursRecord, int, ITSAutoHoursRecordRepository>, ITSAutoHoursRecordService
    {
        private readonly (string, string) _user;

        public TSAutoHoursRecordService(IRepositoryFactory repositoryFactory, IUserService userService) : base(repositoryFactory)
        {
            _user = userService.GetUserDataForVersion();
        }

        public void Validate(TSAutoHoursRecord entity, IValidationRecipient validationRecipient)
        {
            throw new NotImplementedException();
        }


        #region ITSAutoHoursRecordService implements

        public override TSAutoHoursRecord Add(TSAutoHoursRecord tsAutoHoursRecord)
        {
            if (tsAutoHoursRecord == null) throw new ArgumentNullException(nameof(tsAutoHoursRecord));

            var tsAutoHoursRecordRepository = RepositoryFactory.GetRepository<ITSAutoHoursRecordRepository>();
            tsAutoHoursRecord.InitBaseFields(Tuple.Create(_user.Item1, _user.Item2));
            return tsAutoHoursRecordRepository.Add(tsAutoHoursRecord);
        }

        public IList<TSAutoHoursRecord> GetAll()
        {
            return GetAll(null);
        }

        public IList<TSAutoHoursRecord> GetAll(Expression<Func<TSAutoHoursRecord, bool>> conditionFunc)
        {
            return GetAll(conditionFunc, null);
        }

        public IList<TSAutoHoursRecord> GetAll(Expression<Func<TSAutoHoursRecord, bool>> conditionFunc, bool withDeleted)
        {
            IList<TSAutoHoursRecord> result = null;
            if (withDeleted)
                RunWithoutDeletedFilter(() => { result = GetAll(conditionFunc).Where(x => x.IsDeleted).ToList(); });
            else
                result = GetAll(conditionFunc);
            return result;
        }

        public IList<TSAutoHoursRecord> GetAll(Expression<Func<TSAutoHoursRecord, bool>> conditionFunc,
           Func<IQueryable<TSAutoHoursRecord>, IOrderedQueryable<TSAutoHoursRecord>> orderFunc)
        {
            IList<TSAutoHoursRecord> list = RepositoryFactory.GetRepository<ITSAutoHoursRecordRepository>().GetAll();

            if (conditionFunc != null)
            {
                list = list.AsQueryable().Where(conditionFunc).ToList();

                if (orderFunc != null)
                    list = orderFunc(list.AsQueryable()).ToList();
            }

            return list;
        }

        public override TSAutoHoursRecord GetById(int id)
        {
            var tsAutoRepository = RepositoryFactory.GetRepository<ITSAutoHoursRecordRepository>();
            var tsAutoHoursRecord = tsAutoRepository.GetById(id);

            tsAutoHoursRecord.Versions = tsAutoRepository.GetVersions(tsAutoHoursRecord.ID, true);
            return tsAutoHoursRecord;
        }

        public TSAutoHoursRecord GetVersion(int id, int version)
        {
            return GetVersion(id, version, false);
        }

        public TSAutoHoursRecord GetVersion(int id, int version, bool includeRelations)
        {
            var tsAutoHoursRecordRepository = RepositoryFactory.GetRepository<ITSAutoHoursRecordRepository>();
            var tsAutoHoursRecord = tsAutoHoursRecordRepository.GetVersion(id, version);
            tsAutoHoursRecord.Versions = new List<TSAutoHoursRecord>();
            return tsAutoHoursRecord;
        }

        public override TSAutoHoursRecord Update(TSAutoHoursRecord tsAutoHoursRecord)
        {
            if (tsAutoHoursRecord == null) throw new ArgumentNullException(nameof(tsAutoHoursRecord));
            var tsAutoHoursRecordRepository = RepositoryFactory.GetRepository<ITSAutoHoursRecordRepository>();

            var originalItem = tsAutoHoursRecordRepository.FindNoTracking(tsAutoHoursRecord.ID);

            tsAutoHoursRecord.UpdateBaseFields(Tuple.Create(_user.Item1, _user.Item2), originalItem.ID, originalItem);
            originalItem.FreeseVersion(originalItem.ID);

            tsAutoHoursRecordRepository.Add(originalItem);
            return tsAutoHoursRecordRepository.Update(tsAutoHoursRecord);

        }

        #endregion

    }
}