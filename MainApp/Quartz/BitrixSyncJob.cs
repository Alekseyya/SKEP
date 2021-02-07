using System;
using System.Threading.Tasks;
using BL.Implementation;
using Core.Config;
using Data;
using Data.Implementation;
using MainApp.BitrixSync;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Quartz;



namespace MainApp.Quartz
{
    [DisallowConcurrentExecution]
    public class BitrixSyncJob : IJob
    {
        private readonly DbContextOptions<RPCSContext> _dbContextOptions;
        private readonly IMemoryCache _memoryCache;
        private readonly ILogger<BitrixSyncJob> _logger;
        private readonly IOptions<BitrixConfig> _bitrixConfigOptions;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public BitrixSyncJob(IOptions<BitrixConfig> bitrixConfigOptions,IHttpContextAccessor httpContextAccessor, DbContextOptions<RPCSContext> dbContextOptions,
            IMemoryCache memoryCache, ILogger<BitrixSyncJob> logger)
        {
            _bitrixConfigOptions = bitrixConfigOptions;
            _httpContextAccessor = httpContextAccessor;
            _dbContextOptions = dbContextOptions;
            _memoryCache = memoryCache;
            _logger = logger;
        }
        public Task Execute(IJobExecutionContext context)
        {
            try
            {
                _logger.LogInformation("Начало синхронизации с Bitrix");
                var iRPCSDbAccessor = new RPCSSingletonDbAccessor(_dbContextOptions);
                var rPCSRepositoryFactory = new RPCSRepositoryFactory(iRPCSDbAccessor);
                var userService = new UserService(rPCSRepositoryFactory,_httpContextAccessor);
                var budgetService = new BudgetLimitService(rPCSRepositoryFactory, userService);
                var departmentService = new DepartmentService(rPCSRepositoryFactory, userService);
                var employeeService = new EmployeeService(rPCSRepositoryFactory, departmentService, userService);
                var projectService = new ProjectService(rPCSRepositoryFactory, userService);
                
                var costSubItemService = new CostSubItemService(rPCSRepositoryFactory, userService);
                var expensesRecordService = new ExpensesRecordService(rPCSRepositoryFactory,_bitrixConfigOptions);
                var projectRoleService = new ProjectRoleService(rPCSRepositoryFactory);
                var taskSyncWithBitrix = new SyncWithBitrixTask(budgetService, _bitrixConfigOptions, employeeService, projectService, departmentService, costSubItemService, expensesRecordService, projectRoleService);
                string id = Guid.NewGuid().ToString();
                BitrixSyncResult syncWithBitrixResult = null;

                if (taskSyncWithBitrix.Add(id, true) == true)
                {
                    try
                    {
                        syncWithBitrixResult = taskSyncWithBitrix.ProcessLongRunningAction("", id);

                        var fileHtmlReport = string.Empty;

                        foreach (var html in syncWithBitrixResult.fileHtmlReport)
                        {
                            fileHtmlReport += html;
                        }

                        _memoryCache.Set(syncWithBitrixResult.fileId, fileHtmlReport);
                    }
                    catch (Exception)
                    {

                    }

                    taskSyncWithBitrix.Remove(id);
                }
            }
            catch (Exception e)
            {
                string errorMessage = e.Message;
                _logger.LogError(e.Message);
            }
            _logger.LogInformation("Процесс синхронизации закончен!!");
            return Task.CompletedTask;
        }
    }
}