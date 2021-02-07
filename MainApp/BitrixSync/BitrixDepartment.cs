using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MainApp.BitrixSync
{
    public class BitrixDepartment : BitrixListElement
    {
        public Dictionary<string, string> SHORT_NAME;
        public Dictionary<string, string> SHORT_TITLE;
        public Dictionary<string, string> PARENT_DEPARTMENT;
        public Dictionary<string, string> DEPARTMENT_MANAGER;
        public Dictionary<string, string> IS_FINANCIAL_CENTRE;
        public Dictionary<string, string> IS_AUTONOMOUS;
        public Dictionary<string, string> SKIPR_ID;
    }
}
