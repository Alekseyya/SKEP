using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using Core.BL;
using Core.Validation;

using Core.BL.Interfaces;
using Core.Data;
using Core.Data.Interfaces;
using Core.Models;

namespace BL.Implementation
{
    public class VacationRecordService : RepositoryAwareServiceBase<VacationRecord, int, IVacationRecordRepository>, IVacationRecordService
    {
        private readonly (string, string) _user;

        public VacationRecordService(IRepositoryFactory repositoryFactory, IUserService userService) : base(repositoryFactory)
        {
            _user = userService.GetUserDataForVersion();
        }

        public void Validate(VacationRecord entity, IValidationRecipient validationRecipient)
        {
            throw new NotImplementedException();
        }

        #region IVacationService
        public override VacationRecord Add(VacationRecord vacationRecord)
        {
            if (vacationRecord == null) throw new ArgumentNullException(nameof(vacationRecord));

            var vacationRecordRepository = RepositoryFactory.GetRepository<IVacationRecordRepository>();
            vacationRecord.InitBaseFields(Tuple.Create(_user.Item1, _user.Item2));
            return vacationRecordRepository.Add(vacationRecord);
        }

        public IList<VacationRecord> GetAll()
        {
            return GetAll(null);
        }

        public IList<VacationRecord> GetAll(Expression<Func<VacationRecord, bool>> conditionFunc)
        {
            var repository = RepositoryFactory.GetRepository<IVacationRecordRepository>();
            if (conditionFunc != null)
            {
                var vacationRecords = repository.GetAll(conditionFunc);
                return vacationRecords;
            }
            return repository.GetAll();
        }

        public override VacationRecord GetById(int id)
        {
            var vacationRecordRepository = RepositoryFactory.GetRepository<IVacationRecordRepository>();
            var vacationRecord = vacationRecordRepository.GetById(id);

            vacationRecord.Versions = vacationRecordRepository.GetVersions(vacationRecord.ID, true);
            return vacationRecord;
        }

        public VacationRecord GetVersion(int id, int version)
        {
            return GetVersion(id, version, false);
        }

        public VacationRecord GetVersion(int id, int version, bool includeRelations)
        {
            var vactionRecordRepository = RepositoryFactory.GetRepository<IVacationRecordRepository>();
            var vacationRecord = vactionRecordRepository.GetVersion(id, version);
            //TODO Доделать есть надо связи вставить
            vacationRecord.Versions = new List<VacationRecord>();
            return vacationRecord;
        }

        public override VacationRecord Update(VacationRecord vacationRecord)
        {
            if (vacationRecord == null) throw new ArgumentNullException(nameof(vacationRecord));
            var vacationRecordRepository = RepositoryFactory.GetRepository<IVacationRecordRepository>();

            var originalItem = vacationRecordRepository.FindNoTracking(vacationRecord.ID);

            vacationRecord.UpdateBaseFields(Tuple.Create(_user.Item1, _user.Item2), originalItem.ID, originalItem);
            originalItem.FreeseVersion(originalItem.ID);

            vacationRecordRepository.Add(originalItem);
            return vacationRecordRepository.Update(vacationRecord);
        }

        public void UpdateWithoutVersion(VacationRecord vacationRecord)
        {
            if (vacationRecord == null)
                throw new ArgumentNullException(nameof(vacationRecord));

            var vacationRecordRepository = RepositoryFactory.GetRepository<IVacationRecordRepository>();

            vacationRecordRepository.Update(vacationRecord);
        }

        #endregion

    }
}