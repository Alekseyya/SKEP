using System;
using System.Collections.Generic;
using Core.BL;
using Core.Models;


namespace Core.BL.Interfaces
{
    public interface IProductionCalendarService : IEntityValidatingService<ProductionCalendarRecord>, IServiceBase<ProductionCalendarRecord, int>
    {
        ProductionCalendarRecord AddRecord(ProductionCalendarRecord record);
        void DeleteRecord(int id);
        int GetMonthWorkHours(int month, int year);
        int GetWorkHoursBetweenDates(DateTime startDate, DateTime endDate);
        ProductionCalendarRecord GetLastWorkDayInMonth(int year, int month);
        /// <summary>
        /// Получить указанный(числовой) рабочий день в текущей неделе
        /// </summary>
        /// <param name="numberWorkDay">Число</param>
        /// <returns></returns>
        ProductionCalendarRecord GetSpecifiedWorkDayInCurrentWeek(int numberWorkDay);

        /// <summary>
        /// Получить указанный(числовой) рабочий день в выбранной недел
        /// </summary>
        /// <param name="numberWorkDay"></param>
        /// <returns></returns>
        ProductionCalendarRecord GetSpecifiedWorkDayInSelectedWeek(int numberWorkDay, DateTime dateTime);
        int GetSumWorkingHoursForMonth(int year, int month);
        int GetSumWorkingHoursForDateRange(DateTimeRange range);
        IList<ProductionCalendarRecord> GetAllRecords();
        ProductionCalendarRecord GetRecordByDate(DateTime date);
        ProductionCalendarRecord GetRecordById(int id);
        DateTime GetWorkDayOfNextWeek(DateTime dateTime);
        IList<ProductionCalendarRecord> GetRecordsForDateRange(DateTimeRange range);
        Dictionary<DateTime, int> GetWorkHoursByDate(DateTimeRange dateRange);
        ProductionCalendarRecord UpdateRecord(ProductionCalendarRecord record);
        DateTime AddWorkingDaysToDate(DateTime date, int workingDaysNumber);
    }
}