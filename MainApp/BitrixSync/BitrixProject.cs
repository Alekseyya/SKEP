using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web;

namespace MainApp.BitrixSync
{
    public class BitrixProject : BitrixListElement
    {
        public Dictionary<string, string> ProjectTitle;
        public Dictionary<string, string> SKIPR_ID;
        public Dictionary<string, string> ProjectPM;
        public Dictionary<string, string> ProjectCAM;
        public Dictionary<string, string> FinResponsibilityCenter;
        public Dictionary<string, string> ProjectStatus;
        public Dictionary<string, string> ProjectType;
    }
}