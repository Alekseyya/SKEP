
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using Core.Models;

namespace Core.BL.Interfaces
{
    public interface IAppPropertyService : IServiceBase<AppProperty, int>
    {
        void SetAppSetting(string name, string value);
        string GetAppSetting(string name);
        IList<AppProperty> Get(Func<IQueryable<AppProperty>, IList<AppProperty>> expression);
        IList<AppProperty> GetAll();
        IList<AppProperty> GetAll(Expression<Func<AppProperty, bool>> conditionFunc);
        void Add(AppProperty appProperty);
        void Update(AppProperty appProperty);
    }
}
