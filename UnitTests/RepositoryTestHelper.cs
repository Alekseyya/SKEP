
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Core.Data;
using Moq;

namespace RMX.RPCS.UnitTests
{
   public class RepositoryTestHelper
    {
        //public static void SetupMockRepositoryWithIntId<TEntity, TRepo>(List<TEntity> testData,
        //    Func<TEntity, int, bool> compareId, Func<TEntity, TEntity, bool> compareEntities, Func<TEntity, IEnumerable<TEntity>, TEntity> fillNewId)
        //    where TRepo : class, IRepository<TEntity, int>
        //{
        //    var testQueryable = testData.AsQueryable();

        //    var repositoryMock = new Mock<TRepo>(MockBehavior.Strict);
        //    repositoryMock.Setup(m => m.GetById(It.IsAny<int>()))
        //        .Returns<int>(id => testQueryable.SingleOrDefault(item => compareId(item, id)));
        //    repositoryMock.Setup(m => m.GetAll())
        //        .Returns(() => testQueryable.ToList());
        //    repositoryMock.Setup(m => m.GetAll(It.IsAny<Expression<Func<TEntity, bool>>>()))
        //        .Returns<Expression<Func<TEntity, bool>>>(condition => testQueryable.Where(condition).ToList());
        //    repositoryMock.Setup(m => m.GetAll(It.IsAny<Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>>()))
        //        .Returns<Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>>(sortFunc => sortFunc(testQueryable).ToList());
        //    repositoryMock.Setup(m => m.GetAll(
        //        It.IsAny<Expression<Func<TEntity, bool>>>(),
        //        It.IsAny<Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>>(),
        //        It.IsAny<List<Func<IQueryable<TEntity>, IQueryable<TEntity>>>>()
        //        ))
        //        .Returns<
        //            Expression<Func<TEntity, bool>>,
        //            Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>
        //        >((condition, sortFunc) => sortFunc(testQueryable.Where(condition)).ToList());
        //    repositoryMock.Setup(m => m.Add(It.IsAny<TEntity>()))
        //        .Returns((TEntity record) =>
        //        {
        //            record = fillNewId(record, testData);
        //            testData.Add(record);
        //            return record;
        //        });
        //    repositoryMock.Setup(m => m.Update(It.IsAny<TEntity>()))
        //        .Returns<TEntity>(record =>
        //        {
        //            int index = testData.FindIndex(item => compareEntities(item, record));
        //            if (index < 0)
        //                throw new EntityNotFoundException();
        //            testData[index] = record;
        //            return record;
        //        });
        //    repositoryMock.Setup(m => m.Delete(It.IsAny<TEntity>()))
        //        .Callback<TEntity>(record =>
        //        {
        //            var found = testData.SingleOrDefault(item => compareEntities(item, record));
        //            if (found == null)
        //                throw new EntityNotFoundException();
        //            testData.Remove(found);
        //        });
        //    repositoryMock.Setup(m => m.Delete(It.IsAny<int>()))
        //        .Callback<int>(id =>
        //        {
        //            var found = testData.SingleOrDefault(item => compareId(item, id));
        //            if (found == null)
        //                throw new EntityNotFoundException();
        //            testData.Remove(found);
        //        });
        //}

        public static void SetUpGetByIdWithIntId<TEntity, TRepository>(Mock<TRepository> repositoryMock, IEnumerable<TEntity> testData, Func<TEntity, int, bool> compareId)
            where TRepository : class, IRepository<TEntity, int>
        {
            repositoryMock.Setup(m => m.GetById(It.IsAny<int>()))
                .Returns<int>(id => testData.SingleOrDefault(entity => compareId(entity, id)));
        }

        public static void SetUpGetByIdWithIntId<TEntity, TRepository>(Mock<TRepository> repositoryMock, IEnumerable<TEntity> testData, Func<TEntity, int, bool> compareId, IEnumerable<int> validIds)
            where TRepository : class, IRepository<TEntity, int>
        {
            repositoryMock.Setup(m => m.GetById(It.IsIn(validIds)))
                .Returns<int>(id => testData.SingleOrDefault(entity => compareId(entity, id)));
        }

        public static void SetUpGetAll<TEntity, TRepository>(Mock<TRepository> repositoryMock, IEnumerable<TEntity> testData)
            where TRepository : class, IRepository<TEntity, int>
        {
            repositoryMock.Setup(m => m.GetAll())
                .Returns(() => testData.ToList());
        }

        public static void SetUpGetAllWithSort<TEntity, TRepository>(Mock<TRepository> repositoryMock, IEnumerable<TEntity> testData)
            where TRepository : class, IRepository<TEntity, int>
        {
            repositoryMock.Setup(m => m.GetAll(It.IsAny<Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>>()))
                .Returns<Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>>(sortFunc => sortFunc(testData.AsQueryable()).ToList());
        }

        public static void SetUpGetAllWithCondition<TEntity, TRepository>(Mock<TRepository> repositoryMock, IEnumerable<TEntity> testData)
            where TRepository : class, IRepository<TEntity, int>
        {
            repositoryMock.Setup(m => m.GetAll(It.IsAny<Expression<Func<TEntity, bool>>>()))
                .Returns<Expression<Func<TEntity, bool>>>(condition => testData.AsQueryable().Where(condition).ToList());
        }

        //public static void SetUpGetAllWithConditionAndSort<TEntity, TRepository>(Mock<TRepository> repositoryMock, IEnumerable<TEntity> testData)
        //    where TRepository : class, IRepository<TEntity, int>
        //{
        //    repositoryMock.Setup(m => m.GetAll(
        //            It.IsAny<Expression<Func<TEntity, bool>>>(),
        //            It.IsAny<Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>>(),
        //            It.IsAny<List<Func<IQueryable<TEntity>, IQueryable<TEntity>>>>()))
        //        .Returns<Expression<Func<TEntity, bool>>,
        //            Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>, List<Func<IQueryable<TEntity>, IQueryable<TEntity>>>> ((condition, sortFunc, includeFuncs) => sortFunc(testData.AsQueryable().Where(condition)).ToList());
        //}

        public static void SetUpGetCount<TEntity, TRepository>(Mock<TRepository> repositoryMock, IEnumerable<TEntity> testData)
            where TRepository : class, IRepository<TEntity, int>
        {
            repositoryMock.Setup(m => m.GetCount())
                .Returns(() => testData.Count());
        }

        public static void SetUpGetCountWithCondition<TEntity, TRepository>(Mock<TRepository> repositoryMock, IEnumerable<TEntity> testData)
            where TRepository : class, IRepository<TEntity, int>
        {
            repositoryMock.Setup(m => m.GetCount(It.IsAny<Expression<Func<TEntity, bool>>>()))
                .Returns<Expression<Func<TEntity, bool>>>(condition => testData.AsQueryable().Count(condition));
        }

        public static void SetUpAdd<TEntity, TRepository>(Mock<TRepository> repositoryMock)
            where TRepository : class, IRepository<TEntity, int>
        {
            repositoryMock.Setup(m => m.Add(It.Is<TEntity>(null))).Throws<ArgumentNullException>();
            repositoryMock.Setup(m => m.Add(It.IsAny<TEntity>()))
                .Returns<TEntity>(entity => entity);
        }

        public static void SetUpUpdate<TEntity, TRepository>(Mock<TRepository> repositoryMock)
            where TRepository : class, IRepository<TEntity, int>
        {
            repositoryMock.Setup(m => m.Update(It.Is<TEntity>(null))).Throws<ArgumentNullException>();
            repositoryMock.Setup(m => m.Update(It.IsAny<TEntity>()))
                .Returns<TEntity>(entity => entity);
        }

        public static void SetUpDeleteEntity<TEntity, TRepository>(Mock<TRepository> repositoryMock)
            where TRepository : class, IRepository<TEntity, int>
        {
            repositoryMock.Setup(m => m.Delete(It.IsAny<TEntity>()))
                .Verifiable();
            repositoryMock.Setup(m => m.Delete(It.Is<TEntity>(null))).Throws<ArgumentNullException>();
        }

        public static void SetUpDeleteById<TEntity, TRepository>(Mock<TRepository> repositoryMock)
            where TRepository : class, IRepository<TEntity, int>
        {
            repositoryMock.Setup(m => m.Delete(It.IsAny<int>()))
                .Verifiable();
        }

        public static void SetUpGetQueryable<TEntity, TRepository>(Mock<TRepository> repositoryMock, IEnumerable<TEntity> testData)
            where TRepository : class, IRepository<TEntity, int>
        {
            repositoryMock.Setup(m => m.GetQueryable())
                .Returns(() => testData.AsQueryable());
        }
    }
}
