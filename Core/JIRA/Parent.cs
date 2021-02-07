using Newtonsoft.Json;

namespace Core.JIRA
{
    public class Parent
    {
        [JsonProperty("key")]
        public string ParentProject { get; set; }
    }
}
