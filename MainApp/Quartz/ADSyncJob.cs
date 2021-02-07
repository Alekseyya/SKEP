using System;
using System.Threading.Tasks;
using BL.Implementation;
using Core.Config;
using Core.Data;
using Data;
using Data.Implementation;
using MainApp.ADSync;
using MainApp.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Quartz;


namespace MainApp.Quartz
{
    [DisallowConcurrentExecution]
    public class ADSyncJob : IJob
    {
        private readonly ADConfig _adConfig;
        private readonly IOptions<ADConfig> _adConfigOptions;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly DbContextOptions<RPCSContext> _dbContextOptions;
        private readonly IMemoryCache _memoryCache;
        private readonly ILogger<ADSyncJob> _logger;

        public ADSyncJob(IOptions<ADConfig> adConfigOptions, IHttpContextAccessor httpContextAccessor, DbContextOptions<RPCSContext> dbContextOptions, IMemoryCache memoryCache, ILogger<ADSyncJob> logger)
        {
            _adConfig = adConfigOptions.Value;
            _adConfigOptions = adConfigOptions;
            _httpContextAccessor = httpContextAccessor;
            _dbContextOptions = dbContextOptions;
            _memoryCache = memoryCache;
            _logger = logger;
        }
        public Task Execute(IJobExecutionContext context)
        {

            try
            {
                _logger.LogInformation("Запуск синхронизации с AD!");
                var iRPCSDbAccessor = (IRPCSDbAccessor)new RPCSSingletonDbAccessor(_dbContextOptions);
                var rPCSRepositoryFactory = (IRepositoryFactory)new RPCSRepositoryFactory(iRPCSDbAccessor);

                var userService = new UserService(rPCSRepositoryFactory, _httpContextAccessor);
                var departmentService = new DepartmentService(rPCSRepositoryFactory, userService);
                
                var employeeService = new EmployeeService(rPCSRepositoryFactory, departmentService, userService);

                var taskSyncWithAD = new SyncWithADTask(employeeService,_adConfigOptions);
                string id = Guid.NewGuid().ToString();
                ADSyncResult adSyncResult = null;
                if (taskSyncWithAD.Add(id, true) == true)
                {
                    try
                    {
                        adSyncResult = taskSyncWithAD.ProcessLongRunningAction("", id, true);
                        _memoryCache.Set(adSyncResult.fileId, adSyncResult.fileHtmlReport);
                    }
                    catch (Exception)
                    {

                    }

                    taskSyncWithAD.Remove(id);
                }

                if (adSyncResult != null
                    && String.IsNullOrEmpty(adSyncResult.fileHtmlReport) == false
                    && !string.IsNullOrEmpty(_adConfig.SyncEmailRecievers))
                {
                    RPCSEmailHelper.SendHtmlEmailViaSMTP(_adConfig.SyncEmailRecievers,
                        "Отчет о синхронизации данных с Active Directory " + DateTime.Now.ToString(),
                        null,
                        null,
                        adSyncResult.fileHtmlReport,
                        null,
                        null);
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e.Message);
                string errorMessage = e.Message;
            }
            _logger.LogInformation("Окончание процесса синхронизации с AD");
            return Task.CompletedTask;
        }
    }
}