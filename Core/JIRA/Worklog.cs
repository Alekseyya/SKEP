using System;
using Newtonsoft.Json;

namespace Core.JIRA
{
    public class Worklog
    {
        [JsonProperty("author")]
        public Author Author { get; set; }

        [JsonProperty("started")]
        public DateTime Started { get; set; }

        [JsonProperty("timeSpentSeconds")]
        public int TimeSpentSeconds { get; set; }

        [JsonProperty("comment")]
        public string Comment { get; set; }

        [JsonProperty("id")]
        public int Id { get; set; }
    }
}
