using System;
using System.Collections.Generic;
using System.Text;

namespace Core.Config
{
    public class DBDataProcessingConfig
    {
        public string DataProcessingIntervalType { get; set; }
        public string DataProcessingIntervalValue { get; set; }
        public bool DataProcessingIntervalEnabled { get; set; }
    }
}
