using Newtonsoft.Json;

namespace Core.JIRA
{
    public class Author
    {
        [JsonProperty("name")]
        public string Name { get; set; }
    }
}
