using System;
using System.Reflection;

namespace Core.Config
{
    public class OnlyOfficeConfig
    {
        public string DefaultCpFileUrl { get; set; }
        public string TempCPFileUrl { get; set; }
        public string LoginRoleAdmin { get; set; }
        public string LoginRoleProjectManager { get; set; }
        public string LoginRolePmoAdmin { get; set; }
        public string LoginRolePmoChief { get; set; }
        public object this[string propertyName]
        {
            get
            {
                Type myType = typeof(OnlyOfficeConfig);
                PropertyInfo myPropInfo = myType.GetProperty(propertyName);
                return myPropInfo.GetValue(this, null);
            }
            set
            {
                Type myType = typeof(OnlyOfficeConfig);
                PropertyInfo myPropInfo = myType.GetProperty(propertyName);
                myPropInfo.SetValue(this, value, null);

            }

        }
    }
}
