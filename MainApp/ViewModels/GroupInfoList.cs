using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MainApp.ViewModels
{
    public class GroupInfoList<TKey, TValue> : List<TValue>
    {
        public TKey Key { get; }

        public GroupInfoList(TKey key)
        {
            Key = key;
        }
    }

    public class GroupDictInfoList<TKey, TValue> : List<TValue>
    {
        public Dictionary<string, TKey> Keys { get; }

        public GroupDictInfoList(Dictionary<string, TKey> keys)
        {
            Keys = keys;
        }
    }
}
