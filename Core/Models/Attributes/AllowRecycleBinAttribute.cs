using System;
using System.Collections.Generic;
using System.Text;

namespace Core.Models.Attributes
{
    [AttributeUsage(AttributeTargets.Class)]
    public class AllowRecycleBinAttribute : Attribute
    {
        public AllowRecycleBinAttribute()
        {

        }
    }
}
