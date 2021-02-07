using System;
using Core.Data.Interfaces;
using Core.Models;
using Core.Validation;

namespace BL.Validation
{
    public class ProductionСalendarRecordValidator : EntityValidatorBase<ProductionCalendarRecord>
    {
        protected IProductionCalendarRepository Repository { get; }

        public ProductionСalendarRecordValidator(ProductionCalendarRecord entity, IValidationRecipient recipient, IProductionCalendarRepository repository)
            : base(entity, recipient)
        {
            if (repository == null)
                throw new ArgumentNullException(nameof(repository));

            Repository = repository;
        }

        public override void Validate()
        {
            if (Entity.Year != Entity.CalendarDate.Year)
                Recipient.SetError(nameof(Entity.Year), "Год не соответствует календарной дате");
            if (Entity.Month != Entity.CalendarDate.Month)
                Recipient.SetError(nameof(Entity.Month), "Месяц не соответствует календарной дате");
            if (Entity.Day != Entity.CalendarDate.Day)
                Recipient.SetError(nameof(Entity.Day), "День не соответствует календарной дате");
            if (Entity.WorkingHours < 0 || Entity.WorkingHours > 24) // TODO: Ограничить макисмум восемью часами? Вынести максимальное количество часов в настройки?
                Recipient.SetError(nameof(Entity.WorkingHours), "Количество рабочих часов в дне должно быть в диапазоне от 0 до 24");
            int count = Repository.GetCount(record => record.CalendarDate == Entity.CalendarDate.Date);
            if (count > 0) // TODO: Уточнить, валидная ли это ситуация. Возможно, надо заменять запись.
                Recipient.SetError(nameof(Entity.CalendarDate), "Запись с такой датой уже существует");
        }
    }
}