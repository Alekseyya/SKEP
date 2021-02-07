
using System;
using System.Reflection;

namespace Core.Config
{
    public  class CommonConfig
    {
        public string ShowTestCopyWarningMessage { get; set; }
        public string DissalowInputEmployeePositionAsText { get; set; }
        public string ReturnUrlParameter { get; set; }

        public object this[string propertyName]
        {
            get
            {
                Type myType = typeof(CommonConfig);
                PropertyInfo myPropInfo = myType.GetProperty(propertyName);
                return myPropInfo.GetValue(this, null);
            }
            set
            {
                Type myType = typeof(CommonConfig);
                PropertyInfo myPropInfo = myType.GetProperty(propertyName);
                myPropInfo.SetValue(this, value, null);

            }
        }
    }
}
