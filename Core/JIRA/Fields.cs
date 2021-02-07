using System.Collections.Generic;
using Newtonsoft.Json;

namespace Core.JIRA
{
    [JsonConverter(typeof(CustomConverter))]
    public class Fields
    {
        [JsonProperty("customfield_12013")]
        public string JiraPRCCustomField { get; set; } //PRC
        public Parent Parent { get; set; }
        public JiraProject JiraProject { get; set; }

        [JsonProperty("customfield_11200")]
        public string JiraEpicLinkCustomField { get; set; } //Epic Link

        [JsonProperty("customfield_11700")]
        public string JiraEpicNameCustomField { get; set; } //Epic Name

        [JsonProperty("customfield_12700")]
        public string JiraSupportPRCCustomField { get; set; } //Код проекта ТП

        public List<Worklog> WorkLogs { get; set; }
    }
}
