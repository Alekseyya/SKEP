using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace Core.Data
{
    public interface IRepository<TEntity, TId>
    {
        TEntity GetById(TId id);
        TEntity GetByIdInclude(TId id, params Expression<Func<TEntity, object>>[] includes);
        TEntity FindNoTracking(TId id);

        IList<TEntity> GetAll();

        IList<TEntity> GetAll(Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>> sortFunc);

        IList<TEntity> GetAll(Expression<Func<TEntity, bool>> condition);

        IList<TEntity> GetAll(Expression<Func<TEntity, bool>> condition, Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>> sortFunc);

        IList<TEntity> GetAll(Expression<Func<TEntity, bool>> condition, Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>> sortFunc,Func<IQueryable<TEntity>, IQueryable<TEntity>> includeFuncs);

        int GetCount();

        int GetCount(Expression<Func<TEntity, bool>> condition);

        IList<TEntity> GetWindow(int startFrom, int windowSize,
            Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>> sortFunc);

        IList<TEntity> GetWindow(Expression<Func<TEntity, bool>> condition, int startFrom, int windowSize,
            Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>> sortFunc);

        TEntity Add(TEntity entity);
        void Add(IList<TEntity> listEntry, bool recreateContext, int commitCount);
        TEntity AddWithUnmodifiedRelations(TEntity entity);

        TEntity Update(TEntity entity);
        IList<TEntity> Update(IList<TEntity> entities, int commitCount);


        void Delete(TEntity entity);

        void Delete(TId id);
        void RemoveRange(IList<TEntity> entries);

        IQueryable<TEntity> GetQueryable();

        IQueryable<TEntity> GetQueryableAsNoTracking();
    }
}
