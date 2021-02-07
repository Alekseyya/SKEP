using System;
using System.Collections.Generic;
using System.Text;

namespace Core.Models.Attributes
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public class DisplayInRecycleBinAttribute : Attribute
    {
        public DisplayInRecycleBinAttribute()
        {
        }
        public int Order { get; set; }
    }
}
