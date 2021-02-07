using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace MainApp.BitrixSync
{
    public class BitrixCSI:BitrixListElement
    {
        public Dictionary<string, string> CostSubitemDescription;
        public Dictionary<string, string> CostItemTitle;
        public Dictionary<string, string> CostSubitemTitle;
        public Dictionary<string, string> ReviewerStage1User;
        public Dictionary<string, string> ReviewerStage2User;
        public Dictionary<string, string> ReviewerStage1Mode;
        public Dictionary<string, string> CostType;
        public Dictionary<string, string> TSFO;
        public Dictionary<string, string> SKIPR_ID;
    }
}