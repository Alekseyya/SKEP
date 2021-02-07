using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using Core.BL.Interfaces;
using Core.Extensions;


namespace BL.Implementation
{
   public class ReflectionService : IReflectionService
    {
        public List<(string field, object value)> GetFieldValuesFromObjectThroughProperties<T>(T entry)
        {
            var properties = entry.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public)
                    .Where(p => typeof(IEnumerable).IsAssignableFrom(typeof(string)))
                    .Where(p => p.PropertyType == typeof(DateTime) ||
                                p.PropertyType == typeof(DateTime?) ||
                                p.PropertyType == typeof(string) ||
                                p.PropertyType == typeof(int) ||
                                p.PropertyType == typeof(int?) ||
                                p.PropertyType == typeof(double) ||
                                p.PropertyType == typeof(double?) ||
                                p.PropertyType == typeof(decimal) ||
                                p.PropertyType == typeof(decimal?) ||
                                p.PropertyType.IsEnum ||
                                p.PropertyType == typeof(bool) || p.PropertyType == typeof(bool?))
                    .Where(p => p.CanRead && p.CanWrite)
                    .Where(p => !p.Name.Equals("ItemID") && !p.Name.Equals("IsVersion") && !p.Name.Equals("VersionNumber")
                                && !p.Name.Equals("AuthorSID") && !p.Name.Equals("EditorSID") && !p.Name.Equals("DisplayEditor") && !p.Name.Equals("FullName")
                                && !p.Name.Equals("IsDeleted") && !p.Name.Equals("DeletedDate") && !p.Name.Equals("DeletedBy") && !p.Name.Equals("DeletedBySID"));

            var listTuples = new List<(string field, object value)>();

            foreach (var property in properties)
            {
                if (property.GetValue(entry) != null
                    && (property.PropertyType == typeof(int?) || property.PropertyType == typeof(int))
                    && property.Name.EndsWith("ID") && property.Name.Equals("ID") == false)
                {
                    var virtualProperty = entry.GetType().GetProperty(property.Name.Replace("ID", ""));
                    if (virtualProperty == null)
                        continue;

                    var lookupValue = virtualProperty.GetValue(entry);

                    var virtualPropertyProps = virtualProperty.PropertyType.GetProperties(BindingFlags.Instance | BindingFlags.Public).ToList();
                    PropertyInfo vProperty = virtualPropertyProps.FirstOrDefault(x => x.Name == "FullName");
                    if (vProperty == null)
                        vProperty = virtualPropertyProps.FirstOrDefault(x => x.Name == "Title");
                    if (vProperty == null)
                        vProperty = virtualPropertyProps.FirstOrDefault(x => x.Name == "ShortName");
                    listTuples.Add((field: property.Name, value: lookupValue != null ? vProperty.GetValue(lookupValue) : null));
                }
                else if (property.GetValue(entry) != null && property.PropertyType.IsEnum)
                {
                    var enumValue = (Enum)property.GetValue(entry);
                    listTuples.Add((field: property.Name, value: enumValue != null ? enumValue.GetAttributeOfType<DisplayAttribute>().Name : null));
                }
                else if (property.GetValue(entry) != null && (property.PropertyType == typeof(bool) || property.PropertyType == typeof(bool?)))
                {
                    listTuples.Add((field: property.Name, value: (bool)property.GetValue(entry) == false ? "Нет" : "Да"));
                }
                else
                {
                    if (property.GetValue(entry) == null)
                        continue;

                    listTuples.Add((field: property.Name, value: property.GetValue(entry)));
                }
            }
            return listTuples;
        }
    }
}
