using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MainApp.ViewModels
{
    public class EntityViewModel<T> : IViewModel where T : class
    {
        public T Entity { get; set; }
    }
}
