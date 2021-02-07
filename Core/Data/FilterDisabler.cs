//using EntityFramework.DynamicFilters;
using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;

//using System.Data.Entity;
using Z.EntityFramework.Plus;

namespace Core.Data
{
    public sealed class FilterDisabler : IDisposable
    {
        private bool _disposed = false;

        private DbContext _dbContext;

        private string _filterName;

        public FilterDisabler(DbContext dbContext, string filterName)
        {
            if (dbContext == null)
                throw new ArgumentNullException(nameof(dbContext));
            if (string.IsNullOrWhiteSpace(filterName))
                throw new ArgumentException("Не указано название фильтра", nameof(filterName));

            _dbContext = dbContext;
            _filterName = filterName;

            //TODO Добавить!!!!!!!!!!
            //_dbContext.DisableFilter(_filterName);
            _dbContext.Filter(filterName).Disable();
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                if (_dbContext != null)
                    //TODO Добавить!!!!!!!!!!!!!
                    //_dbContext.EnableFilter(_filterName);
                    _dbContext.Filter(_filterName);
                GC.SuppressFinalize(this);
            }
        }
    }
}
