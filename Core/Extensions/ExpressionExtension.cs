using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.AspNetCore.Mvc;

namespace Core.Extensions
{
    public static class ExpressionExtension
    {
        public static Expression<TDelegate> AndAlso<TDelegate>(this Expression<TDelegate> left, Expression<TDelegate> right)
        {
            return Expression.Lambda<TDelegate>(Expression.AndAlso(left.Body, right.Body), left.Parameters[0]);
        }
        public static Expression<Func<T, TResult>> FuncToExpression<T, TResult>(Func<T, TResult> method)
        {
            return x => method(x);
        }
        public static string GetPropertyName<T>(Expression<Func<T>> expression)
        {
            MemberExpression propertyExpression = (MemberExpression)expression.Body;
            MemberInfo propertyMember = propertyExpression.Member;

            Object[] displayAttributes = propertyMember.GetCustomAttributes(typeof(DisplayAttribute), true);
            if (displayAttributes != null && displayAttributes.Length == 1)
                return ((DisplayAttribute)displayAttributes[0]).Name;

            return propertyMember.Name;
        }

        public static string GetDisplayName(this PropertyInfo prop)
        {
            return (prop.GetCustomAttribute(typeof(DisplayAttribute)) as DisplayAttribute)?.Name;
        }

        public static string GetPropertyName<T, P>(Expression<Func<T, P>> expression)
        {
            MemberExpression propertyExpression = (MemberExpression)expression.Body;
            MemberInfo propertyMember = propertyExpression.Member;

            Object[] displayAttributes = propertyMember.GetCustomAttributes(typeof(DisplayAttribute), true);
            if (displayAttributes != null && displayAttributes.Length == 1)
                return ((DisplayAttribute)displayAttributes[0]).Name;

            return propertyMember.Name;
        }


        public static string GetBaseNameInLink(this PropertyInfo propertyInfo, object entity)
        {
            var virtualProp = entity.GetType().GetProperty(propertyInfo.Name.Replace("ID", ""));
            if (virtualProp == null)
                return string.Empty;
            var virtualCurrentValue = virtualProp.GetValue(entity);
            if (virtualCurrentValue != null)
            {
                var virtualType = virtualCurrentValue.GetType();
                var virtualTypeProps = virtualType.GetProperties(BindingFlags.Public | BindingFlags.Instance).ToList();
                PropertyInfo vProperty = virtualTypeProps.FirstOrDefault(x => x.Name == "FullName");
                if (vProperty == null)
                    vProperty = virtualTypeProps.FirstOrDefault(x => x.Name == "Title");
                if (vProperty == null)
                    vProperty = virtualTypeProps.FirstOrDefault(x => x.Name == "ShortName");
                if (vProperty != null)
                    return vProperty.GetValue(virtualCurrentValue).ToString();
            }
            return string.Empty;
        }

        public static string GetDisplayName<TModel, TProperty>(this TModel model, Expression<Func<TModel, TProperty>> expression)
        {

            Type type = typeof(TModel);

            MemberExpression memberExpression = (MemberExpression)expression.Body;
            string propertyName = ((memberExpression.Member is PropertyInfo) ? memberExpression.Member.Name : null);

            // First look into attributes on a type and it's parents
            DisplayAttribute attr;
            attr = (DisplayAttribute)type.GetProperty(propertyName).GetCustomAttributes(typeof(DisplayAttribute), true).SingleOrDefault();

            // Look for [MetadataType] attribute in type hierarchy
            // http://stackoverflow.com/questions/1910532/attribute-isdefined-doesnt-see-attributes-applied-with-metadatatype-class
            if (attr == null)
            {
                ModelMetadataTypeAttribute metadataType = (ModelMetadataTypeAttribute)type.GetCustomAttributes(typeof(ModelMetadataTypeAttribute), true).FirstOrDefault();
                if (metadataType != null)
                {
                    var property = metadataType.MetadataType.GetProperty(propertyName);
                    if (property != null)
                    {
                        attr = (DisplayAttribute)property.GetCustomAttributes(typeof(DisplayNameAttribute), true).SingleOrDefault();
                    }
                }
            }
            return (attr != null) ? attr.Name : String.Empty;


        }
    }
}