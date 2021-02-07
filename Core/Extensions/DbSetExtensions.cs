using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using Microsoft.EntityFrameworkCore;

namespace Core.Extensions
{
    public static class DbSetExtensions
    {
        //todo Этот момент надо протестить!!
        public static List<T> Where<T>(this DbSet<T> dbSet, T entry, int id) where T : class
        {
            var xParameter = Expression.Parameter(typeof(T), "x");
            var xProperty = Expression.Property(xParameter, "ID");
            var lastMemeber = xProperty;
            var valueExpression = Expression.Constant(id, typeof(int));
            var equelityExpression = Expression.Equal(xProperty, valueExpression);
            //var valueCast = Expression.Convert(xParameter, entry);
            ///var lambda = Expression.Lambda<Func<T, bool>>(equelityExpression, xParameter);
            var lambda = Expression.Lambda<Func<T, bool>>(equelityExpression, xParameter);
            return dbSet.Cast<T>().Where(lambda).ToList();
        }

        public static MethodCallExpression ConvertToType(
            ParameterExpression sourceParameter,
            PropertyInfo sourceProperty,
            TypeCode typeCode)
        {
            var sourceExpressionProperty = Expression.Property(sourceParameter, sourceProperty);
            var changeTypeMethod = typeof(Convert).GetMethod("ChangeType", new Type[] { typeof(object), typeof(TypeCode) });
            var callExpressionReturningObject = Expression.Call(changeTypeMethod, sourceExpressionProperty, Expression.Constant(typeCode));
            return callExpressionReturningObject;
        }

        public static IQueryable<T> Where<T>(this List<T> entry, int id)
        {
            var xParameter = Expression.Parameter(entry.First().GetType(), "x");
            var xProperty = Expression.Property(xParameter, "ID");
            var lastMemeber = xProperty;
            var valueExpression = Expression.Constant(id, typeof(int));
            var equelityExpression = Expression.Equal(xProperty, valueExpression);
            var lambda = Expression.Lambda<Func<T, bool>>(equelityExpression, xParameter);
            return entry.AsQueryable().Where(lambda);
        }
        public static T ConvertValue<T>(string value)
        {
            return (T)Convert.ChangeType(value, typeof(T));
        }
    }
}
