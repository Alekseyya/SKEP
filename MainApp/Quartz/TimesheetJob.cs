using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BL.Implementation;
using Core.BL.Interfaces;
using Core.Config;
using Core.Data;
using Data;
using Data.Implementation;
using MainApp.Helpers;
using MainApp.TimesheetProcessing;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Quartz;


namespace MainApp.Quartz
{
    [DisallowConcurrentExecution]
    public class TimesheetJob : IJob
    {
        private readonly DbContextOptions<RPCSContext> _dbOptions;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IMemoryCache _memoryCache;
        private readonly IOptions<ADConfig> _adConfigOptions;
        private readonly IOptions<BitrixConfig> _bitrixConfigOptions;
        private readonly IOptions<OnlyOfficeConfig> _onlyOfficeOptions;
        private readonly IOptions<TimesheetConfig> _timesheetConfigOptions;
        private readonly TimesheetConfig _timesheetConfig;
        private readonly IOptions<SMTPConfig> _smtpConfigOptions;
        private readonly IOptions<JiraConfig> _jiraConfigOptions;
        private readonly ILogger<TimesheetJob> _timesheetJobLogger;
        private readonly ILogger<TSHoursRecordService> _tsHoursRecordServiceLogger;

        public TimesheetJob(
            DbContextOptions<RPCSContext> dbOptions, IHttpContextAccessor httpContextAccessor, IMemoryCache memoryCache,
            IOptions<ADConfig> adConfigOptions,
            IOptions<BitrixConfig> bitrixConfigOptions,
            IOptions<OnlyOfficeConfig> onlyOfficeOptions,
            IOptions<TimesheetConfig> timesheetConfigOptions,
            IOptions<SMTPConfig> smtpConfigOptions, IOptions<JiraConfig> jiraConfigOptions, ILogger<TimesheetJob> timesheetJobLogger, ILogger<TSHoursRecordService> tsHoursRecordServiceLogger)
        {
            _dbOptions = dbOptions;
            _httpContextAccessor = httpContextAccessor;
            _memoryCache = memoryCache;
            _adConfigOptions = adConfigOptions;
            _bitrixConfigOptions = bitrixConfigOptions;
            _onlyOfficeOptions = onlyOfficeOptions;
            _timesheetConfigOptions = timesheetConfigOptions;
            _smtpConfigOptions = smtpConfigOptions;
            _jiraConfigOptions = jiraConfigOptions;
            _timesheetJobLogger = timesheetJobLogger;
            _tsHoursRecordServiceLogger = tsHoursRecordServiceLogger;
            _timesheetConfig = timesheetConfigOptions.Value;
        }

        public Task Execute(IJobExecutionContext context)
        {
            try
            {
                var iRPCSDbAccessor = (IRPCSDbAccessor)new RPCSSingletonDbAccessor(_dbOptions);
                var rPCSRepositoryFactory = (IRepositoryFactory)new RPCSRepositoryFactory(iRPCSDbAccessor);

                var userService = (IUserService)new UserService(rPCSRepositoryFactory, _httpContextAccessor);
                var tsAutoHoursRecordService = (ITSAutoHoursRecordService)new TSAutoHoursRecordService(rPCSRepositoryFactory, userService);
                var vacationRecordService = (IVacationRecordService)new VacationRecordService(rPCSRepositoryFactory, userService);
                var reportingPeriodService = (IReportingPeriodService)new ReportingPeriodService(rPCSRepositoryFactory);
                var productionCalendarService = (IProductionCalendarService)new ProductionCalendarService(rPCSRepositoryFactory);
                var tsHoursRecordService = (ITSHoursRecordService)new TSHoursRecordService(rPCSRepositoryFactory, userService, _tsHoursRecordServiceLogger);
                var projectService = (IProjectService)new ProjectService(rPCSRepositoryFactory, userService);
                var departmentService = new DepartmentService(rPCSRepositoryFactory, userService);
                var employeeService = (IEmployeeService)new EmployeeService(rPCSRepositoryFactory, departmentService, userService);
                var projectMembershipService = (IProjectMembershipService)new ProjectMembershipService(rPCSRepositoryFactory);
                var employeeCategoryService = new EmployeeCategoryService(rPCSRepositoryFactory);
                var projectReportRecords = new ProjectReportRecordService(rPCSRepositoryFactory);
                var applicationUserService = new ApplicationUserService(rPCSRepositoryFactory, employeeService, userService,
                    departmentService, _httpContextAccessor, _memoryCache, projectService, _onlyOfficeOptions);
                var appPropertyService = new AppPropertyService(rPCSRepositoryFactory, _adConfigOptions, _bitrixConfigOptions, _onlyOfficeOptions, _timesheetConfigOptions);
                var ooService = new OOService(applicationUserService, _onlyOfficeOptions);
                var financeService = new FinanceService(rPCSRepositoryFactory, iRPCSDbAccessor, applicationUserService, appPropertyService, ooService);
                var timesheetService = new TimesheetService(employeeService, employeeCategoryService, tsAutoHoursRecordService, tsHoursRecordService,
                    projectService, projectReportRecords, vacationRecordService, productionCalendarService, financeService, _timesheetConfigOptions);
                var projectExternalWorkspace = new ProjectExternalWorkspaceService(rPCSRepositoryFactory);
                var jiraService = new JiraService(userService, _jiraConfigOptions,projectExternalWorkspace, projectService);


                var taskTimesheetProcessing = new TimesheetProcessingTask(tsAutoHoursRecordService, vacationRecordService, reportingPeriodService, productionCalendarService,
                    tsHoursRecordService, userService, projectService, employeeService, projectMembershipService, timesheetService, _timesheetConfigOptions, _smtpConfigOptions, _jiraConfigOptions, jiraService, projectExternalWorkspace);

                _timesheetJobLogger.LogInformation("Начало синхронизации с Timesheet!");
                string id = Guid.NewGuid().ToString();
                TimesheetProcessingResult timesheetProcessingResult = null;
                var fileHtmlReport = string.Empty;

                if (taskTimesheetProcessing.Add(id, true) == true)
                {
                    try
                    {
                        bool syncWithExternalTimesheet = false;
                        bool processVacationRecords = false;
                        bool processTSAutoHoursRecords = false;
                        bool sendTSEmailNotifications = false;
                        bool syncWithJIRA = false;
                        bool syncWithJIRASendEmailNotifications = false;

                        
                        try
                        {
                            syncWithExternalTimesheet = _timesheetConfig.ProcessingSyncWithExternalTimesheets;
                        }
                        catch (Exception)
                        {
                        }

                        try
                        {
                            processVacationRecords = _timesheetConfig.ProcessingProcessVacationRecords;
                        }
                        catch (Exception)
                        {
                        }

                        try
                        {
                            processTSAutoHoursRecords = _timesheetConfig.ProcessingProcessTSAutoHoursRecords;
                        }
                        catch (Exception)
                        {
                        }
                        try
                        {
                            sendTSEmailNotifications = _timesheetConfig.ProcessingSendTSEmailNotifications;
                        }
                        catch (Exception)
                        {
                        }

                        try
                        {
                            syncWithJIRA = _timesheetConfig.ProcessingSyncWithJIRA;
                        }
                        catch (Exception)
                        {
                        }


                        timesheetProcessingResult = taskTimesheetProcessing.ProcessLongRunningAction("", id,
                            syncWithExternalTimesheet, DateTime.MinValue, DateTime.MinValue, false, true, true, true, false, 350,
                            processVacationRecords,
                            processTSAutoHoursRecords,
                            sendTSEmailNotifications, DateTime.Today,
                            syncWithJIRA, DateTime.MinValue, DateTime.MinValue, false, DateTime.Today, syncWithJIRASendEmailNotifications);



                        foreach (var html in timesheetProcessingResult.fileHtmlReport)
                        {
                            fileHtmlReport += html;
                        }

                        _memoryCache.Set(timesheetProcessingResult.fileId, fileHtmlReport);
                    }
                    catch (Exception)
                    {

                    }
                    taskTimesheetProcessing.Remove(id);
                }
                if (timesheetProcessingResult != null
                    && String.IsNullOrEmpty(fileHtmlReport) == false
                    && !string.IsNullOrEmpty(_timesheetConfig.ProcessingReportEmailReceivers))
                {

                    byte[] binFileHtmlReport = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(fileHtmlReport)).ToArray();
                    using (MemoryStream streamFileHtmlReport = new MemoryStream(binFileHtmlReport))
                    {
                        try
                        {
                            string subject = "Отчет об обработке данных Таймшит " + DateTime.Now.ToString();
                            string bodyHtml = RPCSEmailHelper.GetSimpleHtmlEmailBody("Отчет об обработке данных Таймшит", "Отчет об обработке данных Таймшит во вложении.", null);
                            RPCSEmailHelper.SendHtmlEmailViaSMTP(_timesheetConfig.ProcessingReportEmailReceivers,
                                subject,
                                null,
                                null,
                                bodyHtml,
                                null,
                                null,
                                streamFileHtmlReport,
                                "TimesheetProcessingReport" + DateTime.Now.ToString("ddMMyyHHmmss") + ".html");
                        }
                        catch (Exception)
                        {

                        }

                    }
                }
            }
            catch (Exception e)
            {
                _timesheetJobLogger.LogError(e.Message);
                Console.WriteLine(e);
                throw;
            }
            _timesheetJobLogger.LogInformation("Синхронизация с Timesheet закончена!");
            return Task.CompletedTask;
        }
    }
}