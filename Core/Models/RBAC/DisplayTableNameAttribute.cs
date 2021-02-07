using System;
using System.Collections.Generic;
using System.Text;

namespace Core.Models.RBAC
{
    [AttributeUsage(AttributeTargets.Class)]
    public class DisplayTableNameAttribute : Attribute
    {
        public DisplayTableNameAttribute(string name)
        {
            this.Name = name;
        }
        public string Name { get; }
    }
}
