using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace Core.Extensions
{
    public static class EnumExtention
    {
        public static List<T> GetCollectionList<T>(this Type enumType, Expression<Func<T, bool>> conditionFunc) where T : struct
        {
            if (enumType.IsEnum)
            {
                return Enum.GetValues(enumType).Cast<T>().AsQueryable().Where(conditionFunc).ToList();
            }
            return null;
        }

        public static TAttribute GetAttributeOfType<TAttribute>(this Enum enumValue) where TAttribute : Attribute
        {
            return enumValue.GetType()
                .GetMember(enumValue.ToString())
                .First()
                .GetCustomAttribute<TAttribute>();
        }

        public static List<T> GetCollectionList<T>(this Enum enumType, Expression<Func<T, bool>> conditionFunc) where T : Enum
        {
            return Enum.GetValues(enumType.GetType()).Cast<T>().AsQueryable().Where(conditionFunc).ToList();
        }

        public static List<T> GetCollectionList<T>(this Enum enumType) where T : Enum
        {
            return Enum.GetValues(enumType.GetType()).Cast<T>().ToList();
        }

        #region String to Enum
        public static T ParseEnum<T>(string inString, bool ignoreCase = true, bool throwException = true) where T : struct
        {
            return (T)ParseEnum<T>(inString, default(T), ignoreCase, throwException);
        }

        public static T ParseEnum<T>(string inString, T defaultValue,
                               bool ignoreCase = true, bool throwException = false) where T : struct
        {
            T returnEnum = defaultValue;

            if (!typeof(T).IsEnum || String.IsNullOrEmpty(inString))
            {
                throw new InvalidOperationException("Invalid Enum Type or Input String 'inString'. " + typeof(T).ToString() + "  must be an Enum");
            }

            try
            {
                bool success = Enum.TryParse<T>(inString, ignoreCase, out returnEnum);
                if (!success && throwException)
                {
                    throw new InvalidOperationException("Invalid Cast");
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Invalid Cast", ex);
            }

            return returnEnum;
        }
        #endregion

        #region Int to Enum
        public static T ParseEnum<T>(int input, bool throwException = true) where T : struct
        {
            return (T)ParseEnum<T>(input, default(T), throwException);
        }
        public static T ParseEnum<T>(int input, T defaultValue, bool throwException = false) where T : struct
        {
            T returnEnum = defaultValue;
            if (!typeof(T).IsEnum)
            {
                throw new InvalidOperationException("Invalid Enum Type. " + typeof(T).ToString() + "  must be an Enum");
            }
            if (Enum.IsDefined(typeof(T), input))
            {
                returnEnum = (T)Enum.ToObject(typeof(T), input);
            }
            else
            {
                if (throwException)
                {
                    throw new InvalidOperationException("Invalid Cast");
                }
            }

            return returnEnum;

        }
        #endregion

        #region String Extension Methods for Enum Parsing
        public static T ToEnum<T>(this string inString, bool ignoreCase = true, bool throwException = true) where T : struct
        {
            return (T)ParseEnum<T>(inString, ignoreCase, throwException);
        }
        public static T ToEnum<T>(this string inString, T defaultValue, bool ignoreCase = true, bool throwException = false) where T : struct
        {
            return (T)ParseEnum<T>(inString, defaultValue, ignoreCase, throwException);
        }
        #endregion

        #region Int Extension Methods for Enum Parsing
        public static T ToEnum<T>(this int input, bool throwException = true) where T : struct
        {
            return (T)ParseEnum<T>(input, default(T), throwException);
        }

        public static T ToEnum<T>(this int input, T defaultValue, bool throwException = false) where T : struct
        {
            return (T)ParseEnum<T>(input, defaultValue, throwException);
        }
        #endregion
    }
}
