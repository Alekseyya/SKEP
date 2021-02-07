using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using BL.Implementation;
using Core.BL;
using Core.BL.Interfaces;
using Core.Config;
using Core.Data;
using Core.DBDataProcessing;
using Core.Helpers;
using Core.Models;
using Core.Models.Attributes;
using Core.Models.RBAC;
using Data;
using Data.Implementation;
using MainApp.ADSync;
using MainApp.BitrixSync;
using MainApp.BudgetLimitRecordsFromExcel;
using MainApp.RBAC.Attributes;
using MainApp.TimesheetImportHoursFromExcel;
using MainApp.TimesheetProcessing;
using MainApp.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using X.PagedList;


namespace MainApp.Controllers
{
    public class ServiceController : Controller
    {
        private readonly IProjectService _projectService;
        private readonly IProductionCalendarService _productionCalendarService;
        private readonly IUserService _userService;
        private readonly IExcelService _excelService;
        private readonly IReflectionService _reflectionService;
        private readonly IProjectStatusRecordService _projectStatusRecordService;
        private readonly IProjectScheduleEntryService _projectScheduleEntryService;
        private readonly IEmployeeService _employeeService;
        private readonly IOptions<ADConfig> _adOptions;
        private readonly IOptions<BitrixConfig> _bitrixOptions;
        private readonly IOptions<OnlyOfficeConfig> _onlyOfficeConfig;
        private readonly IOptions<TimesheetConfig> _timesheetOptions;
        private readonly IOptions<SMTPConfig> _smtpConfig;
        private readonly IOptions<JiraConfig> _jiraConfig;
        private readonly DbContextOptions<RPCSContext> _dbOptions;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IMemoryCache _memoryCache;
        private readonly IApplicationUserService _applicationUserService;
        private readonly ITSHoursRecordService _tsHoursRecordService;
        private readonly ILogger<TSHoursRecordService> _tsHoursRecordServiceLogger;
        private readonly IDepartmentService _departmentService;
        private readonly ICostItemService _costItemService;
        private readonly ICostSubItemService _costSubItemService;
        private readonly IBudgetLimitService _budgetLimitService;
        private readonly IVacationRecordService _vacationRecordService;
        private readonly IProjectScheduleEntryTypeService _projectScheduleEntryTypeService;
        private readonly ITSAutoHoursRecordService _tsAutoHoursRecordService;


        private readonly int _pageSize = 50;

        private TimesheetProcessingTask _taskTimesheetProcessing;
        private TimesheetImportHoursFromExcelTask _timesheetImportHoursFromExcelTask;
        private BudgetLimitImportRecordsFromExcelTask _budgetLimitImportRecordsFromExcelTask;
        private SyncWithBitrixTask _taskSyncWithBitrix;
        private ImportDataFromADTask _taskImportDataFromAD;
        private SyncWithADTask _taskSyncWithAD;
        private DBDataProcessingTask _dbDataProcessingTask;
        private IServiceService _serviceService;

        delegate string ProcessImportDataFromADTask(string userIdentityName, string id);
        delegate ADSyncResult ProcessSyncWithADTask(string userIdentityName, string id, bool saveDataInAD);
        delegate BitrixSyncResult ProcessSyncWithBitrixTask(string userIdentityName, string id);
        delegate DBDataProcessingTaskResult ProcessDBDataProcessingTask(string userIdentityName, string id);
        delegate TimesheetProcessingResult ProcessTimesheetProcessingTask(string userIdentityName, string id,
            bool syncWithExternalTimesheet,
            DateTime syncWithExtTSPeriodStart, DateTime syncWithExtTSPeriodEnd,
            bool deleteExtTSSyncedRecordsBeforeSync, bool updateExtTSAlreadyAddedRecords, bool getHoursFromExternalTimesheet, bool getVacationsFromExternalTimesheet, bool stopOnSyncWithExternalTSError, int batchSaveRecordsLimitOnSyncWithExternalTS,
            bool processVacationRecords,
            bool processTSAutoHoursRecords,
            bool sendTSEmailNotifications,
            DateTime sendTSEmailNotificationsAtDate,
            bool syncWithJIRA,
            DateTime syncWithJIRAPeriodStart, DateTime syncWithJIRAPeriodEnd,
            bool deleteJIRASyncedRecordsBeforeSync, DateTime syncWithJIRAAtDate, bool processingSyncWithJIRASendEmailNotifications);


        public ServiceController(IServiceProvider serviceProvider)
        {
            _projectService = serviceProvider.GetService<IProjectService>();
            _productionCalendarService = serviceProvider.GetService<IProductionCalendarService>();
            _userService = serviceProvider.GetService<IUserService>();
            _excelService = serviceProvider.GetService<IExcelService>();
            _reflectionService = serviceProvider.GetService<IReflectionService>();
            _projectStatusRecordService = serviceProvider.GetService<IProjectStatusRecordService>();
            _projectScheduleEntryService = serviceProvider.GetService<IProjectScheduleEntryService>();
            _employeeService = serviceProvider.GetService<IEmployeeService>();
            _adOptions = serviceProvider.GetService<IOptions<ADConfig>>();
            _bitrixOptions = serviceProvider.GetService<IOptions<BitrixConfig>>();
            _onlyOfficeConfig = serviceProvider.GetService<IOptions<OnlyOfficeConfig>>();
            _timesheetOptions = serviceProvider.GetService<IOptions<TimesheetConfig>>();
            _smtpConfig = serviceProvider.GetService<IOptions<SMTPConfig>>();
            _jiraConfig = serviceProvider.GetService<IOptions<JiraConfig>>();
            _dbOptions = serviceProvider.GetService<DbContextOptions<RPCSContext>>();
            _httpContextAccessor = serviceProvider.GetService<IHttpContextAccessor>();
            _memoryCache = serviceProvider.GetService<IMemoryCache>();
            _applicationUserService = serviceProvider.GetService<IApplicationUserService>();
            _tsHoursRecordService = serviceProvider.GetService<ITSHoursRecordService>();
            _tsHoursRecordServiceLogger = serviceProvider.GetService<ILogger<TSHoursRecordService>>();
            _budgetLimitService = serviceProvider.GetService<IBudgetLimitService>();
            _vacationRecordService = serviceProvider.GetService<IVacationRecordService>();
            _projectScheduleEntryTypeService = serviceProvider.GetService<IProjectScheduleEntryTypeService>();
            _tsAutoHoursRecordService = serviceProvider.GetService<ITSAutoHoursRecordService>();
            _serviceService = serviceProvider.GetService<IServiceService>();
            InitTasks();
        }

        protected void InitTasks()
        {
            var iRPCSDbAccessor = (IRPCSDbAccessor)new RPCSSingletonDbAccessor(_dbOptions);
            var rPCSRepositoryFactory = (IRepositoryFactory)new RPCSRepositoryFactory(iRPCSDbAccessor);

            var userService = new UserService(rPCSRepositoryFactory,_httpContextAccessor);
            var tsAutoHoursRecordService = new TSAutoHoursRecordService(rPCSRepositoryFactory, userService);
            var vacationRecordService = new VacationRecordService(rPCSRepositoryFactory, userService);
            var reportingPeriodService = new ReportingPeriodService(rPCSRepositoryFactory);
            var productionCalendarService = new ProductionCalendarService(rPCSRepositoryFactory);
            var tsHoursRecordService = new TSHoursRecordService(rPCSRepositoryFactory, userService, _tsHoursRecordServiceLogger);
            var projectService = new ProjectService(rPCSRepositoryFactory, userService);
            var departmentService = new DepartmentService(rPCSRepositoryFactory, userService);
            var employeeService = new EmployeeService(rPCSRepositoryFactory,departmentService, userService);
            var employeeOrganisationService = new EmployeeOrganisationService(rPCSRepositoryFactory);
            var budgetLimitService = new BudgetLimitService(rPCSRepositoryFactory, userService);
            var costSubItemService = new CostSubItemService(rPCSRepositoryFactory, userService);
            var projectMembershipService = new ProjectMembershipService(rPCSRepositoryFactory);
            var expensesRecordService = new ExpensesRecordService(rPCSRepositoryFactory,_bitrixOptions);
            var employeeCategoryService = new EmployeeCategoryService(rPCSRepositoryFactory);
            var projectReportRecords = new ProjectReportRecordService(rPCSRepositoryFactory);
            var applicationUserService = new ApplicationUserService(rPCSRepositoryFactory,employeeService,userService,
                departmentService, _httpContextAccessor, _memoryCache,projectService, _onlyOfficeConfig);
            var appPropertyService = new AppPropertyService(rPCSRepositoryFactory, _adOptions, _bitrixOptions,_onlyOfficeConfig, _timesheetOptions);
            var ooService = new OOService(applicationUserService, _onlyOfficeConfig);
            var finaceService = new FinanceService(rPCSRepositoryFactory,iRPCSDbAccessor,applicationUserService,appPropertyService, ooService);
            var timesheetService = new TimesheetService(employeeService, employeeCategoryService, tsAutoHoursRecordService,
                tsHoursRecordService, projectService, projectReportRecords, vacationRecordService, productionCalendarService, finaceService, _timesheetOptions);
            var projectExternalWorkspaceService = new ProjectExternalWorkspaceService(rPCSRepositoryFactory);
            var projectRoleService = new ProjectRoleService(rPCSRepositoryFactory);
            var jiraService = new JiraService(userService, _jiraConfig, projectExternalWorkspaceService, _projectService);



            _taskTimesheetProcessing = new TimesheetProcessingTask(tsAutoHoursRecordService, vacationRecordService, reportingPeriodService,
                productionCalendarService, tsHoursRecordService, userService, projectService, employeeService, projectMembershipService, timesheetService, _timesheetOptions,_smtpConfig, _jiraConfig, jiraService, projectExternalWorkspaceService);
            _timesheetImportHoursFromExcelTask = new TimesheetImportHoursFromExcelTask(tsHoursRecordService, productionCalendarService, employeeService, projectService, userService);
            _budgetLimitImportRecordsFromExcelTask = new BudgetLimitImportRecordsFromExcelTask(budgetLimitService, costSubItemService, departmentService, userService);
            _taskSyncWithBitrix = new SyncWithBitrixTask(budgetLimitService, _bitrixOptions, employeeService, projectService, departmentService, costSubItemService, expensesRecordService,projectRoleService);
            _taskImportDataFromAD = new ImportDataFromADTask(employeeService);
            _taskSyncWithAD = new SyncWithADTask(employeeService, _adOptions);
            _dbDataProcessingTask = new DBDataProcessingTask(employeeOrganisationService, employeeService);
        }

        public ActionResult Index()
        {
            // ReSharper disable once Mvc.ViewNotResolved
            return View();
        }

        [OperationActionFilter(nameof(Operation.EmployeeIDServiceAccess))]
        public ActionResult EmployeeIDService(string employeeIDServiceRequestMode, int? employeeIDFromDB, string employeeTitleInput, string employeeIDInput)
        {
            ViewBag.Message = "EmployeeID для сотрудников";

            string resultText = "";

            ViewBag.EmployeeIDServiceRequestMode = employeeIDServiceRequestMode;
            ViewBag.EmployeesFromDB = _employeeService.Get(x => x.ToList().OrderBy(e => e.FullName).ToList());
            ViewBag.EmployeeIDFromDB = employeeIDFromDB;
            ViewBag.EmployeeTitleInput = employeeTitleInput;
            ViewBag.EmployeeIDInput = employeeIDInput;

            if (String.IsNullOrEmpty(employeeIDServiceRequestMode) == false)
            {
                switch (employeeIDServiceRequestMode)
                {
                    case "EmployeeIDBySelectedEmployee":
                        resultText = (employeeIDFromDB.HasValue) ? _employeeService.GetADEmployeeIDByEmployeeIDInDB(employeeIDFromDB.Value) : "";
                        break;
                    case "EmployeeIDByEmployeeTitle":
                        resultText = _employeeService.GetADEmployeeIDBySearchString(employeeTitleInput.Trim());
                        break;
                    case "EmployeeTitleByEmployeeID":
                        resultText = _employeeService.GetEmployeeTitleByADEmployeeID(employeeIDInput);
                        break;
                    default:
                        break;
                }

                if (String.IsNullOrEmpty(resultText) == true)
                {
                    resultText = "<не найдено>";
                }
            }

            ViewBag.ResultText = resultText;

            return View();
        }

        [OperationActionFilter(nameof(Operation.ADSyncAccess))]
        public ActionResult ImportDataFromADService()
        {
            ViewBag.Message = "Импорт данных из Active Directory";

            ViewBag.CurrentTaskId = _taskImportDataFromAD.GetIdOfRunningSingleTask();

            return View();
        }

        [ATSHoursRecordCreateUpdateMyHours]
        public ActionResult ImportTSHoursFromExcel()
        {
            ViewBag.Years = _productionCalendarService.GetAllRecords().Select(x => new { x.Year }).Distinct().OrderBy(x => x.Year).ToList();
            ViewBag.Months = Enumerable.Empty<SelectListItem>();

            ViewBag.CurrentTaskId = _timesheetImportHoursFromExcelTask.GetIdOfRunningSingleTask();

            return View();
        }


        delegate TimesheetImportHoursFromExcelResult ProcessTimesheetImportTask(string userIdentityName, string fileId, DataTable timesheetHoursRecordSheetDataTable, int reportMonth,
            int reportHoursInMonth, int reportYear, bool onlyValidate, bool rewriteTSHoursRecords, string currentUserName, string currentUserSID);

        delegate BudgetLimitImportRecordsFromExcelResult ProcessBudgetLimitImportTask(string userIdentityName, string id, DataTable budgetLimitTable, int reportYear, bool onlyValidate);

        [HttpPost]
        public void StartImportTSHoursFromExcel(string id, string reportPeriod, string reportYear, bool onlyValidate, bool rewriteTSHoursRecords, IFormFile file)
        {
            var intReportMonth = string.IsNullOrEmpty(reportPeriod) ? 1 : int.Parse(reportPeriod.Split('.')[0]);
            var reportHoursInMonth = int.Parse(reportPeriod.Split('|')[1]);
            var intReportYear = string.IsNullOrEmpty(reportYear) ? 2019 : int.Parse(reportYear);
            var currUser = _userService.GetUserDataForVersion();

            var timesheetHoursRecordSheetDataTable = new DataTable();
            timesheetHoursRecordSheetDataTable = ExcelHelper.ExportColumnsAndData(timesheetHoursRecordSheetDataTable, file.OpenReadStream());

            if (_timesheetImportHoursFromExcelTask.Add(id, true) == true)
            {
                var processTask = new ProcessTimesheetImportTask(_timesheetImportHoursFromExcelTask.ProcessLongRunningAction);
                string userIdentityName = User.Identity.Name;
                Task.Run(() => processTask.Invoke(userIdentityName, id, timesheetHoursRecordSheetDataTable, intReportMonth,
                    intReportYear, reportHoursInMonth, onlyValidate, rewriteTSHoursRecords, currUser.Item1, currUser.Item2)).ContinueWith(EndImportTSHoursFromExcel);
            }
        }

        public void EndImportTSHoursFromExcel(Task<TimesheetImportHoursFromExcelResult> result)
        {
            TimesheetImportHoursFromExcelResult timesheetProcessingResult = result.Result;
            var fileHtmlReport = string.Empty;

            foreach (var html in timesheetProcessingResult.fileHtmlReport)
            {
                fileHtmlReport += html;
            }

            _memoryCache.Set(timesheetProcessingResult.fileId, fileHtmlReport);

            _timesheetImportHoursFromExcelTask.Remove(timesheetProcessingResult.fileId);
        }

        [OperationActionFilter(nameof(Operation.ADSyncAccess))]
        public ActionResult SyncADService()
        {
            ViewBag.Message = "Синхронизация с Active Directory";

            ViewBag.CurrentTaskId = _taskSyncWithAD.GetIdOfRunningSingleTask();

            return View();
        }


        [OperationActionFilter(nameof(Operation.ADSyncAccess))]
        [HttpPost]
        public void StartImportDataFromAD(string id)
        {
            if (_taskImportDataFromAD.Add(id, true) == true)
            {
                ProcessImportDataFromADTask processTask = new ProcessImportDataFromADTask(_taskImportDataFromAD.ProcessLongRunningAction);
                string userIdentityName = User.Identity.Name;
                Task.Run(() => processTask.Invoke(userIdentityName, id)).ContinueWith(EndImportDataFromAD);
            }
        }

        [OperationActionFilter(nameof(Operation.ADSyncAccess))]
        public void EndImportDataFromAD(Task<string> result)
        {
            string id = result.Result;

            _taskImportDataFromAD.Remove(id);
        }

        [OperationActionFilter(nameof(Operation.ADSyncAccess))]
        [HttpPost]
        public void StartSyncWithAD(string id, bool saveDataInAD)
        {
            if (_taskSyncWithAD.Add(id, true) == true)
            {
                ProcessSyncWithADTask processTask = new ProcessSyncWithADTask(_taskSyncWithAD.ProcessLongRunningAction);
                string userIdentityName = User.Identity.Name;
                Task.Run(() => processTask.Invoke(userIdentityName, id, saveDataInAD)).ContinueWith(EndSyncWithAD);
            }
        }

        [OperationActionFilter(nameof(Operation.ADSyncAccess))]
        public void EndSyncWithAD(Task<ADSyncResult> result)
        {
            ADSyncResult adSyncResult = result.Result;

            _memoryCache.Set(adSyncResult.fileId, adSyncResult.fileHtmlReport);

            _taskSyncWithAD.Remove(adSyncResult.fileId);
        }

        [OperationActionFilter(nameof(Operation.BitrixSyncAccess))]
        public ActionResult SyncBitrixService()
        {
            ViewBag.Message = "Синхронизация данных с Б24";

            ViewBag.CurrentTaskId = _taskSyncWithBitrix.GetIdOfRunningSingleTask();

            return View();
        }

        [OperationActionFilter(nameof(Operation.BitrixSyncAccess))]
        [HttpPost]
        public void StartSyncWithBitrix(string id)
        {
            if (_taskSyncWithBitrix.Add(id, true) == true)
            {
                ProcessSyncWithBitrixTask processTask = new ProcessSyncWithBitrixTask(_taskSyncWithBitrix.ProcessLongRunningAction);
                string userIdentityName = User.Identity.Name;
                Task.Run(() => processTask.Invoke(userIdentityName, id)).ContinueWith(EndSyncWithBitrix);
            }
        }

        [OperationActionFilter(nameof(Operation.BitrixSyncAccess))]
        public void EndSyncWithBitrix(/*IAsyncResult*/ Task<BitrixSyncResult> result)
        {
            BitrixSyncResult syncWithBitrixResult = result.Result;
            var fileHtmlReport = string.Empty;

            foreach (var html in syncWithBitrixResult.fileHtmlReport)
            {
                fileHtmlReport += html;
            }

            _memoryCache.Set(syncWithBitrixResult.fileId, fileHtmlReport);

            _taskSyncWithBitrix.Remove(syncWithBitrixResult.fileId);
        }


        [OperationActionFilter(nameof(Operation.TimesheetProcessingAccess))]
        public ActionResult TimesheetService()
        {
            ViewBag.Message = "Обработка данных Timesheet";

            ViewBag.CurrentTaskId = _taskTimesheetProcessing.GetIdOfRunningSingleTask();

            return View();
        }
        
        [HttpGet]
        [OperationActionFilter(nameof(Operation.AdminFullAccess))]
        public ActionResult ExportDataToExcel()
        {
            var listTables = new Dictionary<string, string>();
            foreach (var rpcsProperty in typeof(RPCSContext).GetProperties()
                .Where(x => x.Name != "Database" && x.Name != "ChangeTracker" && x.Name != "Configuration").Select(x => new { x.PropertyType, x.Name }))
            {
                foreach (var displayTableNameClass in (DisplayTableNameAttribute[])rpcsProperty.PropertyType.GetGenericArguments()[0].GetCustomAttributes(typeof(DisplayTableNameAttribute), true))
                {
                    listTables.Add(rpcsProperty.PropertyType.GetGenericArguments()[0].Name,
                        string.IsNullOrEmpty(displayTableNameClass.Name) ? rpcsProperty.PropertyType.GetGenericArguments()[0].Name : displayTableNameClass.Name);
                }
            }
            ViewBag.TablesName = listTables.Select(x => new SelectListItem()
            {
                Text = x.Value,
                Value = x.Key
            });
            return View();
        }

        [HttpGet]
        [OperationActionFilter(nameof(Operation.AdminFullAccess))]
        public ActionResult ExportListDataToExcel(string tableName)
        {
            if (string.IsNullOrEmpty(tableName))
                return RedirectToAction("ExportDataToExcel");

            var dataTable = new DataTable();
            var tableTitle = string.Empty;
            using (var db = new RPCSContext(_dbOptions))
            {
                var contextPropertyTable = db.GetType().GetProperties().FirstOrDefault(x => x.PropertyType.GetGenericArguments()[0].Name == tableName);
                var entryList = (contextPropertyTable.GetValue(db, null) as IEnumerable<object>).Cast<object>().ToList();
                tableTitle = contextPropertyTable.PropertyType.GetGenericArguments()[0].GetCustomAttribute<DisplayTableNameAttribute>().Name;
                if (entryList != null && entryList.Count != 0)
                {
                    dataTable = _excelService.CreateDataTableByEntryList(entryList);
                }
                else
                {
                    dataTable.Columns.Add("ID", typeof(string)).Caption = "ИД";
                }
            }
            return File(_excelService.CreateBinaryByDataTable(dataTable, "Выгрузка", "Выгрузка таблицы " + tableTitle),
                ExcelHelper.ExcelContentType, tableName + "_Export_" + DateTime.Now.ToString("ddMMyyHHmmss") + ".xlsx");
        }

        [HttpGet]
        [OperationActionFilter(nameof(Operation.AdminFullAccess))]
        public ActionResult RecycleBin(int? page, string tableName)
        {
            List<(string Value, string Key, object Type)> listTables = new List<(string Text, string Value, object Type)>();
            foreach (var property in typeof(RPCSContext).GetProperties()
                .Where(x => x.Name != "Database" && x.Name != "ChangeTracker" && x.Name != "Configuration")
                .Where(x => x.PropertyType.GetGenericArguments()[0].BaseType == typeof(BaseModel)))
            {
                if (property.PropertyType.GetGenericArguments()[0].GetCustomAttribute<AllowRecycleBinAttribute>() != null)
                {
                    foreach (var displayTableNameClass in (DisplayTableNameAttribute[])property.PropertyType.GetGenericArguments()[0].GetCustomAttributes(typeof(DisplayTableNameAttribute), true))
                    {
                        listTables.Add((property.PropertyType.GetGenericArguments()[0].Name,
                            string.IsNullOrEmpty(displayTableNameClass.Name) ? property.PropertyType.GetGenericArguments()[0].Name : displayTableNameClass.Name, property.PropertyType.GetGenericArguments()[0]));
                    }
                }

            }

            ViewBag.TablesName = listTables.Select(x => new SelectListItem()
            {
                Text = x.Key,
                Value = x.Value
            });
            ViewBag.CurrentTableName = tableName;

            page = page.HasValue ? page : 1;
            StaticPagedList<IViewModel> pagedList = null;
            if (!string.IsNullOrEmpty(tableName))
            {
                var tableType = listTables.FirstOrDefault(x => x.Value.Contains(tableName)).Type;
                int countItem;
                switch (tableType)
                {
                    case Type _ when tableType == typeof(BudgetLimit):
                        var budgetLimits = _budgetLimitService.Get(ps => ps.OrderByDescending(x => x.DeletedDate).Skip((page.Value - 1) * _pageSize).Take(_pageSize).ToList(), GetEntityMode.Deleted);
                        countItem = budgetLimits.Count;
                        var budgetlimitViewModel = new List<EntityViewModel<BudgetLimit>>();
                        foreach (var budgetLimit in budgetLimits)
                        {
                            budgetlimitViewModel.Add(new EntityViewModel<BudgetLimit>() { Entity = budgetLimit });
                        }
                        pagedList = new StaticPagedList<IViewModel>(budgetlimitViewModel, page.Value, _pageSize, countItem);
                        break;
                    case Type _ when tableType == typeof(CostItem):
                        var costItems = _costItemService.Get(ps => ps.OrderByDescending(x => x.DeletedDate).Skip((page.Value - 1) * _pageSize).Take(_pageSize).ToList(), GetEntityMode.Deleted);
                        countItem = costItems.Count;
                        var costItemViewModel = new List<EntityViewModel<CostItem>>();
                        foreach (var costItem in costItems)
                        {
                            costItemViewModel.Add(new EntityViewModel<CostItem>() { Entity = costItem });
                        }
                        pagedList = new StaticPagedList<IViewModel>(costItemViewModel, page.Value, _pageSize, countItem);
                        break;
                    case Type _ when tableType == typeof(CostSubItem):
                        var costSubItems = _costSubItemService.Get(ps => ps.OrderByDescending(x => x.DeletedDate).Skip((page.Value - 1) * _pageSize).Take(_pageSize).ToList(), GetEntityMode.Deleted);
                        countItem = costSubItems.Count;
                        var costSubItemViewModel = new List<EntityViewModel<CostSubItem>>();
                        foreach (var costSubItem in costSubItems)
                        {
                            costSubItemViewModel.Add(new EntityViewModel<CostSubItem>() { Entity = costSubItem });
                        }
                        pagedList = new StaticPagedList<IViewModel>(costSubItemViewModel, page.Value, _pageSize, countItem);
                        break;
                    case Type _ when tableType == typeof(Department):
                        var departments = _departmentService.Get(ps => ps.OrderByDescending(x => x.DeletedDate).Skip((page.Value - 1) * _pageSize).Take(_pageSize).ToList(), GetEntityMode.Deleted);
                        countItem = departments.Count;
                        var departmentViewModel = new List<EntityViewModel<Department>>();
                        foreach (var department in departments)
                        {
                            departmentViewModel.Add(new EntityViewModel<Department>() { Entity = department });
                        }
                        pagedList = new StaticPagedList<IViewModel>(departmentViewModel, page.Value, _pageSize, countItem);
                        break;
                    case Type _ when tableType == typeof(Employee):
                        var employees = _employeeService.Get(ps => ps.OrderByDescending(x => x.DeletedDate).Skip((page.Value - 1) * _pageSize).Take(_pageSize).ToList(), GetEntityMode.Deleted);
                        countItem = employees.Count;
                        var employeeListViewModel = new List<EntityViewModel<Employee>>();
                        foreach (var employee in employees)
                        {
                            employeeListViewModel.Add(new EntityViewModel<Employee>() { Entity = employee });
                        }
                        pagedList = new StaticPagedList<IViewModel>(employeeListViewModel, page.Value, _pageSize, countItem);
                        break;
                    case Type _ when tableType == typeof(Project):
                        var projectList = _projectService.Get(ps => ps.OrderByDescending(x => x.DeletedDate).Skip((page.Value - 1) * _pageSize).Take(_pageSize).ToList(), GetEntityMode.Deleted);
                        countItem = projectList.Count;
                        var projectListViewModel = new List<EntityViewModel<Project>>();
                        foreach (var project in projectList)
                        {
                            projectListViewModel.Add(new EntityViewModel<Project>() { Entity = project });
                        }
                        pagedList = new StaticPagedList<IViewModel>(projectListViewModel, page.Value, _pageSize, countItem);
                        break;

                    case Type _ when tableType == typeof(ProjectStatusRecord):
                        var projectStatusRecordList = _projectStatusRecordService.Get(ps => ps.OrderByDescending(x => x.DeletedDate).Skip((page.Value - 1) * _pageSize).Take(_pageSize).ToList(),
                            GetEntityMode.Deleted);
                        countItem = projectStatusRecordList.Count;
                        var projectStatusRecordListViewModel = new List<EntityViewModel<ProjectStatusRecord>>();
                        foreach (var projectStatusRecord in projectStatusRecordList)
                        {
                            projectStatusRecordListViewModel.Add(new EntityViewModel<ProjectStatusRecord>() { Entity = projectStatusRecord });
                        }
                        pagedList = new StaticPagedList<IViewModel>(projectStatusRecordListViewModel, page.Value, _pageSize, countItem);
                        break;

                    case Type _ when tableType == typeof(ProjectScheduleEntry):
                        var projectScheduleEntryList = _projectScheduleEntryService.Get(ps => ps.OrderByDescending(x => x.DeletedDate).Skip((page.Value - 1) * _pageSize).Take(_pageSize).ToList(),
                            GetEntityMode.Deleted);
                        countItem = projectScheduleEntryList.Count;
                        var projectScheduleEntryListViewModel = new List<EntityViewModel<ProjectScheduleEntry>>();
                        foreach (var projectScheduleEntry in projectScheduleEntryList)
                        {
                            projectScheduleEntryListViewModel.Add(new EntityViewModel<ProjectScheduleEntry>() { Entity = projectScheduleEntry });
                        }
                        pagedList = new StaticPagedList<IViewModel>(projectScheduleEntryListViewModel, page.Value, _pageSize, countItem);
                        break;
                    case Type _ when tableType == typeof(ProjectScheduleEntryType):
                        var scheduleEntryTypes = _projectScheduleEntryTypeService.Get(ps => ps.OrderByDescending(x => x.DeletedDate).Skip((page.Value - 1) * _pageSize).Take(_pageSize).ToList(),
                            GetEntityMode.Deleted);
                        countItem = scheduleEntryTypes.Count;
                        var scheduleEntryListViewModel = new List<EntityViewModel<ProjectScheduleEntryType>>();
                        foreach (var projectScheduleEntryType in scheduleEntryTypes)
                        {
                            scheduleEntryListViewModel.Add(new EntityViewModel<ProjectScheduleEntryType>() { Entity = projectScheduleEntryType });
                        }
                        pagedList = new StaticPagedList<IViewModel>(scheduleEntryListViewModel, page.Value, _pageSize, countItem);
                        break;
                    case Type _ when tableType == typeof(TSHoursRecord):
                        var tsHoursRecords = _tsHoursRecordService.Get(ps => ps.OrderByDescending(x => x.DeletedDate).Skip((page.Value - 1) * _pageSize).Take(_pageSize).ToList(), GetEntityMode.Deleted);
                        countItem = tsHoursRecords.Count;
                        var tsHoursRecordListViewModel = new List<EntityViewModel<TSHoursRecord>>();
                        foreach (var tsHoursRecord in tsHoursRecords)
                        {
                            tsHoursRecordListViewModel.Add(new EntityViewModel<TSHoursRecord>() { Entity = tsHoursRecord });
                        }
                        pagedList = new StaticPagedList<IViewModel>(tsHoursRecordListViewModel, page.Value, _pageSize, countItem);
                        break;
                    case Type _ when tableType == typeof(TSAutoHoursRecord):
                        var autoHoursRecords = _tsAutoHoursRecordService.Get(ps => ps.OrderByDescending(x => x.DeletedDate).Skip((page.Value - 1) * _pageSize).Take(_pageSize).ToList(), GetEntityMode.Deleted);
                        countItem = autoHoursRecords.Count;
                        var tsAutoHoursRecords = new List<EntityViewModel<TSAutoHoursRecord>>();
                        foreach (var tsHoursRecord in autoHoursRecords)
                        {
                            tsAutoHoursRecords.Add(new EntityViewModel<TSAutoHoursRecord>() { Entity = tsHoursRecord });
                        }
                        pagedList = new StaticPagedList<IViewModel>(tsAutoHoursRecords, page.Value, _pageSize, countItem);
                        break;
                    case Type _ when tableType == typeof(Project):
                        var projects = _projectService.Get(ps => ps.OrderByDescending(x => x.DeletedDate).Skip((page.Value - 1) * _pageSize).Take(_pageSize).ToList(), GetEntityMode.Deleted);
                        countItem = projects.Count;
                        var projectViewModels = new List<EntityViewModel<Project>>();
                        foreach (var project in projects)
                        {
                            projectViewModels.Add(new EntityViewModel<Project>() { Entity = project });
                        }
                        pagedList = new StaticPagedList<IViewModel>(projectViewModels, page.Value, _pageSize, countItem);
                        break;
                    case Type _ when tableType == typeof(VacationRecord):
                        var vacationRecords = _vacationRecordService.Get(ps => ps.OrderByDescending(x => x.DeletedDate).Skip((page.Value - 1) * _pageSize).Take(_pageSize).ToList(), GetEntityMode.Deleted);
                        countItem = vacationRecords.Count;
                        var vacationRecordViewModel = new List<EntityViewModel<VacationRecord>>();
                        foreach (var vacationRecord in vacationRecords)
                        {
                            vacationRecordViewModel.Add(new EntityViewModel<VacationRecord>() { Entity = vacationRecord });
                        }
                        pagedList = new StaticPagedList<IViewModel>(vacationRecordViewModel, page.Value, _pageSize, countItem);
                        break;
                }
            }
            return View(pagedList);
        }

        [HttpPost]
        [OperationActionFilter(nameof(Operation.AdminFullAccess))]
        public ActionResult RecycleBinDelete(string tableName, int? deletedId, string ids)
        {
            var listIdRows = !string.IsNullOrEmpty(ids) ? ids.Split(',').Select(Int32.Parse).ToList() : null;
            if (listIdRows != null)
                foreach (var id in listIdRows)
                {
                    //Если не смогли удалить какую-то запись - ошибка!
                    if (!_serviceService.RecycleBinDeleteInTable(tableName, id))
                    {
                        Response.StatusCode = 500;
                        return Json(new
                        {
                            Message = $"Id - {id} - Невозможно удалить, так как на удаляемый элемент ссылаются другие элементы в Корзине." +
                                      $" Сначала необходимо удалить элементы, которые ссылаются на данный элемент."
                        });
                    }
                }
            else if (!string.IsNullOrEmpty(tableName) && (deletedId != null && deletedId != 0))
            {
                if (!_serviceService.RecycleBinDeleteInTable(tableName, deletedId.Value))
                {
                    Response.StatusCode = 500;
                    return Json(new
                    {
                        Message = $"Id - {deletedId} - Невозможно удалить, так как на удаляемый элемент ссылаются другие элементы в Корзине." +
                                  $" Сначала необходимо удалить элементы, которые ссылаются на данный элемент."
                    });
                }
            }
            return RedirectToAction("RecycleBin", "Service", new { tableName = tableName });
        }

        private void RecycleBinDeleteInTable(string tableName, int? deletedId)
        {
            switch (tableName)
            {
                case nameof(BudgetLimit):
                    if (_budgetLimitService.Get(x => x.Where(b => b.ID == (int)deletedId).ToList(), GetEntityMode.Deleted).FirstOrDefault() != null)
                        _budgetLimitService.Delete((int)deletedId);
                    _budgetLimitService.DeleteRelatedEntries((int)deletedId);
                    break;
                case nameof(CostItem):
                    if (_costItemService.Get(x => x.Where(b => b.ID == (int)deletedId).ToList(), GetEntityMode.Deleted).FirstOrDefault() != null)
                        _costItemService.Delete((int)deletedId);
                    _costItemService.DeleteRelatedEntries((int)deletedId);
                    break;
                case nameof(CostSubItem):
                    if (_costSubItemService.Get(x => x.Where(b => b.ID == (int)deletedId).ToList(), GetEntityMode.Deleted).FirstOrDefault() != null)
                        _costSubItemService.Delete((int)deletedId);
                    _costSubItemService.DeleteRelatedEntries((int)deletedId);
                    break;
                case nameof(Department):
                    if (_departmentService.Get(x => x.Where(b => b.ID == (int)deletedId).ToList(), GetEntityMode.Deleted).FirstOrDefault() != null)
                        _departmentService.Delete((int)deletedId);
                    _departmentService.DeleteRelatedEntries((int)deletedId);
                    break;
                case nameof(Employee):
                    if (_employeeService.Get(x => x.Where(b => b.ID == (int)deletedId).ToList(), GetEntityMode.Deleted).FirstOrDefault() != null)
                        _employeeService.Delete((int)deletedId);
                    _employeeService.DeleteRelatedEntries((int)deletedId);
                    break;
                case nameof(ProjectStatusRecord):
                    if (_projectStatusRecordService.GetByIdWithDeleteFilter((int)deletedId) != null)
                    {
                        _projectStatusRecordService.Delete((int)deletedId);
                        _projectStatusRecordService.DeleteRelatedEntries((int)deletedId);
                    }
                    break;
                case nameof(ProjectScheduleEntry):
                    if (_projectScheduleEntryService.Get(x => x.Where(b => b.ID == (int)deletedId).ToList(), GetEntityMode.Deleted).FirstOrDefault() != null)
                        _projectScheduleEntryService.Delete((int)deletedId);
                    _projectScheduleEntryService.DeleteRelatedEntries((int)deletedId);
                    break;
                case nameof(ProjectScheduleEntryType):
                    if (_projectScheduleEntryTypeService.Get(x => x.Where(b => b.ID == (int)deletedId).ToList(), GetEntityMode.Deleted).FirstOrDefault() != null)
                        _projectScheduleEntryTypeService.Delete((int)deletedId);
                    _projectScheduleEntryTypeService.DeleteRelatedEntries((int)deletedId);
                    break;
                case nameof(TSHoursRecord):
                    if (_tsHoursRecordService.Get(x => x.Where(b => b.ID == (int)deletedId).ToList(), GetEntityMode.Deleted).FirstOrDefault() != null)
                        _tsHoursRecordService.Delete((int)deletedId);
                    _tsHoursRecordService.DeleteRelatedEntries((int)deletedId);
                    break;
                case nameof(TSAutoHoursRecord):
                    if (_tsAutoHoursRecordService.Get(x => x.Where(b => b.ID == (int)deletedId).ToList(), GetEntityMode.Deleted).FirstOrDefault() != null)
                        _tsAutoHoursRecordService.Delete((int)deletedId);
                    _tsAutoHoursRecordService.DeleteRelatedEntries((int)deletedId);
                    break;
                case nameof(Project):
                    if (_projectService.Get(x => x.Where(b => b.ID == (int)deletedId).ToList(), GetEntityMode.Deleted).FirstOrDefault() != null)
                        _projectService.Delete((int)deletedId);
                    _projectService.DeleteRelatedEntries((int)deletedId);
                    break;
                case nameof(VacationRecord):
                    if (_vacationRecordService.Get(x => x.Where(b => b.ID == (int)deletedId).ToList(), GetEntityMode.Deleted).FirstOrDefault() != null)
                        _vacationRecordService.Delete((int)deletedId);
                    _vacationRecordService.DeleteRelatedEntries((int)deletedId);
                    break;
            }
        }

        private void RecycleBinRestoreInTable(string tableName, int? restoreId)
        {
            switch (tableName)
            {
                case nameof(BudgetLimit):
                    _budgetLimitService.RestoreFromRecycleBin((int)restoreId);
                    break;
                case nameof(CostItem):
                    _costItemService.RestoreFromRecycleBin((int)restoreId);
                    break;
                case nameof(CostSubItem):
                    _costSubItemService.RestoreFromRecycleBin((int)restoreId);
                    break;
                case nameof(Department):
                    _departmentService.RestoreFromRecycleBin((int)restoreId);
                    break;
                case nameof(Employee):
                    _employeeService.RestoreFromRecycleBin((int)restoreId);
                    break;
                case nameof(ProjectStatusRecord):
                    _projectStatusRecordService.RestoreFromRecycleBin((int)restoreId);
                    break;
                case nameof(ProjectScheduleEntry):
                    _projectScheduleEntryService.RestoreFromRecycleBin((int)restoreId);
                    break;
                case nameof(ProjectScheduleEntryType):
                    _projectScheduleEntryTypeService.RestoreFromRecycleBin((int)restoreId);
                    break;
                case nameof(TSHoursRecord):
                    _tsHoursRecordService.RestoreFromRecycleBin((int)restoreId);
                    break;
                case nameof(TSAutoHoursRecord):
                    _tsAutoHoursRecordService.RestoreFromRecycleBin((int)restoreId);
                    break;
                case nameof(Project):
                    _projectService.RestoreFromRecycleBin((int)restoreId);
                    break;
                case nameof(VacationRecord):
                    _vacationRecordService.RestoreFromRecycleBin((int)restoreId);
                    break;
            }
        }

        [HttpPost]
        [OperationActionFilter(nameof(Operation.AdminFullAccess))]
        public ActionResult RecycleBinRestore(string tableName, int? restoreId, string ids)
        {
            var listIdRows = !string.IsNullOrEmpty(ids) ? ids.Split(',').Select(Int32.Parse).ToList() : null;
            if (listIdRows != null)
                foreach (var id in listIdRows)
                {
                    if (!_serviceService.RecycleBinRestoreInTable(tableName, id))
                    {
                        Response.StatusCode = 500;
                        return Json(new
                        {
                            Message =
                                $"Id - {id} - Невозможно восстановить, так как восстанавливаемый элемент ссылается на другие элементы, которые находятся в Корзине." +
                                $" Сначала необходимо восстановить элементы, на которые ссылается данный элемент"
                        });
                    }
                }
            else
            if (!string.IsNullOrEmpty(tableName) && (restoreId != null && restoreId != 0))
                if (!_serviceService.RecycleBinRestoreInTable(tableName, restoreId.Value))
                {
                    Response.StatusCode = 500;
                    return Json(new
                    {
                        Message =
                            $"Id - {restoreId.Value} - Невозможно восстановить, так как восстанавливаемый элемент ссылается на другие элементы, которые находятся в Корзине." +
                            $" Сначала необходимо восстановить элементы, на которые ссылается данный элемент"
                    });
                }

            return RedirectToAction("RecycleBin", new { tableName = tableName });
        }

        [OperationActionFilter(nameof(Operation.TimesheetProcessingAccess))]
        [HttpPost]
        public void StartTimesheetProcessing(string id,
            bool syncWithExternalTimesheet,
            string processingSyncWithExtTSPeriodDateStart,
            string processingSyncWithExtTSPeriodDateEnd,
            bool deleteExtTSSyncedRecordsBeforeSync, bool updateExtTSAlreadyAddedRecords, bool getHoursFromExternalTimesheet, bool getVacationsFromExternalTimesheet, int? batchSaveRecordsLimitOnSyncWithExternalTS,
            bool processVacationRecords,
            bool processTSAutoHoursRecords,
            bool sendTSEmailNotifications,
            string processingSendTSEmailNotificationsAtDate,
            bool syncWithJIRA,
            string processingSyncWithJIRAPeriodDateStart,
            string processingSyncWithJIRAPeriodDateEnd,
            bool processingSyncWithJIRASendEmailNotifications,
            bool deleteJIRASyncedRecordsBeforeSync, string processingSyncWithJIRAAtDate)
        {
            //External Timesheet
            DateTime syncWithExtTSPeriodStart = DateTime.MinValue;
            DateTime syncWithExtTSPeriodEnd = DateTime.MinValue;

            try
            {
                syncWithExtTSPeriodStart = Convert.ToDateTime(processingSyncWithExtTSPeriodDateStart);
            }
            catch (Exception)
            {
                syncWithExtTSPeriodStart = DateTime.MinValue;
            }

            try
            {
                syncWithExtTSPeriodEnd = Convert.ToDateTime(processingSyncWithExtTSPeriodDateEnd);
            }
            catch (Exception)
            {
                syncWithExtTSPeriodEnd = DateTime.MinValue;
            }

            if (batchSaveRecordsLimitOnSyncWithExternalTS == null || batchSaveRecordsLimitOnSyncWithExternalTS.HasValue == false)
            {
                batchSaveRecordsLimitOnSyncWithExternalTS = 350;
            }

            //Jira

            DateTime syncWithJIRAPeriodStart = DateTime.MinValue;
            DateTime syncWithJIRAPeriodEnd = DateTime.MinValue;

            try
            {
                syncWithJIRAPeriodStart = Convert.ToDateTime(processingSyncWithJIRAPeriodDateStart);
            }
            catch (Exception)
            {
                syncWithJIRAPeriodStart = DateTime.MinValue;
            }

            try
            {
                syncWithJIRAPeriodEnd = Convert.ToDateTime(processingSyncWithJIRAPeriodDateEnd);
            }
            catch (Exception)
            {
                syncWithJIRAPeriodEnd = DateTime.MinValue;
            }



            DateTime syncWithJIRAAtDate = DateTime.Today;

            try
            {
                syncWithJIRAAtDate = Convert.ToDateTime(processingSyncWithJIRAAtDate);
            }
            catch (Exception)
            {
                syncWithJIRAAtDate = DateTime.Today;
            }

            DateTime sendTSEmailNotificationsAtDate = DateTime.Today;

            try
            {
                sendTSEmailNotificationsAtDate = Convert.ToDateTime(processingSendTSEmailNotificationsAtDate);
            }
            catch (Exception)
            {
                sendTSEmailNotificationsAtDate = DateTime.Today;
            }

            if (_taskTimesheetProcessing.Add(id, true) == true)
            {
                ProcessTimesheetProcessingTask processTask = new ProcessTimesheetProcessingTask(_taskTimesheetProcessing.ProcessLongRunningAction);
               
                string userIdentityName = User.Identity.Name;
                Task.Run(() => processTask.Invoke(userIdentityName, id,
                    syncWithExternalTimesheet,
                    syncWithExtTSPeriodStart, syncWithExtTSPeriodEnd,
                    deleteExtTSSyncedRecordsBeforeSync, updateExtTSAlreadyAddedRecords, getHoursFromExternalTimesheet, getVacationsFromExternalTimesheet, true,
                    batchSaveRecordsLimitOnSyncWithExternalTS.Value,
                    processVacationRecords,
                    processTSAutoHoursRecords,
                    sendTSEmailNotifications,
                    sendTSEmailNotificationsAtDate,
                    syncWithJIRA,
                    syncWithJIRAPeriodStart, syncWithJIRAPeriodEnd,
                    deleteJIRASyncedRecordsBeforeSync, syncWithJIRAAtDate, processingSyncWithJIRASendEmailNotifications)).ContinueWith(EndTimesheetProcessing);
            }
        }

        [OperationActionFilter(nameof(Operation.TimesheetProcessingAccess))]
        public void EndTimesheetProcessing(Task<TimesheetProcessingResult> result)
        {
            TimesheetProcessingResult timesheetProcessingResult = result.Result;

            var fileHtmlReport = string.Empty;

            foreach (var html in timesheetProcessingResult.fileHtmlReport)
            {
                fileHtmlReport += html;
            }

            _memoryCache.Set(timesheetProcessingResult.fileId, fileHtmlReport);

            _taskTimesheetProcessing.Remove(timesheetProcessingResult.fileId);
        }

        [OperationActionFilter(nameof(Operation.OOAccessAllow))]
        public ActionResult OOAuthService(string ooAuthServiceRequestMode, string docServerPassword)
        {
            ViewBag.Message = "Аутентификация на сервере хранения файлов";

            if (String.IsNullOrEmpty(ooAuthServiceRequestMode) == false)
            {
                ApplicationUser user = _applicationUserService.GetUser();

                switch (ooAuthServiceRequestMode)
                {
                    case "Auth":
                        if (String.IsNullOrEmpty(docServerPassword) == false)
                        {
                            _applicationUserService.SetOOPassword(docServerPassword);
                        }
                        break;
                    case "ClearAuth":
                        _applicationUserService.ClearOOPassword();
                        break;
                }
            }

            return View();
        }

        [OperationActionFilter(nameof(Operation.FinDataCreateUpdate))]
        public ActionResult ImportBudgetLimitRecordsFromExcel()
        {
            int budgetLimitSummaryStartYear = 2010;
            ViewBag.Years = new SelectList(Enumerable.Range(budgetLimitSummaryStartYear, DateTime.Today.Year - budgetLimitSummaryStartYear + 10).Select(x =>
               new SelectListItem()
               {
                   Text = x.ToString(),
                   Value = x.ToString(),
               }), "Value", "Text", DateTime.Today.Year);

            ViewBag.CurrentTaskId = _budgetLimitImportRecordsFromExcelTask.GetIdOfRunningSingleTask();

            return View();
        }

        [HttpPost]
        public void StartImportBudgetLimitRecordsFromExcel(string id, string reportYear, bool onlyValidate, IFormFile file)
        {
            var intReportYear = string.IsNullOrEmpty(reportYear) ? 2019 : int.Parse(reportYear);

            DataTable budgetLimitRecordsDataTable = BudgetHelper.ExportDataToDefaultTable(file.OpenReadStream());

            if (_budgetLimitImportRecordsFromExcelTask.Add(id, true) == true)
            {
                var processTask = new ProcessBudgetLimitImportTask(_budgetLimitImportRecordsFromExcelTask.ProcessLongRunningAction);
                string userIdentityName = User.Identity.Name;
                Task.Run(() => processTask.Invoke(userIdentityName, id, budgetLimitRecordsDataTable, intReportYear, onlyValidate)).ContinueWith(EndImportBudgetLimitRecordsFromExcel);
            }
        }

        public void EndImportBudgetLimitRecordsFromExcel( Task<BudgetLimitImportRecordsFromExcelResult> result)
        {
            BudgetLimitImportRecordsFromExcelResult budgetLimitResult = result.Result;
            var fileHtmlReport = string.Empty;

            foreach (var html in budgetLimitResult.FileHtmlReport)
                fileHtmlReport += html;
            _memoryCache.Set(budgetLimitResult.FileId, fileHtmlReport);

            _budgetLimitImportRecordsFromExcelTask.Remove(budgetLimitResult.FileId);
        }


        [OperationActionFilter(nameof(Operation.AdminFullAccess))]
        public ActionResult DBDataProcessingService()
        {
            ViewBag.Message = "Обработка данных БД";

            ViewBag.CurrentTaskId = _dbDataProcessingTask.GetIdOfRunningSingleTask();

            return View();
        }

        [OperationActionFilter(nameof(Operation.AdminFullAccess))]
        [HttpPost]
        public void StartDBDataProcessing(string id)
        {
            if (_dbDataProcessingTask.Add(id, true) == true)
            {
                var processTask = new ProcessDBDataProcessingTask(_dbDataProcessingTask.ProcessLongRunningAction);
                string userIdentityName = User.Identity.Name;
                Task.Run(() => processTask.Invoke(userIdentityName, id)).ContinueWith(EndDBDataProcessing);
            }
        }

        [OperationActionFilter(nameof(Operation.AdminFullAccess))]
        public void EndDBDataProcessing(Task<DBDataProcessingTaskResult> result)
        {
            DBDataProcessingTaskResult dbTaskResult = result.Result;
            var fileHtmlReport = string.Empty;

            foreach (var html in dbTaskResult.fileHtmlReport)
            {
                fileHtmlReport += html;
            }
            _memoryCache.Set(dbTaskResult.fileId, fileHtmlReport);

            _dbDataProcessingTask.Remove(dbTaskResult.fileId);
        }

    }
}