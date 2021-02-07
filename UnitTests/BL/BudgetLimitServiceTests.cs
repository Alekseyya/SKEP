using System;
using System.Collections.Generic;
using System.Linq;
using BL.Implementation;
using Core.BL.Interfaces;
using Core.Data;
using Core.Data.Interfaces;
using Core.Models;
using Moq;
using Xunit;

namespace RMX.RPCS.UnitTests.BL
{
   public class BudgetLimitServiceTests
    {
        private IList<BudgetLimit> _testBudgets;
        private IList<ExpensesRecord> _testExpenses;

        private Mock<IBudgetLimitRepository> _repositoryBudgetMock;
        private Mock<IExpensesRecordRepository> _repositoryExpensesMock;
        private Mock<IUserService> _userServiceMock;

        private Mock<IRepositoryFactory> _repositoryFactoryMock;

        public BudgetLimitServiceTests()
        {
            _testBudgets = new List<BudgetLimit>
            {
                new BudgetLimit{ProjectID=1, CostSubItemID=1, DepartmentID=1, Year=2018, Month=3, LimitAmount=6.1M, LimitAmountApproved=6, FundsExpendedAmount=2, ID=1},

                new BudgetLimit{ProjectID=1, CostSubItemID=1, DepartmentID=1, Year=2018, Month=2, LimitAmount=2.1M, LimitAmountApproved=2, FundsExpendedAmount=1, ID=2},
            };
            _testExpenses = new List<ExpensesRecord>
            {
                new ExpensesRecord{ExpensesDate=new DateTime(2018, 3, 1), Amount=1, ProjectID=1, CostSubItemID=1, DepartmentID=1, RecordStatus=ExpensesRecordStatus.ActuallySpent, ID=1},
                new ExpensesRecord{ExpensesDate=new DateTime(2018, 3, 1), Amount=3, ProjectID=1, CostSubItemID=1, DepartmentID=1, RecordStatus=ExpensesRecordStatus.ActuallySpent, ID=2},
                new ExpensesRecord{ExpensesDate=new DateTime(2018, 3, 1), Amount=1, ProjectID=1, CostSubItemID=1, DepartmentID=1, RecordStatus=ExpensesRecordStatus.Reserved, ID=3},
                new ExpensesRecord{ExpensesDate=new DateTime(2018, 3, 1), Amount=1, ProjectID=1, CostSubItemID=1, DepartmentID=1, RecordStatus=ExpensesRecordStatus.Reserved, ID=4}
            };

            _repositoryBudgetMock = new Mock<IBudgetLimitRepository>(MockBehavior.Strict);
            _repositoryExpensesMock = new Mock<IExpensesRecordRepository>(MockBehavior.Strict);
            _userServiceMock = new Mock<IUserService>(MockBehavior.Strict);

            _repositoryFactoryMock = new Mock<IRepositoryFactory>(MockBehavior.Strict);
            _repositoryFactoryMock.Setup(m => m.GetRepository<IBudgetLimitRepository>()).Returns(_repositoryBudgetMock.Object);
            _repositoryFactoryMock.Setup(m => m.GetRepository<IExpensesRecordRepository>()).Returns(_repositoryExpensesMock.Object);

            RepositoryTestHelper.SetUpGetQueryable(_repositoryBudgetMock, _testBudgets);
            RepositoryTestHelper.SetUpGetQueryable(_repositoryExpensesMock, _testExpenses);

        }
        [Fact]
        public void TestBudgetLimitService_GetLimitData_NoData()
        {
            var svc = new BudgetLimitService(_repositoryFactoryMock.Object, _userServiceMock.Object);

            var result = svc.GetLimitData(1, 1, 1, 1, 1);
            _repositoryBudgetMock.Verify(m => m.GetQueryable(), Times.Once());
            _repositoryExpensesMock.Verify(m => m.GetQueryable(), Times.Never());
            Assert.Null(result);
        }

        [Fact]
        public void TestBudgetLimitService_GetLimitData_WithData_NoExpenses()
        {
            var svc = new BudgetLimitService(_repositoryFactoryMock.Object, _userServiceMock.Object);

            var result = svc.GetLimitData(1, 1, 1, 2018, 2);
            _repositoryBudgetMock.Verify(m => m.GetQueryable(), Times.Once());
            _repositoryExpensesMock.Verify(m => m.GetQueryable(), Times.Once());
            Assert.NotNull(result);
            Assert.Equal(result.LimitAmount, 2.1M);
            Assert.Equal(result.LimitAmountActuallySpent, 0M);
            Assert.Equal(result.LimitAmountReserved, 0M);
        }

        [Fact]
        public void TestBudgetLimitService_GetLimitData_WithData_WithExpenses()
        {
            var svc = new BudgetLimitService(_repositoryFactoryMock.Object, _userServiceMock.Object);

            var result = svc.GetLimitData(1, 1, 1, 2018, 3);
            _repositoryBudgetMock.Verify(m => m.GetQueryable(), Times.Once());
            _repositoryExpensesMock.Verify(m => m.GetQueryable(), Times.Once());
            Assert.NotNull(result);
            Assert.Equal(result.LimitAmount, 6.1M);
            Assert.Equal(result.LimitAmountActuallySpent, 4M);
            Assert.Equal(result.LimitAmountReserved, 2M);
        }

        [Fact]
        public void TestBudgetLimitService_GetLimitDataSummary_NoData()
        {
            var svc = new BudgetLimitService(_repositoryFactoryMock.Object,_userServiceMock.Object);

            var result = svc.GetLimitDataSummary(1, 1, 1, 1);
            _repositoryBudgetMock.Verify(m => m.GetQueryable(), Times.Once());
            _repositoryExpensesMock.Verify(m => m.GetQueryable(), Times.Once());
            Assert.NotNull(result);
            Assert.Equal(result.Count(), 0);
        }

        [Fact]
        public void TestBudgetLimitService_GetLimitDataSummary_WithData()
        {
            var svc = new BudgetLimitService(_repositoryFactoryMock.Object,_userServiceMock.Object);

            var result = svc.GetLimitDataSummary(1, 1, 1, 2018);
            _repositoryBudgetMock.Verify(m => m.GetQueryable(), Times.Once());
            _repositoryExpensesMock.Verify(m => m.GetQueryable(), Times.Once());
            Assert.NotNull(result);
            Assert.Equal(result.Count(), 2);
            Assert.Equal(result.ElementAt(0).FactPlanPercent, 0);
            Assert.Equal(result.ElementAt(1).FactPlanPercent, 98);
        }

    }
}
