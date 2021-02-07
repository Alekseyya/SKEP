using System;
using System.Collections.Generic;
using System.Text;

namespace Core.Models.RBAC
{
    public class StoredDataItem
    {
        public string id { get; set; }
        public string label { get; set; }
        public string value { get; set; }
    }

    public class StoredData
    {
        public List<StoredDataItem> Items { get; set; }
        public StoredData()
        {
            Items = new List<StoredDataItem>();
        }
    }
}
