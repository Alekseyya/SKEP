using System;
using System.Reflection;

namespace Core.Config
{
   public class ADConfig
    {
        public ADConfig()
        {
        }
        public string SyncUser { get; set; }
        public string SyncPassword { get; set; }
        public string SyncContainers { get; set; }
        public string SyncEmailRecievers { get; set; }
        public string SyncIntervalType { get; set; }
        public string SyncIntervalValue { get; set; }
        public bool SyncIntervalEnabled { get; set; }

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

        public int Hours
        {
            get
            {
                return (int.TryParse(SyncIntervalValue.Split('.')[0], out int result) && result <= 23) ? result: throw new ArgumentException("Неправильно указаны часы в файле config");
            }
        }

        public int Minutes
        {
            get
            {
                return int.TryParse(SyncIntervalValue.Split('.')[1], out int result) && (result >= 1 && result <= 59)
                    ? result
                    : throw new ArgumentException("Неправильно указаны минуты в фале config");
            }
        }
        public object this[string propertyName]
        {
            get
            {
                Type myType = typeof(ADConfig);
                PropertyInfo myPropInfo = myType.GetProperty(propertyName);
                return myPropInfo.GetValue(this, null);
            }
            set
            {
                Type myType = typeof(ADConfig);
                PropertyInfo myPropInfo = myType.GetProperty(propertyName);
                myPropInfo.SetValue(this, value, null);

            }

        }
    }
}
