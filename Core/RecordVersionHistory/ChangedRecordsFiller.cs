using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using Core.Extensions;
using Core.Models;


namespace Core.RecordVersionHistory
{
    public class ChangeInfoRecord
    {
        public int ID { get; set; }
        public string Name { get; set; }
        public string Value { get; set; }
    }

    public class ChangedRecordsFiller
    {
        public static IEnumerable<ChangeInfoRecord> GetChangedData(BaseModel currObj, BaseModel prevObj)
        {
            var result = new List<ChangeInfoRecord>();

            Type objType = currObj.GetType();
            Type prevObjType = currObj.GetType();
            if (objType != prevObjType)
                return result;

            var properties = objType.GetProperties(BindingFlags.Public | BindingFlags.Instance);

            foreach (var property in properties)
            {
                string name = property.Name;
                var pInfo = property.GetCustomAttributes(typeof(DisplayAttribute), false)
                                    .Cast<DisplayAttribute>().FirstOrDefault();

                if (pInfo != null)
                    name = pInfo.Name;

                if (!property.CanRead || !property.CanWrite)
                    continue; //нам нужны get;set; св-ва

                if (property.DeclaringType == typeof(BaseModel) || property.Name == "ID" || property.Name == "Versions")
                    continue;//отсекаем св-ва базовой модели и id

                if (property.PropertyType == typeof(int?) && property.Name.EndsWith("ID"))
                {
                    // это id других сущностей
                    try
                    {
                        if (property.GetValue(currObj) == null)
                            continue;

                        var virtualProp = objType.GetProperty(property.Name.Replace("ID", ""));
                        if (virtualProp == null)
                            continue;

                        var virtualCurrValue = virtualProp.GetValue(currObj);
                        var virtualPrevValue = virtualProp.GetValue(prevObj);
                        var virtualType = virtualCurrValue.GetType();
                        var virtualTypeProps = virtualType.GetProperties(BindingFlags.Public | BindingFlags.Instance).ToList();
                        PropertyInfo vProperty = virtualTypeProps.Where(x => x.Name == "FullName").FirstOrDefault();
                        if (vProperty == null)
                            vProperty = virtualTypeProps.Where(x => x.Name == "Title").FirstOrDefault();
                        if (vProperty == null)
                            vProperty = virtualTypeProps.Where(x => x.Name == "ShortName").FirstOrDefault();


                        if (vProperty != null
                            && virtualCurrValue != null
                            && virtualCurrValue != virtualPrevValue
                            && vProperty.GetValue(virtualCurrValue).ToString() != null)
                        {
                            var record = new ChangeInfoRecord { Name = name, Value = vProperty.GetValue(virtualCurrValue).ToString() };
                            result.Add(record);
                            continue;
                        }

                    }
                    catch (Exception ex)
                    {
                        continue;
                    }
                }

                if (property.GetGetMethod().IsVirtual)
                    continue;//виртуальные св-ва, их обрабатываем в int? <nameID>

                if (property.CustomAttributes.Count() == 0)
                    continue;

                if (property.PropertyType == typeof(bool))
                {
                    if (property.GetValue(currObj) != null
                        && property.GetValue(prevObj) != null
                        && (bool)property.GetValue(currObj) != (bool)property.GetValue(prevObj))
                    {
                        if ((bool)property.GetValue(currObj) == true)
                        {
                            var record = new ChangeInfoRecord { Name = name, Value = "Да" };
                            result.Add(record);
                        }
                        else
                        {
                            var record = new ChangeInfoRecord { Name = name, Value = "Нет" };
                            result.Add(record);
                        }

                        continue;
                    }
                }

                string currValue = property.GetValue(currObj) != null ? property.GetValue(currObj).ToString() : null;
                string prevValue = property.GetValue(prevObj) != null ? property.GetValue(prevObj).ToString() : null;

                if (property.PropertyType.IsEnum == true)
                {
                    if (property.GetValue(currObj) != null
                        && property.GetValue(prevObj) != null
                        && property.GetValue(currObj).Equals(property.GetValue(prevObj)) == false)
                    {
                        var record = new ChangeInfoRecord { Name = name, Value = ((Enum)property.GetValue(currObj)).GetAttributeOfType<DisplayAttribute>().Name };
                        result.Add(record);
                        continue;
                    }
                }
                if (currValue != null && currValue != prevValue)
                {
                    var record = new ChangeInfoRecord { Name = name, Value = currValue.ToString() };
                    result.Add(record);
                    continue;
                }
            }
            return result;
        }
    }
}
