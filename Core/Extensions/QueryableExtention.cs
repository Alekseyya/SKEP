using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.Internal;
using Microsoft.EntityFrameworkCore.Storage;

namespace Core.Extensions
{
    public static class QueryableExtention
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


        private static readonly TypeInfo QueryCompilerTypeInfo = typeof(QueryCompiler).GetTypeInfo();

        private static readonly FieldInfo QueryCompilerField = typeof(EntityQueryProvider).GetTypeInfo().DeclaredFields.First(x => x.Name == "_queryCompiler");

        private static readonly FieldInfo QueryModelGeneratorField = QueryCompilerTypeInfo.DeclaredFields.First(x => x.Name == "_queryModelGenerator");

        private static readonly FieldInfo DataBaseField = QueryCompilerTypeInfo.DeclaredFields.Single(x => x.Name == "_database");

        private static readonly PropertyInfo DatabaseDependenciesField = typeof(Database).GetTypeInfo().DeclaredProperties.Single(x => x.Name == "Dependencies");

        public static string ToSql<TEntity>(this IQueryable<TEntity> query) where TEntity : class
        {
            var queryCompiler = (QueryCompiler)QueryCompilerField.GetValue(query.Provider);
            var modelGenerator = (QueryModelGenerator)QueryModelGeneratorField.GetValue(queryCompiler);
            var queryModel = modelGenerator.ParseQuery(query.Expression);
            var database = (IDatabase)DataBaseField.GetValue(queryCompiler);
            var databaseDependencies = (DatabaseDependencies)DatabaseDependenciesField.GetValue(database);
            var queryCompilationContext = databaseDependencies.QueryCompilationContextFactory.Create(false);
            var modelVisitor = (RelationalQueryModelVisitor)queryCompilationContext.CreateQueryModelVisitor();
            modelVisitor.CreateQueryExecutor<TEntity>(queryModel);
            var sql = modelVisitor.Queries.First().ToString();

            return sql;
        }

        public static IQueryable<TEntity> Where<TEntity>(this IQueryable<TEntity> source, string parameterName)
        {
            var parameter = Expression.Parameter(typeof(TEntity), "x");
            Expression rightExp = Expression.Property(parameter, parameterName);
            var lambda = Expression.Lambda<Func<TEntity, bool>>(rightExp, parameter);
            return source.Where(lambda);
        }
        public static IQueryable<TEntity> Where<TEntity>(this IQueryable<TEntity> source, string parameterName, string parameterName2)
        {
            var parameter = Expression.Parameter(typeof(TEntity), "x");
            Expression right = Expression.Property(parameter, parameterName);
            Expression left = Expression.Property(parameter, parameterName2);
            Expression body = Expression.And(left, right);
            var lambda = Expression.Lambda<Func<TEntity, bool>>(body, parameter);
            return source.Where(lambda);
        }

        public static IQueryable<TEntity> Where<TEntity>(this IQueryable<TEntity> source, string isDeletedParameter, string idParamater, int Id)
        {
            var parameter = Expression.Parameter(typeof(TEntity), "x");
            Expression leftExpr = Expression.Property(parameter, isDeletedParameter);
            Expression rightProperty = Expression.Property(parameter, idParamater);
            Expression rightExpr = Expression.Equal(rightProperty, Expression.Constant(Id));
            Expression expressionBody = Expression.And(leftExpr, rightExpr);
            var lambda = Expression.Lambda<Func<TEntity, bool>>(expressionBody, parameter);
            return source.Where(lambda);
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
