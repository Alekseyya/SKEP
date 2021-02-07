using System;
using System.Collections.Generic;
using System.Text;

namespace Core.Config
{
    public class JiraConfig
    {
        public string ApiUrl { get; set; }
        public string ApiIssue { get; set; }
        public string ApiIssueId { get; set; }
        public string Issue { get; set; }
        public string ApiUser { get; set; }
        public string ApiPassword { get; set; }
        public int TSHoursRecordDescriptionTruncateCharacters { get; set; }
        public string ApiProject { get; set; }
    }
}
