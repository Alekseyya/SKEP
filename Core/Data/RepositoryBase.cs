using System;
using System.Collections.Generic;
//using System.Data.Entity;
//using System.Data.Entity.Infrastructure;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Linq.Expressions;
using Core.Extensions;
using Microsoft.EntityFrameworkCore.Query;

//using Microsoft.EntityFrameworkCore;

namespace Core.Data
{
    public abstract class RepositoryBase<TEntity, TId> : IRepository<TEntity, TId>
            where TEntity : class
    {
        protected RepositoryBase(DbContext dbContext)
        {
            if (dbContext == null)
                throw new ArgumentNullException(nameof(dbContext));

            DbContext = dbContext;
        }

        #region IRepository

        public virtual TEntity GetById(TId id)
        {
            if (id == null)
                throw new ArgumentNullException(nameof(id));

            return GetByIdInternal(id);
        }

        public TEntity FindNoTracking(TId id)
        {
            var keyValues = GetEntityKeyValues(id);
            var entity = DbContextExtention.FirstOfDefaultIdEquals(DbSet.AsNoTracking(), keyValues[0]);
            return entity;
        }

        public virtual IList<TEntity> GetAll()
        {
            return GetAll(null, null, null);
        }

        public virtual IList<TEntity> GetAll(Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>> sortFunc)
        {
            return GetAll(null, sortFunc, null);
        }

        public virtual IList<TEntity> GetAll(Expression<Func<TEntity, bool>> condition)
        {
            return GetAll(condition, null, null);
        }
        public virtual IList<TEntity> GetAll(Expression<Func<TEntity, bool>> condition, Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>> sortFunc)
        {
            var data = GetQueryable();
            return GetAllInternal(data, condition, sortFunc, null);
        }

        public IList<TEntity> GetAll(Expression<Func<TEntity, bool>> condition, Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>> sortFunc, Func<IQueryable<TEntity>, IQueryable<TEntity>> includeFuncs)
        {
            var data = GetQueryable();
            return GetAllInternal(data, condition, sortFunc, includeFuncs);
        }

        public virtual int GetCount()
        {
            return GetCount(null);
        }

        public virtual int GetCount(Expression<Func<TEntity, bool>> condition)
        {
            var data = GetQueryable();
            return GetCountInternal(data, condition);
        }

        public virtual IList<TEntity> GetWindow(int startFrom, int windowSize, Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>> sortFunc)
        {
            return GetWindow(null, startFrom, windowSize, sortFunc);
        }

        public virtual IList<TEntity> GetWindow(Expression<Func<TEntity, bool>> condition, int startFrom, int windowSize, Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>> sortFunc)
        {
            var data = GetQueryable();
            return GetWindowInternal(data, condition, startFrom, windowSize, sortFunc);
        }

        public virtual TEntity Add(TEntity entity)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            DbSet.Add(entity);
            DbContext.SaveChanges();
            return entity;
        }

        public virtual void Add(IList<TEntity> listEntry, bool recreateContext, int commitCount)
        {
            //DbContext.ChangeTracker.AutoDetectChangesEnabled = false;
            int count = 0;
            foreach (var entry in listEntry)
            {
                ++count;
                //Todo оставить тут
                var local = DbContextExtention.FirstOfDefaultIdEquals(DbSet.Local.ToObservableCollection(), entry);
                if (local != null)
                    DbContext.Entry(local).State = EntityState.Detached;

                DbSet.Add(entry);
                if (count % commitCount == 0)
                {
                    DbContext.SaveChanges();
                    if (recreateContext)
                    {
                        DbContext.Dispose();
                        //TODO переделать!!
                        //DbContext = new DbContext("RPCSContext");
                        //DbContext.ChangeTracker.AutoDetectChangesEnabled = false;
                    }
                }
            }
            DbContext.SaveChanges();
        }

        public virtual TEntity AddWithUnmodifiedRelations(TEntity entity)
        {
            if (entity == null) throw new ArgumentNullException(nameof(entity));

            DbContext.Entry(entity).State = EntityState.Added;
            DbContext.SaveChanges();
            return entity;
        }

        public virtual IList<TEntity> Update(IList<TEntity> entities, int commitCount = 100)
        {
            int count = 0;
            foreach (var entity in entities)
            {
                count++;
                //Todo оставить тут
                var local = DbContextExtention.FirstOfDefaultIdEquals(DbSet.Local.ToObservableCollection(), entity);
                if (local != null)
                    DbContext.Entry(local).State = EntityState.Detached;

                if (!DbSet.Local.Any(item => item == entity))
                    DbContext.Entry(entity).State = EntityState.Modified;
                try
                {
                    if (count % commitCount == 0)
                    {
                        DbContext.SaveChanges();
                    }
                }
                //catch (System.Data.Entity.Validation.DbEntityValidationException dbEx)
                //{
                //    Exception raise = dbEx;
                //    foreach (var validationErrors in dbEx.EntityValidationErrors)
                //    {
                //        foreach (var validationError in validationErrors.ValidationErrors)
                //        {
                //            string message = string.Format("{0}:{1}",
                //                validationErrors.Entry.Entity.ToString(),
                //                validationError.ErrorMessage);
                //            // raise a new exception nesting
                //            // the current instance as InnerException
                //            raise = new InvalidOperationException(message, raise);
                //        }
                //    }

                //    throw raise;
                //}
                catch (DbUpdateConcurrencyException exc)
                {
                    throw new EntityNotFoundException(typeof(TEntity).Name, exc);
                }
            }
            DbContext.SaveChanges();

            return entities;
        }

        public virtual TEntity Update(TEntity entity)
        {
            return UpdateInternal(entity);
        }

        public virtual void Delete(TEntity entity)
        {
            DeleteInternal(entity);
        }

        public virtual void Delete(TId id)
        {
            DeleteInternal(id);
        }

        public void RemoveRange(IList<TEntity> entries)
        {
            DbContext.Set<TEntity>().RemoveRange(entries);
            DbContext.SaveChanges();
        }

        public virtual IQueryable<TEntity> GetQueryable()
        {
            return DbSet;
        }

        public IQueryable<TEntity> GetQueryableAsNoTracking()
        {
            return DbSet.AsNoTracking();
        }

        #endregion

        #region Internal implementation

        protected DbContext DbContext { get; }

        protected virtual DbSet<TEntity> DbSet
        {
            get { return DbContext.Set<TEntity>(); }
        }

        protected abstract object[] GetEntityKeyValues(TId id);

        protected abstract TEntity CreateEntityWithId(TId id);

        protected abstract bool CompareEntityId(TEntity entity, TId id);

        protected virtual TEntity GetByIdInternal(TId id)
        {
            var keyValues = GetEntityKeyValues(id);
            var entity = DbSet.Find(keyValues);
            return entity;
        }

        public virtual TEntity GetByIdInclude(TId id, params Expression<Func<TEntity, object>>[] includes)
        {
            var entry = DbContextExtention.FirstOfDefaultIdEquals(Include(includes), id);
            return entry;
        }

        protected IQueryable<TEntity> Include(params Expression<Func<TEntity, object>>[] includes)
        {
            IIncludableQueryable<TEntity, object> query = null;

            if (includes.Length > 0)
            {
                query = DbSet.Include(includes[0]);
            }
            for (int queryIndex = 1; queryIndex < includes.Length; ++queryIndex)
            {
                query = query.Include(includes[queryIndex]);
            }

            return query == null ? DbSet : (IQueryable<TEntity>)query;
        }


        protected virtual IList<TEntity> GetAllInternal(IQueryable<TEntity> data, Expression<Func<TEntity, bool>> condition, Func<IQueryable<TEntity>,
            IOrderedQueryable<TEntity>> sortFunc, Func<IQueryable<TEntity>, IQueryable<TEntity>> includeFuncs)
        {
            if (condition != null)
                data = data.Where(condition);
            List<TEntity> result;
            if (includeFuncs != null)
                data = includeFuncs(data);
            if (sortFunc != null)
            {
                var sortedData = sortFunc(data);
                result = sortedData.ToList();
            }
            else
                result = data.ToList();
            return result;
        }

        protected virtual int GetCountInternal(IQueryable<TEntity> data, Expression<Func<TEntity, bool>> condition)
        {
            if (condition != null)
                data = data.Where(condition);
            return data.Count();
        }

        protected virtual IList<TEntity> GetWindowInternal(IQueryable<TEntity> data, Expression<Func<TEntity, bool>> condition, int startFrom, int windowSize, Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>> sortFunc)
        {
            if (sortFunc == null)
                throw new ArgumentNullException(nameof(sortFunc));
            if (startFrom < 0)
                throw new ArgumentOutOfRangeException(nameof(startFrom), startFrom, "Starting index should be non-negative");
            if (windowSize < 1)
                throw new ArgumentOutOfRangeException(nameof(windowSize), windowSize, "Window size should be positive");

            if (condition != null)
                data = data.Where(condition);
            var orderedData = sortFunc(data);
            var window = orderedData.Skip(startFrom).Take(windowSize);
            var result = window.ToList();
            return result;
        }

        protected virtual TEntity UpdateInternal(TEntity entity)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            //Todo оставить тут
            var local = DbContextExtention.FirstOfDefaultIdEquals(DbSet.Local.ToObservableCollection(), entity);
            if (local != null)
                DbContext.Entry(local).State = EntityState.Detached;

            if (!DbSet.Local.Any(item => item == entity))
                DbContext.Entry(entity).State = EntityState.Modified;
            try
            {
                DbContext.SaveChanges();
            }
            //catch (DbEntityValidationException dbEx)
            //{
            //    Exception raise = dbEx;
            //    foreach (var validationErrors in dbEx.EntityValidationErrors)
            //    {
            //        foreach (var validationError in validationErrors.ValidationErrors)
            //        {
            //            string message = string.Format("{0}:{1}",
            //                validationErrors.Entry.Entity.ToString(),
            //                validationError.ErrorMessage);
            //            // raise a new exception nesting
            //            // the current instance as InnerException
            //            raise = new InvalidOperationException(message, raise);
            //        }
            //    }
            //    throw raise;
            //}
            catch (DbUpdateConcurrencyException exc)
            {
                throw new EntityNotFoundException(typeof(TEntity).Name, exc);
            }
            return entity;
        }

        protected virtual void DeleteInternal(TEntity entity)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            if (!DbSet.Local.Any(item => item == entity))
                DbSet.Attach(entity);
            DbSet.Remove(entity);
            try
            {
                DbContext.SaveChanges();
            }
            catch (DbUpdateConcurrencyException exc)
            {
                throw new EntityNotFoundException(typeof(TEntity).Name, exc);
            }
        }

        protected virtual void DeleteInternal(TId id)
        {
            if (id == null)
                throw new ArgumentNullException(nameof(id));

            var entity = DbSet.Local.FirstOrDefault(item => CompareEntityId(item, id));
            if (entity == null)
            {
                entity = CreateEntityWithId(id);
                DbSet.Attach(entity);
            }
            DbSet.Remove(entity);
            try
            {
                DbContext.SaveChanges();
            }
            
            //catch (Entity.Validation.DbEntityValidationException dbEx)
            //{
            //    Exception raise = dbEx;
            //    foreach (var validationErrors in dbEx.EntityValidationErrors)
            //    {
            //        foreach (var validationError in validationErrors.ValidationErrors)
            //        {
            //            string message = string.Format("{0}:{1}",
            //                validationErrors.Entry.Entity.ToString(),
            //                validationError.ErrorMessage);
            //            // raise a new exception nesting
            //            // the current instance as InnerException
            //            raise = new InvalidOperationException(message, raise);
            //        }
            //    }
            //    throw raise;
            //}
            
            catch (DbUpdateConcurrencyException exc)
            {
                throw new EntityNotFoundException(typeof(TEntity).Name, exc);
            }
            catch (DbUpdateException exc)
            {
                throw exc;
            }
        }

        #endregion
    }
}
