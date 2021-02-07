using System;
using System.Collections.Generic;
using System.Text;

namespace Core.Config
{
    public class MyConfig
    {
        public string ApplicationName { get; set; }
        public string DbConnectionString { get; set; }
        public string DbConnectionStringMysql { get; set; }
        public int Version { get; set; }
    }
}
