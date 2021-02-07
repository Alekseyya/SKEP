using System;
using System.Reflection;

namespace Core.Config
{
    public class SMTPConfig
    {
        public string Server { get; set; }
        public string Login { get; set; }
        public string Password { get; set; }
        public string FromEmail { get; set; }
        public string LinkMyHours { get; set; }
        public string LinkApproveHours { get; set; }
        public string LinkDeclinedHours { get; set; }
        public string TimesheetSendTSEmailNotificationsOnlyTo { get; set; }
        public string TimesheetDontSendTSEmailNotificationsTo { get; set; }

        public object this[string propertyName]
        {
            get
            {
                Type myType = typeof(SMTPConfig);
                PropertyInfo myPropInfo = myType.GetProperty(propertyName);
                return myPropInfo.GetValue(this, null);
            }
            set
            {
                Type myType = typeof(SMTPConfig);
                PropertyInfo myPropInfo = myType.GetProperty(propertyName);
                myPropInfo.SetValue(this, value, null);

            }

        }
    }
}
