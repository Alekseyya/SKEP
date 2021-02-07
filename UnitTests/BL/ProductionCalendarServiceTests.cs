using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using BL.Implementation;
using Core.Data;
using Core.Data.Interfaces;
using Core.Models;
using Moq;
using Xunit;

namespace RMX.RPCS.UnitTests.BL
{
   public class ProductionCalendarServiceTests
    {
        private List<ProductionCalendarRecord> _testData;

        private Mock<IProductionCalendarRepository> _repositoryMock;

        private Mock<IRepositoryFactory> _repositoryFactoryMock;

        public ProductionCalendarServiceTests()
        {
            // Исходные данные специально не отсортированы, чтобы проверять сортировку в тестах
            _testData = new List<ProductionCalendarRecord>
            {
                new ProductionCalendarRecord{Year=2018, Month=3, Day=6, CalendarDate=new DateTime(2018, 3, 6), WorkingHours=8, ID=1},
                new ProductionCalendarRecord{Year=2018, Month=3, Day=11, CalendarDate=new DateTime(2018, 3, 11), WorkingHours=0, ID=2},
                new ProductionCalendarRecord{Year=2018, Month=3, Day=7, CalendarDate=new DateTime(2018, 3, 7), WorkingHours=7, ID=3},
                new ProductionCalendarRecord{Year=2018, Month=3, Day=10, CalendarDate=new DateTime(2018, 3, 10), WorkingHours=0, ID=4},
                new ProductionCalendarRecord{Year=2018, Month=3, Day=8, CalendarDate=new DateTime(2018, 3, 8), WorkingHours=0, ID=5},
                new ProductionCalendarRecord{Year=2018, Month=3, Day=9, CalendarDate=new DateTime(2018, 3, 9), WorkingHours=0, ID=6},
                new ProductionCalendarRecord{Year=2018, Month=3, Day=5, CalendarDate=new DateTime(2018, 3, 5), WorkingHours=8, ID=7}
            };

            _repositoryMock = new Mock<IProductionCalendarRepository>(MockBehavior.Strict);

            _repositoryFactoryMock = new Mock<IRepositoryFactory>(MockBehavior.Strict);
            _repositoryFactoryMock.Setup(m => m.GetRepository<IProductionCalendarRepository>()).Returns(_repositoryMock.Object);
        }

        [Fact]
        public void Test_ProductionCalendarService_AddRecord()
        {
            RepositoryTestHelper.SetUpAdd<ProductionCalendarRecord, IProductionCalendarRepository>(_repositoryMock);

            var newRecord = new ProductionCalendarRecord
            {
                Year = 2018,
                Month = 3,
                Day = 1,
                CalendarDate = new DateTime(2018, 3, 1),
                WorkingHours = 8
            };
            var svc = new ProductionCalendarService(_repositoryFactoryMock.Object);
            var savedRecord = svc.AddRecord(newRecord);
            _repositoryMock.Verify(m => m.Add(newRecord), Times.Once());
            //Assert.That(savedRecord, Is.SameAs(newRecord));
            Assert.Same(savedRecord, newRecord);
        }

        [Fact]
        public void Test_ProductionCalendarService_AddRecord_NoParam()
        {
            var svc = new ProductionCalendarService(_repositoryFactoryMock.Object);
            Assert.Throws<ArgumentNullException>(() => svc.AddRecord(null));
            _repositoryMock.Verify(m => m.Add(It.IsAny<ProductionCalendarRecord>()), Times.Never());
        }

        [Fact]
        public void Test_ProductionCalendarService_DeleteRecord()
        {
            RepositoryTestHelper.SetUpDeleteById<ProductionCalendarRecord, IProductionCalendarRepository>(_repositoryMock);

            var svc = new ProductionCalendarService(_repositoryFactoryMock.Object);
            svc.DeleteRecord(1);

            _repositoryMock.Verify(m => m.Delete(1), Times.Once());
        }

        [Fact]
        public void Test_ProductionCalendarService_GetAllRecords()
        {
            RepositoryTestHelper.SetUpGetAllWithSort(_repositoryMock, _testData);

            var svc = new ProductionCalendarService(_repositoryFactoryMock.Object);
            var records = svc.GetAllRecords();

            _repositoryMock.Verify(m => m.GetAll(It.IsAny<Func<IQueryable<ProductionCalendarRecord>, IOrderedQueryable<ProductionCalendarRecord>>>()), Times.Once());
            
            Assert.Equal(7, records.Count());
            Assert.Equal(records[0].CalendarDate, new DateTime(2018, 3, 5));
            Assert.Equal(records[6].CalendarDate, new DateTime(2018, 3, 11));
        }

        [Fact]
        public void Test_ProductionCalendarService_GetRecordByDate()
        {
            RepositoryTestHelper.SetUpGetAllWithCondition(_repositoryMock, _testData);
            _repositoryMock.Setup(m => m.GetAll(It.IsAny<Expression<Func<ProductionCalendarRecord, bool>>>()))
                .Returns<Expression<Func<ProductionCalendarRecord, bool>>>(condition => _testData.AsQueryable().Where(condition).ToList());

            var svc = new ProductionCalendarService(_repositoryFactoryMock.Object);

            var record = svc.GetRecordByDate(new DateTime(2018, 3, 7));
            _repositoryMock.Verify(m => m.GetAll(It.IsAny<Expression<Func<ProductionCalendarRecord, bool>>>()), Times.Once());
            Assert.NotNull(record);
            Assert.Equal(3, record.ID);
            Assert.Equal(record.CalendarDate, new DateTime(2018, 3, 7));


            record = svc.GetRecordByDate(new DateTime(2018, 3, 1));
            _repositoryMock.Verify(m => m.GetAll(It.IsAny<Expression<Func<ProductionCalendarRecord, bool>>>()), Times.Exactly(2));
            Assert.Null(record);
        }

        //[Fact]
        //public void Test_ProductionCalendarService_GetRecordById()
        //{
        //    RepositoryTestHelper.SetUpGetByIdWithIntId(_repositoryMock, _testData, (item, id) => item.ID == id, new int[] { 1, 2, 10 });

        //    var svc = new ProductionCalendarService(_repositoryFactoryMock.Object);

        //    var record = svc.GetRecordById(1);
        //    _repositoryMock.Verify(m => m.GetById(1), Times.Once());
        //    Assert.That(record, Is.Not.Null);
        //    Assert.That(record.ID, Is.EqualTo(1));

        //    record = svc.GetRecordById(2);
        //    _repositoryMock.Verify(m => m.GetById(2), Times.Once());
        //    Assert.That(record, Is.Not.Null);
        //    Assert.That(record.ID, Is.EqualTo(2));

        //    record = svc.GetRecordById(10);
        //    _repositoryMock.Verify(m => m.GetById(10), Times.Once());
        //    Assert.That(record, Is.Null);
        //}

        //[Fact]
        //public void Test_ProductionCalendarService_GetRecordsForDateRange()
        //{
        //    RepositoryTestHelper.SetUpGetAllWithConditionAndSort(_repositoryMock, _testData);

        //    var svc = new ProductionCalendarService(_repositoryFactoryMock.Object);

        //    var records = svc.GetRecordsForDateRange(new DateTimeRange(new DateTime(2018, 3, 5), new DateTime(2018, 3, 7)));
        //    _repositoryMock.Verify(m => m.GetAll(
        //        It.IsAny<Expression<Func<ProductionCalendarRecord, bool>>>(),
        //        It.IsAny<Func<IQueryable<ProductionCalendarRecord>, IOrderedQueryable<ProductionCalendarRecord>>>()),
        //        Times.Once());
        //    Assert.That(records, Has.Count.EqualTo(3));
        //    Assert.That(records[0].CalendarDate, Is.EqualTo(new DateTime(2018, 3, 5)));
        //    Assert.That(records[1].CalendarDate, Is.EqualTo(new DateTime(2018, 3, 6)));
        //    Assert.That(records[2].CalendarDate, Is.EqualTo(new DateTime(2018, 3, 7)));

        //    records = svc.GetRecordsForDateRange(new DateTimeRange(new DateTime(2018, 3, 5), new DateTime(2018, 3, 5)));
        //    _repositoryMock.Verify(m => m.GetAll(
        //        It.IsAny<Expression<Func<ProductionCalendarRecord, bool>>>(),
        //        It.IsAny<Func<IQueryable<ProductionCalendarRecord>, IOrderedQueryable<ProductionCalendarRecord>>>()),
        //        Times.Exactly(2));
        //    Assert.That(records, Has.Count.EqualTo(1));
        //    Assert.That(records[0].CalendarDate, Is.EqualTo(new DateTime(2018, 3, 5)));

        //    records = svc.GetRecordsForDateRange(new DateTimeRange(new DateTime(2018, 3, 1), new DateTime(2018, 3, 6)));
        //    _repositoryMock.Verify(m => m.GetAll(
        //        It.IsAny<Expression<Func<ProductionCalendarRecord, bool>>>(),
        //        It.IsAny<Func<IQueryable<ProductionCalendarRecord>, IOrderedQueryable<ProductionCalendarRecord>>>()),
        //        Times.Exactly(3));
        //    Assert.That(records, Has.Count.EqualTo(2));
        //    Assert.That(records[0].CalendarDate, Is.EqualTo(new DateTime(2018, 3, 5)));
        //    Assert.That(records[1].CalendarDate, Is.EqualTo(new DateTime(2018, 3, 6)));

        //    records = svc.GetRecordsForDateRange(new DateTimeRange(new DateTime(2018, 3, 25), new DateTime(2018, 3, 30)));
        //    _repositoryMock.Verify(m => m.GetAll(
        //        It.IsAny<Expression<Func<ProductionCalendarRecord, bool>>>(),
        //        It.IsAny<Func<IQueryable<ProductionCalendarRecord>, IOrderedQueryable<ProductionCalendarRecord>>>()),
        //        Times.Exactly(4));
        //    Assert.That(records, Has.Count.EqualTo(0));
        //}

        //[Fact]
        //public void Test_ProductionCalendarService_GetWorkhoursByDate()
        //{
        //    RepositoryTestHelper.SetUpGetAllWithConditionAndSort(_repositoryMock, _testData);

        //    var svc = new ProductionCalendarService(_repositoryFactoryMock.Object);

        //    var records = svc.GetWorkHoursByDate(new DateTimeRange(new DateTime(2018, 2, 26), new DateTime(2018, 3, 11)));
        //    _repositoryMock.Verify(m => m.GetAll(
        //        It.IsAny<Expression<Func<ProductionCalendarRecord, bool>>>(),
        //        It.IsAny<Func<IQueryable<ProductionCalendarRecord>, IOrderedQueryable<ProductionCalendarRecord>>>(), 
        //        It.IsAny<List<Func<IQueryable<ProductionCalendarRecord>, IQueryable<ProductionCalendarRecord>>>>()),
        //        Times.Once());

        //    //Assert.That(records, Has.Count.EqualTo(14));
        //    Assert.Equal(14, records.Count);
        //    //Assert.That(records, Contains.Key(new DateTime(2018, 2, 26)));
        //    Assert.Contains(new DateTime(2018, 2, 26), records.Keys);
        //    //Assert.That(records[new DateTime(2018, 2, 26)], Is.EqualTo(8));
        //    Assert.Equal(8, records[new DateTime(2018, 2, 26)]);

        //    //Assert.That(records, Contains.Key(new DateTime(2018, 2, 27)));
        //    Assert.Contains(new DateTime(2018, 2, 27), records.Keys);
        //    //Assert.That(records[new DateTime(2018, 2, 27)], Is.EqualTo(8));
        //    Assert.Equal(8, records[new DateTime(2018, 2, 27)]);

        //    //Assert.That(records, Contains.Key(new DateTime(2018, 2, 28)));
        //    Assert.Contains(new DateTime(2018, 2, 28), records.Keys);
        //    //Assert.That(records[new DateTime(2018, 2, 28)], Is.EqualTo(8));
        //    Assert.Equal(8, records[new DateTime(2018, 2, 28)]);

        //    //Assert.That(records, Contains.Key(new DateTime(2018, 3, 1)));
        //    Assert.Contains(new DateTime(2018, 3, 1), records.Keys);
        //    //Assert.That(records[new DateTime(2018, 3, 1)], Is.EqualTo(8));
        //    Assert.Equal(8, records[new DateTime(2018, 3, 1)]);

        //    //Assert.That(records, Contains.Key(new DateTime(2018, 3, 2)));
        //    Assert.Contains(new DateTime(2018, 3, 2), records.Keys);
        //    //Assert.That(records[new DateTime(2018, 3, 2)], Is.EqualTo(8));
        //    Assert.Equal(8, records[new DateTime(2018, 3, 2)]);

        //    //Assert.That(records, Contains.Key(new DateTime(2018, 3, 3)));
        //    Assert.Contains(new DateTime(2018, 3, 3), records.Keys);
        //    //Assert.That(records[new DateTime(2018, 3, 3)], Is.EqualTo(0));
        //    Assert.Equal(0, records[new DateTime(2018, 3, 3)]);

        //    //Assert.That(records, Contains.Key(new DateTime(2018, 3, 4)));
        //    Assert.Contains(new DateTime(2018, 3, 4), records.Keys);
        //    //Assert.That(records[new DateTime(2018, 3, 4)], Is.EqualTo(0));
        //    Assert.Equal(0, records[new DateTime(2018, 3, 4)]);

        //    //Assert.That(records, Contains.Key(new DateTime(2018, 3, 5)));
        //    Assert.Contains(new DateTime(2018, 3, 5), records.Keys);
        //    //Assert.That(records[new DateTime(2018, 3, 5)], Is.EqualTo(8));
        //    Assert.Equal(8, records[new DateTime(2018, 3, 5)]);

        //    //Assert.That(records, Contains.Key(new DateTime(2018, 3, 6)));
        //    Assert.Contains(new DateTime(2018, 3, 6), records.Keys);
        //    //Assert.That(records[new DateTime(2018, 3, 6)], Is.EqualTo(8));
        //    Assert.Equal(8, records[new DateTime(2018, 3, 6)]);

        //    //Assert.That(records, Contains.Key(new DateTime(2018, 3, 7)));
        //    Assert.Contains(new DateTime(2018, 3, 7), records.Keys);
        //    //Assert.That(records[new DateTime(2018, 3, 7)], Is.EqualTo(7));
        //    Assert.Equal(7, records[new DateTime(2018, 3, 7)]);

        //    //Assert.That(records, Contains.Key(new DateTime(2018, 3, 8)));
        //    Assert.Contains(new DateTime(2018, 3, 8), records.Keys);
        //    //Assert.That(records[new DateTime(2018, 3, 8)], Is.EqualTo(0));
        //    Assert.Equal(0, records[new DateTime(2018, 3, 8)]);

        //    //Assert.That(records, Contains.Key(new DateTime(2018, 3, 9)));
        //    Assert.Contains(new DateTime(2018, 3, 9), records.Keys);
        //    //Assert.That(records[new DateTime(2018, 3, 9)], Is.EqualTo(0));
        //    Assert.Equal(0, records[new DateTime(2018, 3, 9)]);

        //    //Assert.That(records, Contains.Key(new DateTime(2018, 3, 10)));
        //    Assert.Contains(new DateTime(2018, 3, 10), records.Keys);
        //    //Assert.That(records[new DateTime(2018, 3, 10)], Is.EqualTo(0));
        //    Assert.Equal(0, records[new DateTime(2018, 3, 10)]);

        //    //Assert.That(records, Contains.Key(new DateTime(2018, 3, 11)));
        //    Assert.Contains(new DateTime(2018, 3, 11), records.Keys);
        //    //Assert.That(records[new DateTime(2018, 3, 11)], Is.EqualTo(0));
        //    Assert.Equal(0, records[new DateTime(2018, 3, 11)]);
        //}

        //[Fact]
        //public void Test_ProductionCalendarService_UpdateRecord()
        //{
        //    RepositoryTestHelper.SetUpUpdate<ProductionCalendarRecord, IProductionCalendarRepository>(_repositoryMock);

        //    var svc = new ProductionCalendarService(_repositoryFactoryMock.Object);
        //    var newRecord = new ProductionCalendarRecord
        //    {
        //        ID = 1,
        //        CalendarDate = new DateTime(2018, 3, 6),
        //        Year = 2018,
        //        Month = 3,
        //        Day = 6,
        //        WorkingHours = 4
        //    };

        //    var updatedRecord = svc.UpdateRecord(newRecord);
        //    _repositoryMock.Verify(m => m.Update(newRecord), Times.Once());
        //    Assert.That(updatedRecord, Is.SameAs(newRecord));
        //}

        //[Fact]
        //public void Test_ProductionCalendarService_UpdateRecord_NoParam()
        //{
        //    var svc = new ProductionCalendarService(_repositoryFactoryMock.Object);

        //    Assert.Throws<ArgumentNullException>(() => svc.UpdateRecord(null));
        //    _repositoryMock.Verify(m => m.Update(It.IsAny<ProductionCalendarRecord>()), Times.Never());
        //}

        //[Fact]
        //public void Test_ProductionCalendarService_Validate()
        //{
        //    RepositoryTestHelper.SetUpGetCountWithCondition(_repositoryMock, _testData);
        //    var svc = new ProductionCalendarService(_repositoryFactoryMock.Object);

        //    var validationRecipientMock = new Mock<IValidationRecipient>();
        //    var record = new ProductionCalendarRecord
        //    {
        //        ID = 100,
        //        CalendarDate = new DateTime(2018, 3, 1),
        //        Year = 2018,
        //        Month = 3,
        //        Day = 1,
        //        WorkingHours = 8
        //    };
        //    svc.Validate(record, validationRecipientMock.Object);
        //    _repositoryFactoryMock.Verify(m => m.GetRepository<IProductionCalendarRepository>(), Times.Once());
        //}
    }
}
