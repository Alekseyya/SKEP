using System;
using System.Collections.Generic;
using System.Text;

namespace Core.Models.Timesheet
{
    public class PERMANENT_PROJECTSRecord
    {
        public String PERMANENT_PROJECT_ID = "";
        public String PROJECT_ID = "";
        public String USER_ID = "";
        public int HOURS = 0;
        public String FNAME = "";
        public String PATRONYMIC = "";
        public String LNAME = "";
        public String TERMINATED = "";
        public String HIRED = "";
        public String PROJECT_SHORT_NAME = "";
        public String PROJECT_NAME = "";
        public String PROJECT_DATE_END = "";
        public int rowNumber = -1;
        public DateTime? tsAutoHoursRecord_BeginDate;
        public DateTime? tsAutoHoursRecord_EndDate;
        public double? tsAutoHoursRecord_DayHours;
        //public XSSFRow row = null;
    }
}
