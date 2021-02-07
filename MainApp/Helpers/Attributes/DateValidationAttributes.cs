using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace MainApp.Helpers.Attributes
{
    public class MonthEndDateAttribute : ValidationAttribute
    {
        
        //private static DateTime _monthEndDate;

        //public MonthEndDateAttribute(DateTime monthEndDate)
        //{
        //    _monthEndDate = monthEndDate;
        //}


        //Если сейчас октябрь, до 1 ноября. Если не указано выставить 3 ноября
        public override bool IsValid(object value)
        {
            if (value != null)
            {
                var currentDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day);
                var setValue = (DateTime)value;
                //Если указан не тот год или отчитаться на следующий год
                if (DateTime.Now.Year != setValue.Year || currentDate.AddMonths(1).Year != DateTime.Now.Year)
                    return false;

                var firstDayOnNextMonth = new DateTime(currentDate.Year, currentDate.AddMonths(1).Month, 1);

                if (setValue >= firstDayOnNextMonth)
                    return true;

            }
            return false;
        }
    }
}