using System.Collections.Generic;
using Newtonsoft.Json;

namespace Core.JIRA
{
    public class JiraBaseModel
    {
        [JsonProperty("issues")]
        public List<Issue> Issues { get; set; }
    }
}
