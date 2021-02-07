using System;
using System.Threading.Tasks;
using BL.Implementation;
using Core.DBDataProcessing;
using Data;
using Data.Implementation;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Quartz;


namespace MainApp.Quartz
{
    public class DBDataProcessingJob : IJob
    {
        private readonly DbContextOptions<RPCSContext> _dbContextOptions;
        private readonly IMemoryCache _memoryCache;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public DBDataProcessingJob(DbContextOptions<RPCSContext> dbContextOptions, IMemoryCache memoryCache, IHttpContextAccessor httpContextAccessor)
        {
            _dbContextOptions = dbContextOptions;
            _memoryCache = memoryCache;
            _httpContextAccessor = httpContextAccessor;
        }
        public Task Execute(IJobExecutionContext context)
        {
            try
            {
                // репозиторий
                var iRPCSDbAccessor = (IRPCSDbAccessor)new RPCSSingletonDbAccessor(_dbContextOptions);
                var rPCSRepositoryFactory = new RPCSRepositoryFactory(iRPCSDbAccessor);
                // сервисы
                var employeeOrganisationService = new EmployeeOrganisationService(rPCSRepositoryFactory);
                var userService = new UserService(rPCSRepositoryFactory, _httpContextAccessor);
                var departmentService = new DepartmentService(rPCSRepositoryFactory, userService);
                var employeeService = new EmployeeService(rPCSRepositoryFactory,departmentService, userService);
                // инициализация
                var task = new DBDataProcessingTask(employeeOrganisationService, employeeService);
                string id = Guid.NewGuid().ToString();
                DBDataProcessingTaskResult taskResult = null;

                if (task.Add(id, true) == true)
                {
                    try
                    {
                        taskResult = task.ProcessLongRunningAction("", id);
                        var fileHtmlReport = string.Empty;

                        foreach (var html in taskResult.fileHtmlReport)
                        {
                            fileHtmlReport += html;
                        }

                        _memoryCache.Set(taskResult.fileId, fileHtmlReport);
                    }
                    catch (Exception ex)
                    {

                    }
                    task.Remove(id);
                }
            }
            catch (Exception ex)
            {
                string errorMessage = ex.Message;
            }
            return Task.CompletedTask;
        }
    }
}
