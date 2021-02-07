using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Core.BL;
using Core.BL.Interfaces;
using Core.Config;
using Core.Data;
using Core.Data.Interfaces;
using Core.Models;
using Core.Validation;
using Microsoft.Extensions.Options;

namespace BL.Implementation
{
   public class AppPropertyService : RepositoryAwareServiceBase<AppProperty, int, IAppPropertyRepository>, IAppPropertyService
    {
        private readonly TimesheetConfig _timesheetConfig;
        private readonly ADConfig _adConfig;
        private readonly BitrixConfig _bitrixConfig;
        private readonly OnlyOfficeConfig _onlyOfficeConfig;

        public AppPropertyService(IRepositoryFactory repositoryFactory, IOptions<ADConfig> adOptions,
            IOptions<BitrixConfig> bitrixOptions,
            IOptions<OnlyOfficeConfig> onlyOfficeOptions, IOptions<TimesheetConfig> timesheetOptions) : base(repositoryFactory)
        {
            _timesheetConfig = timesheetOptions.Value ?? throw new ArgumentNullException(nameof(timesheetOptions));
            _adConfig = adOptions.Value ?? throw new ArgumentNullException(nameof(adOptions));
            _bitrixConfig = bitrixOptions.Value ?? throw new ArgumentNullException(nameof(bitrixOptions));
            _onlyOfficeConfig = onlyOfficeOptions.Value ?? throw new ArgumentNullException(nameof(onlyOfficeOptions));
        }

        public void Validate(AppProperty entity, IValidationRecipient validationRecipient)
        {
            throw new NotImplementedException();
        }
        //TODO доделать метод!!!!
        public string GetAppSetting(string name)
        {
            string result = "";
            var appProperty = RepositoryFactory.GetRepository<IAppPropertyRepository>().GetQueryable().FirstOrDefault(ap => ap.Name == name);

            if (appProperty != null)
                result = appProperty.Value;

            if (!string.IsNullOrEmpty(result))
                return result;

            //Todo получить все проперти и значени в них через рефлексию
            //if (ConfigurationManager.AppSettings.AllKeys.Contains(name))
            //    result = ConfigurationManager.AppSettings[name];

            //return result;
            throw new NotImplementedException();
        }

        public void SetAppSetting(string name, string value)
        {
            var appRepository = RepositoryFactory.GetRepository<IAppPropertyRepository>();
            var appProperty = appRepository.GetQueryable().FirstOrDefault(ap => ap.Name == name);
            if (appProperty != null)
            {
                appProperty.Value = value;
                appRepository.Update(appProperty);
            }
            else
            {
                appProperty = new AppProperty
                {
                    Name = name,
                    Value = value
                };
                appRepository.Add(appProperty);
            }

        }

        public IList<AppProperty> GetAll()
        {
            return GetAll(null);
        }

        public IList<AppProperty> GetAll(Expression<Func<AppProperty, bool>> conditionFunc)
        {
            var repository = RepositoryFactory.GetRepository<IAppPropertyRepository>().GetAll();
            IList<AppProperty> appProperties;
            if (conditionFunc != null)
            {
                appProperties = repository.AsQueryable().Where(conditionFunc).ToList();
                return appProperties;
            }

            return repository;
        }

        public void Add(AppProperty appProperty)
        {
            if (appProperty == null)
                throw new ArgumentNullException();

            var appPropertyRepository = RepositoryFactory.GetRepository<IAppPropertyRepository>();
            appPropertyRepository.Add(appProperty);
        }

        public void Update(AppProperty appProperty)
        {
            if (appProperty == null)
                throw new ArgumentNullException();

            var appPropertyRepository = RepositoryFactory.GetRepository<IAppPropertyRepository>();
            appPropertyRepository.Update(appProperty);
        }
    }
}
