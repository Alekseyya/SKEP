using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Linq.Expressions;

namespace Core.Extensions
{
    public static class DbContextExtention
    {
        public static TEntity FirstOfDefaultIdEquals<TEntity, TKey>(
            this IQueryable<TEntity> source, TKey otherKeyValue)
            where TEntity : class
        {
            var parameter = Expression.Parameter(typeof(TEntity), "x");
            var property = Expression.Property(parameter, "ID");
            var equal = Expression.Equal(property, Expression.Constant(otherKeyValue));
            var lambda = Expression.Lambda<Func<TEntity, bool>>(equal, parameter);
            return source.FirstOrDefault(lambda);
        }
        public static TEntity FirstOfDefaultIdEquals<TEntity>(
            this ObservableCollection<TEntity> source, TEntity enity)
            where TEntity : class
        {
            var value = (int)enity.GetType().GetProperty("ID").GetValue(enity, null);
            var parameter = Expression.Parameter(typeof(TEntity), "x");
            var property = Expression.Property(parameter, "ID");
            var equal = Expression.Equal(property, Expression.Constant(value));
            var lambda = Expression.Lambda<Func<TEntity, bool>>(equal, parameter);
            var queryableList = new List<TEntity>(source).AsQueryable();
            return queryableList.FirstOrDefault(lambda);
        }
    }
}
