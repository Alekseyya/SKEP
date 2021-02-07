using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using BL.Implementation;
using Core;
using Core.BL.Interfaces;
using Core.Config;
using Core.Data;
using Core.Extensions;
using Core.Models;
using Core.Models.RBAC;
using Data;
using Data.Implementation;
using MainApp.Dto;
using MainApp.RBAC.Attributes;
using MainApp.TimesheetProcessing;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

using X.PagedList;


namespace MainApp.Controllers
{
    public class VacationRecordController : Controller
    {
        private readonly IVacationRecordService _vacationRecordService;
        private readonly IEmployeeService _employeeService;
        private readonly IUserService _userService;
        private readonly IReportingPeriodService _reportingPeriodService;
        private readonly ITSHoursRecordService _tsHoursRecordService;
        private readonly IProductionCalendarService _productionCalendarService;
        private readonly DbContextOptions<RPCSContext> _rpcsContextOptions;
        private readonly IOptions<OnlyOfficeConfig> _onlyofficeOptions;
        private readonly IOptions<ADConfig> _adOptions;
        private readonly IOptions<BitrixConfig> _bitrixOptions;
        private readonly IOptions<TimesheetConfig> _timesheetOptions;
        private readonly IOptions<SMTPConfig> _smtpOptions;
        private readonly IOptions<JiraConfig> _jiraOptions;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IMemoryCache _memoryCache;
        private readonly IOOService _ooService;
        private readonly ILogger<TSHoursRecordService> _tsHoursRecordServiceLogger;
        private readonly IServiceService _serviceService;


        private TimesheetProcessingTask _taskTimesheetProcessing;

        public VacationRecordController(IVacationRecordService vacationRecordService,
                                        IEmployeeService employeeService,
                                        IUserService userService,
                                        IReportingPeriodService reportingPeriodService,
                                        ITSHoursRecordService tsHoursRecordService,
                                        IProductionCalendarService productionCalendarService, 
                                        DbContextOptions<RPCSContext> rpcsContextOptions,
                                        IOptions<OnlyOfficeConfig> onlyofficeOptions,
                                        IOptions<ADConfig> adOptions,
                                        IOptions<BitrixConfig> bitrixOptions,
                                        IOptions<TimesheetConfig> timesheetOptions,
                                        IOptions<SMTPConfig> smtpOptions,
                                        IOptions<JiraConfig> jiraOptions,
                                        IHttpContextAccessor httpContextAccessor,
                                        IMemoryCache memoryCache,
                                        IOOService ooService,
                                        ILogger<TSHoursRecordService> tsHoursRecordServiceLogger,
                                        IServiceService serviceService)
        {
            if (vacationRecordService == null)
                throw new ArgumentNullException(nameof(vacationRecordService));
            if (employeeService == null)
                throw new ArgumentNullException(nameof(employeeService));
            if (userService == null)
                throw new ArgumentNullException(nameof(userService));
            if (reportingPeriodService == null)
                throw new ArgumentNullException(nameof(reportingPeriodService));
            if (tsHoursRecordService == null)
                throw new ArgumentNullException(nameof(tsHoursRecordService));
            if (productionCalendarService == null)
                throw new ArgumentNullException(nameof(productionCalendarService));

            _vacationRecordService = vacationRecordService;
            _employeeService = employeeService;
            _userService = userService;
            _reportingPeriodService = reportingPeriodService;
            _tsHoursRecordService = tsHoursRecordService;
            _productionCalendarService = productionCalendarService;
            _rpcsContextOptions = rpcsContextOptions;
            _onlyofficeOptions = onlyofficeOptions;
            _adOptions = adOptions;
            _bitrixOptions = bitrixOptions;
            _timesheetOptions = timesheetOptions;
            _smtpOptions = smtpOptions;
            _jiraOptions = jiraOptions;
            _httpContextAccessor = httpContextAccessor;
            _memoryCache = memoryCache;
            _ooService = ooService;
            _tsHoursRecordServiceLogger = tsHoursRecordServiceLogger;
            _serviceService = serviceService;

            InitTasks();

        }

        protected void InitTasks()
        {
            var iRPCSDbAccessor = (IRPCSDbAccessor)new RPCSSingletonDbAccessor(_rpcsContextOptions);
            var rPCSRepositoryFactory = (IRepositoryFactory)new RPCSRepositoryFactory(iRPCSDbAccessor);

            var userService = (IUserService)new UserService(rPCSRepositoryFactory,_httpContextAccessor);
            var tsAutoHoursRecordService = (ITSAutoHoursRecordService)new TSAutoHoursRecordService(rPCSRepositoryFactory, userService);
            var vacationRecordService = (IVacationRecordService)new VacationRecordService(rPCSRepositoryFactory, userService);
            var reportingPeriodService = (IReportingPeriodService)new ReportingPeriodService(rPCSRepositoryFactory);
            var productionCalendarService = (IProductionCalendarService)new ProductionCalendarService(rPCSRepositoryFactory);
            var tsHoursRecordService = (ITSHoursRecordService)new TSHoursRecordService(rPCSRepositoryFactory,userService, _tsHoursRecordServiceLogger);
            var projectService = (IProjectService)new ProjectService(rPCSRepositoryFactory, userService);
            var departmemtService = new DepartmentService(rPCSRepositoryFactory,userService);
            var employeeService = (IEmployeeService)new EmployeeService(rPCSRepositoryFactory,departmemtService, userService);
            var projectMembershipService = (IProjectMembershipService)new ProjectMembershipService(rPCSRepositoryFactory);
            var projectReportRecords = new ProjectReportRecordService(rPCSRepositoryFactory);
            var employeeCategoryService = new EmployeeCategoryService(rPCSRepositoryFactory);
            var applicationUserService = new ApplicationUserService(rPCSRepositoryFactory,employeeService,userService,departmemtService,
                _httpContextAccessor,_memoryCache,projectService,_onlyofficeOptions);
            var appPropertyService = new AppPropertyService(rPCSRepositoryFactory, _adOptions, _bitrixOptions, _onlyofficeOptions,_timesheetOptions);
            var financeService = new FinanceService(rPCSRepositoryFactory,iRPCSDbAccessor,applicationUserService,appPropertyService, _ooService);
            var timesheetService = new TimesheetService(employeeService, employeeCategoryService, tsAutoHoursRecordService,
                tsHoursRecordService, projectService, projectReportRecords, vacationRecordService, productionCalendarService,financeService, _timesheetOptions);
            var projectExternalWorkspaceService = new ProjectExternalWorkspaceService(rPCSRepositoryFactory);
            var jiraService = new JiraService(userService, _jiraOptions, projectExternalWorkspaceService, projectService);

            _taskTimesheetProcessing = new TimesheetProcessingTask(tsAutoHoursRecordService, vacationRecordService, reportingPeriodService,
                productionCalendarService, tsHoursRecordService, userService, projectService, employeeService, projectMembershipService,
                timesheetService,_timesheetOptions, _smtpOptions, _jiraOptions, jiraService, projectExternalWorkspaceService);
        }


        private void SetViewBag(VacationRecord vacationRecord)
        {
            var arrayStatus = new SelectList(Enum.GetValues(typeof(VacationRecordType)).Cast<VacationRecordType>()
                .Where(n => n != VacationRecordType.All)
                .Select(x =>
                {
                    return new SelectListItem
                    {
                        Text = x.GetAttributeOfType<DisplayAttribute>().Name,
                        Value = x.ToString()
                    };
                }), "Value", "Text");

            ViewBag.NewVacationType = arrayStatus;
            ViewBag.EmployeeID = new SelectList(_employeeService.GetCurrentEmployees(new DateTimeRange(DateTime.Today, DateTime.Today)), "ID", "FullName", vacationRecord?.EmployeeID ?? 1);
        }

        [OperationActionFilter(nameof(Operation.VacationRecordView))]
        public ActionResult Index(string searchString, int? page, VacationRecordStatus? statusFilter)
        {

            ViewBag.CurrentFilter = searchString;

            if (statusFilter != null && statusFilter.HasValue)
            {
                ViewBag.CurrentStatusFilter = statusFilter;
            }
            else
            {
                ViewBag.CurrentStatusFilter = VacationRecordStatus.All;
            }

            var vacationList = GetVacationRecordList(searchString, statusFilter);

            int pageSize = 20;
            int pageNumber = (page ?? 1);

            ViewBag.SearchVacation = new SelectList(_employeeService.GetCurrentEmployees(new DateTimeRange(DateTime.Today, DateTime.Today)), "ID", "FullName");
            return View(vacationList.ToPagedList(pageNumber, pageSize));

        }

        private IList<VacationRecord> GetVacationRecordList(string searchString, VacationRecordStatus? statusFilter)
        {

            var vacationRecordList = _vacationRecordService.Get(records => records.Include(x => x.Employee).OrderBy(x => x.VacationBeginDate).ToList());

            if (!String.IsNullOrEmpty(searchString))
            {
                string[] searchTokens = searchString.Split(' ');

                List<string> searchTokensList = new List<string>();
                for (int i = 0; i < searchTokens.Length; i++)
                {
                    if (String.IsNullOrEmpty(searchTokens[i]) == false && String.IsNullOrEmpty(searchTokens[i].Trim()) == false)
                    {
                        searchTokensList.Add(searchTokens[i].Trim().ToLower());
                    }
                }

                if (searchTokensList.Count > 1)
                {
                    vacationRecordList = vacationRecordList.Where(x => searchTokensList.All(stl =>
                    (x.Employee.FirstName != null && x.Employee.FirstName.ToLower().Equals(stl)) ||
                    (x.Employee.MidName != null && x.Employee.MidName.ToLower().Equals(stl)) ||
                    (x.Employee.LastName != null && x.Employee.LastName.ToLower().Equals(stl))
                    )).ToList();
                }
                else
                {
                    vacationRecordList = vacationRecordList.Where(x =>
                        (x.Employee.FirstName != null && x.Employee.FirstName.ToLower().Contains(searchString.Trim().ToLower()))
                        || (x.Employee.MidName != null && x.Employee.MidName.ToLower().Contains(searchString.Trim().ToLower()))
                        || (x.Employee.LastName != null && x.Employee.LastName.ToLower().Contains(searchString.Trim().ToLower()))).ToList();
                }
            }



            if (statusFilter == VacationRecordStatus.ActualVacancyRecord)
            {
                return vacationRecordList.Where(x => x.VacationEndDate >= DateTime.Now).ToList();
            }

            return vacationRecordList;

        }

        [OperationActionFilter(nameof(Operation.VacationRecordView))]
        public ActionResult Details(int? id, int? version)
        {
            if (id == null)
            {
                return StatusCode(StatusCodes.Status400BadRequest);
            }
            VacationRecord vacationRecord = _vacationRecordService.GetById((int)id);
            if (vacationRecord == null)
            {
                return StatusCode(StatusCodes.Status404NotFound);
            }
            if (version != null && version.HasValue)
            {
                var recordVersion = _vacationRecordService.GetVersion(id.Value, version.Value);
                if (recordVersion == null)
                    return StatusCode(StatusCodes.Status404NotFound);
                recordVersion.Versions = new List<VacationRecord>().AsEnumerable();
                return View(recordVersion);
            }

            return View(vacationRecord);
        }

        [OperationActionFilter(nameof(Operation.VacationRecordCreateUpdate))]
        public ActionResult Create()
        {
            SetViewBag(null);
            return View();
        }

        [HttpPost]
        public string CalculateVacationDays(string vacationBeginDate, string vacationEndDate)
        {
            if ((!String.IsNullOrEmpty(vacationBeginDate)) && (!String.IsNullOrEmpty(vacationEndDate)))
            {
                DateTime beginDate = Convert.ToDateTime(vacationBeginDate).Date;
                DateTime endDate = Convert.ToDateTime(vacationEndDate).Date;

                if (!(beginDate > endDate))
                {
                    var holidays = _productionCalendarService.GetAllRecords().Where(x =>
                        x.CalendarDate >= beginDate && x.CalendarDate <= endDate && x.IsCelebratory == true
                    );

                    TimeSpan span = endDate - beginDate;

                    var days = (holidays == null) ? span.TotalDays + 1 : span.TotalDays - holidays.Count() + 1;

                    return days.ToString();

                }
            }

            return "";

        }

        [OperationActionFilter(nameof(Operation.VacationRecordCreateUpdate))]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(VacationRecord vacationRecord)
        {
            var currentUser = _userService.GetUserDataForVersion();

            if (vacationRecord.VacationBeginDate > vacationRecord.VacationEndDate)
            {
                ModelState.AddModelError("VacationBeginDate", "Дата начала не может быть больше даты конца.");
            }
            else if (CheckDateCrossing(vacationRecord))
            {
                ModelState.AddModelError("VacationBeginDate", "Выберите другой день.");
            }
            else
            {
                //SKIPR-563 - временное отключение ограничения, согласно которому нельзя создать запись отпуска, если недостаточно накопленных дней отпуска
                /*EmployeeVacationDto employeeVacationDto = GetAvailableVacationDays(_employeeService.GetById(vacationRecord.EmployeeID));

                if ((employeeVacationDto.AvailableVacationDays < vacationRecord.VacationDays) || (employeeVacationDto.AvailableVacationDays == 0))
                {
                    ModelState.AddModelError("VacationDays", "Доступных дней отпуска: " + employeeVacationDto.AvailableVacationDays);
                }*/
            }

            var reportingPeriodForBeginDate = _reportingPeriodService.GetAll(x => x.Month == vacationRecord.VacationBeginDate.Month && x.Year == vacationRecord.VacationBeginDate.Year).FirstOrDefault();
            var reportingPeriodForEndDate = _reportingPeriodService.GetAll(x => x.Month == vacationRecord.VacationEndDate.Month && x.Year == vacationRecord.VacationEndDate.Year).FirstOrDefault();

            if (reportingPeriodForBeginDate != null && reportingPeriodForBeginDate.NewTSRecordsAllowedUntilDate < DateTime.Now)
                ModelState.AddModelError("VacationBeginDate", "Отчетный период закрыт");
            if (reportingPeriodForEndDate != null && reportingPeriodForEndDate.NewTSRecordsAllowedUntilDate < DateTime.Now)
                ModelState.AddModelError("VacationEndDate", "Отчетный период закрыт");

            if (ModelState.IsValid)
            {
                vacationRecord.RecordSource = VacationRecordSource.UserInput;
                _vacationRecordService.Add(vacationRecord);
                //TODO добавить
                //_taskTimesheetProcessing.ProcessVacationRecords(currentEmployee, vacationRecord);
                return RedirectToAction("Index");
            }

            SetViewBag(vacationRecord);

            return View(vacationRecord);
        }

        private bool CheckDateCrossing(VacationRecord vacationRecord)
        {

            return _vacationRecordService.Get(records => records.Where(x => x.EmployeeID == vacationRecord.EmployeeID
                                                        && vacationRecord.VacationBeginDate <= x.VacationEndDate
                                                        && vacationRecord.VacationEndDate >= x.VacationBeginDate).ToList()).Count() > 0;

        }

        [OperationActionFilter(nameof(Operation.VacationRecordCreateUpdate))]
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return StatusCode(StatusCodes.Status400BadRequest);
            }
            VacationRecord vacationRecord = _vacationRecordService.GetById((int)id);
            if (vacationRecord == null)
            {
                return StatusCode(StatusCodes.Status404NotFound);
            }

            SetViewBag(vacationRecord);
            ViewBag.EmployeeID = new SelectList(_employeeService.Get(x => x.Where(e => e.ID == vacationRecord.EmployeeID).ToList()), "ID", "FullName", vacationRecord?.EmployeeID ?? 1);
            return View(vacationRecord);
        }

        [OperationActionFilter(nameof(Operation.VacationRecordCreateUpdate))]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(VacationRecord vacationRecord)
        {
            var currentUser = _userService.GetUserDataForVersion();
            var currentUserEmployee = _userService.GetEmployeeForCurrentUser();
            var tsHoursRecordByVacationList = _tsHoursRecordService.Get(x => x.Where(t => t.ParentVacationRecordID == vacationRecord.ID).ToList());
            var originalItem = _vacationRecordService.Get(records => records.Where(x => x.ID == vacationRecord.ID).AsNoTracking().ToList()).FirstOrDefault();

            if (vacationRecord.VacationBeginDate > vacationRecord.VacationEndDate)
            {
                ModelState.AddModelError("VacationBeginDate", "Дата начала не может быть больше даты конца.");
            }
            var reportingPeriodForBeginDate = _reportingPeriodService.GetAll(x => x.Month == vacationRecord.VacationBeginDate.Month && x.Year == vacationRecord.VacationBeginDate.Year).FirstOrDefault();
            var reportingPeriodForEndDate = _reportingPeriodService.GetAll(x => x.Month == vacationRecord.VacationEndDate.Month &&
                                                                                 x.Year == vacationRecord.VacationEndDate.Year).FirstOrDefault();

            //дату начала отпуска в закрытый перод менить нельзя
            if (reportingPeriodForBeginDate != null && reportingPeriodForBeginDate.NewTSRecordsAllowedUntilDate < DateTime.Now
                                                    && originalItem.VacationBeginDate != vacationRecord.VacationBeginDate)
                ModelState.AddModelError("VacationBeginDate", "Дату начала отпуска нельзя изменить, так как отчетный период закрыт.");

            //дату окончания отпуска в закрытый перод менить нельзя
            if (reportingPeriodForEndDate != null && reportingPeriodForEndDate.NewTSRecordsAllowedUntilDate < DateTime.Now
                                                    && originalItem.VacationEndDate != vacationRecord.VacationEndDate)
                ModelState.AddModelError("VacationEndDate", "Дату окончания отпуска нельзя изменить, так как отчетный период закрыт.");

            //невозможно изменить вид отпуска если запись в таймшите уже есть
            if (tsHoursRecordByVacationList != null
                && tsHoursRecordByVacationList.Count() != 0
                && originalItem.VacationType != vacationRecord.VacationType)
                ModelState.AddModelError("VacationType", "Невозможно изменить вид отпуска, так как созданы связанные записи ТШ.");

            if (ModelState.IsValid)
            {
                _vacationRecordService.Update(vacationRecord);

                //TODO добавить
                //_taskTimesheetProcessing.ProcessVacationRecords(currentEmployee, vacationRecord);
                return RedirectToAction("Index");
            }

            SetViewBag(vacationRecord);
            ViewBag.EmployeeID = new SelectList(_employeeService.Get(x => x.Where(e => e.ID == originalItem.EmployeeID).ToList()), "ID", "FullName", originalItem?.EmployeeID ?? 1);

            return View(vacationRecord);
        }

        [OperationActionFilter(nameof(Operation.VacationRecordDelete))]
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return StatusCode(StatusCodes.Status400BadRequest);
            }
            VacationRecord vacationRecord = _vacationRecordService.GetById((int)id);
            if (vacationRecord == null)
            {
                return StatusCode(StatusCodes.Status404NotFound);
            }
            return View(vacationRecord);
        }

        [OperationActionFilter(nameof(Operation.VacationRecordDelete))]
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            var vacationRecord = _vacationRecordService.GetById(id);
            var isDeleted = true;
            var errorRecycleToRecycleBin = string.Empty;
            if (vacationRecord != null)
            {
                var user = _userService.GetUserDataForVersion();
                var reportingPeriodForBeginDate = _reportingPeriodService.GetAll(x => x.Month == vacationRecord.VacationBeginDate.Month &&
                                                                                       x.Year == vacationRecord.VacationBeginDate.Year).FirstOrDefault();

                var reportingPeriodForEndDate = _reportingPeriodService.GetAll(x => x.Month == vacationRecord.VacationEndDate.Month &&
                                                                                     x.Year == vacationRecord.VacationEndDate.Year).FirstOrDefault();
                if (reportingPeriodForEndDate != null && reportingPeriodForBeginDate != null &&
                    (reportingPeriodForBeginDate.NewTSRecordsAllowedUntilDate > DateTime.Now &&
                     reportingPeriodForEndDate.NewTSRecordsAllowedUntilDate > DateTime.Now))
                {
                    var parentHoursRecord = _tsHoursRecordService.Get(x => x.Where(t => t.ParentVacationRecordID == id).ToList());
                    if (parentHoursRecord != null && parentHoursRecord.Count > 0)
                    {
                        _tsHoursRecordService.RemoveRange(parentHoursRecord);
                    }
                    var recycleBinInDBRelationVacationRecord = _serviceService.HasRecycleBinInDBRelation(new VacationRecord() { ID = id });
                    if (recycleBinInDBRelationVacationRecord.hasRelated == false)
                    {
                        var recycleToRecycleBinVacationRecord = _vacationRecordService.RecycleToRecycleBin(id, user.Item1, user.Item2);
                        isDeleted = recycleToRecycleBinVacationRecord.toRecycleBin;
                        errorRecycleToRecycleBin = recycleToRecycleBinVacationRecord.toRecycleBin ? string.Empty : recycleToRecycleBinVacationRecord.relatedClassId;
                    }
                    else
                        errorRecycleToRecycleBin = recycleBinInDBRelationVacationRecord.relatedInDBClassId;

                }
            }

            if (!isDeleted)
            {
                ViewBag.RecycleBinError = "Невозможно удалить, так как на удаляемый элемент ссылаются другие элементы в системе." +
                                          $"Сначала необходимо удалить элементы, которые ссылаются на данный элемент. {errorRecycleToRecycleBin}";
                return View(vacationRecord);
            }
            return RedirectToAction("Index");
        }

        //TODO перенести метод в VacationRecordService
        protected EmployeeVacationDto GetAvailableVacationDays(Employee employee)
        {
            EmployeeVacationDto employeeVacationDto = null;

            if (employee.EnrollmentDate.HasValue)
            {
                var employeeVacationRecordList = _vacationRecordService.Get(records => records.Where(x => x.EmployeeID == employee.ID).ToList());

                employeeVacationDto = new EmployeeVacationDto()
                {
                    ID = employee.ID,
                    LastName = employee.LastName,
                    FirstName = employee.FirstName,
                    MidName = employee.MidName,
                    FullName = employee.FullName,

                    EnrollmentDate = employee.EnrollmentDate
                };

                int durationOfVacation = 28;


                employeeVacationDto.VacationPaidDaysUsed = (employeeVacationRecordList.Where(r => r.VacationType == VacationRecordType.VacationPaid).Count() != 0) ?
                    employeeVacationRecordList.Where(r => r.VacationType == VacationRecordType.VacationPaid).Sum(x => x.VacationDays) : 0;

                employeeVacationDto.VacationNoPaidDaysUsed = (employeeVacationRecordList.Where(r => r.VacationType == VacationRecordType.VacationNoPaid).Count() != 0) ?
                    employeeVacationRecordList.Where(r => r.VacationType == VacationRecordType.VacationNoPaid).Sum(x => x.VacationDays) : 0;

                DateTime currentDay = DateTime.Today;
                DateTime enrollmentDate = employeeVacationDto.EnrollmentDate.Value;

                int subtractDays = 0;


                // 1. отпусков без сохранения заработной платы, превышающее 14 календарных дней в текущем рабочем году сотрудника;
                var employeeVacationNoPaidRecordList = employeeVacationRecordList.Where(r => r.VacationType == VacationRecordType.VacationNoPaid).ToList();

                foreach (var employeeVacationNoPaidRecord in employeeVacationNoPaidRecordList)
                {
                    if (employeeVacationNoPaidRecord.VacationBeginDate <= currentDay)
                    {
                        DateTime dateBegin = (employeeVacationNoPaidRecord.VacationBeginDate.Year == currentDay.Year) ? employeeVacationNoPaidRecord.VacationBeginDate : new DateTime(currentDay.Year, 1, 1);
                        DateTime dateEnd = (employeeVacationNoPaidRecord.VacationEndDate <= currentDay) ? employeeVacationNoPaidRecord.VacationEndDate : currentDay;

                        subtractDays += Convert.ToInt32(Math.Floor((dateEnd - dateBegin).TotalDays));
                    }
                }

                if (subtractDays < 14)
                {
                    subtractDays = 0;
                }

                //TODO
                // 2. нахождения в отпуске по уходу за ребенком;
                // 3. отсутствия без уважительных причин.

                enrollmentDate.AddDays(subtractDays);

                employeeVacationDto.SubtractDays = subtractDays;

                int monthCount = (currentDay.Year * 12 + currentDay.Month)
                    - (enrollmentDate.Year * 12 + enrollmentDate.Month)
                    + ((currentDay.Day >= enrollmentDate.Day) ? 0 : -1);

                if ((currentDay - enrollmentDate.AddMonths(monthCount)).TotalDays >= 15)
                {
                    employeeVacationDto.MonthCount = monthCount + 1;
                }
                else
                {
                    employeeVacationDto.MonthCount = monthCount;
                }

                employeeVacationDto.AvailableVacationDays = Convert.ToInt32(Math.Round((((double)durationOfVacation / 12) * employeeVacationDto.MonthCount) - employeeVacationDto.VacationPaidDaysUsed, 0, MidpointRounding.AwayFromZero));
            }

            return employeeVacationDto;
        }

        [HttpPost]
        [OperationActionFilter(nameof(Operation.VacationRecordView))]
        public string CalculationOfAvailableVacationDays(int? employeeID)
        {
            if (employeeID.HasValue)
            {

                var employee = _employeeService.GetById(employeeID.Value);

                if (employee.EnrollmentDate.HasValue)
                {
                    EmployeeVacationDto employeeVacationDto = GetAvailableVacationDays(employee);

                    return JsonConvert.SerializeObject(employeeVacationDto);
                }
            }

            return JsonConvert.SerializeObject("");
        }

        /*protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }*/

    }
}
