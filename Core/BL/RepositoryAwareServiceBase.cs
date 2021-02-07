using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Core.BL.Interfaces;
using Core.Data;
using Core.Extensions;

namespace Core.BL
{
    public enum GetEntityMode
    {
        NotDeletedAndNotVersion = 0,
        Deleted = 1,
        DeletedAndOther = 2,
        Version = 3,
        VersionAndOther = 4,
        VersionAndDeleted = 5,
        DeletedAndVersionAndOther = 6
    }

    public abstract class RepositoryAwareServiceBase<TEntry, TId, TRepository> : IServiceBase<TEntry, TId> where TRepository : IRepository<TEntry, TId>
    {
        protected IRepositoryFactory RepositoryFactory { get; }

        protected void RunWithoutDeletedFilter(Action action)
        {
            try
            {
                RepositoryFactory.DisableDeletedFilter();
                action();
            }
            finally
            {
                RepositoryFactory.EnableDeletedFilter();
            }
        }

        public virtual void RemoveRange(IList<TEntry> entries)
        {
            RepositoryFactory.GetRepository<TRepository>().RemoveRange(entries);
        }

        /// <summary>
        /// Восстановить данные из корзины
        /// </summary>
        /// <param name="id"></param>
        public virtual void RestoreFromRecycleBin(int id)
        {
            if (id != 0)
            {
                try
                {
                    RepositoryFactory.DisableDeletedFilter();
                    var repositoryEntries = RepositoryFactory.GetRepository<TRepository>().GetQueryable().Where("IsDeleted", "ID", id).ToList();
                    foreach (var entry in repositoryEntries)
                    {
                        entry.GetType().GetProperty("IsDeleted")?.SetValue(entry, false);
                        entry.GetType().GetProperty("DeletedDate")?.SetValue(entry, null);
                        entry.GetType().GetProperty("DeletedBy")?.SetValue(entry, null);
                        entry.GetType().GetProperty("DeletedBySID")?.SetValue(entry, null);
                        RepositoryFactory.GetRepository<TRepository>().Update(entry);
                    }
                }
                finally
                {
                    RepositoryFactory.EnableDeletedFilter();
                }
            }
        }

        public virtual TEntry GetByIdWithDeleteFilter(TId id)
        {
            TEntry entity;
            try
            {
                RepositoryFactory.DisableDeletedFilter();
                var repository = RepositoryFactory.GetRepository<TRepository>();
                entity = repository.GetById(id);
                entity.GetType().GetProperty("Versions").SetValue(entity, repository.GetType() //TODO метод для одного параметра 
                    .GetMethod("GetVersions", BindingFlags.Public | BindingFlags.Instance, null, new Type[] { typeof(int) }, null)
                    .Invoke(repository, new object[] { id }));
            }
            finally
            {
                RepositoryFactory.EnableDeletedFilter();
            }

            return entity;
        }

        /// <summary>
        /// Удалить данные из корзины со связынными ItemID записями
        /// </summary>
        /// <param name="parentId"></param>
        public virtual void DeleteRelatedEntries(int parentId)
        {
            var parameter = Expression.Parameter(typeof(TEntry), "x");
            var itemIdProp = Expression.Property(parameter, "ItemID");
            var propertyType = ((PropertyInfo)itemIdProp.Member).PropertyType;
            var nullCheckExp = Expression.NotEqual(itemIdProp, Expression.Constant(null, propertyType));
            var equalCheck = Expression.Equal(itemIdProp, Expression.Convert(Expression.Constant(parentId), propertyType));
            Expression bodyExpression = Expression.And(nullCheckExp, equalCheck);
            var lambda = Expression.Lambda<Func<TEntry, bool>>(bodyExpression, parameter);
            List<TEntry> repositoryEntries;
            try
            {
                //Todo выключаем фильтр(видны только записи с IsDeleted = true)
                RepositoryFactory.DisableDeletedFilter();
                repositoryEntries = RepositoryFactory.GetRepository<TRepository>().GetQueryable().Where(lambda).ToList();

            }
            finally
            {
                RepositoryFactory.EnableDeletedFilter();
            }
            try
            {
                //Todo получаем список записей с включенным фильтром IsVersion = true
                RepositoryFactory.DisableVersionsFilter();
                repositoryEntries.AddRange(RepositoryFactory.GetRepository<TRepository>().GetQueryable().Where(lambda).ToList());
                foreach (var entry in repositoryEntries)
                {
                    RepositoryFactory.GetRepository<TRepository>().Delete(entry);
                }
            }
            finally
            {
                RepositoryFactory.EnableVersionsFilter();
            }
        }

        /// <summary>
        /// Отправить запись в корзину
        /// </summary>
        /// <param name="id"></param>
        /// <param name="user"></param>
        public virtual (bool toRecycleBin, string relatedClassId) RecycleToRecycleBin(int id, string userName, string userSID)
        {
            var toRecycleBin = true;
            var relatedClassId = string.Empty;
            if (id != 0)
            {
                try
                {
                    RepositoryFactory.DisableDeletedFilter();
                    var parameter = Expression.Parameter(typeof(TEntry), "x");
                    Expression leftProperty = Expression.Property(parameter, "IsDeleted");
                    Expression leftExpr = Expression.Equal(leftProperty, Expression.Constant(false));
                    Expression rightProperty = Expression.Property(parameter, "ID");
                    Expression rightExpr = Expression.Equal(rightProperty, Expression.Constant(id));
                    Expression bodyExpr = Expression.AndAlso(leftExpr, rightExpr);
                    var lambda = Expression.Lambda<Func<TEntry, bool>>(bodyExpr, parameter);
                    var repositoryEntries = RepositoryFactory.GetRepository<TRepository>().GetQueryable().Where(lambda).ToList();
                    foreach (var entry in repositoryEntries)
                    {
                        var recycleBinReturTuple = HasRecycleBin(entry);
                        toRecycleBin = recycleBinReturTuple.hasRecycleBin;
                        relatedClassId = recycleBinReturTuple.relatedClassId;

                        //TODO приоритет на помещение в корзину записей трудозатрат с пометкой (Отклонено, Отклонено/Редактируется, На согласовании)
                        if (entry.GetType().BaseType.Name == "TSHoursRecord")
                        {
                            var tsRecordStatusObject = entry.GetType().BaseType.GetProperty("RecordStatus").GetValue(entry, null);
                            if (tsRecordStatusObject.ToString() == "Declined" || tsRecordStatusObject.ToString() == "DeclinedEditing" || tsRecordStatusObject.ToString() == "Approving")
                                toRecycleBin = true;
                        }
                        if (toRecycleBin)
                        {
                            entry.GetType().GetProperty("IsDeleted")?.SetValue(entry, true);
                            entry.GetType().GetProperty("DeletedDate")?.SetValue(entry, DateTime.Now);
                            entry.GetType().GetProperty("DeletedBy")?.SetValue(entry, userName);
                            entry.GetType().GetProperty("DeletedBySID")?.SetValue(entry, userSID);
                            RepositoryFactory.GetRepository<TRepository>().Update(entry);
                        }
                    }
                }
                finally
                {
                    RepositoryFactory.EnableDeletedFilter();
                }
            }
            return (toRecycleBin, relatedClassId);
        }
        /// <summary>
        /// Проверка на ссылки в выбранном классе
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        private (bool hasRecycleBin, string relatedClassId) HasRecycleBin(object obj)
        {
            var isDeleted = true;
            var relationClassId = string.Empty;
            foreach (var property in obj.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (!property.CanRead || !property.CanWrite)
                    continue; //нам нужны get;set; св-ва                
                if (property.DeclaringType.Name == "BaseModel" || property.Name == "ID" || property.Name == "Versions")
                    continue;//отсекаем св-ва базовой модели и id                
                if ((property.PropertyType == typeof(int?) || property.PropertyType == typeof(int)) && property.Name.EndsWith("ID"))
                {
                    var foundPropertyValue = obj.GetType().GetProperty(property.Name.Replace("ID", ""), BindingFlags.Public | BindingFlags.Instance).GetValue(obj, null);
                    if (foundPropertyValue != null && foundPropertyValue.GetType().BaseType.BaseType.Name == "BaseModel")
                    {
                        isDeleted = (bool)foundPropertyValue.GetType().GetProperty("IsDeleted")?.GetValue(foundPropertyValue, null);
                        if (!isDeleted)
                        {
                            relationClassId = foundPropertyValue.GetType().BaseType.Name + ".ID = " + foundPropertyValue.GetType().GetProperty("ID")?.GetValue(foundPropertyValue, null);
                            break;
                        }
                    }
                }
            }
            //Проверка на все классы связанные с этим объектом.
            return (isDeleted, relationClassId);
        }

        public virtual (bool toRecycleBin, string recycleClassId) RecycleToRecycleBinRange(IList<TEntry> entries, string userName, string userSID)
        {
            var toRecycleBin = true;
            var recycleRelatedClassId = string.Empty;
            foreach (var entry in entries)
            {
                var id = entry.GetType().GetProperty("ID")?.GetValue(entry, null);
                if (id != null)
                {
                    var recycleBinTuple = RecycleToRecycleBin((int)id, userName, userSID);
                    if (recycleBinTuple.toRecycleBin == false)
                    {
                        toRecycleBin = false;
                        recycleRelatedClassId = recycleBinTuple.relatedClassId;
                        break;
                    }
                }
            }
            return (toRecycleBin, recycleRelatedClassId);
        }

        public virtual TEntry Add(TEntry entry)
        {
            if (entry == null) throw new ArgumentNullException(nameof(entry));
            return RepositoryFactory.GetRepository<TRepository>().Add(entry);
        }

        public virtual void Delete(TId id)
        {
            RepositoryFactory.GetRepository<TRepository>().Delete(id);
        }

        public virtual TEntry GetById(TId id)
        {
            return RepositoryFactory.GetRepository<TRepository>().GetById(id);
        }

        public virtual TEntry GetById(TId id, params Expression<Func<TEntry, object>>[] includes)
        {
            return RepositoryFactory.GetRepository<TRepository>().GetByIdInclude(id, includes);
        }

        public virtual TEntry Update(TEntry entry)
        {
            if (entry == null) throw new ArgumentNullException(nameof(entry));
            return RepositoryFactory.GetRepository<TRepository>().Update(entry);
        }

        public virtual IList<TEntry> GetInternal(IQueryable<TEntry> repositoryQuery, Func<IQueryable<TEntry>, IList<TEntry>> delegateExpr, GetEntityMode getTypes)
        {
            List<TEntry> result = null;
            switch (getTypes)
            {
                case GetEntityMode.NotDeletedAndNotVersion:
                    result = delegateExpr(repositoryQuery).ToList();
                    break;
                case GetEntityMode.Deleted:
                    try
                    {
                        RepositoryFactory.DisableDeletedFilter();
                        repositoryQuery = repositoryQuery.Where("IsDeleted");
                        result = delegateExpr(repositoryQuery).ToList();
                    }
                    finally
                    {
                        RepositoryFactory.EnableDeletedFilter();
                    }
                    break;
                case GetEntityMode.DeletedAndOther:
                    try
                    {
                        RepositoryFactory.DisableDeletedFilter();
                        result = delegateExpr(repositoryQuery).ToList();
                    }
                    finally
                    {
                        RepositoryFactory.EnableDeletedFilter();
                    }
                    break;
                case GetEntityMode.Version:
                    try
                    {
                        RepositoryFactory.DisableVersionsFilter();
                        repositoryQuery = repositoryQuery.Where("IsVersion");
                        result = delegateExpr(repositoryQuery).ToList();
                    }
                    finally
                    {
                        RepositoryFactory.EnableVersionsFilter();
                    }
                    break;
                case GetEntityMode.VersionAndOther:
                    try
                    {  
                        RepositoryFactory.DisableVersionsFilter();
                        result = delegateExpr(repositoryQuery).ToList();
                    }
                    finally
                    {
                        RepositoryFactory.EnableVersionsFilter();
                    }
                    break;
                case GetEntityMode.VersionAndDeleted:
                    try
                    {
                        RepositoryFactory.DisableVersionsFilter();
                        RepositoryFactory.DisableDeletedFilter();
                        repositoryQuery = repositoryQuery.Where("IsVersion", "IsDeleted");
                        result = delegateExpr(repositoryQuery).ToList();
                    }
                    finally
                    {
                        RepositoryFactory.EnableVersionsFilter();
                        RepositoryFactory.EnableDeletedFilter();
                    }
                    break;
                case GetEntityMode.DeletedAndVersionAndOther:
                    try
                    {
                        RepositoryFactory.DisableVersionsFilter();
                        RepositoryFactory.DisableDeletedFilter();
                        result = delegateExpr(repositoryQuery.Where("IsVersion", "IsDeleted")).ToList();
                    }
                    finally
                    {
                        RepositoryFactory.EnableVersionsFilter();
                        RepositoryFactory.EnableDeletedFilter();
                        if (result != null)
                            result.AddRange(delegateExpr(repositoryQuery));
                        else
                            result = delegateExpr(repositoryQuery).ToList();
                    }
                    break;
            }
            return result;
        }

        public virtual IList<TEntry> Get(Func<IQueryable<TEntry>, IList<TEntry>> delegateExpr)
        {
            return Get(delegateExpr, GetEntityMode.NotDeletedAndNotVersion);
        }

        public virtual IList<TEntry> Get(Func<IQueryable<TEntry>, IList<TEntry>> delegateExpr, GetEntityMode getTypes)
        {
            if (delegateExpr == null) throw new ArgumentNullException(nameof(delegateExpr));
            var repositoryQuery = RepositoryFactory.GetRepository<TRepository>().GetQueryable();
            return GetInternal(repositoryQuery, delegateExpr, getTypes);
        }

        protected RepositoryAwareServiceBase(IRepositoryFactory repositoryFactory)
        {
            if (repositoryFactory == null)
                throw new ArgumentNullException(nameof(repositoryFactory));

            RepositoryFactory = repositoryFactory;
        }
    }
}
