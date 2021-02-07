using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
//using System.Data.Entity;

namespace Core.Data
{
    public abstract class RepositorySafeDeletableBase<TEntity, TId> : RepositoryBase<TEntity, TId>, IRepositorySafeDeletable<TEntity, TId>
            where TEntity : class
    {
        protected RepositorySafeDeletableBase(DbContext dbContext) : base(dbContext)
        { }

        #region IRepositorySafeDeletable

        public override TEntity GetById(TId id)
        {
            return GetById(id, false);
        }

        public TEntity FindNotracking(TId id)
        {
            throw new NotImplementedException();
        }

        public virtual TEntity GetById(TId id, bool withDeleted)
        {
            var entity = GetByIdInternal(id);
            if (entity != null && IsDeleted(entity) && !withDeleted)
                entity = null;
            return entity;
        }

        public override IList<TEntity> GetAll()
        {
            return GetAll(SafeDeletableState.NotDeleted);
        }

        public override IList<TEntity> GetAll(Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>> sortFunc)
        {
            return GetAll(SafeDeletableState.NotDeleted, sortFunc);
        }

        public override IList<TEntity> GetAll(Expression<Func<TEntity, bool>> condition)
        {
            return GetAll(SafeDeletableState.NotDeleted, condition);
        }

        public override IList<TEntity> GetAll(Expression<Func<TEntity, bool>> condition, Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>> sortFunc)
        {
            return GetAll(SafeDeletableState.NotDeleted, condition, sortFunc);
        }

        public virtual IList<TEntity> GetAll(SafeDeletableState state)
        {
            return GetAll(state, null, null);
        }

        public virtual IList<TEntity> GetAll(SafeDeletableState state, Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>> sortFunc)
        {
            return GetAll(state, null, sortFunc);
        }

        public virtual IList<TEntity> GetAll(SafeDeletableState state, Expression<Func<TEntity, bool>> condition)
        {
            return GetAll(state, condition, null);
        }

        public virtual IList<TEntity> GetAll(SafeDeletableState state, Expression<Func<TEntity, bool>> condition, Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>> sortFunc)
        {
            var data = GetQueryable(state);
            return GetAllInternal(data, condition, sortFunc, null);
        }

        public override int GetCount()
        {
            return GetCount(SafeDeletableState.NotDeleted);
        }

        public override int GetCount(Expression<Func<TEntity, bool>> condition)
        {
            return GetCount(SafeDeletableState.NotDeleted, condition);
        }

        public virtual int GetCount(SafeDeletableState state)
        {
            return GetCount(state, null);
        }

        public virtual int GetCount(SafeDeletableState state, Expression<Func<TEntity, bool>> condition)
        {
            var data = GetQueryable(state);
            return GetCountInternal(data, condition);
        }

        public override IList<TEntity> GetWindow(int startFrom, int windowSize, Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>> sortFunc)
        {
            return GetWindow(SafeDeletableState.NotDeleted, startFrom, windowSize, sortFunc);
        }

        public override IList<TEntity> GetWindow(Expression<Func<TEntity, bool>> condition, int startFrom, int windowSize, Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>> sortFunc)
        {
            return GetWindow(SafeDeletableState.NotDeleted, condition, startFrom, windowSize, sortFunc);
        }

        public virtual IList<TEntity> GetWindow(SafeDeletableState state, int startFrom, int windowSize, Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>> sortFunc)
        {
            return GetWindow(state, null, startFrom, windowSize, sortFunc);
        }

        public virtual IList<TEntity> GetWindow(SafeDeletableState state, Expression<Func<TEntity, bool>> condition, int startFrom, int windowSize, Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>> sortFunc)
        {
            var data = GetQueryable(state);
            return GetWindowInternal(data, condition, startFrom, windowSize, sortFunc);
        }

        public override void Delete(TEntity entity)
        {
            Delete(entity, false);
        }

        public override void Delete(TId id)
        {
            Delete(id, false);
        }

        public virtual void Delete(TEntity entity, bool force)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            if (force)
                DeleteInternal(entity);
            else
            {
                if (IsDeleted(entity))
                    throw new EntityNotFoundException(typeof(TEntity).Name);
                MarkAsDeleted(entity);
                UpdateInternal(entity);
            }
        }

        public virtual void Delete(TId id, bool force)
        {
            if (id == null)
                throw new ArgumentNullException(nameof(id));

            if (force)
                DeleteInternal(id);
            else
            {
                var entity = GetById(id, true);
                if (entity == null || IsDeleted(entity))
                    throw new EntityNotFoundException(typeof(TEntity).Name);
                Delete(entity, false);
            }
        }

        public virtual TEntity Restore(TEntity entity)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            if (!IsDeleted(entity))
                throw new EntityNotDeletedException(typeof(TEntity).Name);
            MarkAsNotDeleted(entity);
            return UpdateInternal(entity);
        }

        public virtual TEntity Restore(TId id)
        {
            if (id == null)
                throw new ArgumentNullException(nameof(id));

            var entity = GetById(id, true);
            if (entity == null)
                throw new EntityNotFoundException(typeof(TEntity).Name);
            if (!IsDeleted(entity))
                throw new EntityNotDeletedException(typeof(TEntity).Name);
            MarkAsNotDeleted(entity);
            return UpdateInternal(entity);
        }

        public override IQueryable<TEntity> GetQueryable()
        {
            return GetQueryable(SafeDeletableState.NotDeleted);
        }

        public virtual IQueryable<TEntity> GetQueryable(SafeDeletableState state)
        {
            return FilterByState(DbSet, state);
        }

        #endregion

        #region Internal implementation

        protected abstract Expression<Func<TEntity, bool>> DeletedCondition { get; }

        protected abstract Expression<Func<TEntity, bool>> NotDeletedCondition { get; }

        protected abstract bool IsDeleted(TEntity entity);

        protected abstract TEntity MarkAsDeleted(TEntity entity);

        protected abstract TEntity MarkAsNotDeleted(TEntity entity);

        protected virtual IQueryable<TEntity> FilterByState(IQueryable<TEntity> data, SafeDeletableState state)
        {
            IQueryable<TEntity> result = data;
            switch (state)
            {
                case SafeDeletableState.Deleted:
                    result = data.Where(DeletedCondition);
                    break;
                case SafeDeletableState.NotDeleted:
                    result = data.Where(NotDeletedCondition);
                    break;
            }
            return result;
        }

        #endregion
    }
}
