using Newtonsoft.Json;

namespace Core.JIRA
{
    public class Issue
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("key")]
        public string Key { get; set; }

        [JsonProperty("fields")]
        public Fields Fields { get; set; }
    }
}
