using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Core.Models;


namespace Core.BL.Interfaces
{
    public interface ICostSubItemService : IServiceBase<CostSubItem, int>
    {
        IList<CostSubItem> Get(Func<IQueryable<CostSubItem>, IList<CostSubItem>> expression);
    }
}
