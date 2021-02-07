using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Core.JIRA
{
    public class CustomConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(Fields);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            JObject jObject = JObject.Load(reader);
            var fields = new Fields();
            try
            {
                foreach (var property in jObject.Properties())
                {
                    foreach (var fieldProperty in fields.GetType().GetProperties())
                    {
                        if (fieldProperty.GetCustomAttributes(true).Cast<JsonPropertyAttribute>().FirstOrDefault()?.PropertyName == property.Name)
                        {
                            fieldProperty.SetValue(fields, Convert.ChangeType(property.Value, fieldProperty.PropertyType), null);
                        }
                    }
                }
            }
            catch (Exception e)
            {

            }
            var worklogs = jObject.SelectToken("worklog.worklogs").ToObject<List<Worklog>>();
            fields.WorkLogs = worklogs;
            fields.JiraProject = jObject.SelectToken("project").ToObject<JiraProject>();
            if (jObject.SelectToken("parent") != null)
                fields.Parent = jObject.SelectToken("parent").ToObject<Parent>();
            if (jObject.SelectToken("customfield_12700") != null && jObject.SelectToken("customfield_12700.value") != null)
                fields.JiraSupportPRCCustomField = jObject.SelectToken("customfield_12700.value").ToString();
            if (jObject.SelectToken("customfield_11200") != null)
                fields.JiraEpicLinkCustomField = jObject.SelectToken("customfield_11200").ToString();
            return fields;
        }

        public override bool CanWrite
        {
            get { return false; }
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
}
