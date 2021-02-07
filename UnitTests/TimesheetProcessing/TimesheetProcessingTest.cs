using System;
using System.Collections.Generic;
using Core.BL.Interfaces;
using Core.Models;
using Data;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace RMX.RPCS.UnitTests.TimesheetProcessing
{
    public class TimesheetProcessingTest
    {
        private Mock<ITSAutoHoursRecordService> _tsAutoHoursRecordService;
        private Mock<IVacationRecordService> _vacationRecordService;
        private Mock<IReportingPeriodService> _reportingPeriodsService;
        private Mock<IProductionCalendarService> _productionCalendarService;
        private Mock<ITSHoursRecordService> _tsHoursRecordsService;
        private Mock<IUserService> _usersService;
        private Mock<IProjectService> _projectsService;

        private List<ReportingPeriod> _reportingPeriodsTest;
        private List<VacationRecord> _vacationRecordsTest;
        private List<TSHoursRecord> _tsHoursRecordsTest;
        private List<TSAutoHoursRecord> _tsAutoHoursRecordsTest;
        private List<ProductionCalendarRecord> _productionCalendarRecordsTest;
        private List<Project> _projectsTest;
        private readonly DbContextOptions<RPCSContext> _dbContextOptions;

        public TimesheetProcessingTest()
        {
            _reportingPeriodsTest = new List<ReportingPeriod>()
            {
                new ReportingPeriod() {ID = 1, Year = 2019, Month = 1, NewTSRecordsAllowedUntilDate = new DateTime(2019,2, 3), VacationProjectID = 1, VacationNoPaidProjectID = 2},
                new ReportingPeriod() {ID = 2, Year = 2019, Month = 2, NewTSRecordsAllowedUntilDate = new DateTime(2019, 3,5), VacationProjectID = 1, VacationNoPaidProjectID = 2},
                new ReportingPeriod() {ID = 3, Year = 2019, Month = 3, NewTSRecordsAllowedUntilDate = new DateTime(2019,4,4), VacationProjectID = 1, VacationNoPaidProjectID = 2},
                new ReportingPeriod() {ID = 4, Year = 2019, Month = 4, NewTSRecordsAllowedUntilDate = new DateTime(2019,5,4), VacationProjectID = 1, VacationNoPaidProjectID = 2},
                new ReportingPeriod() {ID = 5, Year = 2019, Month = 5, NewTSRecordsAllowedUntilDate = new DateTime(2019,6,3), VacationProjectID = 1, VacationNoPaidProjectID = 2}
            };
            _vacationRecordsTest = new List<VacationRecord>()
            {
                new VacationRecord(){ID = 1, VacationBeginDate = new DateTime(2019,2, 6), VacationEndDate = new DateTime(2019, 2,7)},
                new VacationRecord(){ID = 2, VacationBeginDate = new DateTime(2019,1, 21), VacationEndDate = new DateTime(2019,1,22)},

                //записи на начало и конец месяца
                new VacationRecord() {ID = 3, VacationBeginDate = new DateTime(2019,2,19), VacationEndDate = new DateTime(2019,2, 25)},
                new VacationRecord() {ID = 4, VacationBeginDate = new DateTime(2019,2,19), VacationEndDate = new DateTime(2019,3, 16)},
                new VacationRecord(){ID =5, VacationBeginDate = new DateTime(2019,2,16), VacationEndDate = new DateTime(2019,2,17)}
            };
            _tsHoursRecordsTest = new List<TSHoursRecord>()
            {
                new TSHoursRecord() {ID =1 , RecordDate = new DateTime(2019,2,23)},
                new TSHoursRecord(){ID =2, RecordDate = new DateTime(2019,2,24)},
                new TSHoursRecord(){ID =2, RecordDate = new DateTime(2019,2,16)},
                new TSHoursRecord(){ID =2, RecordDate = new DateTime(2019,2,17)},
            };
            _projectsTest = new List<Project>()
            {
                new Project(){ID = 1, ShortName = "Проект оплачиваеемого отпуска"},
                new Project(){ID = 2, ShortName = "Проект неоплачиваего отпуска"}
            };

            _productionCalendarRecordsTest = new List<ProductionCalendarRecord>()
            {
                new ProductionCalendarRecord(){CalendarDate = new DateTime(2019,2,1), IsCelebratory = false, WorkingHours = 8},
                new ProductionCalendarRecord(){CalendarDate = new DateTime(2019,2,2), IsCelebratory = true, WorkingHours = 0},
                new ProductionCalendarRecord(){CalendarDate = new DateTime(2019,2,3), IsCelebratory = true, WorkingHours = 0},
                new ProductionCalendarRecord(){CalendarDate = new DateTime(2019,2,4), IsCelebratory = false, WorkingHours = 8},
                new ProductionCalendarRecord(){CalendarDate = new DateTime(2019,2,5), IsCelebratory = false, WorkingHours = 8},
                new ProductionCalendarRecord(){CalendarDate = new DateTime(2019,2,6), IsCelebratory = false, WorkingHours = 8},
                new ProductionCalendarRecord(){CalendarDate = new DateTime(2019,2,7), IsCelebratory = false, WorkingHours = 8},
                new ProductionCalendarRecord(){CalendarDate = new DateTime(2019,2,8), IsCelebratory = false, WorkingHours = 8},
                new ProductionCalendarRecord(){CalendarDate = new DateTime(2019,2,9), IsCelebratory = false, WorkingHours = 8},
                new ProductionCalendarRecord(){CalendarDate = new DateTime(2019,2,10), IsCelebratory = false, WorkingHours = 8},
                new ProductionCalendarRecord(){CalendarDate = new DateTime(2019,2,11), IsCelebratory = false, WorkingHours = 8},
                new ProductionCalendarRecord(){CalendarDate = new DateTime(2019,2,12), IsCelebratory = false, WorkingHours = 8},
                new ProductionCalendarRecord(){CalendarDate = new DateTime(2019,2,13), IsCelebratory = false, WorkingHours = 8},
                new ProductionCalendarRecord(){CalendarDate = new DateTime(2019,2,14), IsCelebratory = false, WorkingHours = 8},
                new ProductionCalendarRecord(){CalendarDate = new DateTime(2019,2,15), IsCelebratory = false, WorkingHours = 8},
                new ProductionCalendarRecord(){CalendarDate = new DateTime(2019,2,16), IsCelebratory = true, WorkingHours = 0},
                new ProductionCalendarRecord(){CalendarDate = new DateTime(2019,2,17), IsCelebratory = true, WorkingHours = 0},
                new ProductionCalendarRecord(){CalendarDate = new DateTime(2019,2,18), IsCelebratory = false, WorkingHours = 8},
                new ProductionCalendarRecord(){CalendarDate = new DateTime(2019,2,19), IsCelebratory = false, WorkingHours = 8},
                new ProductionCalendarRecord(){CalendarDate = new DateTime(2019,2,20), IsCelebratory = false, WorkingHours = 8},
                new ProductionCalendarRecord(){CalendarDate = new DateTime(2019,2,21), IsCelebratory = false, WorkingHours = 8},
                new ProductionCalendarRecord(){CalendarDate = new DateTime(2019,2,22), IsCelebratory = false, WorkingHours = 8},
                new ProductionCalendarRecord(){CalendarDate = new DateTime(2019,2,23), IsCelebratory = true, WorkingHours = 0},
                new ProductionCalendarRecord(){CalendarDate = new DateTime(2019,2,24), IsCelebratory = true, WorkingHours = 0},
                new ProductionCalendarRecord(){CalendarDate = new DateTime(2019,2,25), IsCelebratory = false, WorkingHours = 8},
                new ProductionCalendarRecord(){CalendarDate = new DateTime(2019,2,26), IsCelebratory = false, WorkingHours = 8},
                new ProductionCalendarRecord(){CalendarDate = new DateTime(2019,2,27), IsCelebratory = false, WorkingHours = 8},
                new ProductionCalendarRecord(){CalendarDate = new DateTime(2019,2,28), IsCelebratory = false, WorkingHours = 8}
            };


            _tsAutoHoursRecordService = new Mock<ITSAutoHoursRecordService>(MockBehavior.Strict);
            _vacationRecordService = new Mock<IVacationRecordService>(MockBehavior.Strict);
            _reportingPeriodsService = new Mock<IReportingPeriodService>(MockBehavior.Strict);
            _productionCalendarService = new Mock<IProductionCalendarService>(MockBehavior.Strict);
            _tsHoursRecordsService = new Mock<ITSHoursRecordService>(MockBehavior.Strict);
            _usersService = new Mock<IUserService>(MockBehavior.Strict);
            _projectsService = new Mock<IProjectService>(MockBehavior.Strict);

            // var ob = new DbContextOptionsBuilder<RPCSContext>();
            //ob.UseLazyLoadingProxies();
            //ob.UseNpgsql(Configuration["MyConfig:DbConnectionString"]);
            //return ob.Options;

            _dbContextOptions = new DbContextOptions<RPCSContext>();
    }

        #region VacationTests

        //если отпуск перепадает на выходной
        //[Fact]
        //public void Test_TimesheetProcessingTask_VacationrecordHaveCelebrate()
        //{

        //    _usersService.Setup(x => x.GetUserDataForVersion()).Returns(null);
        //    _reportingPeriodsService.SetupSequence(x => x.GetAll(It.IsAny<Expression<Func<ReportingPeriod, bool>>>()))
        //        .Returns(_reportingPeriodsTest.Where(p => p.Month == 2 && p.Year == 2019).ToList())
        //        .Returns(_reportingPeriodsTest)
        //        .Returns(_reportingPeriodsTest).Returns(_reportingPeriodsTest).Returns(_reportingPeriodsTest);
        //    _vacationRecordService.SetupSequence(x => x.GetAll(It.IsAny<Expression<Func<VacationRecord, bool>>>()))
        //        .Returns(_vacationRecordsTest.Where(x => x.VacationBeginDate >= new DateTime(2019, 2, 16) && x.VacationEndDate <= new DateTime(2019, 2, 17)).ToList())
        //        .Returns(new List<VacationRecord>());


        //    _productionCalendarService.SetupSequence(x => x.GetAllRecords())
        //        .Returns(_productionCalendarRecordsTest.Where(x => x.CalendarDate == new DateTime(2019, 2, 16)).ToList())
        //        .Returns(_productionCalendarRecordsTest.Where(x => x.CalendarDate == new DateTime(2019, 2, 17)).ToList());

        //    _tsHoursRecordsService.SetupSequence(x => x.GetAll(It.IsAny<Expression<Func<TSHoursRecord, bool>>>()))
        //        .Returns(_tsHoursRecordsTest.Where(x => x.RecordDate == new DateTime(2019, 2, 16)).ToList()).Returns(() => null)
        //        .Returns(_tsHoursRecordsTest.Where(x => x.RecordDate == new DateTime(2019, 2, 17)).ToList()).Returns(() => null);
        //    _tsHoursRecordsService.Setup(x => x.Delete(It.IsAny<int>()));

        //    var tptController = new TimesheetProcessingTask(_tsAutoHoursRecordService.Object, _vacationRecordService.Object, _reportingPeriodsService.Object,
        //        _productionCalendarService.Object, _tsHoursRecordsService.Object, _usersService.Object, _projectsService.Object, _dbContextOptions);
        //    var reportLines = new List<string>();
        //    try
        //    {
        //        reportLines = tptController.ProcessVacationRecords(null).ReportLines;
        //    }
        //    catch (NullReferenceException ex)
        //    {
        //        //Assert.AreEqual("", ex.Message);
        //    }
        //    catch (ArgumentNullException ex)
        //    {
        //        //Assert.AreEqual("", ex.Message);
        //    }
        //    //CollectionAssert.AreEqual(reportLines[0], "Удалена запись " + new DateTime(2019, 2, 16).ToShortDateString() + " перепадающая на выходной день.");
        //    //CollectionAssert.AreEqual(reportLines[1], "Удалена запись " + new DateTime(2019, 2, 17).ToShortDateString() + " перепадающая на выходной день.");
        //}

        //не заполнен производственный календарь

        //связанная запись автозагрузки найдена
        //связанная запись автозагрузки не найдена
        #endregion
    }
}
