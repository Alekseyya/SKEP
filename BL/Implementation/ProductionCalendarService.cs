using System;
using System.Collections.Generic;
using System.Linq;
using Core;
using Core.BL;
using Core.BL.Interfaces;
using Core.Data;
using Core.Data.Interfaces;
using Core.Extensions;
using Core.Validation;
using BL.Validation;
using Core.Models;


namespace BL.Implementation
{
    public class ProductionCalendarService : RepositoryAwareServiceBase<ProductionCalendarRecord, int, IProductionCalendarRepository>, IProductionCalendarService
    {
        public ProductionCalendarService(IRepositoryFactory repositoryFactory) : base(repositoryFactory)
        { }


        public int GetMonthWorkHours(int month, int year)
        {
            var records = RepositoryFactory.GetRepository<IProductionCalendarRepository>().GetQueryable().Where(x => (x.Year == year && x.Month == month)).ToList();
            int sum = records.Sum(x => x.WorkingHours);
            return sum;
        }

        public int GetWorkHoursBetweenDates(DateTime startDate, DateTime endDate)
        {
            var records = RepositoryFactory.GetRepository<IProductionCalendarRepository>().GetQueryable().Where(x => (x.CalendarDate >= startDate && x.CalendarDate <= endDate)).ToList();
            int sum = records.Sum(x => x.WorkingHours);
            return sum;
        }
        public ProductionCalendarRecord GetRecordById(int id)
        {
            var repository = RepositoryFactory.GetRepository<IProductionCalendarRepository>();
            return repository.GetById(id);
        }

        public ProductionCalendarRecord GetRecordByDate(DateTime date)
        {
            var repository = RepositoryFactory.GetRepository<IProductionCalendarRepository>();
            var records = repository.GetAll(record => record.CalendarDate == date);
            if (records.Count > 0) // TODO: Если больше одной, то вызывать ошибку или оставить вопрос консистентности данных на стороне слоя данных?
                return records[0];
            return null;
        }

        public ProductionCalendarRecord GetLastWorkDayInMonth(int year, int month)
        {
            var repository = RepositoryFactory.GetRepository<IProductionCalendarRepository>();
            return repository.GetAll(productRecord => productRecord.Year == year && productRecord.Month == month && productRecord.WorkingHours != 0)
                .OrderByDescending(x => x.Day).FirstOrDefault();
        }

        public ProductionCalendarRecord GetSpecifiedWorkDayInCurrentWeek(int numberWorkDay)
        {
            //получить эту неделю
            var currentYear = DateTime.Now.Year;
            var currentWeekNumber = DateTimeExtention.GetIso8601WeekOfYear(DateTime.Now);

            var currentWeekStart = DateTimeExtention.FirstDateOfWeekISO8601(currentYear, currentWeekNumber);
            var currentWeekEnd = DateTimeExtention.LastDateOfWeekISO8601(currentYear, currentWeekNumber);

            ProductionCalendarRecord foundWorkingCalendarDay = null;
            var currentWeekDateRange = currentWeekStart.Range(currentWeekEnd);
            foreach (var date in RepositoryFactory.GetRepository<IProductionCalendarRepository>().GetQueryable().Where(x => currentWeekDateRange.Any(o => x.CalendarDate == o)))
            {
                if (date.WorkingHours > 0)
                    --numberWorkDay;

                if (numberWorkDay == 0)
                {
                    foundWorkingCalendarDay = date;
                    break;
                }
            }
            return foundWorkingCalendarDay;
        }

        public ProductionCalendarRecord GetSpecifiedWorkDayInSelectedWeek(int numberWorkDay, DateTime dateTime)
        {
            //получить эту неделю
            var currentYear = dateTime.Year;
            var currentWeekNumber = DateTimeExtention.GetIso8601WeekOfYear(dateTime);

            var currentWeekStart = DateTimeExtention.FirstDateOfWeekISO8601(currentYear, currentWeekNumber);
            var currentWeekEnd = DateTimeExtention.LastDateOfWeekISO8601(currentYear, currentWeekNumber);

            ProductionCalendarRecord foundWorkingCalendarDay = null;
            var currentWeekDateRange = currentWeekStart.Range(currentWeekEnd);
            foreach (var date in RepositoryFactory.GetRepository<IProductionCalendarRepository>().GetQueryable().Where(x => currentWeekDateRange.Any(o => x.CalendarDate == o)))
            {
                if (date.WorkingHours > 0)
                    --numberWorkDay;

                if (numberWorkDay == 0)
                {
                    foundWorkingCalendarDay = date;
                    break;
                }
            }
            return foundWorkingCalendarDay;
        }

        public int GetSumWorkingHoursForMonth(int year, int month)
        {
            return RepositoryFactory.GetRepository<IProductionCalendarRepository>().GetQueryable()
                .Where(productRecordNumber => productRecordNumber.Year == year && productRecordNumber.Month == month && productRecordNumber.WorkingHours != 0)
                .Sum(x => x.WorkingHours);

        }

        public int GetSumWorkingHoursForDateRange(DateTimeRange range)
        {
            return GetRecordsForDateRange(range).Sum(x => x.WorkingHours);
        }

        public IList<ProductionCalendarRecord> GetAllRecords()
        {
            var repository = RepositoryFactory.GetRepository<IProductionCalendarRepository>();
            var records = repository.GetAll((IQueryable<ProductionCalendarRecord> data) => data.OrderBy(p => p.Year).ThenBy(p => p.Month).ThenBy(p => p.Day));
            return records;
        }

        public IList<ProductionCalendarRecord> GetRecordsForDateRange(DateTimeRange range)
        {
            var repository = RepositoryFactory.GetRepository<IProductionCalendarRepository>();
            var records = repository.GetAll(pcr => range.Begin.Date <= pcr.CalendarDate && pcr.CalendarDate <= range.End.Date,
                data => data.OrderBy(pcr => pcr.CalendarDate));
            return records;
        }

        public ProductionCalendarRecord AddRecord(ProductionCalendarRecord record)
        {
            if (record == null)
                throw new ArgumentNullException(nameof(record));

            var repository = RepositoryFactory.GetRepository<IProductionCalendarRepository>();
            return repository.Add(record);
        }

        public ProductionCalendarRecord UpdateRecord(ProductionCalendarRecord record)
        {
            if (record == null)
                throw new ArgumentNullException(nameof(record));

            var repository = RepositoryFactory.GetRepository<IProductionCalendarRepository>();
            return repository.Update(record);
        }

        public void DeleteRecord(int id)
        {
            var repository = RepositoryFactory.GetRepository<IProductionCalendarRepository>();
            repository.Delete(id);
        }

        public DateTime GetWorkDayOfNextWeek(DateTime dateTime)
        {
            var firstDayOfNextWeek = dateTime.StartOfNextWeek();
            var repository = RepositoryFactory.GetRepository<IProductionCalendarRepository>();
            var record = repository.GetQueryable().FirstOrDefault(pcr => pcr.CalendarDate >= firstDayOfNextWeek && pcr.WorkingHours != 0);
            return record?.CalendarDate ?? default;
        }

        public Dictionary<DateTime, int> GetWorkHoursByDate(DateTimeRange dateRange)
        {
            var workCalenderRecords = GetRecordsForDateRange(dateRange);
            var workhoursByDate = workCalenderRecords.ToDictionary(item => item.CalendarDate, item => item.WorkingHours);
            DateTime date = dateRange.Begin.Date;
            // Суррогатная установка выходных дней на слуйчай, если в производственном календаре нет всех нужных данных
            while (date <= dateRange.End.Date)
            {
                if (!workhoursByDate.ContainsKey(date))
                {
                    int hours = 8;
                    if (date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday)
                        hours = 0;
                    workhoursByDate.Add(date, hours);
                }
                date = date.AddDays(1);
            }
            return workhoursByDate;
        }

        public void Validate(ProductionCalendarRecord entity, IValidationRecipient validationRecipient)
        {
            var repository = RepositoryFactory.GetRepository<IProductionCalendarRepository>();
            var validator = new ProductionСalendarRecordValidator(entity, validationRecipient, repository);
            validator.Validate();
        }

        public DateTime AddWorkingDaysToDate(DateTime date, int workingDaysNumber)
        {
            DateTime result = date;

            var repository = RepositoryFactory.GetRepository<IProductionCalendarRepository>();
            var record = repository.GetQueryable().Where(pcr => pcr.CalendarDate >= date && pcr.WorkingHours != 0).OrderBy(pcr => pcr.CalendarDate).Take(workingDaysNumber).ToList().LastOrDefault();

            if (record != null)
            {
                result = record.CalendarDate;
            }

            return result;
        }
    }
}