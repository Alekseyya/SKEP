using System;
using System.Reflection;

namespace Core.Config
{
    public class BitrixConfig
    {
        public BitrixConfig()
        {
        }
        public string SyncPortalUrl { get; set; }
        public string SyncWebHookUrl { get; set; }
        public string SyncListURLs { get; set; }
        public string SyncIntervalType { get; set; }
        public int SyncDepartmentListID { get; set; }
        public bool SyncDepartmentsEnabled { get; set; }
        public int SyncOrganisationListID { get; set; }
        public IntervalType EnumIntervalType
        {
            set
            {
                switch (SyncIntervalType)
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
        public string SyncIntervalValue { get; set; }
        public bool SyncIntervalEnabled { get; set; }
       
        public string SyncProjectsListID { get; set; }
        public string SyncProjectStatusListID { get; set; }
        public string SyncProjectTypeListID { get; set; }
        public string SyncProjectsEnabled { get; set; }
        public string SyncAfpListIDs { get; set; }
        public string SyncExpensesRecordEnabled { get; set; }
        public string SyncFRCListID { get; set; }
        public string SyncCSIListID { get; set; }
        public string SyncRFTListID { get; set; }
        public string SyncRftListProjectTypeCostSubItemMapping { get; set; }
        public string SyncEmployeeGradEnabled { get; set; }
        public string SyncReqEmployeeEnrollmentListID { get; set; }
        public string SyncReqPayrollChangeListID { get; set; }
        public string SyncProjectRoleListID { get; set; }
        public bool SyncProjectRolesEnabled { get; set; }


        public int Minutes
        {
            get
            {
                return int.TryParse(SyncIntervalValue, out int intervalValue) && intervalValue <= 60 && SyncIntervalType == "minutes" ? 
                    intervalValue : 0;
            }
        }

        public int Hours
        {
            get
            {
                return int.TryParse(SyncIntervalValue, out int intervalValue) && intervalValue <= 12 && SyncIntervalType == "hours"?
                    intervalValue : 0;
            }
        }

        public object this[string propertyName]
        {
            get
            {
                Type myType = typeof(BitrixConfig);
                PropertyInfo myPropInfo = myType.GetProperty(propertyName);
                return myPropInfo.GetValue(this, null);
            }
            set
            {
                Type myType = typeof(BitrixConfig);
                PropertyInfo myPropInfo = myType.GetProperty(propertyName);
                myPropInfo.SetValue(this, value, null);

            }
        }
    }
}
