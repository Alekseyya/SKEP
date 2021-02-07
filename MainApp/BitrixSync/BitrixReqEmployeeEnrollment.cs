using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MainApp.BitrixSync
{
    public class BitrixReqEmployeeEnrollment : BitrixListElement
    {
        public Dictionary<string, string> FULL_NAME;
        public Dictionary<string, string> BIRTH_DATE;
        public Dictionary<string, string> PHONE_NUMBER;
        public Dictionary<string, string> START_DATE_WORK;

        public Dictionary<string, string> DEPARTMENT;

        public Dictionary<string, string> CATEGORY_EMPLOYEE;
        public Dictionary<string, string> POSITION_EMPLOYEE;
        public Dictionary<string, string> EMPLOYEE_QUALIFYING_ROLE;
        public Dictionary<string, string> TERRITORIAL_LOCATION;
        public Dictionary<string, string> POSITION_ACCORDING_OFFICIAL_STAFF_SCHEDULE;
        public Dictionary<string, string> OFFICIAL_REGISTRATION_IN_COMPANY;

        public Dictionary<string, string> TRIAL_PERIOD;

        public Dictionary<string, string> PAYROLL_IS_ENTERED;
        public Dictionary<string, string> PAYROLL_IS_APPROVED;

    }
}
