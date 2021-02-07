using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Globalization;
using System.Collections;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Threading.Tasks;
using BL.Implementation;
using Core;
using Core.BL.Interfaces;
using Core.Common;
using Core.Config;
using Core.Data;
using Core.Helpers;
using Core.Models;
using Core.Models.RBAC;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using MainApp.BitrixSync;
using MainApp.Finance;
using MainApp.RBAC.Attributes;
using MainApp.ReportGenerators;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;


using Data;
using Data.Implementation;

namespace MainApp.Controllers
{
    public class ReportsController : Controller
    {
        private readonly IProductionCalendarService _productionCalendarService;
        private readonly IProjectService _projectService;
        private readonly IEmployeeService _employeeService;
        private readonly IEmployeeQualifyingRoleService _employeeQualifyingRoleService;
        private readonly IEmployeeCategoryService _employeeCategoryService;
        private readonly IDepartmentService _departmentService;
        private readonly DbContextOptions<RPCSContext> _dbOptions;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IOptions<ADConfig> _adOptions;
        private readonly IOptions<OnlyOfficeConfig> _onlyOfficeOptions;
        private readonly IOptions<BitrixConfig> _bitrixConfig;
        private readonly IOptions<TimesheetConfig> _timesheetOptions;
        private readonly IMemoryCache _memoryCache;
        private readonly IApplicationUserService _applicationUserService;
        private readonly IFinanceService _financeService;
        private readonly IOOService _ooService;
        private readonly ILogger<TSHoursRecordService> _tsHoursRecordServiceLogger;

        delegate ReportGeneratorResult AFPReportGeneratorTaskAction(string userIdentityName, string id, DateTime periodStart, DateTime periodEnd);
        delegate ReportGeneratorResult QualifyingRoleRateReportGeneratorTaskAction(string userIdentityName, string id,
            string reportDate,
            bool? reportRecalcQualifyingRoleRates,
            int? reportHoursPlan,
            List<QualifyingRoleRateFRCCalcParamRecord> qualifyingRoleRateFRCCalcParamRecordList,
            List<int> departmentsIDs,
            DataTable employeePayrollSheetDataTable);

        delegate ReportGeneratorResult PMDKReportGeneratorTaskAction(string userIdentityName, string id, string projectShortName, DateTime periodStart, DateTime periodEnd, bool getProfTransactions);
        delegate ReportGeneratorResult DKReportGeneratorTaskAction(string userIdentityName, string id, string projectShortName, DateTime periodStart, DateTime periodEnd, bool getProfTransactions);

        delegate ReportGeneratorResult ProjectsHoursReportGeneratorTaskAction(ProjectsHoursReportParams reportParams);


        delegate ReportGeneratorResult ProjectsHoursForPMReportGeneratorTaskAction(string userIdentityName, string id, DateTime periodStart, DateTime periodEnd,
            string projectShortName);

        delegate ReportGeneratorResult TSHoursUtilizationReportGeneratorTaskAction(TSHoursUtilizationReportParams reportParams);

        private DaykassaReportGeneratorTask _taskPMDKReportGenerator;
        private DaykassaReportGeneratorTask _taskDKReportGenerator;
        private ProjectsHoursReportGeneratorTask _taskProjectsHoursReportGenerator;
        private ProjectsHoursForPMReportGeneratorTask _taskProjectsHoursForPMReportGenerator;
        private QualifyingRoleRateReportGeneratorTask _taskQualifyingRoleRateReportGenerator;
        private ApplicationForPaymentReportGeneratorTask _taskAFPReportGenerator;
        private TSHoursUtilizationReportGeneratorTask _taskTSHoursUtilizationReportGenerator;

        public ReportsController(IProductionCalendarService productionCalendarService,
            IProjectService projectService,
            IEmployeeService employeeService,
            IEmployeeQualifyingRoleService employeeQualifyingRoleService,
            IEmployeeCategoryService employeeCategoryService,
            IDepartmentService departmentService,
            ICostSubItemService costSubItemService,
            DbContextOptions<RPCSContext> dbOptions,
            IHttpContextAccessor httpContextAccessor,
            IQualifyingRoleService qualifyingRoleService,
            IQualifyingRoleRateService qualifyingRoleRateService,
            IOptions<ADConfig> adOptions,
            IOptions<OnlyOfficeConfig> onlyOfficeOptions,
            IOptions<BitrixConfig> bitrixConfig,
            IOptions<TimesheetConfig> timesheetOptions,
            IMemoryCache memoryCache,
            IApplicationUserService applicationUserService, IFinanceService financeService, IOOService ooService, ILogger<TSHoursRecordService> tsHoursRecordServiceLogger)

        {
            _productionCalendarService = productionCalendarService;
            _projectService = projectService;
            _employeeService = employeeService;
            _employeeQualifyingRoleService = employeeQualifyingRoleService;
            _employeeCategoryService = employeeCategoryService;
            _departmentService = departmentService;
            _dbOptions = dbOptions;
            _httpContextAccessor = httpContextAccessor;
            _adOptions = adOptions;
            _onlyOfficeOptions = onlyOfficeOptions;
            _bitrixConfig = bitrixConfig;
            _timesheetOptions = timesheetOptions;
            _memoryCache = memoryCache;
            _applicationUserService = applicationUserService;
            _financeService = financeService;
            _ooService = ooService;
            _tsHoursRecordServiceLogger = tsHoursRecordServiceLogger;

            InitTasks();
        }

        protected void InitTasks()
        {
            var iRPCSDbAccessor = (IRPCSDbAccessor)new RPCSSingletonDbAccessor(_dbOptions);
            var rPCSRepositoryFactory = (IRepositoryFactory)new RPCSRepositoryFactory(iRPCSDbAccessor);

            var projectTypeService = new ProjectTypeService(rPCSRepositoryFactory);
            var userService = new UserService(rPCSRepositoryFactory, _httpContextAccessor);
            var departmentService = new DepartmentService(rPCSRepositoryFactory, userService);
            var projectService = new ProjectService(rPCSRepositoryFactory, userService);
            var employeeService = new EmployeeService(rPCSRepositoryFactory, departmentService, userService);
            var employeeCategoryService = new EmployeeCategoryService(rPCSRepositoryFactory);
            var costSubItemService = new CostSubItemService(rPCSRepositoryFactory, userService);
            var qualifyingRoleService = new QualifyingRoleService(rPCSRepositoryFactory);
            var qualifyingRoleRateService = new QualifyingRoleRateService(rPCSRepositoryFactory);
            var employeeQualifyingRoleService = new EmployeeQualifyingRoleService(rPCSRepositoryFactory);
            var tsAutoHoursRecord = new TSAutoHoursRecordService(rPCSRepositoryFactory, userService);
            var tsHoursRecordService = new TSHoursRecordService(rPCSRepositoryFactory, userService, _tsHoursRecordServiceLogger);
            var projectReportRecordService = new ProjectReportRecordService(rPCSRepositoryFactory);
            var vacationRecordService = new VacationRecordService(rPCSRepositoryFactory, userService);
            var productCalendarService = new ProductionCalendarService(rPCSRepositoryFactory);
            var appPropertyService = new AppPropertyService(rPCSRepositoryFactory, _adOptions, _bitrixConfig, _onlyOfficeOptions, _timesheetOptions);
            var applicationUserService = new ApplicationUserService(rPCSRepositoryFactory, employeeService, userService, departmentService, _httpContextAccessor, _memoryCache, projectService, _onlyOfficeOptions);
            var ooService = new OOService(applicationUserService, _onlyOfficeOptions);
            var financeService = new FinanceService(rPCSRepositoryFactory, iRPCSDbAccessor, applicationUserService, appPropertyService, ooService);
            var timesheetService = new TimesheetService(employeeService, employeeCategoryService, tsAutoHoursRecord, tsHoursRecordService, projectService, projectReportRecordService, vacationRecordService,
                productCalendarService, financeService, _timesheetOptions);


            _taskPMDKReportGenerator = new DaykassaReportGeneratorTask();
            _taskDKReportGenerator = new DaykassaReportGeneratorTask();
            _taskProjectsHoursReportGenerator = new ProjectsHoursReportGeneratorTask(timesheetService);
            _taskProjectsHoursForPMReportGenerator = new ProjectsHoursForPMReportGeneratorTask(timesheetService);
            _taskAFPReportGenerator = new ApplicationForPaymentReportGeneratorTask(projectService, departmentService, costSubItemService, _bitrixConfig);
            _taskQualifyingRoleRateReportGenerator = new QualifyingRoleRateReportGeneratorTask(qualifyingRoleService, employeeCategoryService, qualifyingRoleRateService,
                employeeQualifyingRoleService, employeeService, departmentService, financeService);
            _taskTSHoursUtilizationReportGenerator = new TSHoursUtilizationReportGeneratorTask(departmentService, employeeService, projectTypeService, projectService, tsHoursRecordService);

        }

        public ActionResult Index()
        {
            return View();
        }

        [AProjectsHoursReportView]
        public ActionResult ProjectsHoursReport()
        {
            ViewBag.Message = "Отчет по трудозатратам проектов";
            ViewBag.Years = _productionCalendarService.Get(x => x.ToList()).Select(x => new { x.Year }).Distinct().OrderBy(x => x.Year).ToList();

            ViewBag.Months = Enumerable.Empty<SelectListItem>();

            string currentTaskId = _taskProjectsHoursReportGenerator.GetIdOfRunningSingleTask();

            if (String.IsNullOrEmpty(currentTaskId) == false)
            {
                ViewBag.CurrentTaskId = _taskProjectsHoursReportGenerator.GetIdOfRunningSingleTask();
                ViewBag.CurrentTaskProgress = LongRunningTaskBase.GetStatus(currentTaskId).ToString();
            }

            return View();
        }

        [HttpPost]
        public ActionResult GetMonths(int year)
        {
            Hashtable ht = GetMonthsWorkingHours(year);
            List<int> keys = ht.Keys.OfType<int>().ToList();
            keys.Sort();

            var items = new List<SelectListItem>();

            int commonHours = 0;
            foreach (int key in keys)
            {
                items.Add(new SelectListItem
                {
                    Text = string.Format("{0} ({1}ч)", monthAsString(key), ht[key].ToString()),
                    Value = string.Format("{0}.{1}|{2}", key.ToString().PadLeft(2, '0'), year, ht[key].ToString())
                });
                commonHours = commonHours + (int)ht[key];
            }

            items.Add(new SelectListItem
            {
                Text = string.Format("все месяцы ({0}ч)", commonHours.ToString()),
                Value = string.Format("*.{0}|{1}", year, commonHours.ToString())
            });

            return Json(items);
        }

        Hashtable GetMonthsWorkingHours(int year)
        {
            var records = _productionCalendarService.Get(x => x.Where(m => (m.Year == year)).ToList());

            Hashtable ht = new Hashtable();
            foreach (var rec in records)
            {
                if (!ht.Contains(rec.Month))
                {
                    ht.Add(rec.Month, rec.WorkingHours);
                }
                else
                {
                    int sum = (int)ht[rec.Month] + rec.WorkingHours;
                    ht[rec.Month] = sum;
                }
            }
            return ht;
        }

        string monthAsString(int monthNumber)
        {
            return CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(monthNumber).ToLower();
        }

        [OperationActionFilter(nameof(Operation.ProjectsCostsReportView))]
        public ActionResult ProjectsCostsReport()
        {
            ViewBag.Message = "Отчет по затратам проектов";
            ViewBag.Years = _productionCalendarService.Get(x => x.ToList()).Select(x => new { x.Year }).Distinct().OrderBy(x => x.Year).ToList();
            ViewBag.Months = Enumerable.Empty<SelectListItem>();

            string currentTaskId = _taskProjectsHoursReportGenerator.GetIdOfRunningSingleTask();

            if (String.IsNullOrEmpty(currentTaskId) == false)
            {
                ViewBag.CurrentTaskId = _taskProjectsHoursReportGenerator.GetIdOfRunningSingleTask();
                ViewBag.CurrentTaskProgress = LongRunningTaskBase.GetStatus(currentTaskId).ToString();
            }

            return View();
        }

        [AProjectDetailsView]
        public ActionResult ProjectsHoursForPMReport(int? projectID)
        {
            ViewBag.Message = "Отчет по трудозатратам для руководителя проектов";


            if (projectID == null)
                return StatusCode(StatusCodes.Status400BadRequest);

            ViewBag.PMProjectsFromDB = _projectService.Get(x => x.Where(p => p.ID == projectID).ToList());

            return View();
        }

        [AProjectsHoursReportView]
        [HttpPost]
        public void StartGenerateProjectsHoursReport(string id,
            string reportYear,
            string reportPeriodMode,
            string reportPeriod,
            string reportPeriodEnd,
            string reportPeriodDateStart,
            string reportPeriodDateEnd,
            bool groupByMonth,
            bool useTSHoursRecordsOnly,
            bool useTSHoursRecords,
            bool useTSAutoHoursRecords,
            bool saveResultsInDB,
            bool addToReportNotInDBEmplyees)
        {
            List<int> departmentsIDs = null;


            if (!_applicationUserService.HasAccess(Operation.AdminFullAccess)
                && _applicationUserService.HasAccess(Operation.ProjectsHoursReportViewForManagedEmployees))
            {

                departmentsIDs = _applicationUserService.GetUser().ManagedDepartments.Select(x => x.ID).ToList();
            }

            DateTime periodStart = DateTime.MinValue;
            DateTime periodEnd = DateTime.MinValue;

            try
            {
                periodStart = Convert.ToDateTime(reportPeriodDateStart);
            }
            catch (Exception)
            {
                periodStart = DateTime.MinValue;
            }

            try
            {
                periodEnd = Convert.ToDateTime(reportPeriodDateEnd);
            }
            catch (Exception)
            {
                periodEnd = DateTime.MinValue;
            }

            var reportParams = new ProjectsHoursReportParams
            {
                UserIdentityName = User.Identity.Name,
                ID = id,
                GroupByMonth = groupByMonth,
                SaveResultsInDB = saveResultsInDB,
                UseTSHoursRecordsOnly = useTSHoursRecordsOnly,
                UseTSHoursRecords = useTSHoursRecords,
                UseTSAutoHoursRecords = useTSAutoHoursRecords,
                AddToReportNotInDBEmplyees = addToReportNotInDBEmplyees,
                ShowEmployeeDataInReport = true,
                GetDataFromDaykassa = false,
                EmployeePayrollSheetDataTable = null,
                ProjectsOtherCostsSheetDataTable = null,
                DepartmentsIDs = departmentsIDs
            };

            if (reportPeriodMode.Equals("fixedPeriod") == true)
            {
                if (_taskProjectsHoursReportGenerator.Add(id, saveResultsInDB) == true)
                {
                    ProjectsHoursReportGeneratorTaskAction processTask = new ProjectsHoursReportGeneratorTaskAction(_taskProjectsHoursReportGenerator.ProcessLongRunningAction);

                    reportParams.PeriodStart = reportPeriod;
                    reportParams.PeriodEnd = reportPeriodEnd;
                    reportParams.PeriodStartDate = DateTime.MinValue;
                    reportParams.PeriodEndDate = DateTime.MinValue;
                    reportParams.MonthsWorkingHours = GetMonthsWorkingHours(int.Parse(reportYear));
                    //processTask.BeginInvoke(reportParams, new AsyncCallback(EndGenerateProjectsHoursReport), processTask);
                    Task.Run(() => processTask.Invoke(reportParams)).ContinueWith(EndGenerateProjectsHoursReport);
                }
            }

            if (reportPeriodMode.Equals("dateBeginEndPeriod") == true
                && periodStart.Equals(DateTime.MinValue) == false
                && periodEnd.Equals(DateTime.MinValue) == false)
            {
                if (_taskProjectsHoursReportGenerator.Add(id, saveResultsInDB) == true)
                {
                    ProjectsHoursReportGeneratorTaskAction processTask = new ProjectsHoursReportGeneratorTaskAction(_taskProjectsHoursReportGenerator.ProcessLongRunningAction);

                    reportParams.PeriodStart = null;
                    reportParams.PeriodEnd = null;
                    reportParams.PeriodStartDate = periodStart;
                    reportParams.PeriodEndDate = periodEnd;
                    //processTask.BeginInvoke(reportParams, new AsyncCallback(EndGenerateProjectsHoursReport), processTask);
                    Task.Run(() => processTask.Invoke(reportParams)).ContinueWith(EndGenerateProjectsHoursReport);
                }
            }
        }


        [AProjectsHoursReportView]
        public void EndGenerateProjectsHoursReport(/*IAsyncResult*/ Task<ReportGeneratorResult> result)
        {
            //ProjectsHoursReportGeneratorTaskAction processTask = (ProjectsHoursReportGeneratorTaskAction)result.AsyncState;
            ReportGeneratorResult reportGeneratorResult = result.Result; //processTask.EndInvoke(result);

            InsertReportGeneratorResultInCache(reportGeneratorResult);

            _taskProjectsHoursReportGenerator.Remove(reportGeneratorResult.fileId);
        }


        [OperationActionFilter(nameof(Operation.ProjectsCostsReportView))]
        [HttpPost]
        public void StartGenerateProjectsCostsReport(string id,
            string reportPeriodMode,
            string reportPeriod,
            string reportPeriodDateStart,
            string reportPeriodDateEnd,
            bool useTSHoursRecordsOnly,
            bool useTSHoursRecords,
            bool useTSAutoHoursRecords,
            bool saveResultsInDB,
            bool getDataFromDaykassa,
            string docServerLogin,
            string docServerPassword,
            string employeePayrollSheetFileUrl,
            IFormFile employeePayrollSheetUpload,
            string otherProjectsCostsSheetFileUrl,
            IFormFile otherProjectsCostsSheetUpload)
        {
            DataTable employeePayrollSheetDataTable = new DataTable();
            employeePayrollSheetDataTable.Columns.Add("ADEmployeeID", typeof(string)).Caption = "EmployeeID";
            employeePayrollSheetDataTable.Columns.Add("EmployeeFullName", typeof(string)).Caption = "ФИО";
            employeePayrollSheetDataTable.Columns.Add("PayrollChangeDate", typeof(DateTime)).Caption = "Дата";
            employeePayrollSheetDataTable.Columns.Add("PayrollValue", typeof(double)).Caption = "КОТ";
            employeePayrollSheetDataTable.Columns.Add("Comments", typeof(string)).Caption = "Комментарий";

            Stream employeePayrollSheetStream = null;

            ApplicationUser user = _applicationUserService.GetUser();

            if (String.IsNullOrEmpty(docServerLogin) == true)
            {
                docServerLogin = user.OOLogin;
            }

            if (String.IsNullOrEmpty(docServerPassword) == true)
            {
                docServerPassword = user.OOPassword;
            }

            if (String.IsNullOrEmpty(docServerLogin) == false
                && String.IsNullOrEmpty(docServerPassword) == false)
            {
                if (String.IsNullOrEmpty(employeePayrollSheetFileUrl) == true)
                {
                    employeePayrollSheetFileUrl = _financeService.GetOODefalutCPFileUrl();
                }

                if (String.IsNullOrEmpty(employeePayrollSheetFileUrl) == false)
                {
                    byte[] employeePayrollSheetFileBinData = _ooService.DownloadFile(employeePayrollSheetFileUrl, docServerLogin, docServerPassword);

                    if (employeePayrollSheetFileBinData != null)
                    {
                        employeePayrollSheetStream = new MemoryStream(employeePayrollSheetFileBinData);
                    }
                }
            }

            if (employeePayrollSheetStream == null
                && employeePayrollSheetUpload != null
                && employeePayrollSheetUpload.OpenReadStream() != null)
            {
                employeePayrollSheetStream = employeePayrollSheetUpload.OpenReadStream();
            }

            if (employeePayrollSheetStream != null)
            {
                employeePayrollSheetDataTable = ExcelHelper.ExportData(employeePayrollSheetDataTable, employeePayrollSheetStream);
            }

            DataTable projectsOtherCostsSheetDataTable = new DataTable();
            projectsOtherCostsSheetDataTable.Columns.Add("ADEmployeeID", typeof(string)).Caption = "EmployeeID";
            projectsOtherCostsSheetDataTable.Columns.Add("EmployeeFullName", typeof(string)).Caption = "ФИО";
            projectsOtherCostsSheetDataTable.Columns.Add("RecordDate", typeof(DateTime)).Caption = "Месяц";
            projectsOtherCostsSheetDataTable.Columns.Add("ProjectShortName", typeof(string)).Caption = "Код проекта";
            projectsOtherCostsSheetDataTable.Columns.Add("PerformanceBonusValue", typeof(double)).Caption = "Performance Bonus/Премия";
            projectsOtherCostsSheetDataTable.Columns.Add("OvertimePayrollValue", typeof(double)).Caption = "С/У";
            projectsOtherCostsSheetDataTable.Columns.Add("OvertimePayrollRate", typeof(double)).Caption = "Коэффициент С/У";
            projectsOtherCostsSheetDataTable.Columns.Add("OtherCostsValue", typeof(double)).Caption = "Расходы";
            projectsOtherCostsSheetDataTable.Columns.Add("Comments", typeof(string)).Caption = "Комментарий";

            Stream otherProjectsCostsSheetStream = null;

            if (String.IsNullOrEmpty(otherProjectsCostsSheetFileUrl) == false
                && String.IsNullOrEmpty(docServerLogin) == false
                && String.IsNullOrEmpty(docServerPassword) == false)
            {
                byte[] otherProjectsCostsSheetFileBinData = _ooService.DownloadFile(otherProjectsCostsSheetFileUrl, docServerLogin, docServerPassword);

                if (otherProjectsCostsSheetFileBinData != null)
                {
                    otherProjectsCostsSheetStream = new MemoryStream(otherProjectsCostsSheetFileBinData);
                }
            }

            if (otherProjectsCostsSheetStream == null
                && otherProjectsCostsSheetUpload != null
                && otherProjectsCostsSheetUpload.OpenReadStream() != null)
            {
                otherProjectsCostsSheetStream = otherProjectsCostsSheetUpload.OpenReadStream();
            }

            if (otherProjectsCostsSheetStream != null)
            {
                projectsOtherCostsSheetDataTable = ExcelHelper.ExportData(projectsOtherCostsSheetDataTable, otherProjectsCostsSheetStream);
            }

            DateTime periodStart = DateTime.MinValue;
            DateTime periodEnd = DateTime.MinValue;

            try
            {
                periodStart = Convert.ToDateTime(reportPeriodDateStart);
            }
            catch (Exception)
            {
                periodStart = DateTime.MinValue;
            }

            try
            {
                periodEnd = Convert.ToDateTime(reportPeriodDateEnd);
            }
            catch (Exception)
            {
                periodEnd = DateTime.MinValue;
            }


            var reportParams = new ProjectsHoursReportParams
            {
                UserIdentityName = User.Identity.Name,
                ID = id,
                UseTSHoursRecordsOnly = useTSHoursRecordsOnly,
                UseTSHoursRecords = useTSHoursRecords,
                UseTSAutoHoursRecords = useTSAutoHoursRecords,
                SaveResultsInDB = saveResultsInDB,
                AddToReportNotInDBEmplyees = false,
                GetDataFromDaykassa = getDataFromDaykassa,
                EmployeePayrollSheetDataTable = employeePayrollSheetDataTable,
                ProjectsOtherCostsSheetDataTable = projectsOtherCostsSheetDataTable,
                DepartmentsIDs = null
            };

            reportParams.ShowEmployeeDataInReport = false;

            if (_applicationUserService.HasAccess(Operation.OOAccessFullPayrollAccess) == true
                || _applicationUserService.HasAccess(Operation.OOAccessFullReadPayrollAccess) == true)
            {
                reportParams.ShowEmployeeDataInReport = true;
            }

            if (reportPeriodMode.Equals("fixedPeriod") == true)
            {
                if (_taskProjectsHoursReportGenerator.Add(id, saveResultsInDB) == true)
                {
                    ProjectsHoursReportGeneratorTaskAction processTask = new ProjectsHoursReportGeneratorTaskAction(_taskProjectsHoursReportGenerator.ProcessLongRunningAction);

                    reportParams.PeriodStart = reportPeriod;
                    reportParams.PeriodEnd = null;
                    reportParams.PeriodStartDate = DateTime.MinValue;
                    reportParams.PeriodEndDate = DateTime.MinValue;
                    //processTask.BeginInvoke(reportParams, new AsyncCallback(EndGenerateProjectsCostsReport), processTask);
                    Task.Run(() => processTask.Invoke(reportParams)).ContinueWith(EndGenerateProjectsCostsReport);
                }
            }

            if (reportPeriodMode.Equals("dateBeginEndPeriod") == true
                && periodStart.Equals(DateTime.MinValue) == false
                && periodEnd.Equals(DateTime.MinValue) == false)
            {
                if (_taskProjectsHoursReportGenerator.Add(id, saveResultsInDB) == true)
                {
                    ProjectsHoursReportGeneratorTaskAction processTask = new ProjectsHoursReportGeneratorTaskAction(_taskProjectsHoursReportGenerator.ProcessLongRunningAction);

                    reportParams.PeriodStart = null;
                    reportParams.PeriodEnd = null;
                    reportParams.PeriodStartDate = periodStart;
                    reportParams.PeriodEndDate = periodEnd;
                    //processTask.BeginInvoke(reportParams, new AsyncCallback(EndGenerateProjectsCostsReport), processTask);
                    Task.Run(() => processTask.Invoke(reportParams)).ContinueWith(EndGenerateProjectsCostsReport);
                }
            }
        }


        [OperationActionFilter(nameof(Operation.ProjectsCostsReportView))]
        public void EndGenerateProjectsCostsReport(/*IAsyncResult*/Task<ReportGeneratorResult> result)
        {
            //ProjectsHoursReportGeneratorTaskAction processTask = (ProjectsHoursReportGeneratorTaskAction)result.AsyncState;
            ReportGeneratorResult reportGeneratorResult = result.Result; //processTask.EndInvoke(result);

            InsertReportGeneratorResultInCache(reportGeneratorResult);

            _taskProjectsHoursReportGenerator.Remove(reportGeneratorResult.fileId);
        }

        [AProjectDetailsView]
        [HttpPost]
        public void StartGenerateProjectsHoursForPMReport(string id, int? projectID,
            string reportPeriodDateStart,
            string reportPeriodDateEnd)
        {

            _taskProjectsHoursForPMReportGenerator.Add(id, false);
            ProjectsHoursForPMReportGeneratorTaskAction processTask = new ProjectsHoursForPMReportGeneratorTaskAction(_taskProjectsHoursForPMReportGenerator.ProcessLongRunningAction);

            string projectShortName = null;

            if (projectID != null)
            {
                projectShortName = _projectService.GetById((int)projectID).ShortName;
            }

            DateTime periodStart = Convert.ToDateTime(reportPeriodDateStart);
            DateTime periodEnd = Convert.ToDateTime(reportPeriodDateEnd);

            processTask.BeginInvoke(User.Identity.Name, id, periodStart, periodEnd, projectShortName, new AsyncCallback(EndGenerateProjectsHoursForPMReport), processTask);
        }

        /*[AProjectDetailsView]*/
        public void EndGenerateProjectsHoursForPMReport(IAsyncResult result)
        {
            ProjectsHoursForPMReportGeneratorTaskAction processTask = (ProjectsHoursForPMReportGeneratorTaskAction)result.AsyncState;
            ReportGeneratorResult reportGeneratorResult = processTask.EndInvoke(result);

            if (reportGeneratorResult.fileBinData != null)
            {
                _memoryCache.Set(reportGeneratorResult.fileId, reportGeneratorResult.fileBinData.ToArray());
            }

            _taskProjectsHoursForPMReportGenerator.Remove(reportGeneratorResult.fileId);
        }

        [OperationActionFilter(nameof(Operation.EmployeePayrollReportView))]
        public ActionResult EmployeeEnrollmentReport()
        {
            ViewBag.Message = "Отчет о найме сотрудников";

            return View();
        }

        [OperationActionFilter(nameof(Operation.EmployeePayrollReportView))]
        [HttpPost]
        public FileContentResult EmployeeEnrollmentReportGenerate(string reportPeriodDateStart,
            string reportPeriodDateEnd)
        {

            ApplicationUser user = _applicationUserService.GetUser();

            byte[] binData = null;

            DateTime periodStart = DateTime.MinValue;
            DateTime periodEnd = DateTime.MinValue;

            try
            {
                periodStart = Convert.ToDateTime(reportPeriodDateStart);
            }
            catch (Exception)
            {
                periodStart = DateTime.MinValue;
            }

            try
            {
                periodEnd = Convert.ToDateTime(reportPeriodDateEnd);
            }
            catch (Exception)
            {
                periodEnd = DateTime.MinValue;
            }

            DataTable employeePayrollSheetDataTable = _financeService.GetEmployeePayrollSheetDataTableFromOO(user, false);

            BitrixHelper bitrixHelper = new BitrixHelper(_bitrixConfig);

            List<BitrixReqEmployeeEnrollment> bitrixReqEmployeeEnrollmentList = bitrixHelper.GetBitrixReqEmployeeEnrollmentList();
            List<BitrixReqPayrollChange> bitrixReqPayrollChangeList = bitrixHelper.GetBitrixReqPayrollChangeList();

            List<Employee> employeeList = null;

            if (_applicationUserService.HasAccess(Operation.OOAccessFullPayrollAccess) == true
                || _applicationUserService.HasAccess(Operation.OOAccessFullReadPayrollAccess) == true)
            {
                employeeList = _employeeService.Get(x => x.ToList()).ToList();
            }
            else if (_applicationUserService.HasAccess(Operation.OOAccessSubEmplReadPayrollAccess) == true)
            {
                employeeList = _employeeService.GetAllManagedEmployees(_applicationUserService.GetUser().ManagedDepartments).ToList();
            }
            else
            {
                employeeList = new List<Employee>();
            }

            employeeList = employeeList.Where(e => /*e.EnrollmentDate >= periodStart &&*/ e.EnrollmentDate <= periodEnd).OrderBy(e => e.EnrollmentDate).ToList();

            DataTable dataTable = new DataTable();
            dataTable.Columns.Add("ReqEnrollmentNum", typeof(string)).Caption = "№ заявки";
            dataTable.Columns["ReqEnrollmentNum"].ExtendedProperties["Width"] = (double)10;
            dataTable.Columns.Add("FCDepartmentShortName", typeof(int)).Caption = "Код";
            dataTable.Columns["FCDepartmentShortName"].ExtendedProperties["Width"] = (double)6;
            dataTable.Columns.Add("EmployeeTitle", typeof(string)).Caption = "Фамилия Имя Отчество";
            dataTable.Columns["EmployeeTitle"].ExtendedProperties["Width"] = (double)39;
            dataTable.Columns.Add("EmployeeQualifyingRoleType", typeof(string)).Caption = "Характеристика планируемой деятельности";
            dataTable.Columns["EmployeeQualifyingRoleType"].ExtendedProperties["Width"] = (double)9;
            dataTable.Columns.Add("Date", typeof(DateTime)).Caption = "Дата";
            dataTable.Columns["Date"].ExtendedProperties["Width"] = (double)11;
            dataTable.Columns.Add("EmployeeGrad", typeof(int)).Caption = "Грейд";
            dataTable.Columns["EmployeeGrad"].ExtendedProperties["Width"] = (double)8;

            dataTable.Columns.Add("EmployeePayrollProbation", typeof(int)).Caption = "Выплаты ИС итого";
            dataTable.Columns["EmployeePayrollProbation"].ExtendedProperties["Width"] = (double)10;
            dataTable.Columns.Add("PaymentMethodProbation", typeof(string)).Caption = "Способ выплат ИС";
            dataTable.Columns["PaymentMethodProbation"].ExtendedProperties["Width"] = (double)20;
            dataTable.Columns.Add("EmployeeProbationPeriod", typeof(int)).Caption = "ИС";
            dataTable.Columns["EmployeeProbationPeriod"].ExtendedProperties["Width"] = (double)8;

            dataTable.Columns.Add("EmployeePayroll", typeof(int)).Caption = "Выплаты итого";
            dataTable.Columns["EmployeePayroll"].ExtendedProperties["Width"] = (double)10;
            dataTable.Columns.Add("PaymentMethod", typeof(string)).Caption = "Способ выплат";
            dataTable.Columns["PaymentMethod"].ExtendedProperties["Width"] = (double)20;

            dataTable.Columns.Add("Organisation", typeof(string)).Caption = "Компания";
            dataTable.Columns["Organisation"].ExtendedProperties["Width"] = (double)20;

            dataTable.Columns.Add("AdditionallyInfo", typeof(string)).Caption = "Доп. отметки";
            dataTable.Columns["AdditionallyInfo"].ExtendedProperties["Width"] = (double)20;

            dataTable.Columns.Add("_ISGROUPROW_", typeof(bool)).Caption = "_ISGROUPROW_";

            foreach (Employee employee in employeeList)
            {
                string fcDepartmentShortName = "";
                int fcFepartmentShortNameIntValue = 0;

                try
                {
                    Department fcDepartment = employee.Department;

                    while (fcDepartment.ParentDepartment != null && fcDepartment.IsFinancialCentre == false)
                    {
                        fcDepartment = fcDepartment.ParentDepartment;
                    }

                    if (fcDepartment.IsFinancialCentre == true)
                    {
                        fcDepartmentShortName = fcDepartment.ShortName;
                    }

                    fcFepartmentShortNameIntValue = Convert.ToInt32(fcDepartmentShortName);
                }
                catch (Exception)
                {
                    fcFepartmentShortNameIntValue = 0;
                }

                DateTime employeePayrollChangeMinDate = DateTime.MinValue;

                if (employee.EnrollmentDate != null && employee.EnrollmentDate.HasValue == true)
                {
                    employeePayrollChangeMinDate = employee.EnrollmentDate.Value.Date.AddDays(-20);
                }

                string employeePayrollPaymentMethod = "";
                string employeePayrollAdditionallyInfo = "";

                    string reqEnrollmentNum = "";
                    int? employeeProbationPeriod = null;

                    BitrixReqEmployeeEnrollment bitrixReqEmployeeEnrollment = bitrixReqEmployeeEnrollmentList.Where(x => x.FULL_NAME != null
                        && x.FULL_NAME.FirstOrDefault().Value.Trim() == employee.FullName).FirstOrDefault();

                    if (bitrixReqEmployeeEnrollment != null)
                    {
                        reqEnrollmentNum = bitrixReqEmployeeEnrollment.NAME;

                        try
                        {
                            if (bitrixReqEmployeeEnrollment.TRIAL_PERIOD != null
                                && String.IsNullOrEmpty(bitrixReqEmployeeEnrollment.TRIAL_PERIOD.FirstOrDefault().Value) == false)
                            {
                                employeeProbationPeriod = Convert.ToInt32(bitrixReqEmployeeEnrollment.TRIAL_PERIOD.FirstOrDefault().Value);
                            }
                        }
                        catch (Exception)
                        {

                        }
                    }

                    List<EmployeePayrollRecord> recordsEnrollment = _financeService.GetEmployeePayrollRecordsFromDataTable(employeePayrollSheetDataTable, employee)
                        .Where(r => r.PayrollChangeDate >= employeePayrollChangeMinDate && r.RecordType != EmployeePayrollRecordType.PayrollChange)
                        .OrderBy(r => r.PayrollChangeDate).ToList();
                    if (recordsEnrollment != null && recordsEnrollment.Count() != 0)
                    {
                        if (recordsEnrollment.Count() >= 2)
                        {
                            employeePayrollPaymentMethod = recordsEnrollment[1].PaymentMethod;
                            employeePayrollAdditionallyInfo = recordsEnrollment[1].AdditionallyInfo;
                        }
                        else
                        {
                            employeePayrollPaymentMethod = recordsEnrollment[0].PaymentMethod;
                            employeePayrollAdditionallyInfo = recordsEnrollment[0].AdditionallyInfo;
                        }
                    }

                    if (employee.EnrollmentDate != null
                        && employee.EnrollmentDate >= periodStart && employee.EnrollmentDate <= periodEnd)
                    {
                    if (recordsEnrollment != null && recordsEnrollment.Count() != 0)
                    {
                        if (recordsEnrollment.Count() >= 2)
                        {
                            dataTable.Rows.Add(reqEnrollmentNum, fcFepartmentShortNameIntValue,
                                employee.FullName,
                                "",
                                employee.EnrollmentDate,
                                null,
                                Convert.ToInt32(recordsEnrollment[0].PayrollValue / 1000),
                                recordsEnrollment[0].PaymentMethod,
                                employeeProbationPeriod,
                                Convert.ToInt32(recordsEnrollment[1].PayrollValue / 1000),
                                recordsEnrollment[1].PaymentMethod,
                                ((employee.Organisation != null) ? employee.Organisation.Title : ""),
                                recordsEnrollment[1].AdditionallyInfo);
                        }
                        else
                        {
                            dataTable.Rows.Add(reqEnrollmentNum, fcFepartmentShortNameIntValue,
                                employee.FullName,
                                "",
                                employee.EnrollmentDate,
                                null,
                                Convert.ToInt32(recordsEnrollment[0].PayrollValue / 1000),
                                recordsEnrollment[0].PaymentMethod,
                                employeeProbationPeriod,
                                Convert.ToInt32(recordsEnrollment[0].PayrollValue / 1000),
                                recordsEnrollment[0].PaymentMethod,
                                ((employee.Organisation != null) ? employee.Organisation.Title : ""),
                                recordsEnrollment[0].AdditionallyInfo);
                        }
                    }
                    else
                    {
                        dataTable.Rows.Add(reqEnrollmentNum, fcFepartmentShortNameIntValue,
                                employee.FullName,
                                "",
                                employee.EnrollmentDate,
                                null,
                                0,
                                "-",
                                employeeProbationPeriod,
                                0,
                                "-",
                                ((employee.Organisation != null) ? employee.Organisation.Title : ""),
                                "-");
                    }

                    if (employee.EmployeeGradID != null && employee.EmployeeGrad != null)
                    {
                        int employeeGradShortNameIntValue = 0;

                        try
                        {
                            employeeGradShortNameIntValue = Convert.ToInt32(employee.EmployeeGrad.ShortName);
                        }
                        catch (Exception)
                        {
                            employeeGradShortNameIntValue = 0;
                        }

                        if (employeeGradShortNameIntValue != 0)
                        {
                            dataTable.Rows[dataTable.Rows.Count - 1]["EmployeeGrad"] = employeeGradShortNameIntValue;
                        }
                    }

                    //string employeeQualifyingRoleTitle = "";
                    string employeeQualifyingRoleType = "";

                    EmployeeQualifyingRole employeeQualifyingRole = _employeeQualifyingRoleService.Get(x => x
                        .Where(eqr => eqr.EmployeeID == employee.ID).OrderBy(eqr => eqr.QualifyingRoleDateEnd).Where(eqr =>
                            eqr.QualifyingRoleDateBegin != null
                            && eqr.QualifyingRoleDateBegin <= employee.EnrollmentDate
                            && (eqr.QualifyingRoleDateEnd == null || eqr.QualifyingRoleDateEnd >= employee.EnrollmentDate))
                        .ToList()).FirstOrDefault();

                    if (employeeQualifyingRole != null)
                    {
                        //employeeQualifyingRoleTitle = employeeQualifyingRole.QualifyingRole.FullName;
                        employeeQualifyingRoleType = ((DisplayAttribute)(employeeQualifyingRole.QualifyingRole.RoleType.GetType().GetMember(employeeQualifyingRole.QualifyingRole.RoleType.ToString()).First().GetCustomAttributes(true)[0])).Name;

                        //dataTable.Rows[dataTable.Rows.Count - 1]["EmployeeQualifyingRoleTitle"] = employeeQualifyingRoleTitle;
                        dataTable.Rows[dataTable.Rows.Count - 1]["EmployeeQualifyingRoleType"] = employeeQualifyingRoleType;
                    }
                }

                List<EmployeePayrollRecord> recordsPayrollChange = _financeService.GetEmployeePayrollRecordsFromDataTable(employeePayrollSheetDataTable, employee)
                        .Where(r => r.PayrollChangeDate >= employeePayrollChangeMinDate && r.RecordType == EmployeePayrollRecordType.PayrollChange
                        && r.PayrollValue != null && r.PayrollValue != 0
                        && r.PayrollChangeDate >= periodStart && r.PayrollChangeDate <= periodEnd)
                        .OrderBy(r => r.PayrollChangeDate).ToList();

                if (recordsPayrollChange != null && recordsPayrollChange.Count() != 0)
                {
                    foreach (var record in recordsPayrollChange)
                    {
                        string reqPayrollChangeNum = "";
                        if (String.IsNullOrEmpty(record.SourceElementID) == false)
                        {
                            BitrixReqPayrollChange bitrixReqPayrollChange = bitrixReqPayrollChangeList.Where(x => x.ID == record.SourceElementID).FirstOrDefault();

                            if (bitrixReqPayrollChange != null)
                            {
                                reqPayrollChangeNum = bitrixReqPayrollChange.NAME;
                            }
                        }

                        int payrollValueIntValue = 0;

                        try
                        {
                            payrollValueIntValue = Convert.ToInt32(record.PayrollValue / 1000);
                        }
                        catch (Exception)
                        {
                            payrollValueIntValue = 0;
                        }

                        dataTable.Rows.Add(reqPayrollChangeNum, fcFepartmentShortNameIntValue,
                                employee.FullName,
                                "",
                                record.PayrollChangeDate,
                                record.EmployeeGrad,
                                0,
                                employeePayrollPaymentMethod,
                                0,
                                payrollValueIntValue,
                                employeePayrollPaymentMethod,
                                ((employee.Organisation != null) ? employee.Organisation.Title : ""),
                                employeePayrollAdditionallyInfo);

                        string employeeQualifyingRoleType = "";

                        EmployeeQualifyingRole employeeQualifyingRole = _employeeQualifyingRoleService.Get(x => x
                            .Where(eqr => eqr.EmployeeID == employee.ID).OrderBy(eqr => eqr.QualifyingRoleDateEnd).Where(eqr =>
                                eqr.QualifyingRoleDateBegin != null
                                && eqr.QualifyingRoleDateBegin <= record.PayrollChangeDate
                                && (eqr.QualifyingRoleDateEnd == null || eqr.QualifyingRoleDateEnd >= record.PayrollChangeDate))
                            .ToList()).FirstOrDefault();

                        if (employeeQualifyingRole != null)
                        {
                            //employeeQualifyingRoleTitle = employeeQualifyingRole.QualifyingRole.FullName;
                            employeeQualifyingRoleType = ((DisplayAttribute)(employeeQualifyingRole.QualifyingRole.RoleType.GetType().GetMember(employeeQualifyingRole.QualifyingRole.RoleType.ToString()).First().GetCustomAttributes(true)[0])).Name;

                            //dataTable.Rows[dataTable.Rows.Count - 1]["EmployeeQualifyingRoleTitle"] = employeeQualifyingRoleTitle;
                            dataTable.Rows[dataTable.Rows.Count - 1]["EmployeeQualifyingRoleType"] = employeeQualifyingRoleType;
                        }
                        dataTable.Rows[dataTable.Rows.Count - 1]["_ISGROUPROW_"] = true;
                    }
                }
            }

            DataView dataView = new DataView(dataTable);
            dataView.Sort = "Date ASC";
            dataTable = dataView.ToTable();

            using (MemoryStream stream = new MemoryStream())
            {
                using (SpreadsheetDocument doc = SpreadsheetDocument.Create(stream, SpreadsheetDocumentType.Workbook))
                {
                    WorkbookPart workbookPart = ExcelHelper.CreateWorkbookPart(doc, "Отчет " + periodEnd.ToShortDateString());

                    WorksheetPart rId1 = ExcelHelper.CreateWorksheetPartAndImportDataTable(workbookPart, "rId1", 1, 1, (uint)dataTable.Columns.Count,
                        null, dataTable, 1, 1);

                    doc.WorkbookPart.Workbook.Save();
                }

                stream.Position = 0;
                BinaryReader b = new BinaryReader(stream);
                binData = b.ReadBytes((int)stream.Length);
            }

            return File(binData, ExcelHelper.ExcelContentType, "Отчет_о_найме_" + periodEnd.ToString("dd.MM.yyyy") + ".xlsx");
        }

        [OperationActionFilter(nameof(Operation.EmployeePayrollReportView))]
        public ActionResult EmployeePayrollReport()
        {
            ViewBag.Message = "Отчет по КОТ сотрудников";

            return View();
        }

        [OperationActionFilter(nameof(Operation.EmployeePayrollReportView))]
        [HttpPost]
        public FileContentResult EmployeePayrollReportGenerate(string reportDate,
            bool? payrollFormat,
            string docServerLogin,
            string docServerPassword,
            string employeePayrollSheetFileUrl,
            IFormFile employeePayrollSheetUpload)
        {
            if (payrollFormat == null)
            {
                payrollFormat = false;
            }

            byte[] binData = null;

            DataTable employeePayrollSheetDataTable = new DataTable();
            employeePayrollSheetDataTable.Columns.Add("ADEmployeeID", typeof(string)).Caption = "EmployeeID";
            employeePayrollSheetDataTable.Columns.Add("EmployeeFullName", typeof(string)).Caption = "ФИО";
            employeePayrollSheetDataTable.Columns.Add("PayrollChangeDate", typeof(DateTime)).Caption = "Дата";
            employeePayrollSheetDataTable.Columns.Add("PayrollValue", typeof(double)).Caption = "КОТ";
            employeePayrollSheetDataTable.Columns.Add("Comments", typeof(string)).Caption = "Комментарий";

            Stream employeePayrollSheetStream = null;

            ApplicationUser user = _applicationUserService.GetUser();

            if (String.IsNullOrEmpty(docServerLogin) == true)
            {
                docServerLogin = user.OOLogin;
            }

            if (String.IsNullOrEmpty(docServerPassword) == true)
            {
                docServerPassword = user.OOPassword;
            }

            if (String.IsNullOrEmpty(employeePayrollSheetFileUrl) == true)
            {
                employeePayrollSheetFileUrl = _financeService.GetOODefalutCPFileUrl();
            }

            if (String.IsNullOrEmpty(employeePayrollSheetFileUrl) == false
                && String.IsNullOrEmpty(docServerLogin) == false
                && String.IsNullOrEmpty(docServerPassword) == false)
            {
                byte[] employeePayrollSheetFileBinData = _ooService.DownloadFile(employeePayrollSheetFileUrl, docServerLogin, docServerPassword);

                if (employeePayrollSheetFileBinData != null)
                {
                    employeePayrollSheetStream = new MemoryStream(employeePayrollSheetFileBinData);
                }
            }

            if (employeePayrollSheetStream == null
                && employeePayrollSheetUpload != null
                && employeePayrollSheetUpload.OpenReadStream() != null)
            {
                employeePayrollSheetStream = employeePayrollSheetUpload.OpenReadStream();
            }

            if (employeePayrollSheetStream != null)
            {
                employeePayrollSheetDataTable = ExcelHelper.ExportData(employeePayrollSheetDataTable, employeePayrollSheetStream);
            }

            DateTime rDate = DateTime.MinValue;

            try
            {
                rDate = Convert.ToDateTime(reportDate);
            }
            catch (Exception)
            {
                rDate = DateTime.MinValue;
            }

            List<Employee> currentEmployeeList = null;

            if (_applicationUserService.HasAccess(Operation.OOAccessFullPayrollAccess) == true
                || _applicationUserService.HasAccess(Operation.OOAccessFullReadPayrollAccess) == true)
            {
                currentEmployeeList = _employeeService.GetCurrentEmployees(new DateTimeRange(rDate, rDate)).ToList()
                    .Where(e => e.Department != null)
                    .ToList().OrderBy(e => e.Department.ShortName + e.FullName).ToList();

            }
            else if (_applicationUserService.HasAccess(Operation.OOAccessSubEmplReadPayrollAccess) == true)
            {
                currentEmployeeList = _employeeService.GetAllManagedEmployees(_applicationUserService.GetUser().ManagedDepartments).ToList().Where(e => (e.EnrollmentDate == null || e.EnrollmentDate.Value <= rDate)
                                                                                        && (e.DismissalDate == null || e.DismissalDate >= rDate) && e.IsVacancy == false)
                    .Where(e => e.Department != null)
                    .OrderBy(e => e.Department.ShortName + e.FullName).ToList();
            }
            else
            {
                currentEmployeeList = new List<Employee>();
            }

            DataTable dataTable = new DataTable();

            if (payrollFormat == false)
            {
                dataTable.Columns.Add("FCDepartmentShortName", typeof(string)).Caption = "ЦФО";
                dataTable.Columns["FCDepartmentShortName"].ExtendedProperties["Width"] = (double)9;
                dataTable.Columns.Add("DepartmentShortName", typeof(int)).Caption = "Код";
                dataTable.Columns["DepartmentShortName"].ExtendedProperties["Width"] = (double)6;
                dataTable.Columns.Add("Department", typeof(string)).Caption = "Подразделение";
                dataTable.Columns["Department"].ExtendedProperties["Width"] = (double)60;
                dataTable.Columns.Add("DepartmentEmployeeCount", typeof(int)).Caption = "Кол-во";
                dataTable.Columns["DepartmentEmployeeCount"].ExtendedProperties["Width"] = (double)10;
                dataTable.Columns.Add("Position", typeof(string)).Caption = "Позиция";
                dataTable.Columns["Position"].ExtendedProperties["Width"] = (double)45;
                dataTable.Columns.Add("EmployeeTitle", typeof(string)).Caption = "Фамилия Имя Отчество";
                dataTable.Columns["EmployeeTitle"].ExtendedProperties["Width"] = (double)39;
                dataTable.Columns.Add("EmployeePayroll", typeof(int)).Caption = "КОТ";
                dataTable.Columns["EmployeePayroll"].ExtendedProperties["Width"] = (double)8;
                dataTable.Columns.Add("EmployeeGrad", typeof(int)).Caption = "Грейд";
                dataTable.Columns["EmployeeGrad"].ExtendedProperties["Width"] = (double)8;
                dataTable.Columns.Add("EmployeeCategoryTitle", typeof(string)).Caption = "Категория";
                dataTable.Columns["EmployeeCategoryTitle"].ExtendedProperties["Width"] = (double)39;
                dataTable.Columns.Add("PositionOfficial", typeof(string)).Caption = "Должность по ШР";
                dataTable.Columns["PositionOfficial"].ExtendedProperties["Width"] = (double)45;
                dataTable.Columns.Add("EmployeeQualifyingRoleTitle", typeof(string)).Caption = "УПР";
                dataTable.Columns["EmployeeQualifyingRoleTitle"].ExtendedProperties["Width"] = (double)39;
                dataTable.Columns.Add("EmployeeQualifyingRoleType", typeof(string)).Caption = "Тип";
                dataTable.Columns["EmployeeQualifyingRoleType"].ExtendedProperties["Width"] = (double)9;
                dataTable.Columns.Add("EmployeeEnrollmentDate", typeof(DateTime)).Caption = "Принят";
                dataTable.Columns["EmployeeEnrollmentDate"].ExtendedProperties["Width"] = (double)12;
                dataTable.Columns.Add("_ISGROUPROW_", typeof(bool)).Caption = "_ISGROUPROW_";

                foreach (var group in currentEmployeeList.GroupBy(e => e.Department.ShortName))
                {
                    string fcDepartmentShortName = "";

                    Department fcDepartment = group.FirstOrDefault().Department;

                    while (fcDepartment.ParentDepartment != null && fcDepartment.IsFinancialCentre == false)
                    {
                        fcDepartment = fcDepartment.ParentDepartment;
                    }

                    if (fcDepartment.IsFinancialCentre == true)
                    {
                        fcDepartmentShortName = fcDepartment.DisplayShortTitle;
                    }

                    int departmentShortNameIntValue = 0;

                    try
                    {
                        departmentShortNameIntValue = Convert.ToInt32(group.Key.Replace("-", ""));
                    }
                    catch (Exception)
                    {
                        departmentShortNameIntValue = 0;
                    }

                    dataTable.Rows.Add(fcDepartmentShortName, departmentShortNameIntValue, "", group.Count(), "", "");

                    int departmentRowIndex = dataTable.Rows.Count - 1;

                    dataTable.Rows[departmentRowIndex]["_ISGROUPROW_"] = true;

                    foreach (var employeeItem in group)
                    {
                        String employeeItemFullName = "";

                        if (String.IsNullOrEmpty(employeeItem.LastName) == false)
                        {
                            employeeItemFullName = employeeItem.LastName;
                        }
                        if (String.IsNullOrEmpty(employeeItem.FirstName) == false)
                        {
                            employeeItemFullName += " " + employeeItem.FirstName;
                        }
                        if (String.IsNullOrEmpty(employeeItem.MidName) == false)
                        {
                            employeeItemFullName += " " + employeeItem.MidName;
                        }

                        employeeItemFullName = employeeItemFullName.Trim();

                        dataTable.Rows.Add(fcDepartmentShortName,
                            departmentShortNameIntValue,
                            ((group.First().Department != null) ? group.First().Department.Title : ""),
                            null,
                            ((employeeItem.EmployeePositionTitle != null) ? employeeItem.EmployeePositionTitle : ""),
                            employeeItemFullName);

                        if (employeeItem.EmployeeGradID != null && employeeItem.EmployeeGrad != null)
                        {
                            int employeeGradShortNameIntValue = 0;

                            try
                            {
                                employeeGradShortNameIntValue = Convert.ToInt32(employeeItem.EmployeeGrad.ShortName);
                            }
                            catch (Exception)
                            {
                                employeeGradShortNameIntValue = 0;
                            }

                            if (employeeGradShortNameIntValue != 0)
                            {
                                dataTable.Rows[dataTable.Rows.Count - 1]["EmployeeGrad"] = employeeGradShortNameIntValue;
                            }
                        }

                        string employeeCategoryTitle = "";

                        EmployeeCategory employeeCategory = _employeeCategoryService.Get(x => x
                                .Where(ec => ec.EmployeeID == employeeItem.ID).OrderBy(ec => ec.CategoryDateEnd).Where(
                                    ec =>
                                        ec.CategoryDateBegin != null
                                        && ec.CategoryDateBegin <= rDate
                                        && (ec.CategoryDateEnd == null || ec.CategoryDateEnd >= rDate)).ToList())
                            .FirstOrDefault();


                        if (employeeCategory != null)
                        {
                            employeeCategoryTitle = ((DisplayAttribute)(employeeCategory.CategoryType.GetType().GetMember(employeeCategory.CategoryType.ToString()).First().GetCustomAttributes(true)[0])).Name;
                        }

                        if (String.IsNullOrEmpty(employeeCategoryTitle) == false)
                        {
                            dataTable.Rows[dataTable.Rows.Count - 1]["EmployeeCategoryTitle"] = employeeCategoryTitle;
                        }

                        if (employeeItem.EmployeePositionOfficial != null)
                        {
                            dataTable.Rows[dataTable.Rows.Count - 1]["PositionOfficial"] = employeeItem.EmployeePositionOfficial.FullName;
                        }

                        string employeeQualifyingRoleTitle = "";
                        string employeeQualifyingRoleType = "";

                        EmployeeQualifyingRole employeeQualifyingRole = _employeeQualifyingRoleService.Get(x => x
                            .Where(eqr => eqr.EmployeeID == employeeItem.ID).OrderBy(eqr => eqr.QualifyingRoleDateEnd)
                            .Where(eqr => eqr.QualifyingRoleDateBegin != null
                                          && eqr.QualifyingRoleDateBegin <= rDate
                                          && (eqr.QualifyingRoleDateEnd == null || eqr.QualifyingRoleDateEnd >= rDate))
                            .ToList()).FirstOrDefault();


                        if (employeeQualifyingRole != null)
                        {
                            employeeQualifyingRoleTitle = employeeQualifyingRole.QualifyingRole.FullName;
                            employeeQualifyingRoleType = ((DisplayAttribute)(employeeQualifyingRole.QualifyingRole.RoleType.GetType().GetMember(employeeQualifyingRole.QualifyingRole.RoleType.ToString()).First().GetCustomAttributes(true)[0])).Name;
                        }

                        if (String.IsNullOrEmpty(employeeQualifyingRoleTitle) == false)
                        {
                            dataTable.Rows[dataTable.Rows.Count - 1]["EmployeeQualifyingRoleTitle"] = employeeQualifyingRoleTitle;
                            dataTable.Rows[dataTable.Rows.Count - 1]["EmployeeQualifyingRoleType"] = employeeQualifyingRoleType;
                        }

                        if (employeeItem.EnrollmentDate != null && employeeItem.EnrollmentDate.HasValue == true)
                        {
                            dataTable.Rows[dataTable.Rows.Count - 1]["EmployeeEnrollmentDate"] = employeeItem.EnrollmentDate.Value;
                        }

                        double employeePayrollValue = _financeService.GetEmployeePayrollOnDate(employeePayrollSheetDataTable, employeeItem, rDate, false, null);

                        if (employeePayrollValue != 0)
                        {
                            dataTable.Rows[dataTable.Rows.Count - 1]["EmployeePayroll"] = (int)employeePayrollValue;
                        }

                    }
                }
            }
            else
            {
                dataTable.Columns.Add("FCDepartmentShortName", typeof(int)).Caption = "Код";
                dataTable.Columns["FCDepartmentShortName"].ExtendedProperties["Width"] = (double)6;
                dataTable.Columns.Add("Position", typeof(string)).Caption = "Должность";
                dataTable.Columns["Position"].ExtendedProperties["Width"] = (double)51;
                dataTable.Columns.Add("EmployeeTitle", typeof(string)).Caption = "Фамилия Имя Отчество";
                dataTable.Columns["EmployeeTitle"].ExtendedProperties["Width"] = (double)39;
                dataTable.Columns.Add("EmployeePayrollChangeDate", typeof(DateTime)).Caption = "Дата изменения";
                dataTable.Columns["EmployeePayrollChangeDate"].ExtendedProperties["Width"] = (double)12;
                dataTable.Columns.Add("EmployeePayroll", typeof(int)).Caption = "КОТ";
                dataTable.Columns["EmployeePayroll"].ExtendedProperties["Width"] = (double)18;
                dataTable.Columns.Add("EmployeeCategoryTitle", typeof(string)).Caption = "Категория";
                dataTable.Columns["EmployeeCategoryTitle"].ExtendedProperties["Width"] = (double)39;

                foreach (var employeeItem in currentEmployeeList)
                {
                    String employeeFCDepartmentShortName = "";

                    Department fcDepartment = employeeItem.Department;

                    while (fcDepartment.ParentDepartment != null && fcDepartment.IsFinancialCentre == false)
                    {
                        fcDepartment = fcDepartment.ParentDepartment;
                    }

                    if (fcDepartment.IsFinancialCentre == true)
                    {
                        employeeFCDepartmentShortName = fcDepartment.ShortName;
                    }

                    int employeeFCDepartmentShortNameIntValue = 0;

                    try
                    {
                        employeeFCDepartmentShortNameIntValue = Convert.ToInt32(employeeFCDepartmentShortName);
                    }
                    catch (Exception)
                    {
                        employeeFCDepartmentShortNameIntValue = 0;
                    }

                    dataTable.Rows.Add((employeeFCDepartmentShortNameIntValue != 0) ? employeeFCDepartmentShortNameIntValue : (int?)null/*employeeFCDepartmentShortName*/,
                       ((employeeItem.EmployeePositionTitle != null) ? employeeItem.EmployeePositionTitle : ""),
                       employeeItem.FullName);

                    DateTime EmployeePayrollChangeDateValue = _financeService.GetEmployeePayrollLastChangeDateOnDate(employeePayrollSheetDataTable, employeeItem, rDate);

                    if (EmployeePayrollChangeDateValue > DateTime.MinValue)
                    {
                        dataTable.Rows[dataTable.Rows.Count - 1]["EmployeePayrollChangeDate"] = EmployeePayrollChangeDateValue;
                    }

                    double employeePayrollValue = _financeService.GetEmployeePayrollOnDate(employeePayrollSheetDataTable, employeeItem, rDate, false, null);

                    if (employeePayrollValue != 0)
                    {
                        dataTable.Rows[dataTable.Rows.Count - 1]["EmployeePayroll"] = (int)employeePayrollValue;
                    }

                    string employeeCategoryTitle = "";

                    EmployeeCategory employeeCategory = _employeeCategoryService.Get(x => x
                            .Where(ec => ec.EmployeeID == employeeItem.ID).OrderBy(ec => ec.CategoryDateEnd).Where(
                                ec =>
                                    ec.CategoryDateBegin != null
                                    && ec.CategoryDateBegin <= rDate
                                    && (ec.CategoryDateEnd == null || ec.CategoryDateEnd >= rDate)).ToList())
                        .FirstOrDefault();


                    if (employeeCategory != null)
                    {
                        employeeCategoryTitle = ((DisplayAttribute)(employeeCategory.CategoryType.GetType().GetMember(employeeCategory.CategoryType.ToString()).First().GetCustomAttributes(true)[0])).Name;
                    }

                    if (String.IsNullOrEmpty(employeeCategoryTitle) == false)
                    {
                        dataTable.Rows[dataTable.Rows.Count - 1]["EmployeeCategoryTitle"] = employeeCategoryTitle;
                    }
                }

                DataView dataView = new DataView(dataTable);
                dataView.Sort = "FCDepartmentShortName ASC, EmployeeTitle ASC";
                dataTable = dataView.ToTable();
            }

            using (MemoryStream stream = new MemoryStream())
            {
                using (SpreadsheetDocument doc = SpreadsheetDocument.Create(stream, SpreadsheetDocumentType.Workbook))
                {
                    WorkbookPart workbookPart = ExcelHelper.CreateWorkbookPart(doc, "КОТ");

                    WorksheetPart rId1 = ExcelHelper.CreateWorksheetPartAndImportDataTable(workbookPart, "rId1", 1, 1, (uint)dataTable.Columns.Count,
                        null, dataTable, 1, 1);

                    doc.WorkbookPart.Workbook.Save();
                }

                stream.Position = 0;
                BinaryReader b = new BinaryReader(stream);
                binData = b.ReadBytes((int)stream.Length);
            }

            return File(binData, ExcelHelper.ExcelContentType, "КОТ" /*+ rDate.ToString("dd.MM.yyyy")*/ + ".xlsx");

        }



        [OperationActionFilter(nameof(Operation.FinReportView))]
        public ActionResult DKReport()
        {
            ViewBag.Message = "Отчет по данным Daykassa";

            return View();
        }


        [OperationActionFilter(nameof(Operation.FinReportView))]
        [HttpPost]
        public void StartGenerateDKReport(string id,
            string reportPeriodDateStart,
            string reportPeriodDateEnd)
        {
            DateTime periodStart = DateTime.MinValue;
            DateTime periodEnd = DateTime.MinValue;

            try
            {
                periodStart = Convert.ToDateTime(reportPeriodDateStart);
            }
            catch (Exception)
            {
                periodStart = DateTime.MinValue;
            }

            try
            {
                periodEnd = Convert.ToDateTime(reportPeriodDateEnd);
            }
            catch (Exception)
            {
                periodEnd = DateTime.MinValue;
            }

            _taskDKReportGenerator.Add(id, false);
            DKReportGeneratorTaskAction processTask = new DKReportGeneratorTaskAction(_taskDKReportGenerator.ProcessLongRunningAction);

            //processTask.BeginInvoke(User.Identity.Name, id, null, periodStart, periodEnd, true, new AsyncCallback(EndGenerateDKReport), processTask);
            string userIdentityName = User.Identity.Name;
            Task.Run(() => processTask.Invoke(userIdentityName, id, null, periodStart, periodEnd, true)).ContinueWith(EndGenerateDKReport);

        }

        [OperationActionFilter(nameof(Operation.FinReportView))]
        public void EndGenerateDKReport(/*IAsyncResult*/ Task<ReportGeneratorResult> result)
        {
            //DKReportGeneratorTaskAction processTask = (DKReportGeneratorTaskAction)result.AsyncState;
            ReportGeneratorResult reportGeneratorResult = result.Result;// processTask.EndInvoke(result);

            InsertReportGeneratorResultInCache(reportGeneratorResult);

            _taskDKReportGenerator.Remove(reportGeneratorResult.fileId);
        }

        [AProjectDetailsView]
        public ActionResult PMDKReport(int? projectID)
        {
            ViewBag.Message = "Выгрузка DK для руководителя проектов";

            ApplicationUser user = _applicationUserService.GetUser();

            if (projectID == null)
            {
                ViewBag.PMProjectsFromDB = _applicationUserService.GetMyProjects().OrderBy(p => p.ShortName).ToList();
            }
            else
            {
                ViewBag.PMProjectsFromDB = _projectService.Get(x => x.Where(p => p.ID == projectID).ToList());
            }


            return View();
        }


        [AProjectDetailsView]
        [HttpPost]
        public void StartGeneratePMDKReport(string id,
            int? projectID,
            string reportPeriodDateStart,
            string reportPeriodDateEnd)
        {
            Project project = _projectService.GetById(projectID.Value);

            DateTime periodStart = DateTime.MinValue;
            DateTime periodEnd = DateTime.MinValue;

            try
            {
                periodStart = Convert.ToDateTime(reportPeriodDateStart);
            }
            catch (Exception)
            {
                periodStart = DateTime.MinValue;
            }

            try
            {
                periodEnd = Convert.ToDateTime(reportPeriodDateEnd);
            }
            catch (Exception)
            {
                periodEnd = DateTime.MinValue;
            }

            _taskPMDKReportGenerator.Add(id, false);
            PMDKReportGeneratorTaskAction processTask = new PMDKReportGeneratorTaskAction(_taskPMDKReportGenerator.ProcessLongRunningAction);

            //processTask.BeginInvoke(User.Identity.Name, id, project.ShortName, periodStart, periodEnd, false, new AsyncCallback(EndGeneratePMDKReport), processTask);
            string userIdentityName = User.Identity.Name;
            Task.Run(() => processTask.Invoke(userIdentityName, id, project.ShortName, periodStart, periodEnd, false)).ContinueWith(EndGeneratePMDKReport);

        }

        /*[AProjectDetailsView]*/
        public void EndGeneratePMDKReport(/*IAsyncResult*/ Task<ReportGeneratorResult> result)
        {
            //PMDKReportGeneratorTaskAction processTask = (PMDKReportGeneratorTaskAction)result.AsyncState;
            ReportGeneratorResult reportGeneratorResult = result.Result; //processTask.EndInvoke(result);

            InsertReportGeneratorResultInCache(reportGeneratorResult);

            _taskPMDKReportGenerator.Remove(reportGeneratorResult.fileId);
        }

        [OperationActionFilter(nameof(Operation.FinReportView))]
        public ActionResult ApplicationForPaymentReport()
        {
            ViewBag.Message = "Выгрузка данных о расходах из Б24";

            return View();
        }



        [OperationActionFilter(nameof(Operation.FinReportView))]
        [HttpPost]
        public void StartGenerateAFPReport(string id,
            string reportPeriodDateStart,
            string reportPeriodDateEnd)
        {
            DateTime periodStart = DateTime.MinValue;
            DateTime periodEnd = DateTime.MinValue;

            try
            {
                periodStart = Convert.ToDateTime(reportPeriodDateStart);
            }
            catch (Exception)
            {
                periodStart = DateTime.MinValue;
            }

            try
            {
                periodEnd = Convert.ToDateTime(reportPeriodDateEnd);
            }
            catch (Exception)
            {
                periodEnd = DateTime.MinValue;
            }

            _taskAFPReportGenerator.Add(id, false);
            AFPReportGeneratorTaskAction processTask = new AFPReportGeneratorTaskAction(_taskAFPReportGenerator.ProcessLongRunningAction);

            //processTask.BeginInvoke(User.Identity.Name, id, periodStart, periodEnd, new AsyncCallback(EndGenerateAFPReport), processTask);
            string userIdentityName = User.Identity.Name;
            Task.Run(() => processTask.Invoke(userIdentityName, id, periodStart, periodEnd)).ContinueWith(EndGenerateAFPReport);

        }

        [OperationActionFilter(nameof(Operation.FinReportView))]
        public void EndGenerateAFPReport(/*IAsyncResult*/ Task<ReportGeneratorResult> result)
        {
            //AFPReportGeneratorTaskAction processTask = (AFPReportGeneratorTaskAction)result.AsyncState;
            ReportGeneratorResult reportGeneratorResult = result.Result;//processTask.EndInvoke(result);

            InsertReportGeneratorResultInCache(reportGeneratorResult);

            _taskAFPReportGenerator.Remove(reportGeneratorResult.fileId);
        }

        [OperationActionFilter(nameof(Operation.QualifyingRoleRateView))]
        public ActionResult QualifyingRoleRateReport()
        {
            ViewBag.Message = "Ставки УПР";

            var departmentFRCList = _departmentService.Get(x => x.Where(d => d.IsFinancialCentre).OrderBy(d => d.ShortName).ToList());

            string qualifyingRoleRateHoursPlanCalcParam = _financeService.GetQualifyingRoleRateHoursPlanCalcParam();

            if (String.IsNullOrEmpty(qualifyingRoleRateHoursPlanCalcParam) == true)
            {
                qualifyingRoleRateHoursPlanCalcParam = "168";
            }

            ViewBag.QualifyingRoleRateHoursPlanCalcParam = qualifyingRoleRateHoursPlanCalcParam;

            List<QualifyingRoleRateFRCCalcParamRecord> qualifyingRoleRateFRCCalcParamRecordList = new List<QualifyingRoleRateFRCCalcParamRecord>();

            string qualifyingRoleRateFRCCalcParamRecordListJson = _financeService.GetQualifyingRoleRateFRCCalcParamRecordListJson();

            if (String.IsNullOrEmpty(qualifyingRoleRateFRCCalcParamRecordListJson) == false)
            {
                var list = JsonConvert.DeserializeObject<List<QualifyingRoleRateFRCCalcParamRecord>>(qualifyingRoleRateFRCCalcParamRecordListJson);

                int i = 0;
                foreach (var record in list)
                {
                    QualifyingRoleRateFRCCalcParamRecord qualifyingRoleRateFRCCalcParamRecord = new QualifyingRoleRateFRCCalcParamRecord
                    {
                        ID = i,
                        Department = _departmentService.GetById(record.DepartmentID),
                        DepartmentID = record.DepartmentID,
                        FRCCorrectionFactor = record.FRCCorrectionFactor,
                        FRCInflationRate = record.FRCInflationRate
                    };

                    qualifyingRoleRateFRCCalcParamRecordList.Add(qualifyingRoleRateFRCCalcParamRecord);
                    i++;
                }

                foreach (Department department in departmentFRCList)
                {
                    if (qualifyingRoleRateFRCCalcParamRecordList.Where(pr => pr.DepartmentID == department.ID).FirstOrDefault() == null)
                    {
                        QualifyingRoleRateFRCCalcParamRecord qualifyingRoleRateFRCCalcParamRecord = new QualifyingRoleRateFRCCalcParamRecord
                        {
                            ID = i,
                            Department = department,
                            DepartmentID = department.ID,
                            FRCCorrectionFactor = 1.0,
                            FRCInflationRate = 1.2
                        };

                        qualifyingRoleRateFRCCalcParamRecordList.Add(qualifyingRoleRateFRCCalcParamRecord);
                        i++;
                    }
                }
            }
            else
            {
                int i = 0;
                foreach (Department department in departmentFRCList)
                {
                    QualifyingRoleRateFRCCalcParamRecord qualifyingRoleRateFRCCalcParamRecord = new QualifyingRoleRateFRCCalcParamRecord
                    {
                        ID = i,
                        Department = department,
                        DepartmentID = department.ID,
                        FRCCorrectionFactor = 1.0,
                        FRCInflationRate = 1.2
                    };

                    qualifyingRoleRateFRCCalcParamRecordList.Add(qualifyingRoleRateFRCCalcParamRecord);
                    i++;
                }
            }

            ViewBag.QualifyingRoleRateFRCCalcParamRecordList = qualifyingRoleRateFRCCalcParamRecordList;

            return View();
        }

        private List<Employee> GetEmployeesInDepartment(int departmentID)
        {

            var employes = _employeeService.Get(x => x.Where(e =>
                    (e.EnrollmentDate == null || e.EnrollmentDate.Value <= DateTime.Today)
                    && (e.DismissalDate == null || e.DismissalDate >= DateTime.Today)).Include(e => e.EmployeePosition)
                .Where(e => e.DepartmentID == departmentID)
                .ToList()
                .OrderBy(e => e.Department.ShortName + e.FullName).ToList()).ToArray();

            foreach (var department in _departmentService.Get(p => p.Where(x => x.ParentDepartmentID == departmentID).ToList()))
            {
                var result = GetEmployeesInDepartment(department.ID);
                employes = employes.Concat(result).ToArray();
            }

            return employes.ToList();
        }



        [OperationActionFilter(nameof(Operation.QualifyingRoleRateView))]
        [HttpPost]
        public void StartGenerateQualifyingRoleRateReport(
            string id,
            string reportDate,
            bool reportRecalcQualifyingRoleRates,
            int? reportHoursPlan,
            List<QualifyingRoleRateFRCCalcParamRecord> qualifyingRoleRateFRCCalcParamRecordList)
        {

            ApplicationUser user = _applicationUserService.GetUser();

            if (reportRecalcQualifyingRoleRates == true)
            {
                if (!(_applicationUserService.HasAccess(Operation.OOAccessFullPayrollAccess) == true
                    || _applicationUserService.HasAccess(Operation.OOAccessFullReadPayrollAccess) == true)
                    || _applicationUserService.HasAccess(Operation.QualifyingRoleCreateUpdate) == false)
                {
                    reportRecalcQualifyingRoleRates = false;
                }
            }

            DataTable employeePayrollSheetDataTable = _financeService.GetEmployeePayrollSheetDataTableFromOO(user, false);

            DateTime rDate = DateTime.MinValue;

            try
            {
                rDate = Convert.ToDateTime(reportDate);
            }
            catch (Exception)
            {
                rDate = DateTime.MinValue;
            }

            List<int> departmentsIDs = null;

            if (_applicationUserService.HasAccess(Operation.OOAccessFullPayrollAccess) == true
                || _applicationUserService.HasAccess(Operation.OOAccessFullReadPayrollAccess) == true)
            {
                departmentsIDs = null;
            }
            else if (_applicationUserService.HasAccess(Operation.OOAccessSubEmplReadPayrollAccess) == true)
            {
                departmentsIDs = _applicationUserService.GetUser().ManagedDepartments.Select(x => x.ID).ToList();
            }
            else
            {
                departmentsIDs = new List<int>();
            }

            if (_taskQualifyingRoleRateReportGenerator.Add(id, reportRecalcQualifyingRoleRates) == true)
            {
                QualifyingRoleRateReportGeneratorTaskAction processTask = new QualifyingRoleRateReportGeneratorTaskAction(_taskQualifyingRoleRateReportGenerator.ProcessLongRunningAction);

                /*processTask.BeginInvoke(User.Identity.Name, id,
                    reportDate,
                    reportRecalcQualifyingRoleRates,
                    reportHoursPlan,
                    qualifyingRoleRateFRCCalcParamRecordList,
                    departmentsIDs,
                    employeePayrollSheetDataTable,
                    new AsyncCallback(EndGenerateQualifyingRoleRateReport), processTask);*/
                string userIdentityName = User.Identity.Name;
                Task.Run(() => processTask.Invoke(userIdentityName, id,
                    reportDate,
                    reportRecalcQualifyingRoleRates,
                    reportHoursPlan,
                    qualifyingRoleRateFRCCalcParamRecordList,
                    departmentsIDs,
                    employeePayrollSheetDataTable)).ContinueWith(EndGenerateQualifyingRoleRateReport);
            }

        }

        [OperationActionFilter(nameof(Operation.QualifyingRoleRateView))]
        public void EndGenerateQualifyingRoleRateReport(/*IAsyncResult*/ Task<ReportGeneratorResult> result)
        {
            //QualifyingRoleRateReportGeneratorTaskAction processTask = (QualifyingRoleRateReportGeneratorTaskAction)result.AsyncState;
            ReportGeneratorResult reportGeneratorResult = result.Result;// processTask.EndInvoke(result);

            InsertReportGeneratorResultInCache(reportGeneratorResult);

            _taskQualifyingRoleRateReportGenerator.Remove(reportGeneratorResult.fileId);
        }

        protected void InsertReportGeneratorResultInCache(ReportGeneratorResult reportGeneratorResult)
        {
            if (String.IsNullOrEmpty(reportGeneratorResult.htmlErrorReport) == true && reportGeneratorResult.fileBinData != null)
            {
                _memoryCache.Set(reportGeneratorResult.fileId, reportGeneratorResult.fileBinData.ToArray());
            }
            else if (String.IsNullOrEmpty(reportGeneratorResult.htmlErrorReport) == false)
            {
                _memoryCache.Set(reportGeneratorResult.fileId + LongRunningTaskController.ErrorReportIDPostfix, reportGeneratorResult.htmlErrorReport);
            }
        }
        [OperationActionFilter(nameof(Operation.TSHoursUtilizationReportView))]
        public ActionResult TSHoursUtilizationReport()
        {
            ViewBag.Message = "Отчет по утилизации";

            IList<Department> departmentSelectList = _departmentService.Get(departments => departments.Where(d => d.IsFinancialCentre).OrderBy(d => d.ShortName).ToList());

            ViewBag.DepartmentID = new SelectList(departmentSelectList.ToList(),
                    "ID", "FullName", departmentSelectList.FirstOrDefault().ID);

            ViewBag.Years = _productionCalendarService.Get(x => x.ToList()).Select(x => new { x.Year }).Distinct().OrderBy(x => x.Year).ToList();

            ViewBag.Months = Enumerable.Empty<SelectListItem>();

            return View();

        }

        [OperationActionFilter(nameof(Operation.TSHoursUtilizationReportView))]
        [HttpPost]
        public void StartGenerateTSHoursUtilizationReport(
            string id,
            int[] departmentIDs,
            string reportYear,
            string reportPeriodMode,
            string reportPeriod,
            string reportPeriodEnd,
            string reportPeriodDateStart,
            string reportPeriodDateEnd)
        {
            List<int> departmentsIDs = null;
            ApplicationUser user = _applicationUserService.GetUser();

            DateTime periodStart = DateTime.MinValue;
            DateTime periodEnd = DateTime.MinValue;

            if (departmentIDs != null && departmentIDs.Count() != 0)
            {
                departmentsIDs = new List<int>();
                departmentsIDs.AddRange(departmentIDs);
            }

            try
            {
                periodStart = Convert.ToDateTime(reportPeriodDateStart);
            }
            catch (Exception)
            {
                periodStart = DateTime.MinValue;
            }

            try
            {
                periodEnd = Convert.ToDateTime(reportPeriodDateEnd);
            }
            catch (Exception)
            {
                periodEnd = DateTime.MinValue;
            }

            var reportParams = new TSHoursUtilizationReportParams
            {
                UserIdentityName = User.Identity.Name,
                ID = id,
                PeriodStart = reportPeriod,
                PeriodEnd = reportPeriodEnd,
                DepartmentsIDs = departmentsIDs
            };

            if (_taskTSHoursUtilizationReportGenerator.Add(id, false) == true)
            {
                TSHoursUtilizationReportGeneratorTaskAction processTask = new TSHoursUtilizationReportGeneratorTaskAction(_taskTSHoursUtilizationReportGenerator.ProcessLongRunningAction);

                string userIdentityName = User.Identity.Name;
                Task.Run(() => processTask.Invoke(reportParams)).ContinueWith(EndGenerateTSHoursUtilizationReport);
            }

        }

        [OperationActionFilter(nameof(Operation.TSHoursUtilizationReportView))]
        public void EndGenerateTSHoursUtilizationReport(Task<ReportGeneratorResult> result)
        {
            ReportGeneratorResult reportGeneratorResult = result.Result;

            InsertReportGeneratorResultInCache(reportGeneratorResult);

            _taskQualifyingRoleRateReportGenerator.Remove(reportGeneratorResult.fileId);
        }
    }
}