using Newtonsoft.Json;

namespace Core.JIRA
{
    public class JiraProject
    {
        [JsonProperty("name")]
        public string ProjectName { get; set; }

        [JsonProperty("key")]
        public string ProjectKey { get; set; }
    }
}
