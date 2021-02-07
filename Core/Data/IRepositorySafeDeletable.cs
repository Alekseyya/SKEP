using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Core.Data
{
    public interface IRepositorySafeDeletable<TEntity, TId> : IRepository<TEntity, TId>
    {
        TEntity GetById(TId id, bool withDeleted);
        TEntity FindNotracking(TId id);

        IList<TEntity> GetAll(SafeDeletableState state);

        IList<TEntity> GetAll(SafeDeletableState state, Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>> sortFunc);

        IList<TEntity> GetAll(SafeDeletableState state, Expression<Func<TEntity, bool>> condition);

        IList<TEntity> GetAll(SafeDeletableState state, Expression<Func<TEntity, bool>> condition, Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>> sortFunc);

        int GetCount(SafeDeletableState state);

        int GetCount(SafeDeletableState state, Expression<Func<TEntity, bool>> condition);

        IList<TEntity> GetWindow(SafeDeletableState state, int startFrom, int windowSize, Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>> sortFunc);

        IList<TEntity> GetWindow(SafeDeletableState state, Expression<Func<TEntity, bool>> condition, int startFrom, int windowSize, Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>> sortFunc);

        void Delete(TEntity entity, bool force);

        void Delete(TId id, bool force);

        TEntity Restore(TEntity entity);

        TEntity Restore(TId id);

        IQueryable<TEntity> GetQueryable(SafeDeletableState state);
    }
}
