
using System;

namespace Core.Config
{
   public class TimesheetConfig
    {

        public bool ProcessingSyncWithExternalTimesheets { get; set; }
        public bool ProcessingProcessVacationRecords { get; set; }
        public string ProcessingReportEmailReceivers { get; set; }
        public bool ProcessingProcessTSAutoHoursRecords { get; set; }
        public bool ProcessingSendTSEmailNotifications { get; set; }
        public bool AllowSendTSEmailNotifications { get; set; }
        public string EmailNotificationsSubjectPrefix { get; set; }
        public string ProcessingProcessVacationRecordsStartDate { get; set; }

        public string OraDBHost { get; set; }
        public string OracleDBPort { get; set; }
        public string OracleDBSN { get; set; }
        public string OracleDBUserID { get; set; }
        public string OracleDBPassword { get; set; }
        public bool ProcessingSyncWithJIRASendEmailNotifications { get; set; }
        public IntervalType EnumIntervalType
        {
            set
            {
                switch (ProcessingIntervalType)
                {
                    case "seconds":
                        value = IntervalType.Seconds;
                        break;
                    case "minutes":
                        value = IntervalType.Minutes;
                        break;
                    case "hours":
                        value = IntervalType.Hours;
                        break;
                    case "daily":
                        break;
                    default:
                        throw new ArgumentException("Проверьте корректность введенного типа");

                }
            }
        }

        public string ProcessingIntervalType { get; set; }

        public string ProcessingIntervalValue { get; set; }
        public bool ProcessingIntervalEnabled { get; set; }

        public string SendTSEmailNotificationsOnlyTo { get; set; }
        public bool DontSendTSEmailNotificationsTo { get; set; }
        public bool ProcessingSyncWithJIRA { get; set; }
        

        public int Hours
        {
            get
            {
                return int.TryParse(ProcessingIntervalValue.Split('.')[0], out int result) && result <= 23
                    ? result
                    : throw new ArgumentException("Неправильно указаны часы в файле config");
            }
        }

        public int Minutes
        {
            get
            {
                return int.TryParse(ProcessingIntervalValue.Split('.')[1], out int result) && (result >= 1 && result <= 59)
                    ? result
                    : throw new ArgumentException("Неправильно указаны минуты в фале config");
            }
        }
    }
}
