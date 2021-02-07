using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Data;
using Xunit;
using Xunit.Abstractions;

namespace RMX.RPCS.UnitTests.BL
{
    public class ServiceServiceTests
    {
        private readonly ITestOutputHelper output;
        
        private class MyDictionary
        {
            public string Key { get; set; }
            public string Value { get; set; }
            public string Property { get; set; }
        }

        [Fact]
        public void Test_ServiceService_SwitchTest()
        {
            TestSwithObject("MyDictionary1");
        }

        private void TestSwithObject(string obj)
        {
            switch (obj)
            {
                case { } _ when obj == nameof(MyDictionary) && obj.Length > 1:
                    output.WriteLine("O__________O");
                    break;
                default:
                    output.WriteLine("Error");
                    break;
            }
        }

        [Fact]
        public void Test_ServiceService_HasRecycleBinInDBRelation_GetListRelationsClassesRepeated()
        {
            var dictionary = new List<MyDictionary>();
            foreach (var entry in typeof(RPCSContext).GetProperties().Where(x =>
                x.Name != "Database" && x.Name != "ChangeTracker" && x.Name != "Configuration"))
            {
                foreach (var propertyContext in typeof(RPCSContext).GetProperties().Where(x => x.Name != "Database"
                                                                                               && x.Name != "ChangeTracker" && x.Name != "Configuration" && x.Name != entry.Name))
                {
                    foreach (var property in propertyContext.PropertyType.GenericTypeArguments.First().GetProperties(BindingFlags.Public | BindingFlags.Instance))
                    {
                        if (property.PropertyType == entry.PropertyType.GenericTypeArguments.First())
                        {
                            var type = propertyContext.PropertyType.GenericTypeArguments.First();
                            dictionary.Add(new MyDictionary() { Key = entry.Name, Value = type.Name, Property = property.Name + "ID" });
                        }
                    }
                }
            }

            output.WriteLine("Нужно для того, чтобы составить правильные свичи!");
            foreach (var item in dictionary.GroupBy(x => x.Value).Select(g => g.First()).OrderBy(x => x.Value))
            {
                output.WriteLine("!!!" + "      " + item.Value);
                output.WriteLine("asd");
                foreach (var itemKey in dictionary.Where(x => x.Value == item.Value).OrderBy(x => x.Value))
                {
                    output.WriteLine(itemKey.Key.Remove(itemKey.Key.Length - 1) + " - " + itemKey.Property);
                }
                output.WriteLine("__________");
            }
        }


        [Fact]
        public void Test_ServiceService_HasRecycleBinInDBRelation_GetListRelationsClasses()
        {
            foreach (var entry in typeof(RPCSContext).GetProperties().Where(x =>
                x.Name != "Database" && x.Name != "ChangeTracker" && x.Name != "Configuration"))
            {
                output.WriteLine(entry.Name);
                output.WriteLine("asd");
                output.WriteLine("В этих классах есть " + entry.Name);

                var counter = 0;
                foreach (var propertyContext in typeof(RPCSContext).GetProperties().Where(x => x.Name != "Database"
                                                                                               && x.Name != "ChangeTracker" && x.Name != "Configuration" && x.Name != entry.Name))
                {
                    foreach (var property in propertyContext.PropertyType.GenericTypeArguments.First().GetProperties(BindingFlags.Public | BindingFlags.Instance))
                    {   //Если в классе найден выбранные типа ProjectID - Project
                        //Projects - c s на конце!!
                        //TODO Перенести в метод!!
                        if (property.PropertyType == entry.PropertyType.GenericTypeArguments.First())
                        {
                            var type = propertyContext.PropertyType.GenericTypeArguments.First();
                            output.WriteLine(++counter + "." + type.Name + " " + property.Name + "ID");
                        }
                    }
                }
                output.WriteLine("___________________________________________");
            }
        }

        private void PrintRelationsOfEntry(string entry)
        {
            var counter = 0;
            foreach (var propertyContext in typeof(RPCSContext).GetProperties().Where(x => x.Name != "Database"
                                                                                           && x.Name != "ChangeTracker" && x.Name != "Configuration" && x.Name != entry))
            {
                foreach (var property in propertyContext.PropertyType.GenericTypeArguments.First().GetProperties(BindingFlags.Public | BindingFlags.Instance))
                {   //Если в классе найден выбранные типа ProjectID - Project
                    //Projects - c s на конце!!
                    if ((property.PropertyType == typeof(int?) || property.PropertyType == typeof(int)) && property.Name.Contains(entry.Remove(entry.Length - 1) + "ID"))
                    {
                        var type = propertyContext.PropertyType.GenericTypeArguments.First();
                        output.WriteLine(++counter + "." + type.Name);
                    }
                }
            }
        }
    }
}
