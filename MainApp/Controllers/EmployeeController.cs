using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.IO;
using Newtonsoft.Json;
using System.Text.RegularExpressions;
using System.Dynamic;
using System.ComponentModel.DataAnnotations;
using Core.BL;
using Core.BL.Interfaces;
using Core.Config;
using Core.Helpers;
using Core.Models;
using Core.Models.RBAC;
using Core.RecordVersionHistory;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using MainApp.BitrixSync;
using MainApp.RBAC.Attributes;
using MainApp.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;





using X.PagedList;


namespace MainApp.Controllers
{
    public enum EmployeeViewType
    {
        [Display(Name = "Все сотрудники ГК")]
        AllActualEmployee,
        [Display(Name = "Все сотрудники ГК и вакансии")]
        AllActualEmployeeAndVacancy
    }


    public class SearchItem
    {
        public string Value { get; set; }
        public string Text { get; set; }
        public object Item { get; set; }
    }

    public class EmployeeController : Controller
    {
        private readonly IOptions<BitrixConfig> _bitrixConfigOptions;
        private readonly IPermissionValidatorService _permissionValidatorService;

        private readonly IEmployeeService _employeeService;
        private readonly IEmployeePositionService _employeePositionService;
        private readonly IUserService _userService;
        private readonly IApplicationUserService _applicationUserService;
        private readonly IOOService _ooService;
        private readonly IFinanceService _financeService;
        private readonly IEmployeePositionAssignmentService _employeePositionAssignmentService;
        private readonly IEmployeeOrganisationService _employeeOrganisationService;
        private readonly IServiceService _serviceService;
        private readonly IEmployeePositionOfficialService _employeePositionOfficialService;
        private readonly IDepartmentService _departmentService;
        private readonly IEmployeeGradService _employeeGradService;
        private readonly IOrganisationService _organisationService;
        private readonly IEmployeeLocationService _employeeLocationService;
        private readonly IEmployeeCategoryService _employeeCategoryService;
        private readonly IEmployeeQualifyingRoleService _employeeQualifyingRoleService;
        private readonly IEmployeeGradAssignmentService _employeeGradAssignmentService;
        private readonly IEmployeeDepartmentAssignmentService _employeeDepartmentAssignmentService;
        private readonly IEmployeePositionOfficialAssignmentService _employeePositionOfficialAssignmentService;
        private readonly IVacationRecordService _vacationRecordService;

        public EmployeeController(IOptions<BitrixConfig> bitrixConfigOptions, IPermissionValidatorService permissionValidatorService, IEmployeeService employeeService,
            IEmployeePositionService employeePositionService,
            IEmployeePositionOfficialService employeePositionOfficialService,
            IDepartmentService departmentService,
            IEmployeeGradService employeeGradService,
            IOrganisationService organisationService,
            IEmployeeLocationService employeeLocationService,
            IEmployeeCategoryService employeeCategoryService,
            IEmployeeQualifyingRoleService employeeQualifyingRoleService,
            IEmployeeGradAssignmentService employeeGradAssignmentService,
            IEmployeeDepartmentAssignmentService employeeDepartmentAssignmentService,
            IEmployeePositionOfficialAssignmentService employeePositionOfficialAssignmentService,
            IVacationRecordService vacationRecordService, IUserService userService,
            IApplicationUserService applicationUserService,
            IOOService ooService, IFinanceService financeService, IEmployeePositionAssignmentService employeePositionAssignmentService, IEmployeeOrganisationService employeeOrganisationService, IServiceService serviceService)

        {
            _bitrixConfigOptions = bitrixConfigOptions;
            _permissionValidatorService = permissionValidatorService;
            _employeeService = employeeService;
            _employeePositionService = employeePositionService;
            _employeePositionOfficialService = employeePositionOfficialService;
            _departmentService = departmentService;
            _employeeGradService = employeeGradService;
            _organisationService = organisationService;
            _employeeLocationService = employeeLocationService;
            _employeeCategoryService = employeeCategoryService;
            _employeeQualifyingRoleService = employeeQualifyingRoleService;
            _employeeGradAssignmentService = employeeGradAssignmentService;
            _employeeDepartmentAssignmentService = employeeDepartmentAssignmentService;
            _employeePositionOfficialAssignmentService = employeePositionOfficialAssignmentService;
            _vacationRecordService = vacationRecordService;
            _userService = userService;
            _applicationUserService = applicationUserService;
            _ooService = ooService;
            _financeService = financeService;
            _employeePositionAssignmentService = employeePositionAssignmentService;
            _employeeOrganisationService = employeeOrganisationService;
            _serviceService = serviceService;
        }

        [OperationActionFilter(nameof(Operation.EmployeeView))]
        public ActionResult Index(string searchString, int? page)
        {
            if (searchString != null)
            {
                page = 1;
            }

            List<Employee> employeeList = null;

            ApplicationUser user = _applicationUserService.GetUser();

            if (_applicationUserService.HasAccess(Operation.EmployeeCreateUpdate) == true
               || _applicationUserService.HasAccess(Operation.EmployeeADUpdate) == true
               || _applicationUserService.HasAccess(Operation.EmployeePersonalDataView) == true
               || _applicationUserService.HasAccess(Operation.EmployeeFullListView) == true)
            {
                employeeList = _employeeService.Get(x => x.Include(e => e.Department).Include(e => e.EmployeePosition).Include(e => e.EmployeeGrad).ToList()).ToList();
            }
            else if (_applicationUserService.HasAccess(Operation.EmployeeSubEmplPersonalDataView) == true)
            {
                employeeList = _employeeService.GetAllManagedEmployees(_applicationUserService.GetUser().ManagedDepartments).ToList();
            }
            else
            {
                int userEmployeeID = _applicationUserService.GetEmployeeID();
                employeeList = _employeeService.Get(x =>
                    x.Include(e => e.Department).Include(e => e.EmployeePosition).Include(e => e.EmployeeGrad)
                        .Where(e => e.ID == userEmployeeID).ToList()).ToList();

                string alternateExternalEmployeeListURL = RPCSHelper.GetAlternateExternalEmployeeListURL();

                if (String.IsNullOrEmpty(alternateExternalEmployeeListURL) == false)
                {
                    return Redirect(alternateExternalEmployeeListURL);
                }
            }

            if (!String.IsNullOrEmpty(searchString))
            {
                string[] searchTokens = searchString.Split(' ');

                List<string> searchTokensList = new List<string>();
                for (int i = 0; i < searchTokens.Length; i++)
                {
                    if (String.IsNullOrEmpty(searchTokens[i]) == false
                        && String.IsNullOrEmpty(searchTokens[i].Trim()) == false)
                    {
                        searchTokensList.Add(searchTokens[i].Trim().ToLower());
                    }
                }
                if (searchTokensList.Count > 1)
                {
                    employeeList = employeeList.Where(e => searchTokensList.All(stl => (e.FirstName != null && e.FirstName.ToLower().Equals(stl))
                                           || (e.LastName != null && e.LastName.ToLower().Equals(stl))
                                           || (e.MidName != null && (e.MidName.ToLower().Split(' ')[0].Equals(stl) || (e.MidName.ToLower().Split(' ').Length == 2 && e.MidName.ToLower().Split(' ')[1].Equals(stl))))
                                           || (e.Email != null && e.Email.ToLower().Contains(stl))
                                           || (e.EmployeePositionTitle != null && e.EmployeePositionTitle.ToLower().Contains(stl))
                                           /*|| (e.Department != null && e.Department.ShortName.ToLower().Equals(stl.ToLower()))*/)).ToList();
                }
                else if (searchString.Trim().Length < searchString.Length)
                {
                    employeeList = employeeList.Where(e => (e.FirstName != null && e.FirstName.ToLower().Equals(searchString.Trim().ToLower()))
                                           || (e.LastName != null && e.LastName.ToLower().Equals(searchString.Trim().ToLower()))
                                           || (e.MidName != null && e.MidName.ToLower().Equals(searchString.Trim().ToLower()))
                                           || (e.Email != null && e.Email.ToLower().Equals(searchString.Trim().ToLower()))
                                           || (e.EmployeePositionTitle != null && e.EmployeePositionTitle.ToLower().Equals(searchString.Trim().ToLower()))
                                           || (e.Department != null && e.Department.ShortName != null && e.Department.ShortName.ToLower().Equals(searchString.Trim().ToLower()))).ToList();
                }
                else
                {
                    employeeList = employeeList.Where(e => (e.FirstName != null && e.FirstName.ToLower().Contains(searchString.Trim().ToLower()))
                                           || (e.LastName != null && e.LastName.ToLower().Contains(searchString.Trim().ToLower()))
                                           || (e.MidName != null && e.MidName.ToLower().Contains(searchString.Trim().ToLower()))
                                           || (e.Email != null && e.Email.ToLower().Contains(searchString.Trim().ToLower()))
                                           || (e.EmployeePositionTitle != null && e.EmployeePositionTitle.ToLower().Contains(searchString.Trim().ToLower()))
                                           || (e.Department != null && e.Department.ShortName != null && e.Department.ShortName.ToLower().Equals(searchString.Trim().ToLower()))).ToList();
                }
            }

            employeeList = employeeList.OrderBy(e => e.FullName).ToList();

            if (_applicationUserService.HasAccess(Operation.EmployeeCreateUpdate | Operation.EmployeePersonalDataView | Operation.EmployeeADUpdate | Operation.EmployeeFullListView) == false)
            {
                employeeList = employeeList.Where(e => e.IsVacancy != true
                    && String.IsNullOrEmpty(e.EmployeePositionTitle) == false
                    && (e.DismissalDate == null || e.DismissalDate >= DateTime.Today)
                    && e.Department != null).OrderBy(e => e.FullName).ToList();
            }

            int pageSize = 20;
            int pageNumber = (page ?? 1);

            ViewBag.SearchEmployees = new SelectList(employeeList.OrderBy(e => e.LastName), "ID", "FullName");

            return View(employeeList.ToPagedList(pageNumber, pageSize));
        }

        [OperationActionFilter(nameof(Operation.EmployeeView))]
        public ActionResult Details(int? id, int? version)
        {
            if (id == null)
            {
                return StatusCode(StatusCodes.Status400BadRequest);
            }
            if (version != null && version.HasValue)
            {
                int employeeID = id.Value;

                Employee employeeVersion = _employeeService.Get(e => e.Where(x => x.ItemID == employeeID
                                                                                   && ((x.VersionNumber == version.Value) || (version.Value == 0 && x.VersionNumber == null))).ToList(), GetEntityMode.VersionAndOther).FirstOrDefault();

                if (employeeVersion == null)
                    return StatusCode(StatusCodes.Status404NotFound);



                employeeVersion.EmployeeCategories = _employeeCategoryService.Get(x =>
                    x.Where(ec => ec.EmployeeID == employeeID).OrderByDescending(ec => ec.CategoryDateBegin).ThenByDescending(ec => ec.ID).ToList());
                employeeVersion.EmployeeQualifyingRoles = _employeeQualifyingRoleService.Get(x =>
                    x.Include(eqr => eqr.QualifyingRole).Where(eqr => eqr.EmployeeID == employeeID)
                        .OrderByDescending(eqr => eqr.QualifyingRoleDateBegin).ThenByDescending(eqr => eqr.ID).ToList());
                employeeVersion.EmployeeGradAssignments = _employeeGradAssignmentService.Get(x =>
                    x.Include(ega => ega.Employee).Include(ega => ega.EmployeeGrad)
                        .Where(ega => ega.EmployeeID == employeeID).OrderByDescending(ega => ega.BeginDate).ThenByDescending(ega => ega.ID).ToList());
                employeeVersion.EmployeeDepartmentAssignments = _employeeDepartmentAssignmentService.Get(x =>
                    x.Include(eda => eda.Employee).Include(eda => eda.Department)
                        .Where(eda => eda.EmployeeID == employeeID).OrderByDescending(eda => eda.BeginDate).ThenByDescending(eda => eda.ID).ToList());
                employeeVersion.EmployeePositionAssignments = _employeePositionAssignmentService.Get(x =>
                    x.Include(epa => epa.Employee).Include(epa => epa.EmployeePosition)
                        .Where(epa => epa.EmployeeID == employeeID).OrderByDescending(epa => epa.BeginDate).ThenByDescending(epa => epa.ID).ToList());
                
                employeeVersion.VacationRecords = _vacationRecordService.Get(v =>
                    v.Where(evr => evr.EmployeeID == employeeID).Include(evr => evr.Employee)
                        .OrderByDescending(x => x.VacationBeginDate).ToList());
                employeeVersion.EmployeeOrganisation = _employeeOrganisationService.Get(eos => eos.Where(eo => eo.EmployeeID == employeeID)
                .Include(eo => eo.EmployeePositionOfficial)
                .Include(eo => eo.Organisation)
                .OrderByDescending(x => x.OrganisationDateBegin).ToList());

                employeeVersion.Versions = new List<Employee>().AsEnumerable();

                ViewBag.Title = employeeVersion.FullName;

                return View(employeeVersion);
            }
            else
            {
                Employee employee = _employeeService.GetById(id.Value);
                if (employee == null)
                {
                    return StatusCode(StatusCodes.Status404NotFound);
                }


                employee.EmployeeCategories = _employeeCategoryService.Get(x =>
                    x.Where(ec => ec.EmployeeID == employee.ID).ToList().OrderByDescending(ec => ec.CategoryDateBegin).ThenByDescending(ec => ec.ID).ToList());
                employee.EmployeeQualifyingRoles = _employeeQualifyingRoleService.Get(x =>
                    x.Include(eqr => eqr.QualifyingRole).Where(eqr => eqr.EmployeeID == employee.ID)
                        .OrderByDescending(eqr => eqr.QualifyingRoleDateBegin).ThenByDescending(eqr => eqr.ID).ToList());
                employee.EmployeeGradAssignments = _employeeGradAssignmentService.Get(x =>
                    x.Include(ega => ega.Employee).Include(ega => ega.EmployeeGrad)
                        .Where(ega => ega.EmployeeID == employee.ID).OrderByDescending(ega => ega.BeginDate).ThenByDescending(ega => ega.ID).ToList());
                employee.EmployeeDepartmentAssignments = _employeeDepartmentAssignmentService.Get(x =>
                    x.Include(eda => eda.Employee).Include(eda => eda.Department)
                        .Where(eda => eda.EmployeeID == employee.ID).OrderByDescending(eda => eda.BeginDate).ThenByDescending(eda => eda.ID).ToList());
                employee.EmployeePositionAssignments = _employeePositionAssignmentService.Get(x =>
                    x.Include(epa => epa.Employee).Include(epa => epa.EmployeePosition)
                        .Where(epa => epa.EmployeeID == employee.ID).OrderByDescending(epa => epa.BeginDate).ThenByDescending(epa => epa.ID).ToList());
                employee.VacationRecords = _vacationRecordService.Get(v =>
                    v.Where(evr => evr.EmployeeID == employee.ID).Include(evr => evr.Employee)
                        .OrderByDescending(x => x.VacationBeginDate).ToList());

                employee.EmployeeOrganisation = _employeeOrganisationService.Get(eos => eos.Where(eo => eo.EmployeeID == employee.ID)
                .Include(eo => eo.EmployeePositionOfficial)
                .Include(eo => eo.Organisation)
                .OrderByDescending(x => x.OrganisationDateBegin).ToList());

                employee.Versions = _employeeService.Get(x => x
                    .Where(p => p.ItemID == employee.ID || p.ID == employee.ID)
                    .OrderByDescending(p => p.VersionNumber).ToList(), GetEntityMode.VersionAndOther);

                int versionsCount = employee.Versions.Count();
                for (int i = 0; i < versionsCount; i++)
                {

                    if (i == versionsCount - 1)
                        continue;

                    var changes = ChangedRecordsFiller.GetChangedData(employee.Versions.ElementAt(i), employee.Versions.ElementAt(i + 1));
                    employee.Versions.ElementAt(i).ChangedRecords = changes;
                }

                ViewBag.Title = employee.FullName;

                return View(employee);
            }
        }

        [OperationActionFilter(nameof(Operation.EmployeeCreateUpdate))]
        public ActionResult Create()
        {
            ViewBag.EmployeePositionID = new SelectList(_employeePositionService.Get(x => x.ToList().OrderBy(ep => ep.ShortName).ToList()), "ID", "FullName");
            ViewBag.EmployeePositionOfficialID = new SelectList(_employeePositionOfficialService.Get(x => x.ToList().OrderBy(ep => ep.ShortName).ToList()), "ID", "FullName");
            ViewBag.EmployeePositionFromList = true;
            ViewBag.DepartmentID = new SelectList(_departmentService.Get(x => x.ToList().OrderBy(d => d.ShortName).ToList()), "ID", "FullName");
            ViewBag.OrganisationID = new SelectList(_organisationService.Get(x => x.ToList().OrderBy(o => o.ShortName).ToList()), "ID", "FullName");
            ViewBag.EmployeeLocationID = new SelectList(_employeeLocationService.Get(x => x.ToList().OrderBy(el => el.ShortName).ToList()), "ID", "FullName");
            ViewBag.EmployeeGradID = new SelectList(_employeeGradService.Get(x => x.ToList().OrderBy(e => e.ShortName).ToList()), "ID", "FullName");
            
            return View();
        }

        [OperationActionFilter(nameof(Operation.EmployeeCreateUpdate))]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(string DepartmentDateStart, string EmployeePositionDateStart,
            string EmployeePositionOfficialDateStart, string EmployeeGradDateStart,
            string employeePositionEditMode, Employee employee, int? isvacancy)
        {
            if (employee.EnrollmentDate.HasValue && employee.ProbationEndDate.HasValue)
            {
                if (employee.EnrollmentDate > employee.ProbationEndDate)
                    ModelState.AddModelError("ProbationEndDate", "Дата завершения испытательного срока не может быть ранее, чем значение 'Принят'");
            }

            if (isvacancy != null && isvacancy.Value == 1)
            {//создание вакансии
                employee.IsVacancy = true;

                if (String.IsNullOrEmpty(employee.VacancyID))
                {
                    ModelState.AddModelError("VacancyID", "Необходимо указать ID вакансии");
                }

                if (ModelState.IsValid)
                {
                    if (employee.EmployeePositionID.HasValue == true)
                    {
                        employee.EmployeePositionTitle = _employeePositionService.GetById(employee.EmployeePositionID.Value).Title;
                    }
                    _employeeService.Add(employee);
                    return RedirectToAction("Index");
                }
            }
            else
            {
                // Приводим телефоны к нужному формату
                if (!String.IsNullOrEmpty(employee.PersonalMobilePhoneNumber))
                {
                    employee.PersonalMobilePhoneNumber = FormatPhoneNumber(employee.PersonalMobilePhoneNumber);
                }

                if (!String.IsNullOrEmpty(employee.PublicMobilePhoneNumber))
                {
                    employee.PublicMobilePhoneNumber = FormatPhoneNumber(employee.PublicMobilePhoneNumber);
                }

                if (!String.IsNullOrEmpty(employee.WorkPhoneNumber))
                {
                    employee.WorkPhoneNumber = FormatPhoneNumber(employee.WorkPhoneNumber);
                }

                // Приводим ФИО к нужному формату
                if (!String.IsNullOrEmpty(employee.FirstName))
                {
                    employee.FirstName = FormatName(employee.FirstName);
                }

                if (!String.IsNullOrEmpty(employee.LastName))
                {
                    employee.LastName = FormatName(employee.LastName);
                }

                if (!String.IsNullOrEmpty(employee.MidName))
                {
                    employee.MidName = FormatName(employee.MidName);
                }

                // Проверим, что сотрудник с таким ФИО отсутствует в базе
                if (EmployeeWithSameNameExist(employee.FirstName, employee.MidName, employee.LastName, employee.ID))
                {
                    ModelState.AddModelError("MidName", @"Карточка сотрудника с указанным ФИО уже есть в системе.");
                }

                // Валидация полей с датами
                if (employee.DepartmentID.HasValue)
                {
                    if (String.IsNullOrEmpty(DepartmentDateStart))
                    {
                        ModelState.AddModelError("Department", "Необходимо указать дату вступления в действие изменений.");
                    }
                }

                if (employee.EmployeeGradID.HasValue)
                {
                    if (String.IsNullOrEmpty(EmployeeGradDateStart))
                    {
                        ModelState.AddModelError("EmployeeGrad", "Необходимо указать дату вступления в действие изменений.");
                    }
                }


                if (employee.EmployeePositionOfficialID.HasValue)
                {
                    if (String.IsNullOrEmpty(EmployeePositionOfficialDateStart))
                    {
                        ModelState.AddModelError("EmployeePositionOfficial", "Необходимо указать дату вступления в действие изменений.");
                    }
                }

                if (RPCSHelper.IsDissalowInputEmployeePositionAsText() == true)
                {
                    employeePositionEditMode = "EmployeePositionFromList";
                }

                if (employeePositionEditMode.Equals("EmployeePositionFromList") == true)
                {
                    if (employee.EmployeePositionID.HasValue == true)
                    {
                        if (String.IsNullOrEmpty(EmployeePositionDateStart))
                        {
                            ModelState.AddModelError("EmployeePositionTitle", "Необходимо указать дату вступления в действие изменений.");
                        }
                    }
                }
                else if (employeePositionEditMode.Equals("EmployeePositionAsText") == true)
                {
                    if (String.IsNullOrEmpty(employee.EmployeePositionTitle) == false)
                    {
                        if (String.IsNullOrEmpty(EmployeePositionDateStart))
                        {
                            ModelState.AddModelError("EmployeePositionTitle", "Необходимо указать дату вступления в действие изменений.");
                        }
                    }
                }
                

                if (ModelState.IsValid)
                {
                    EmployeeGradAssignment employeeGradAssignment = null;

                    if (employee.EmployeeGradID.HasValue == true)
                    {
                        employeeGradAssignment = new EmployeeGradAssignment();

                        employeeGradAssignment.EmployeeGradID = employee.EmployeeGradID;
                        employeeGradAssignment.BeginDate = Convert.ToDateTime(EmployeeGradDateStart);
                        employeeGradAssignment.Comments = "Указано в карточке сотрудника пользователем: " + User.Identity.Name;
                    }

                    EmployeeDepartmentAssignment employeeDepartmentAssignment = null;

                    if (employee.DepartmentID.HasValue == true)
                    {
                        employeeDepartmentAssignment = new EmployeeDepartmentAssignment();

                        employeeDepartmentAssignment.DepartmentID = employee.DepartmentID;
                        employeeDepartmentAssignment.BeginDate = Convert.ToDateTime(DepartmentDateStart);
                        employeeDepartmentAssignment.Comments = "Указано в карточке сотрудника пользователем: " + User.Identity.Name;
                    }

                    string employeePositionTitle = "";
                    EmployeePositionAssignment employeePositionAssignment = null;

                    if (employeePositionEditMode.Equals("EmployeePositionFromList") == true)
                    {
                        if (employee.EmployeePositionID.HasValue == true)
                        {
                            employeePositionTitle = _employeePositionService.GetById(employee.EmployeePositionID.Value).Title;

                            employeePositionAssignment = new EmployeePositionAssignment();

                            employeePositionAssignment.EmployeePositionID = employee.EmployeePositionID;
                            employeePositionAssignment.EmployeePositionTitle = employeePositionTitle;
                            employeePositionAssignment.BeginDate = Convert.ToDateTime(EmployeePositionDateStart);
                            employeePositionAssignment.Comments = "Указано в карточке сотрудника пользователем: " + User.Identity.Name;
                        }
                    }
                    else if (employeePositionEditMode.Equals("EmployeePositionAsText") == true)
                    {
                        if (String.IsNullOrEmpty(employee.EmployeePositionTitle) == false)
                        {
                            employeePositionTitle = employee.EmployeePositionTitle;

                            employeePositionAssignment = new EmployeePositionAssignment();

                            //temp fix - необходимо сделать поле EmployeePositionID не обязательным (удалить атрибут [Required])
                            employeePositionAssignment.EmployeePositionID = 1;
                            employeePositionAssignment.EmployeePositionTitle = employee.EmployeePositionTitle;
                            employeePositionAssignment.BeginDate = Convert.ToDateTime(EmployeePositionDateStart);
                            employeePositionAssignment.Comments = "Указано в карточке сотрудника пользователем: " + User.Identity.Name;

                            employee.EmployeePositionID = null;
                        }
                    }

                    //EmployeePositionOfficialAssignment employeePositionOfficialAssignment = null;

                    //if (employee.EmployeePositionOfficialID.HasValue == true)
                    //{
                    //    string employeePositionOfficialTitle = _employeePositionOfficialService
                    //        .GetById(employee.EmployeePositionOfficialID.Value).Title;

                    //    employeePositionOfficialAssignment = new EmployeePositionOfficialAssignment();

                    //    employeePositionOfficialAssignment.EmployeePositionOfficialID = employee.EmployeePositionID;
                    //    // employeePositionOfficialAssignment.EmployeePositionTitle = employeePositionTitle;
                    //    employeePositionOfficialAssignment.BeginDate = Convert.ToDateTime(EmployeePositionDateStart);
                    //    employeePositionOfficialAssignment.Comments = "Указано в карточке сотрудника пользователем: " + User.Identity.Name;
                    //}

                    employee.EmployeePositionTitle = employeePositionTitle;
                    _employeeService.Add(employee);

                    if (employeeGradAssignment != null)
                    {
                        employeeGradAssignment.EmployeeID = employee.ID;
                        _employeeGradAssignmentService.Add(employeeGradAssignment);
                    }

                    if (employeeDepartmentAssignment != null)
                    {
                        employeeDepartmentAssignment.EmployeeID = employee.ID;
                        _employeeDepartmentAssignmentService.Add(employeeDepartmentAssignment);
                    }
                    
                    //if (employeePositionOfficialAssignment != null)
                    //{
                    //    employeePositionOfficialAssignment.EmployeeID = employee.ID;
                    //    _employeePositionOfficialAssignmentService.Add(employeePositionOfficialAssignment);
                    //}

                    return RedirectToAction("Details", new { id = employee.ID });
                }
            }

            ViewBag.EmployeePositionID = new SelectList(_employeePositionService.Get(x => x.ToList().OrderBy(ep => ep.ShortName).ToList()), "ID", "FullName", employee.EmployeePositionID);
            ViewBag.EmployeePositionOfficialID = new SelectList(_employeePositionOfficialService.Get(x => x.ToList().OrderBy(ep => ep.ShortName).ToList()), "ID", "FullName", employee.EmployeePositionOfficialID);
            ViewBag.DepartmentID = new SelectList(_departmentService.Get(x => x.ToList().OrderBy(d => d.ShortName).ToList()), "ID", "FullName", employee.DepartmentID);
            ViewBag.OrganisationID = new SelectList(_organisationService.Get(x => x.ToList().OrderBy(o => o.ShortName).ToList()), "ID", "FullName", employee.OrganisationID);
            ViewBag.EmployeeLocationID = new SelectList(_employeeLocationService.Get(x => x.ToList().OrderBy(el => el.ShortName).ToList()), "ID", "FullName", employee.EmployeeLocationID);
            ViewBag.EmployeeGradID = new SelectList(_employeeGradService.Get(x => x.ToList().OrderBy(e => e.ShortName).ToList()), "ID", "FullName", employee.EmployeeGradID);

            ViewBag.EmployeePositionFromList = employee.EmployeePositionID.HasValue;

            ViewBag.Message = "Запрос не прошел валидацию.";
            return View(employee);
        }

        private string Format11DigitsNumber(string phoneNumber)
        {
            if (phoneNumber.Length == 11)
            {
                if (phoneNumber[0] == '8' || phoneNumber[0] == '7')
                {
                    return string.Format("+7({0}){1}-{2}-{3}", phoneNumber.Substring(1, 3), phoneNumber.Substring(4, 3), phoneNumber.Substring(7, 2), phoneNumber.Substring(9, 2));
                }
            }

            return phoneNumber;
        }

        private string FormatPhoneNumber(string number)
        {
            string phoneNumber = number.Trim();
            if (phoneNumber == "") { return phoneNumber; }

            Regex digitsOnly = new Regex(@"[^\d]");
            phoneNumber = digitsOnly.Replace(phoneNumber, "");

            if (phoneNumber.Length == 11)
            {
                // Стандартный номер
                phoneNumber = Format11DigitsNumber(phoneNumber);
            }
            else
            {
                // Нестандартный номер
                if (phoneNumber.Length < 11)
                {
                    if (phoneNumber.Length == 10)
                    {
                        phoneNumber = Format11DigitsNumber("8" + phoneNumber);
                    }
                    else
                    {
                        // короткий
                        //return phoneNumber;
                    }
                }
                else
                {
                    // с добавочным
                    string number1 = phoneNumber.Substring(0, 11);
                    string cummulativeNumber = "";
                    string number2 = "";
                    for (int i = 0; i < number.Length - 1; i++)
                    {
                        if (char.IsDigit(number[i]))
                        {
                            cummulativeNumber = cummulativeNumber + number[i];
                            if (cummulativeNumber == number1)
                            {
                                if (number.Length > i + 1)
                                {
                                    number2 = number.Substring(i + 1).Trim();
                                }
                                break;
                            }
                        }
                    }

                    phoneNumber = Format11DigitsNumber(number1) + " " + number2;
                    return phoneNumber;
                }
            }

            return phoneNumber;
        }

        private string FormatName(string name)
        {
            if (String.IsNullOrEmpty(name))
            {
                return name;
            }

            string formattedName = "";
            string[] fnParts = name.Trim().Split('-');
            foreach (string fnPart in fnParts)
            {
                formattedName = formattedName + FirstCharToUpper(fnPart.Trim()) + "-";
            }
            return formattedName.TrimEnd('-');
        }

        private string FirstCharToUpper(string input)
        {
            if (String.IsNullOrEmpty(input))
            {
                return input;
            }
            return input.First().ToString().ToUpper() + input.Substring(1).ToLower();
        }

        [AEmployeeAnyUpdate]
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return StatusCode(StatusCodes.Status400BadRequest);
            }

            Employee employee = _employeeService.GetById(id.Value);
            if (employee == null)
            {
                return StatusCode(StatusCodes.Status404NotFound);
            }
            if (employee.IsVersion)
                return StatusCode(StatusCodes.Status403Forbidden);

            ViewBag.EmployeePositionID = new SelectList(_employeePositionService.Get(x => x.ToList().OrderBy(ep => ep.ShortName).ToList()), "ID", "FullName", employee.EmployeePositionID);
            if (employee.EmployeePositionID.HasValue == true)
            {
                ViewBag.EmployeePositionFromList = true;
            }
            else
            {
                ViewBag.EmployeePositionFromList = false;
            }

            ViewBag.EmployeePositionOfficialID = new SelectList(_employeePositionOfficialService.Get(x => x.ToList().OrderBy(ep => ep.ShortName).ToList()), "ID", "FullName", employee.EmployeePositionOfficialID);

            ViewBag.DepartmentID = new SelectList(_departmentService.Get(x => x.ToList().OrderBy(d => d.ShortName).ToList()), "ID", "FullName", employee.DepartmentID);
            ViewBag.OrganisationID = new SelectList(_organisationService.Get(x => x.ToList().OrderBy(o => o.ShortName).ToList()), "ID", "FullName", employee.OrganisationID);
            ViewBag.EmployeeLocationID = new SelectList(_employeeLocationService.Get(x => x.ToList().OrderBy(el => el.ShortName).ToList()), "ID", "FullName", employee.EmployeeLocationID);
            ViewBag.EmployeeGradID = new SelectList(_employeeGradService.Get(x => x.ToList().OrderBy(e => e.ShortName).ToList()), "ID", "FullName", employee.EmployeeGradID);
            
            return View(employee);
        }

        //TODO - метод чтобы в случае AEmployeeSelfUpdate на вход поступали только разрешенные поля, остальные отбрасывались
        [AEmployeeAnyUpdate]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(string DepartmentDateStart, string EmployeePositionDateStart,
            string EmployeePositionOfficialDateStart, string EmployeeGradDateStart,
            string employeePositionEditMode,
            Employee employee,
            int? isvacancy, int? employeefromvacancy)
        {
            if (employee.EnrollmentDate.HasValue && employee.ProbationEndDate.HasValue)
            {
                if (employee.EnrollmentDate > employee.ProbationEndDate)
                    ModelState.AddModelError("ProbationEndDate", "Дата завершения испытательного срока не может быть ранее, чем значение 'Принят'");
            }

            if (isvacancy != null && isvacancy.Value == 1)
            {
                employee.IsVacancy = true;

                if (String.IsNullOrEmpty(employee.VacancyID))
                {
                    ModelState.AddModelError("VacancyID", "Необходимо указать ID вакансии");
                }

                if (ModelState.IsValid)
                {

                    if (employee.EmployeePositionID.HasValue == true)
                    {
                        employee.EmployeePositionTitle = _employeePositionService.GetById(employee.EmployeePositionID.Value).Title;
                    }
                    _employeeService.Update(employee);

                    return RedirectToAction("Details", new { id = employee.ID });
                }
            }
            else
            {
                Employee originalEmployee = _employeeService.Get(x => x.AsNoTracking().Where(e => e.ID == employee.ID).ToList()).FirstOrDefault();

                // Приводим телефоны к нужному формату
                if (!String.IsNullOrEmpty(employee.PersonalMobilePhoneNumber))
                {
                    employee.PersonalMobilePhoneNumber = FormatPhoneNumber(employee.PersonalMobilePhoneNumber);
                }

                if (!String.IsNullOrEmpty(employee.PublicMobilePhoneNumber))
                {
                    employee.PublicMobilePhoneNumber = FormatPhoneNumber(employee.PublicMobilePhoneNumber);
                }

                if (!String.IsNullOrEmpty(employee.WorkPhoneNumber))
                {
                    employee.WorkPhoneNumber = FormatPhoneNumber(employee.WorkPhoneNumber);
                }

                // Приводим ФИО к нужному формату
                if (!String.IsNullOrEmpty(employee.FirstName))
                {
                    employee.FirstName = FormatName(employee.FirstName);
                }

                if (!String.IsNullOrEmpty(employee.LastName))
                {
                    employee.LastName = FormatName(employee.LastName);
                }

                if (!String.IsNullOrEmpty(employee.MidName))
                {
                    employee.MidName = FormatName(employee.MidName);
                }

                // Проверим, что сотрудник с таким ФИО отсутствует в базе
                if (EmployeeWithSameNameExist(employee.FirstName, employee.MidName, employee.LastName, employee.ID))
                {
                    ModelState.AddModelError("MidName", @"Карточка сотрудника с указанным ФИО уже есть в системе.");
                }


                // Валидация полей с датами
                if (employee.DepartmentID.HasValue == true
                        && (originalEmployee.DepartmentID.HasValue == false
                        || originalEmployee.DepartmentID != employee.DepartmentID))
                {
                    if (String.IsNullOrEmpty(DepartmentDateStart))
                    {
                        ModelState.AddModelError("Department", "Необходимо указать дату вступления в действие изменений.");
                    }
                }

                if (employee.EmployeeGradID.HasValue == true
                        && (originalEmployee.EmployeeGradID.HasValue == false
                        || originalEmployee.EmployeeGradID != employee.EmployeeGradID))
                {
                    if (String.IsNullOrEmpty(EmployeeGradDateStart))
                    {
                        ModelState.AddModelError("EmployeeGrad", "Необходимо указать дату вступления в действие изменений.");
                    }
                }

                if (employee.EmployeePositionOfficialID.HasValue == true
                    && (originalEmployee.EmployeePositionOfficialID.HasValue == false
                    || originalEmployee.EmployeePositionOfficialID != employee.EmployeePositionOfficialID))
                {
                    if (String.IsNullOrEmpty(EmployeePositionOfficialDateStart))
                    {
                        ModelState.AddModelError("EmployeePositionOfficial", "Необходимо указать дату вступления в действие изменений.");
                    }
                }

                if (RPCSHelper.IsDissalowInputEmployeePositionAsText() == true)
                {
                    employeePositionEditMode = "EmployeePositionFromList";
                }

                if (employeePositionEditMode != null
                    && employeePositionEditMode.Equals("EmployeePositionFromList") == true)
                {
                    if (employee.EmployeePositionID.HasValue == true
                        && (originalEmployee.EmployeePositionID.HasValue == false
                        || originalEmployee.EmployeePositionID != employee.EmployeePositionID))
                    {
                        if (String.IsNullOrEmpty(EmployeePositionDateStart))
                        {
                            ModelState.AddModelError("EmployeePositionTitle", "Необходимо указать дату вступления в действие изменений.");
                        }
                    }
                }
                else if (employeePositionEditMode != null
                    && employeePositionEditMode.Equals("EmployeePositionAsText") == true)
                {
                    if (String.IsNullOrEmpty(employee.EmployeePositionTitle) == false
                        && (String.IsNullOrEmpty(originalEmployee.EmployeePositionTitle) == true
                        || originalEmployee.EmployeePositionTitle.Equals(employee.EmployeePositionTitle) == false))
                    {
                        if (String.IsNullOrEmpty(EmployeePositionDateStart))
                        {
                            ModelState.AddModelError("EmployeePositionTitle", "Необходимо указать дату вступления в действие изменений.");
                        }
                    }
                }
                
                if (ModelState.IsValid)
                {
                    if (employeefromvacancy != null && employeefromvacancy.Value == 1)
                        employee.IsVacancy = false;

                    if (employee.EmployeeGradID.HasValue == true
                        && (originalEmployee.EmployeeGradID.HasValue == false
                        || originalEmployee.EmployeeGradID != employee.EmployeeGradID))
                    {

                        // Проверим, есть ли запись об изменении грейда в истории и если нет, то добавим
                        if (originalEmployee.EmployeeGradID.HasValue == true)
                        {
                            if (_employeeGradAssignmentService.Get(x => x.Where(e => (e.EmployeeID == originalEmployee.ID)).ToList()).Count == 0)
                            {
                                EmployeeGradAssignment employeeGradAssignmentPrev = new EmployeeGradAssignment();

                                employeeGradAssignmentPrev.EmployeeID = originalEmployee.ID;
                                employeeGradAssignmentPrev.EmployeeGradID = originalEmployee.EmployeeGradID;
                                employeeGradAssignmentPrev.Comments = "Изменено в карточке сотрудника пользователем: " + User.Identity.Name;

                                _employeeGradAssignmentService.Add(employeeGradAssignmentPrev);
                            }
                        }

                        EmployeeGradAssignment employeeGradAssignment = new EmployeeGradAssignment();

                        employeeGradAssignment.EmployeeID = employee.ID;
                        employeeGradAssignment.EmployeeGradID = employee.EmployeeGradID;
                        employeeGradAssignment.BeginDate = Convert.ToDateTime(EmployeeGradDateStart);
                        employeeGradAssignment.Comments = "Изменено в карточке сотрудника пользователем: " + User.Identity.Name;

                        _employeeGradAssignmentService.Add(employeeGradAssignment);
                    }
                    if (employee.DepartmentID.HasValue == true
                        && (originalEmployee.DepartmentID.HasValue == false
                        || originalEmployee.DepartmentID != employee.DepartmentID))
                    {
                        // Проверим, есть ли запись о подразделении в истории и если нет, то добавим
                        if (originalEmployee.DepartmentID.HasValue == true)
                        {
                            if (_employeeDepartmentAssignmentService.Get(x => x.Where(e => (e.EmployeeID == originalEmployee.ID)).ToList()).Count == 0)
                            {
                                EmployeeDepartmentAssignment employeeDepartmentAssignmentPrev = new EmployeeDepartmentAssignment();

                                employeeDepartmentAssignmentPrev.EmployeeID = employee.ID;
                                employeeDepartmentAssignmentPrev.DepartmentID = originalEmployee.DepartmentID;
                                employeeDepartmentAssignmentPrev.Comments = "Изменено в карточке сотрудника пользователем: " + User.Identity.Name;

                                _employeeDepartmentAssignmentService.Add(employeeDepartmentAssignmentPrev);
                            }
                        }

                        EmployeeDepartmentAssignment employeeDepartmentAssignment = new EmployeeDepartmentAssignment();

                        employeeDepartmentAssignment.EmployeeID = employee.ID;
                        employeeDepartmentAssignment.DepartmentID = employee.DepartmentID;
                        employeeDepartmentAssignment.BeginDate = Convert.ToDateTime(DepartmentDateStart);
                        employeeDepartmentAssignment.Comments = "Изменено в карточке сотрудника пользователем: " + User.Identity.Name;

                        _employeeDepartmentAssignmentService.Add(employeeDepartmentAssignment);
                    }

                    //if (employee.EmployeePositionOfficialID.HasValue == true
                    //    && (originalEmployee.EmployeePositionOfficialID.HasValue == false
                    //    || originalEmployee.EmployeePositionOfficialID != employee.EmployeePositionOfficialID))
                    //{
                    //    string employeePositionOfficialTitle = _employeePositionOfficialService.GetById(employee.EmployeePositionOfficialID.Value).Title;

                    //    // Проверим, есть ли запись об офиц. должности в истории и если нет, то добавим
                    //    if (/*String.IsNullOrEmpty(originalEmployee.EmployeePositionOfficialTitle) == false ||*/ originalEmployee.EmployeePositionOfficialID.HasValue == true)
                    //    {
                    //        if (_employeePositionOfficialAssignmentService.Get(x => x.Where(e => (e.EmployeeID == originalEmployee.ID)).ToList()).Count == 0)
                    //        {
                    //            EmployeePositionOfficialAssignment employeePositionOfficialAssignmentPrev = new EmployeePositionOfficialAssignment();

                    //            employeePositionOfficialAssignmentPrev.EmployeeID = employee.ID;
                    //            employeePositionOfficialAssignmentPrev.EmployeePositionOfficialID = originalEmployee.EmployeePositionOfficialID;
                    //            if (originalEmployee.EmployeePositionID == null)
                    //            {
                    //                employeePositionOfficialAssignmentPrev.EmployeePositionOfficialID = 1;
                    //            }
                    //            //employeePositionOfficialAssignmentPrev.EmployeePositionTitle = originalEmployee.EmployeePositionTitle;
                    //            employeePositionOfficialAssignmentPrev.Comments = "Изменено в карточке сотрудника пользователем: " + User.Identity.Name;

                    //            _employeePositionOfficialAssignmentService.Add(employeePositionOfficialAssignmentPrev);
                    //        }
                    //    }

                    //    EmployeePositionOfficialAssignment employeePositioOfficialnAssignment = new EmployeePositionOfficialAssignment();

                    //    employeePositioOfficialnAssignment.EmployeeID = employee.ID;
                    //    employeePositioOfficialnAssignment.EmployeePositionOfficialID = employee.EmployeePositionOfficialID;
                    //    // employeePositioOfficialnAssignment.EmployeePositionTitle = employeePositionTitle;
                    //    employeePositioOfficialnAssignment.BeginDate = Convert.ToDateTime(EmployeePositionOfficialDateStart);
                    //    employeePositioOfficialnAssignment.Comments = "Изменено в карточке сотрудника пользователем: " + User.Identity.Name;

                    //    _employeePositionOfficialAssignmentService.Add(employeePositioOfficialnAssignment);
                    //}

                    string employeePositionTitle = "";

                    if (employeePositionEditMode != null
                    && employeePositionEditMode.Equals("EmployeePositionFromList") == true)
                    {
                        if (employee.EmployeePositionID.HasValue == true
                            && (originalEmployee.EmployeePositionID.HasValue == false
                            || originalEmployee.EmployeePositionID != employee.EmployeePositionID))
                        {
                            employeePositionTitle = _employeePositionService.GetById(employee.EmployeePositionID.Value).Title;

                            // Проверим, есть ли запись о должности в истории и если нет, то добавим
                            if (String.IsNullOrEmpty(originalEmployee.EmployeePositionTitle) == false || originalEmployee.EmployeePositionID.HasValue == true)
                            {
                                if (_employeePositionAssignmentService.Get(x => x.Where(e => (e.EmployeeID == originalEmployee.ID)).ToList()).Count == 0)
                                {
                                    EmployeePositionAssignment employeePositionAssignmentPrev = new EmployeePositionAssignment();

                                    employeePositionAssignmentPrev.EmployeeID = employee.ID;
                                    employeePositionAssignmentPrev.EmployeePositionID = originalEmployee.EmployeePositionID;
                                    if (originalEmployee.EmployeePositionID == null)
                                    {
                                        employeePositionAssignmentPrev.EmployeePositionID = 1;
                                    }
                                    employeePositionAssignmentPrev.EmployeePositionTitle = originalEmployee.EmployeePositionTitle;
                                    employeePositionAssignmentPrev.Comments = "Изменено в карточке сотрудника пользователем: " + User.Identity.Name;

                                    _employeePositionAssignmentService.Add(employeePositionAssignmentPrev);
                                }
                            }

                            EmployeePositionAssignment employeePositionAssignment = new EmployeePositionAssignment();

                            employeePositionAssignment.EmployeeID = employee.ID;
                            employeePositionAssignment.EmployeePositionID = employee.EmployeePositionID;
                            employeePositionAssignment.EmployeePositionTitle = employeePositionTitle;
                            employeePositionAssignment.BeginDate = Convert.ToDateTime(EmployeePositionDateStart);
                            employeePositionAssignment.Comments = "Изменено в карточке сотрудника пользователем: " + User.Identity.Name;

                            _employeePositionAssignmentService.Add(employeePositionAssignment);

                        }
                    }
                    else if (employeePositionEditMode != null
                    && employeePositionEditMode.Equals("EmployeePositionAsText") == true)
                    {
                        if (String.IsNullOrEmpty(employee.EmployeePositionTitle) == false
                            && (String.IsNullOrEmpty(originalEmployee.EmployeePositionTitle) == true
                            || originalEmployee.EmployeePositionTitle.Equals(employee.EmployeePositionTitle) == false))
                        {
                            employeePositionTitle = employee.EmployeePositionTitle;

                            // Проверим, есть ли запись о должности в истории
                            if (String.IsNullOrEmpty(originalEmployee.EmployeePositionTitle) == false || originalEmployee.EmployeePositionID.HasValue == true)
                            {
                                if (_employeePositionAssignmentService.Get(x => x.Where(e => (e.EmployeeID == originalEmployee.ID)).ToList()).Count == 0)
                                {
                                    EmployeePositionAssignment employeePositionAssignmentPrev = new EmployeePositionAssignment();

                                    employeePositionAssignmentPrev.EmployeeID = employee.ID;
                                    employeePositionAssignmentPrev.EmployeePositionID = originalEmployee.EmployeePositionID;
                                    if (originalEmployee.EmployeePositionID == null)
                                    {
                                        employeePositionAssignmentPrev.EmployeePositionID = 1;
                                    }
                                    employeePositionAssignmentPrev.EmployeePositionTitle = originalEmployee.EmployeePositionTitle;
                                    employeePositionAssignmentPrev.Comments = "Изменено в карточке сотрудника пользователем: " + User.Identity.Name;

                                    _employeePositionAssignmentService.Add(employeePositionAssignmentPrev);
                                }
                            }

                            EmployeePositionAssignment employeePositionAssignment = new EmployeePositionAssignment();

                            employeePositionAssignment.EmployeeID = employee.ID;
                            //temp fix - необходимо сделать поле EmployeePositionID не обязательным (удалить атрибут [Required])
                            employeePositionAssignment.EmployeePositionID = 1;
                            employeePositionAssignment.EmployeePositionTitle = employee.EmployeePositionTitle;
                            employeePositionAssignment.BeginDate = Convert.ToDateTime(EmployeePositionDateStart);
                            employeePositionAssignment.Comments = "Изменено в карточке сотрудника пользователем: " + User.Identity.Name;

                            _employeePositionAssignmentService.Add(employeePositionAssignment);

                            employee.EmployeePositionID = null;
                        }
                    }

                    if (String.IsNullOrEmpty(employeePositionTitle) == true)
                    {
                        employeePositionTitle = originalEmployee.EmployeePositionTitle;
                    }

                    employee.EmployeePositionTitle = employeePositionTitle;

                    employee.ADEmployeeID = originalEmployee.ADEmployeeID;

                    _employeeService.Update(employee);
                    return RedirectToAction("Details", new { id = employee.ID });
                }
            }
            ViewBag.EmployeePositionID = new SelectList(_employeePositionService.Get(x => x.ToList().OrderBy(ep => ep.ShortName).ToList()), "ID", "FullName", employee.EmployeePositionID);
            ViewBag.EmployeePositionOfficialID = new SelectList(_employeePositionOfficialService.Get(x => x.ToList().OrderBy(ep => ep.ShortName).ToList()), "ID", "FullName", employee.EmployeePositionOfficialID);
            ViewBag.DepartmentID = new SelectList(_departmentService.Get(x => x.ToList().OrderBy(d => d.ShortName).ToList()), "ID", "FullName", employee.DepartmentID);
            ViewBag.OrganisationID = new SelectList(_organisationService.Get(x => x.ToList().OrderBy(o => o.ShortName).ToList()), "ID", "FullName", employee.OrganisationID);
            ViewBag.EmployeeLocationID = new SelectList(_employeeLocationService.Get(x => x.ToList().OrderBy(o => o.ShortName).ToList()), "ID", "FullName", employee.EmployeeLocationID);
            ViewBag.EmployeeGradID = new SelectList(_employeeGradService.Get(x => x.ToList().OrderBy(e => e.ShortName).ToList()), "ID", "FullName", employee.EmployeeGradID);
            ViewBag.EmployeePositionFromList = employee.EmployeePositionID.HasValue;

            ViewBag.DepartmentDateStart = DepartmentDateStart;
            ViewBag.EmployeePositionDateStart = EmployeePositionDateStart;
            ViewBag.EmployeePositionOfficialDateStart = EmployeePositionOfficialDateStart;
            ViewBag.EmployeeGradDateStart = EmployeeGradDateStart;
            
            ViewBag.Message = "Запрос не прошел валидацию";
            return View(employee);
        }

        private bool EmployeeWithSameNameExist(string firstName, string midName, string lastName, int currentEmployeeID)
        {

            var employees = _employeeService.Get(x => x.Where(e => (e.FirstName == firstName
                                                                     && e.MidName == midName
                                                                     && e.LastName == lastName
                                                                     && e.ID != currentEmployeeID)).ToList());
            List<Employee> employeeList = employees.ToList();
            return (employeeList.Count > 0);
        }

        [OperationActionFilter(nameof(Operation.EmployeeDelete))]
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return StatusCode(StatusCodes.Status400BadRequest);
            }
            Employee employee = _employeeService.GetById(id.Value);
            if (employee == null)
            {
                return StatusCode(StatusCodes.Status404NotFound);
            }
            return View(employee);
        }

        [OperationActionFilter(nameof(Operation.EmployeeDelete))]
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            Employee employee = _employeeService.GetById(id);
            var user = _userService.GetUserDataForVersion();
            var recycleBinInDBRelation = _serviceService.HasRecycleBinInDBRelation(employee);
            if (recycleBinInDBRelation.hasRelated == false)
            {
                var recycleToRecycleBin = _employeeService.RecycleToRecycleBin(employee.ID, user.Item1, user.Item2);
                if (!recycleToRecycleBin.toRecycleBin)
                {
                    ViewBag.RecycleBinError =
                        "Невозможно удалить, так как на удаляемый элемент ссылаются другие элементы в системе." +
                        "Сначала необходимо удалить элементы, которые ссылаются на данный элемент. " +
                        recycleToRecycleBin.relatedClassId;
                    return View(employee);
                }
            }
            else
            {
                ViewBag.RecycleBinError =
                    "Невозможно удалить, так как на удаляемый элемент ссылаются другие элементы в системе." +
                    $"Сначала необходимо удалить элементы, которые ссылаются на данный элемент. {recycleBinInDBRelation.relatedInDBClassId}";
                return View(employee);
            }
            return RedirectToAction("Index");
        }

        [OperationActionFilter(nameof(Operation.OrgChartView))]
        public ActionResult OrgChartList(String searchString, EmployeeViewType? viewType)
        {
            ApplicationUser user = _applicationUserService.GetUser();

            if (_applicationUserService.HasAccess(Operation.EmployeeCreateUpdate) == false)
            {
                viewType = EmployeeViewType.AllActualEmployee;
            }

            if (!(_applicationUserService.HasAccess(Operation.EmployeeCreateUpdate) == true
               || _applicationUserService.HasAccess(Operation.EmployeeADUpdate) == true
               || _applicationUserService.HasAccess(Operation.EmployeePersonalDataView) == true
               || _applicationUserService.HasAccess(Operation.EmployeeSubEmplPersonalDataView) == true
               || _applicationUserService.HasAccess(Operation.EmployeeFullListView) == true))
            {
                string alternateExternalOrgChartListURL = RPCSHelper.GetAlternateExternalOrgChartListURL();

                if (String.IsNullOrEmpty(alternateExternalOrgChartListURL) == false)
                {
                    return Redirect(alternateExternalOrgChartListURL);
                }
            }

            var searchItems = GetAllSearchItems();
            SetOrgChartListViewBag(searchItems, viewType);
            var employees = GetOrgChartListmployees(searchString, searchItems, user);
            if (employees == null)
                return View();

            switch (viewType)
            {
                case EmployeeViewType.AllActualEmployee:
                    employees = employees.Where(x => x.IsVacancy != true).ToList();
                    break;
                default:
                    break;

            }

            var employeeList = employees.OrderBy(e => e.Department.ShortName + (e.Department.DepartmentManager != e).ToString() + e.FullName);
            return View(employeeList.ToList());
        }

        private List<Employee> GetOrgChartListmployees(String searchString, Dictionary<string, SearchItem> searchItems, ApplicationUser user)
        {
            List<Employee> employeeList = null;

            if (_applicationUserService.HasAccess(Operation.EmployeeCreateUpdate) == true
               || _applicationUserService.HasAccess(Operation.EmployeeADUpdate) == true
               || _applicationUserService.HasAccess(Operation.EmployeePersonalDataView) == true
               || _applicationUserService.HasAccess(Operation.EmployeeFullListView) == true)
            {
                employeeList = _employeeService.Get(x => x.Include(e => e.Department).Include(e => e.EmployeePosition).Include(e => e.EmployeeGrad).ToList()).ToList();
            }
            else if (_applicationUserService.HasAccess(Operation.EmployeeSubEmplPersonalDataView) == true)
            {
                employeeList = _employeeService.GetAllManagedEmployees(_applicationUserService.GetUser().ManagedDepartments).ToList();
            }
            else
            {
                int userEmployeeID = _applicationUserService.GetEmployeeID();
                employeeList = _employeeService.Get(x =>
                    x.Include(e => e.Department).Include(e => e.EmployeePosition).Include(e => e.EmployeeGrad)
                        .Where(e => e.ID == userEmployeeID).ToList()).ToList();
            }

            employeeList = employeeList.Where(e => (e.EnrollmentDate == null || e.EnrollmentDate.Value <= DateTime.Today)
              && (e.DismissalDate == null || e.DismissalDate >= DateTime.Today)
              && e.Department != null).ToList();

            if (String.IsNullOrEmpty(searchString))
                return employeeList;

            var searchItem = searchItems.Values.FirstOrDefault(x => x.Text == searchString);
            if (searchItem == null)
            {
                var lowerSearchString = searchString.ToLower();
                var result = employeeList.Where(x => x.FullName.ToLower().Contains(lowerSearchString) ||
                    (x.Department != null && x.Department.ShortName.ToLower().Contains(lowerSearchString))).ToList();
                foreach (var item in result.ToList())
                {
                    if (item.DepartmentID.HasValue)
                        result = result.Union(GetEmployeesInDepartment(item.DepartmentID.Value)).ToList();
                }
                return result.OrderBy(e => e.Department.ShortName + e.FullName).ToList();
            }

            var d1 = searchItem.Item.GetType();
            var d2 = typeof(Department);
            if (searchItem.Item is Department)
            {
                var department = searchItem.Item as Department;
                return GetEmployeesInDepartment(department.ID).ToList();
            }

            return new List<Employee> { searchItem.Item as Employee };
        }

        private void SetOrgChartListViewBag(Dictionary<string, SearchItem> searchItems, EmployeeViewType? viewType)
        {
            if (viewType != null && viewType.HasValue)
            {
                ViewBag.CurrentViewType = viewType;
            }
            else
            {
                viewType = EmployeeViewType.AllActualEmployee;
                ViewBag.CurrentViewType = EmployeeViewType.AllActualEmployee;
            }

            ViewBag.SearchEmployees = new SelectList(searchItems.Values.OrderBy(x => x.Value), "Value", "Text");
        }

        private Dictionary<string, SearchItem> GetAllSearchItems()
        {
            var departments = new Dictionary<string, SearchItem>();
            var result = new Dictionary<string, SearchItem>();
            var key_template = "{0}_{1}";
            foreach (var employee in _employeeService.Get(e => e.Include(x => x.Department).ToList()))
            {
                var employee_key = String.Format(key_template, employee.GetType().Name, employee.ID);
                result[employee_key] = new SearchItem() { Value = employee_key, Text = employee.FullName, Item = employee };

                var department = employee.Department;
                if (department == null)
                    continue;

                var department_key = String.Format(key_template, department.GetType().Name, department.ID);
                result[department_key] = new SearchItem() { Value = department_key, Text = department.ShortName, Item = department };
            }

            return result;
        }

        private ICollection<Employee> GetEmployeesInDepartment(int departmentID)
        {
            var employes = _employeeService.Get(x => x.Include(e => e.Department).Where(e =>
                    (e.EnrollmentDate == null || e.EnrollmentDate.Value <= DateTime.Today)
                    && (e.DismissalDate == null || e.DismissalDate >= DateTime.Today))
                .Where(e => e.DepartmentID == departmentID).Include(e => e.EmployeePosition)
                .Include(e => e.EmployeeGrad)
                .ToList()
                .OrderBy(e => e.Department.ShortName + e.FullName).ToList());

            foreach (var department in _departmentService.Get(d => d.Where(x => x.ParentDepartmentID == departmentID).ToList()))
            {
                var result = GetEmployeesInDepartment(department.ID);
                employes = employes.Concat(result).ToList();
            }

            return employes;
        }

        [OperationActionFilter(nameof(Operation.EmployeeExcelExport))]
        [HttpGet]
        public FileContentResult ExportOrgChartListToExcel(EmployeeViewType? viewType)
        {
            byte[] binData = null;

            List<Employee> employeeList = new List<Employee>();

            ApplicationUser user = _applicationUserService.GetUser();

            if (_applicationUserService.HasAccess(Operation.EmployeeCreateUpdate) == true
               || _applicationUserService.HasAccess(Operation.EmployeeADUpdate) == true
               || _applicationUserService.HasAccess(Operation.EmployeePersonalDataView) == true
               || _applicationUserService.HasAccess(Operation.EmployeeFullListView) == true)
            {
                employeeList = _employeeService.Get(x => x.Where(e =>
                        (e.EnrollmentDate == null || e.EnrollmentDate.Value <= DateTime.Today)
                        && (e.DismissalDate == null || e.DismissalDate >= DateTime.Today)
                        && e.Department != null).Include(e => e.EmployeePosition).Include(e => e.Department)
                    .Include(e => e.EmployeeLocation).ToList()).ToList();
            }
            else if (_applicationUserService.HasAccess(Operation.EmployeeSubEmplPersonalDataView) == true)
            {
                employeeList = _employeeService.GetAllManagedEmployees(_applicationUserService.GetUser().ManagedDepartments).ToList().Where(e => (e.EnrollmentDate == null || e.EnrollmentDate.Value <= DateTime.Today)
                    && (e.DismissalDate == null || e.DismissalDate >= DateTime.Today)
                    && e.Department != null).ToList();
            }
            else
            {
                employeeList = new List<Employee>();
            }

            switch (viewType)
            {
                case EmployeeViewType.AllActualEmployee:
                    employeeList = employeeList.Where(x => x.IsVacancy != true).ToList();
                    break;
                default:
                    break;
            }


            DataTable dataTable = new DataTable();

            dataTable.Columns.Add("DepartmentShortName", typeof(string)).Caption = "Код подразделения";
            dataTable.Columns["DepartmentShortName"].ExtendedProperties["Width"] = (double)9;

            dataTable.Columns.Add("OrganisationFullName", typeof(string)).Caption = "Организация";
            dataTable.Columns["OrganisationFullName"].ExtendedProperties["Width"] = (double)40;

            dataTable.Columns.Add("DepartmentPosition", typeof(string)).Caption = "Подразделение/позиция";
            dataTable.Columns["DepartmentPosition"].ExtendedProperties["Width"] = (double)52;
            dataTable.Columns.Add("EmployeeFullName", typeof(string)).Caption = "Фамилия Имя Отчество";
            dataTable.Columns["EmployeeFullName"].ExtendedProperties["Width"] = (double)41;

            dataTable.Columns.Add("EmployeeGrad", typeof(string)).Caption = "Грейд";
            dataTable.Columns["EmployeeGrad"].ExtendedProperties["Width"] = (double)11;

            dataTable.Columns.Add("EmployeeCount", typeof(string)).Caption = "Кол-во сотрудников";
            dataTable.Columns["EmployeeCount"].ExtendedProperties["Width"] = (double)11;


            dataTable.Columns.Add("EmployeeLocation", typeof(string)).Caption = "Территориальное расположение";
            dataTable.Columns["EmployeeLocation"].ExtendedProperties["Width"] = (double)25;

            dataTable.Columns.Add("EmployeeEmail", typeof(string)).Caption = "Электронная почта";
            dataTable.Columns["EmployeeEmail"].ExtendedProperties["Width"] = (double)25;
            dataTable.Columns.Add("EmployeePhone", typeof(string)).Caption = "Телефон для связи общедоступный";
            dataTable.Columns["EmployeePhone"].ExtendedProperties["Width"] = (double)25;


            dataTable.Columns.Add("Comments", typeof(string)).Caption = "Примечания";
            dataTable.Columns["Comments"].ExtendedProperties["Width"] = (double)41;
            dataTable.Columns.Add("_ISGROUPROW_", typeof(bool)).Caption = "_ISGROUPROW_";


            employeeList = employeeList.OrderBy(e => e.Department.ShortName + (e.Department.DepartmentManager != e).ToString() + e.FullName).ToList();
            foreach (var group in employeeList.GroupBy(e => e.Department.ShortName))
            {

                if (!(group.Count() == 1
                    && group.Key.Contains("-") == true
                    && group.Key.Contains("-1") == false
                    && group.First().Department.Title.Equals(group.First().EmployeePositionTitle)))
                {
                    dataTable.Rows.Add(
                        group.First().Department.DisplayShortName,
                        ((_permissionValidatorService.HasAccess(User, Operation.DepartmentCreateUpdate) && group.First().Organisation != null) ? group.First().Organisation.FullName : ""),
                        ((group.First().Department != null) ? group.First().Department.Title : ""),
                        "",
                        "",
                        group.Count(),
                        "",
                        "",
                        "",
                        ((_permissionValidatorService.HasAccess(User, Operation.DepartmentCreateUpdate) && group.First().Department != null && group.First().Department.Comments != null) ? group.First().Department.Comments : ""),
                        true);
                }

                foreach (var employee in group)
                {
                    dataTable.Rows.Add(
                        group.First().Department.DisplayShortName,
                        ((_applicationUserService.HasAccess(Operation.EmployeePersonalDataView | Operation.EmployeeSubEmplPersonalDataView) && employee.Organisation != null) ? employee.Organisation.FullName : ""),
                        ((employee.EmployeePositionTitle != null) ? employee.EmployeePositionTitle : ""),
                        employee.FullName,
                        ((_applicationUserService.HasAccess(Operation.EmployeePersonalDataView | Operation.EmployeeSubEmplPersonalDataView) && employee.EmployeeGrad != null) ? employee.EmployeeGrad.Title : ""),
                        "",
                        ((employee.EmployeeLocation != null) ? employee.EmployeeLocation.Title : ""),
                        employee.Email,
                        employee.PublicMobilePhoneNumber,
                        ((_applicationUserService.HasAccess(Operation.EmployeePersonalDataView | Operation.EmployeeSubEmplPersonalDataView) && employee.Comments != null) ? employee.Comments : ""), false);
                }
            }

            dataTable.Rows.Add("", "", "", "", "", employeeList.Count());

            if (_applicationUserService.HasAccess(Operation.EmployeePersonalDataView | Operation.EmployeeSubEmplPersonalDataView) == false)
            {
                dataTable.Columns.Remove("OrganisationFullName");
                dataTable.Columns.Remove("EmployeeGrad");
                dataTable.Columns.Remove("Comments");
            }


            using (MemoryStream stream = new MemoryStream())
            {
                using (SpreadsheetDocument doc = SpreadsheetDocument.Create(stream, SpreadsheetDocumentType.Workbook))
                {
                    WorkbookPart workbookPart = ExcelHelper.CreateWorkbookPart(doc, "Сотрудники ГК");

                    WorksheetPart rId1 = ExcelHelper.CreateWorksheetPartAndImportDataTable(workbookPart, "rId1", 1, 1, (uint)dataTable.Columns.Count,
                        "Подразделения и сотрудники ГК", dataTable, 3, 1);

                    doc.WorkbookPart.Workbook.Save();
                }

                stream.Position = 0;
                BinaryReader b = new BinaryReader(stream);
                binData = b.ReadBytes((int)stream.Length);


            }

            return File(binData, ExcelHelper.ExcelContentType, "EmployeesOrgChartList" /*+ DateTime.Now.ToString("ddMMyyHHmmss")*/ + ".xlsx");
        }

        [OperationActionFilter(nameof(Operation.EmployeeExcelExport))]
        [HttpGet]
        public FileContentResult ExportListToExcel()
        {
            byte[] binData = null;

            List<Employee> employeeList = new List<Employee>();

            ApplicationUser user = _applicationUserService.GetUser();

            if (_applicationUserService.HasAccess(Operation.EmployeeCreateUpdate) == true
               || _applicationUserService.HasAccess(Operation.EmployeeADUpdate) == true
               || _applicationUserService.HasAccess(Operation.EmployeePersonalDataView) == true
               || _applicationUserService.HasAccess(Operation.EmployeeFullListView) == true)
            {
                employeeList = _employeeService.Get(x =>
                    x.Include(e => e.Department).Include(e => e.EmployeePosition).Include(e => e.EmployeeGrad).ToList()
                        .OrderBy(e => e.FullName).ToList()).ToList();
            }
            else if (_applicationUserService.HasAccess(Operation.EmployeeSubEmplPersonalDataView) == true)
            {
                employeeList = _employeeService.GetAllManagedEmployees(_applicationUserService.GetUser().ManagedDepartments).OrderBy(e => e.FullName).ToList();
            }
            else
            {
                employeeList = new List<Employee>();
            }

            DataTable dataTable = new DataTable();

            dataTable.Columns.Add("RowNum", typeof(string)).Caption = "№";
            dataTable.Columns["RowNum"].ExtendedProperties["Width"] = (double)6;
            dataTable.Columns.Add("EmployeeFullName", typeof(string)).Caption = "Фамилия Имя Отчество";
            dataTable.Columns["EmployeeFullName"].ExtendedProperties["Width"] = (double)41;
            dataTable.Columns.Add("BirthdayDate", typeof(DateTime)).Caption = "Дата рождения";
            dataTable.Columns["BirthdayDate"].ExtendedProperties["Width"] = (double)10;
            dataTable.Columns.Add("Department", typeof(string)).Caption = "Подразделение";
            dataTable.Columns["Department"].ExtendedProperties["Width"] = (double)75;
            dataTable.Columns.Add("Position", typeof(string)).Caption = "Должность";
            dataTable.Columns["Position"].ExtendedProperties["Width"] = (double)52;
            dataTable.Columns.Add("PositionOfficial", typeof(string)).Caption = "Должность по трудовой книжке";
            dataTable.Columns["PositionOfficial"].ExtendedProperties["Width"] = (double)52;
            dataTable.Columns.Add("Email", typeof(string)).Caption = "E-mail";
            dataTable.Columns["Email"].ExtendedProperties["Width"] = (double)35;
            dataTable.Columns.Add("Organisation", typeof(string)).Caption = "Организация";
            dataTable.Columns["Organisation"].ExtendedProperties["Width"] = (double)25;
            dataTable.Columns.Add("EmployeeLocation", typeof(string)).Caption = "Терр-e расположение";
            dataTable.Columns["EmployeeLocation"].ExtendedProperties["Width"] = (double)25;
            dataTable.Columns.Add("OfficeName", typeof(string)).Caption = "Офис (№ кабинета)";
            dataTable.Columns["OfficeName"].ExtendedProperties["Width"] = (double)25;
            dataTable.Columns.Add("WorkPhoneNumber", typeof(string)).Caption = "Рабочий телефон";
            dataTable.Columns["WorkPhoneNumber"].ExtendedProperties["Width"] = (double)25;
            dataTable.Columns.Add("PersonalMobilePhoneNumber", typeof(string)).Caption = "Мобильный телефон личный";
            dataTable.Columns["PersonalMobilePhoneNumber"].ExtendedProperties["Width"] = (double)25;
            dataTable.Columns.Add("PublicMobilePhoneNumber", typeof(string)).Caption = "Мобильный телефон общедоступный";
            dataTable.Columns["PublicMobilePhoneNumber"].ExtendedProperties["Width"] = (double)25;
            dataTable.Columns.Add("SkypeLogin", typeof(string)).Caption = "Skype";
            dataTable.Columns["SkypeLogin"].ExtendedProperties["Width"] = (double)25;
            dataTable.Columns.Add("Specialization", typeof(string)).Caption = "Специализация";
            dataTable.Columns["Specialization"].ExtendedProperties["Width"] = (double)60;
            dataTable.Columns.Add("ADLogin", typeof(string)).Caption = "Имя учетной записи в AD";
            dataTable.Columns["ADLogin"].ExtendedProperties["Width"] = (double)25;
            dataTable.Columns.Add("EnrollmentDate", typeof(DateTime)).Caption = "Принят";
            dataTable.Columns["EnrollmentDate"].ExtendedProperties["Width"] = (double)10;

            dataTable.Columns.Add("ProbationEndDate", typeof(DateTime)).Caption = "Дата завершения испытательного срока";
            dataTable.Columns["ProbationEndDate"].ExtendedProperties["Width"] = (double)45;

            dataTable.Columns.Add("DismissalDate", typeof(DateTime)).Caption = "Уволен";
            dataTable.Columns["DismissalDate"].ExtendedProperties["Width"] = (double)10;
            dataTable.Columns.Add("DismissalReason", typeof(string)).Caption = "Причина увольнения";
            dataTable.Columns["DismissalReason"].ExtendedProperties["Width"] = (double)60;
            dataTable.Columns.Add("Comments", typeof(string)).Caption = "Примечание";
            dataTable.Columns["Comments"].ExtendedProperties["Width"] = (double)60;
            dataTable.Columns.Add("EmployeeGrad", typeof(int)).Caption = "Грейд";
            dataTable.Columns["EmployeeGrad"].ExtendedProperties["Width"] = (double)8;
            dataTable.Columns.Add("EmployeeCategoryTitle", typeof(string)).Caption = "Категория";
            dataTable.Columns["EmployeeCategoryTitle"].ExtendedProperties["Width"] = (double)39;
            dataTable.Columns.Add("EmployeeQualifyingRoleTitle", typeof(string)).Caption = "УПР";
            dataTable.Columns["EmployeeQualifyingRoleTitle"].ExtendedProperties["Width"] = (double)39;

            if (_applicationUserService.HasAccess(Operation.EmployeeADUpdate | Operation.ADSyncAccess) == true)
            {
                dataTable.Columns.Add("ADEmployeeID", typeof(string)).Caption = "ADEmployeeID";
                dataTable.Columns["ADEmployeeID"].ExtendedProperties["Width"] = (double)25;
            }

            int rowNum = 1;
            foreach (var employee in employeeList)
            {

                dataTable.Rows.Add(rowNum.ToString(),
                    employee.FullName,
                    employee.BirthdayDate,
                    ((employee.Department != null) ? employee.Department.FullName : ""),
                    ((employee.EmployeePositionTitle != null) ? employee.EmployeePositionTitle : ""),
                    (_applicationUserService.HasAccess(Operation.EmployeePersonalDataView | Operation.EmployeeSubEmplPersonalDataView) == true && (employee.EmployeePositionOfficial != null) ? employee.EmployeePositionOfficial.Title : ""),
                    employee.Email,
                    (_applicationUserService.HasAccess(Operation.EmployeePersonalDataView | Operation.EmployeeSubEmplPersonalDataView) == true && (employee.Organisation != null) ? employee.Organisation.FullName : ""),
                    ((employee.EmployeeLocation != null) ? employee.EmployeeLocation.FullName : ""),
                    employee.OfficeName,
                    employee.WorkPhoneNumber,
                    (_applicationUserService.HasAccess(Operation.EmployeePersonalDataView | Operation.EmployeeSubEmplPersonalDataView) == true) ? employee.PersonalMobilePhoneNumber : "",
                    employee.PublicMobilePhoneNumber,
                    employee.SkypeLogin,
                    employee.Specialization,
                    employee.ADLogin,
                    employee.EnrollmentDate,
                    employee.ProbationEndDate,
                    employee.DismissalDate,
                    (_applicationUserService.HasAccess(Operation.EmployeePersonalDataView | Operation.EmployeeSubEmplPersonalDataView) == true) ? employee.Comments : "");

                if (_applicationUserService.HasAccess(Operation.EmployeePersonalDataView | Operation.EmployeeSubEmplPersonalDataView) == true)
                {
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
                }

                if (_applicationUserService.HasAccess(Operation.EmployeePersonalDataView | Operation.EmployeeSubEmplPersonalDataView | Operation.EmployeeCategoryView) == true)
                {
                    string employeeCategoryTitle = "";

                    EmployeeCategory employeeCategory = _employeeCategoryService.Get(x => x
                            .Where(ec => ec.EmployeeID == employee.ID).OrderBy(ec => ec.CategoryDateEnd).Where(ec =>
                                ec.CategoryDateBegin != null
                                && ec.CategoryDateBegin <= DateTime.Today
                                && (ec.CategoryDateEnd == null || ec.CategoryDateEnd >= DateTime.Today)).ToList())
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

                if (_applicationUserService.HasAccess(Operation.EmployeePersonalDataView | Operation.EmployeeSubEmplPersonalDataView | Operation.EmployeeQualifyingRoleView) == true)
                {
                    string employeeQualifyingRoleTitle = "";

                    EmployeeQualifyingRole employeeQualifyingRole = _employeeQualifyingRoleService.Get(x => x
                        .Where(eqr => eqr.EmployeeID == employee.ID).OrderBy(eqr => eqr.QualifyingRoleDateEnd).Where(eqr =>
                            eqr.QualifyingRoleDateBegin != null
                            && eqr.QualifyingRoleDateBegin <= DateTime.Today
                            && (eqr.QualifyingRoleDateEnd == null || eqr.QualifyingRoleDateEnd >= DateTime.Today))
                        .ToList()).FirstOrDefault();

                    if (employeeQualifyingRole != null)
                    {
                        employeeQualifyingRoleTitle = employeeQualifyingRole.QualifyingRole.FullName;
                    }

                    if (String.IsNullOrEmpty(employeeQualifyingRoleTitle) == false)
                    {
                        dataTable.Rows[dataTable.Rows.Count - 1]["EmployeeQualifyingRoleTitle"] = employeeQualifyingRoleTitle;
                    }
                }

                if (_applicationUserService.HasAccess(Operation.EmployeeADUpdate | Operation.ADSyncAccess) == true
                    && dataTable.Columns.Contains("ADEmployeeID") == true)
                {
                    dataTable.Rows[dataTable.Rows.Count - 1]["ADEmployeeID"] = employee.ADEmployeeID;
                }

                rowNum++;
            }



            using (MemoryStream stream = new MemoryStream())
            {
                using (SpreadsheetDocument doc = SpreadsheetDocument.Create(stream, SpreadsheetDocumentType.Workbook))
                {
                    WorkbookPart workbookPart = ExcelHelper.CreateWorkbookPart(doc, "Сотрудники ГК");

                    WorksheetPart rId1 = ExcelHelper.CreateWorksheetPartAndImportDataTable(workbookPart, "rId1", 1, 1, (uint)dataTable.Columns.Count,
                        "Список сотрудников ГК", dataTable, 3, 1);

                    doc.WorkbookPart.Workbook.Save();
                }

                stream.Position = 0;
                BinaryReader b = new BinaryReader(stream);
                binData = b.ReadBytes((int)stream.Length);


            }

            return File(binData, ExcelHelper.ExcelContentType, "EmployeeList" /*+ DateTime.Now.ToString("ddMMyyHHmmss")*/ + ".xlsx");
        }

        [OperationActionFilter(nameof(Operation.OOAccessAllow))]
        public string GetPayrollData(int? id, string employeeFullName)
        {
            ApplicationUser user = _applicationUserService.GetUser();

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

            if (id != null && id.HasValue && id != -1)
            {
                Employee employee = employeeList.Where(e => e.ID == id).FirstOrDefault();

                if (employee != null)
                {
                    DataTable employeePayrollSheetDataTable = _financeService.GetEmployeePayrollSheetDataTableFromOO(user, false);

                    List<EmployeePayrollRecord> records = _financeService.GetEmployeePayrollRecordsFromDataTable(employeePayrollSheetDataTable, employee).OrderBy(r => r.PayrollChangeDate).ToList();

                    return JsonConvert.SerializeObject(records);
                }
                else
                {
                    return "false";
                }
            }
            else if (String.IsNullOrEmpty(employeeFullName) == false)
            {
                DataTable employeePayrollSheetDataTable = _financeService.GetEmployeePayrollSheetDataTableFromOO(user, true);

                Employee employee = new Employee();

                employee.ID = -1;

                employee.LastName = employeeFullName.Split(' ')[0].Trim();
                employee.FirstName = employeeFullName.Split(' ')[1].Trim();
                employee.MidName = employeeFullName.Split(' ')[2].Trim();

                List<EmployeePayrollRecord> records = _financeService.GetEmployeePayrollRecordsFromDataTable(employeePayrollSheetDataTable, employee).OrderBy(r => r.PayrollChangeDate).ToList();

                return JsonConvert.SerializeObject(records);

            }
            else
            {
                return "false";
            }
        }

        [OperationActionFilter(nameof(Operation.OOAccessAllow))]
        public ActionResult PayrollDataSave(int? ID, int? employeeID, int? bitrixReqEnrollmentID, bool? saveResultInBitrix, string employeeFullName, string PayrollChangeDate, string PayrollValue,
            string PaymentMethodProbation, string PaymentMethod, string AdditionallyInfo)
        {
            bool result = false;
            ApplicationUser user = _applicationUserService.GetUser();
            bool tempCPFile = false;

            if (employeeID != null && employeeID.HasValue == true && employeeID != -1)
            {
                tempCPFile = false;
            }
            else
            {
                tempCPFile = true;
            }

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

            DataTable employeePayrollSheetDataTable = _financeService.GetEmployeePayrollSheetDataTableFromOO(user, tempCPFile);

            if (employeePayrollSheetDataTable != null)
            {
                Employee employee = null;
                if (employeeID != null && employeeID.HasValue == true && employeeID != -1)
                {
                    employee = employeeList.Where(e => e.ID == employeeID).FirstOrDefault();
                }
                else if (String.IsNullOrEmpty(employeeFullName) == false)
                {
                    employee = new Employee();

                    employee.ID = -1;

                    employee.LastName = employeeFullName.Split(' ')[0].Trim();
                    employee.FirstName = employeeFullName.Split(' ')[1].Trim();
                    employee.MidName = employeeFullName.Split(' ')[2].Trim();
                }

                double payrollValue = 0;

                try
                {
                    payrollValue = Convert.ToDouble(PayrollValue.Replace(".", ","));
                }
                catch (Exception)
                {
                    payrollValue = 0;
                }

                if (employee != null)
                {
                    if (ID != null && ID.HasValue == true)
                    {
                        if (tempCPFile == true)
                        {
                            employeePayrollSheetDataTable.Rows[ID.Value - 1]["PayrollChangeDate"] = Convert.ToDateTime(PayrollChangeDate);
                            employeePayrollSheetDataTable.Rows[ID.Value - 1]["PayrollValue"] = payrollValue;
                            employeePayrollSheetDataTable.Rows[ID.Value - 1]["Comments"] = "Указано пользователем: " + User.Identity.Name + ", " + DateTime.Now.ToString();

                            if (employeePayrollSheetDataTable.Columns.Contains("PaymentMethodProbation") == true)
                            {
                                employeePayrollSheetDataTable.Rows[ID.Value - 1]["PaymentMethodProbation"] = PaymentMethodProbation;
                            }

                            if (employeePayrollSheetDataTable.Columns.Contains("PaymentMethod") == true)
                            {
                                employeePayrollSheetDataTable.Rows[ID.Value - 1]["PaymentMethod"] = PaymentMethod;
                            }

                            if (employeePayrollSheetDataTable.Columns.Contains("AdditionallyInfo") == true)
                            {
                                employeePayrollSheetDataTable.Rows[ID.Value - 1]["AdditionallyInfo"] = AdditionallyInfo;
                            }
                        }
                    }
                    else
                    {
                        employeePayrollSheetDataTable.Rows.Add(employee.ADEmployeeID,
                            employee.FullName,
                            Convert.ToDateTime(PayrollChangeDate),
                            payrollValue,
                            "Указано пользователем: " + User.Identity.Name + ", " + DateTime.Now.ToString());

                        if (employeePayrollSheetDataTable.Columns.Contains("PaymentMethodProbation") == true)
                        {
                            employeePayrollSheetDataTable.Rows[employeePayrollSheetDataTable.Rows.Count - 1]["PaymentMethodProbation"] = PaymentMethodProbation;
                        }

                        if (employeePayrollSheetDataTable.Columns.Contains("PaymentMethod") == true)
                        {
                            employeePayrollSheetDataTable.Rows[employeePayrollSheetDataTable.Rows.Count - 1]["PaymentMethod"] = PaymentMethod;
                        }

                        if (employeePayrollSheetDataTable.Columns.Contains("AdditionallyInfo") == true)
                        {
                            employeePayrollSheetDataTable.Rows[employeePayrollSheetDataTable.Rows.Count - 1]["AdditionallyInfo"] = AdditionallyInfo;
                        }
                    }

                    result = _financeService.PutEmployeePayrollSheetDataTableToOO(user, employeePayrollSheetDataTable, tempCPFile);

                    if (tempCPFile == true
                        && bitrixReqEnrollmentID != null && bitrixReqEnrollmentID.HasValue == true
                        && saveResultInBitrix == true)
                    {
                        BitrixHelper bitrixHelper = new BitrixHelper(_bitrixConfigOptions);
                        BitrixReqEmployeeEnrollment bitrixReqEmployeeEnrollment = bitrixHelper.GetBitrixReqEmployeeEnrollmentById(bitrixReqEnrollmentID.Value.ToString());

                        //if (bitrixReqEmployeeEnrollment.PAYROLL_IS_ENTERED != null)
                        {
                            bitrixHelper.UpdateBitrixListElement(bitrixReqEmployeeEnrollment, nameof(bitrixReqEmployeeEnrollment.PAYROLL_IS_ENTERED), "Y");
                        }
                    }
                }
            }

            if (result == true)
            {
                return Content("true");
            }
            else
            {
                return Content("false");
            }
        }

        [OperationActionFilter(nameof(Operation.OOAccessAllow))]
        public ActionResult EmployeePayrollSummary(string mode)
        {
            ViewBag.Message = "Сводка по КОТ";

            dynamic model = new ExpandoObject();
            model.EmployeePayrollList = new List<Employee>();
            model.LastAddedEmployeePayrollRecordList = new List<EmployeePayrollRecord>();

            ApplicationUser user = _applicationUserService.GetUser();

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

            if (_ooService.CheckPayrollAccess() == true)
            {
                DataTable employeePayrollSheetDataTable = _financeService.GetEmployeePayrollSheetDataTableFromOO(user, false);
                List<EmployeePayrollRecord> employeePayrollRecordFullList = _financeService.GetFullListEmployeePayrollRecordsFromDataTable(employeePayrollSheetDataTable, _employeeService.Get(x => x.ToList()).ToList());
                List<EmployeePayrollRecord> lastAddedEmployeePayrollRecordList = employeePayrollRecordFullList.Where(r => r.PayrollChangeDate >= DateTime.Today.AddMonths(-6)).ToList()
                    .GroupBy(r => r.EmployeeID)
                    .SelectMany(r => r.Where(r1 => r1.PayrollChangeDate == r.Max(r2 => r2.PayrollChangeDate))).ToList();

                switch (mode)
                {
                    case "latestpayrolldata":
                        model.EmployeePayrollList = employeeList.Where(e => lastAddedEmployeePayrollRecordList.Any(r => r.EmployeeID == e.ID)).OrderBy(e => e.FullName).ToList();
                        model.LastAddedEmployeePayrollRecordList = lastAddedEmployeePayrollRecordList;

                        ViewBag.Message = "Изменения КОТ за последние 6 месяцев";
                        ViewBag.Mode = "latestpayrolldata";
                        break;

                    case "nopayrolldata":
                    default:
                        model.EmployeePayrollList = employeeList.Where(e => !employeePayrollRecordFullList.Any(r => r.EmployeeID == e.ID)).OrderBy(e => e.FullName).ToList();

                        ViewBag.Message = "Сотрудники без КОТ";
                        ViewBag.Mode = "nopayrolldata";
                        break;
                }
            }

            return View(model);
        }

        [OperationActionFilter(nameof(Operation.OOAccessFullPayrollAccess))]
        public ActionResult ApproveEmployeePayrollRecords(string adEmployeeID, int? bitrixReqEnrollmentID, bool? saveResultInBitrix,
            bool? testMode)
        {
            ApplicationUser user = _applicationUserService.GetUser();

            if (bitrixReqEnrollmentID != null
                && bitrixReqEnrollmentID.HasValue == true
                && String.IsNullOrEmpty(adEmployeeID) == false)
            {
                var employeeList = _employeeService.Get(x => x.Where(e => e.ADEmployeeID == adEmployeeID).ToList());

                if (employeeList == null || employeeList.Count() != 1)
                {
                    return StatusCode(StatusCodes.Status400BadRequest);
                }

                Employee employee = employeeList.FirstOrDefault();

                DateTime employeePayrollChangeMinDate = DateTime.MinValue;

                if (employee.EnrollmentDate != null && employee.EnrollmentDate.HasValue == true)
                {
                    employeePayrollChangeMinDate = employee.EnrollmentDate.Value.Date.AddDays(-20);
                }

                DataTable employeePayrollSheetDataTableTemp = _financeService.GetEmployeePayrollSheetDataTableFromOO(user, true);
                List<EmployeePayrollRecord> recordsTemp = _financeService.GetEmployeePayrollRecordsFromDataTable(employeePayrollSheetDataTableTemp, employee)
                    .Where(r => r.PayrollChangeDate >= employeePayrollChangeMinDate)
                    .OrderBy(r => r.PayrollChangeDate).ToList();

                if (recordsTemp == null || recordsTemp.Count() == 0)
                {
                    return StatusCode(StatusCodes.Status400BadRequest);
                }

                DataTable employeePayrollSheetDataTable = _financeService.GetEmployeePayrollSheetDataTableFromOO(user, false);
                List<EmployeePayrollRecord> records = _financeService.GetEmployeePayrollRecordsFromDataTable(employeePayrollSheetDataTable, employee)
                    .Where(r => r.PayrollChangeDate >= employeePayrollChangeMinDate)
                    .OrderBy(r => r.PayrollChangeDate).ToList();

                if (records != null && records.Count() != 0)
                {
                    return StatusCode(StatusCodes.Status400BadRequest);
                }

                foreach (var record in recordsTemp)
                {
                    employeePayrollSheetDataTable.Rows.Add(employee.ADEmployeeID,
                        employee.FullName,
                        record.PayrollChangeDate,
                        record.PayrollValue,
                        "Указано пользователем: " + User.Identity.Name + ", " + DateTime.Now.ToString());

                    if (employeePayrollSheetDataTable.Columns.Contains("PaymentMethod") == true)
                    {
                        employeePayrollSheetDataTable.Rows[employeePayrollSheetDataTable.Rows.Count - 1]["PaymentMethod"] = record.PaymentMethod;
                    }

                    if (employeePayrollSheetDataTable.Columns.Contains("AdditionallyInfo") == true)
                    {
                        employeePayrollSheetDataTable.Rows[employeePayrollSheetDataTable.Rows.Count - 1]["AdditionallyInfo"] = record.AdditionallyInfo;
                    }
                }

                if (testMode != true)
                {
                    _financeService.PutEmployeePayrollSheetDataTableToOO(user, employeePayrollSheetDataTable, false);
                }

                if (saveResultInBitrix == true)
                {
                    BitrixHelper bitrixHelper = new BitrixHelper(_bitrixConfigOptions);
                    BitrixReqEmployeeEnrollment bitrixReqEmployeeEnrollment = bitrixHelper.GetBitrixReqEmployeeEnrollmentById(bitrixReqEnrollmentID.Value.ToString());

                    //if (bitrixReqEmployeeEnrollment.PAYROLL_IS_APPROVED != null)
                    {
                        bitrixHelper.UpdateBitrixListElement(bitrixReqEmployeeEnrollment, nameof(bitrixReqEmployeeEnrollment.PAYROLL_IS_APPROVED), "Y");
                    }
                }

                return RedirectToAction("EmployeePayroll",
                    new
                    {
                        bitrixReqEnrollmentID = bitrixReqEnrollmentID,
                        allowApproveRecords = true,
                        saveResultInBitrix = saveResultInBitrix,
                        testMode = testMode
                    });
            }
            else
            {
                return StatusCode(StatusCodes.Status400BadRequest);
            }

        }

        [OperationActionFilter(nameof(Operation.OOAccessAllow))]
        public ActionResult EmployeePayroll(int? id, int? bitrixUserID, int? bitrixReqEnrollmentID, bool? allowApproveRecords, bool? saveResultInBitrix,
            bool? testMode)
        {
            Employee employee = null;

            ViewBag.AllowApproveRecords = false;
            ViewBag.RecordsApproved = false;
            ViewBag.TestMode = false;

            if (testMode == true)
            {
                ViewBag.TestMode = true;
            }

            ApplicationUser user = _applicationUserService.GetUser();

            if (bitrixUserID != null && bitrixUserID.HasValue == true)
            {
                BitrixHelper bitrixHelper = new BitrixHelper(_bitrixConfigOptions);
                BitrixUser bitrixUser = bitrixHelper.GetBitrixUserByID(bitrixUserID.Value.ToString());
                if (bitrixUser != null
                    && String.IsNullOrEmpty(bitrixUser.EMAIL) == false)
                {
                    employee = _employeeService.Get(x =>
                        x.Where(e => e.Email != null && e.Email.ToLower().Trim() == bitrixUser.EMAIL.ToLower().Trim()).ToList()).FirstOrDefault();
                }
            }
            else if (bitrixReqEnrollmentID != null && bitrixReqEnrollmentID.HasValue == true)
            {
                ViewBag.BitrixReqEnrollmentID = bitrixReqEnrollmentID;
                ViewBag.SaveResultInBitrix = (saveResultInBitrix != null && saveResultInBitrix.HasValue == true) ? saveResultInBitrix.Value : false;

                BitrixHelper bitrixHelper = new BitrixHelper(_bitrixConfigOptions);
                BitrixReqEmployeeEnrollment bitrixReqEmployeeEnrollment = bitrixHelper.GetBitrixReqEmployeeEnrollmentById(bitrixReqEnrollmentID.Value.ToString());
                if (bitrixReqEmployeeEnrollment != null
                    && bitrixReqEmployeeEnrollment.FULL_NAME != null
                    && String.IsNullOrEmpty(bitrixReqEmployeeEnrollment.FULL_NAME.FirstOrDefault().Value) == false)
                {
                    string employeeFullName = bitrixReqEmployeeEnrollment.FULL_NAME.FirstOrDefault().Value.Trim();

                    Employee employeeInDB = _employeeService.FindEmployeeByFullName(employeeFullName);// Get(x => x.Where(e => e.FullName == employeeFullName).ToList()).FirstOrDefault();

                    if (employeeInDB != null)
                    {
                        ViewBag.EmployeeIDInDB = employeeInDB.ID;

                        DateTime employeePayrollChangeMinDate = DateTime.MinValue;

                        if (employeeInDB.EnrollmentDate != null && employeeInDB.EnrollmentDate.HasValue == true)
                        {
                            employeePayrollChangeMinDate = employeeInDB.EnrollmentDate.Value.Date.AddDays(-20);
                            ViewBag.EmployeeEnrollmentDate = employeeInDB.EnrollmentDate.Value.Date;
                        }
                        else
                        {
                            ViewBag.EmployeeEnrollmentDate = null;
                        }

                        DataTable employeePayrollSheetDataTable = _financeService.GetEmployeePayrollSheetDataTableFromOO(user, false);
                        List<EmployeePayrollRecord> records = _financeService.GetEmployeePayrollRecordsFromDataTable(employeePayrollSheetDataTable, employeeInDB)
                            .Where(r => r.PayrollChangeDate >= employeePayrollChangeMinDate)
                            .OrderBy(r => r.PayrollChangeDate).ToList();
                        if (records != null && records.Count() != 0)
                        {
                            ViewBag.RecordsApproved = true;
                        }

                        DataTable employeePayrollSheetDataTableTemp = _financeService.GetEmployeePayrollSheetDataTableFromOO(user, true);
                        List<EmployeePayrollRecord> recordsTemp = _financeService.GetEmployeePayrollRecordsFromDataTable(employeePayrollSheetDataTableTemp, employeeInDB)
                            .Where(r => r.PayrollChangeDate >= employeePayrollChangeMinDate)
                            .OrderBy(r => r.PayrollChangeDate).ToList();

                        if (recordsTemp != null && recordsTemp.Count() != 0)
                        {
                            ViewBag.EmployeePayrollRecordChangeDate = recordsTemp.FirstOrDefault().PayrollChangeDate.Value.Date;

                            if (_applicationUserService.HasAccess(Operation.OOAccessFullPayrollAccess) == true && allowApproveRecords == true)
                            {
                                ViewBag.AllowApproveRecords = true;
                            }
                        }

                    }


                    employee = new Employee();

                    employee.ID = -1;

                    try
                    {
                        employee.LastName = employeeFullName.Split(' ')[0].Trim();
                        employee.FirstName = employeeFullName.Split(' ')[1].Trim();
                        employee.MidName = employeeFullName.Split(' ')[2].Trim();
                    }
                    catch (Exception)
                    {

                    }

                    if (employeeInDB != null)
                    {
                        employee.ADEmployeeID = employeeInDB.ADEmployeeID;
                    }
                }
            }
            else if (id != null && id.HasValue == true && id != -1)
            {
                employee = _employeeService.GetById(id.Value);
            }
            else
            {
                return StatusCode(StatusCodes.Status400BadRequest);
            }

            if (employee == null)
            {
                return StatusCode(StatusCodes.Status404NotFound);
            }

            return View(employee);
        }

        [OperationActionFilter(nameof(Operation.EmployeeCreateUpdate))]
        public ActionResult EmployeeBitrixReqEnrollment(int? id, int? bitrixReqEnrollmentID)
        {
            Employee employee = null;

            if (bitrixReqEnrollmentID != null && bitrixReqEnrollmentID.HasValue == true)
            {
                ViewBag.BitrixReqEnrollmentID = bitrixReqEnrollmentID;

                BitrixHelper bitrixHelper = new BitrixHelper(_bitrixConfigOptions);
                BitrixReqEmployeeEnrollment bitrixReqEmployeeEnrollment = bitrixHelper.GetBitrixReqEmployeeEnrollmentById(bitrixReqEnrollmentID.Value.ToString());
                if (bitrixReqEmployeeEnrollment != null
                    && bitrixReqEmployeeEnrollment.FULL_NAME != null
                    && String.IsNullOrEmpty(bitrixReqEmployeeEnrollment.FULL_NAME.FirstOrDefault().Value) == false)
                {
                    string employeeFullName = bitrixReqEmployeeEnrollment.FULL_NAME.FirstOrDefault().Value.Trim();
                    employee = _employeeService.FindEmployeeByFullName(employeeFullName);// Get(x => x.Where(e => e.FullName == employeeFullName).ToList()).FirstOrDefault();

                    if (employee == null)
                    {
                        employee = new Employee();

                        employee.ID = -1;

                        try
                        {
                            employee.LastName = employeeFullName.Split(' ')[0].Trim();
                            employee.FirstName = employeeFullName.Split(' ')[1].Trim();
                            employee.MidName = employeeFullName.Split(' ')[2].Trim();
                        }
                        catch (Exception)
                        {

                        }
                    }
                }
            }
            else if (id != null && id.HasValue == true && id != -1)
            {
                employee = _employeeService.GetById(id.Value);
            }
            else
            {
                return StatusCode(StatusCodes.Status400BadRequest);
            }

            return View(employee);
        }

        [OperationActionFilter(nameof(Operation.EmployeeCreateUpdate))]
        public ActionResult EmployeeCreateByBitrixReqEnrollment(int? bitrixReqEnrollmentID)
        {
            ApplicationUser user = _applicationUserService.GetUser();

            if (bitrixReqEnrollmentID != null && bitrixReqEnrollmentID.HasValue == true)
            {
                Employee employee = null;

                BitrixHelper bitrixHelper = new BitrixHelper(_bitrixConfigOptions);
                BitrixReqEmployeeEnrollment bitrixReqEmployeeEnrollment = bitrixHelper.GetBitrixReqEmployeeEnrollmentById(bitrixReqEnrollmentID.Value.ToString());
                if (bitrixReqEmployeeEnrollment != null
                    && bitrixReqEmployeeEnrollment.FULL_NAME != null
                    && String.IsNullOrEmpty(bitrixReqEmployeeEnrollment.FULL_NAME.FirstOrDefault().Value) == false)
                {
                    string listID = bitrixHelper.GetBitrixReqEmployeeEnrollmentListID();
                    Dictionary<string, string> POSITION_EMPLOYEE_DisplayValues = bitrixHelper.GetBitrixListPropertyDisplayValuesForm(listID, "POSITION_EMPLOYEE");
                    Dictionary<string, string> EMPLOYEE_QUALIFYING_ROLE_DisplayValues = bitrixHelper.GetBitrixListPropertyDisplayValuesForm(listID, "EMPLOYEE_QUALIFYING_ROLE");
                    Dictionary<string, string> TERRITORIAL_LOCATION_DisplayValues = bitrixHelper.GetBitrixListPropertyDisplayValuesForm(listID, "TERRITORIAL_LOCATION");
                    Dictionary<string, string> POSITION_ACCORDING_OFFICIAL_STAFF_SCHEDULE_ROLE_DisplayValues = bitrixHelper.GetBitrixListPropertyDisplayValuesForm(listID, "POSITION_ACCORDING_OFFICIAL_STAFF_SCHEDULE");
                    Dictionary<string, string> OFFICIAL_REGISTRATION_IN_COMPANY_DisplayValues = bitrixHelper.GetBitrixListPropertyDisplayValuesForm(listID, "OFFICIAL_REGISTRATION_IN_COMPANY");

                    string employeeFullName = RPCSHelper.NormalizeAndTrimString(bitrixReqEmployeeEnrollment.FULL_NAME.FirstOrDefault().Value);

                    employee = _employeeService.FindEmployeeByFullName(employeeFullName); // Get(x => x.Where(e => e.FullName == employeeFullName).ToList()).FirstOrDefault();

                    if (employee == null && String.IsNullOrEmpty(employeeFullName) == false)
                    {
                        employee = new Employee();

                        employee.LastName = employeeFullName.Split(' ')[0].Trim();
                        employee.FirstName = employeeFullName.Split(' ')[1].Trim();
                        employee.MidName = employeeFullName.Split(' ')[2].Trim();

                        if (bitrixReqEmployeeEnrollment.BIRTH_DATE != null)
                        {
                            employee.BirthdayDate = Convert.ToDateTime(bitrixReqEmployeeEnrollment.BIRTH_DATE.FirstOrDefault().Value);
                        }

                        if (bitrixReqEmployeeEnrollment.START_DATE_WORK != null)
                        {
                            employee.EnrollmentDate = Convert.ToDateTime(bitrixReqEmployeeEnrollment.START_DATE_WORK.FirstOrDefault().Value);
                        }

                        if (bitrixReqEmployeeEnrollment.POSITION_EMPLOYEE != null
                            && String.IsNullOrEmpty(bitrixReqEmployeeEnrollment.POSITION_EMPLOYEE?.FirstOrDefault().Value) == false)
                        {
                            string POSITION_EMPLOYEE = BitrixHelper.ParseBitrixListPropertyDisplayValueByID(POSITION_EMPLOYEE_DisplayValues, bitrixReqEmployeeEnrollment.POSITION_EMPLOYEE?.FirstOrDefault().Value);

                            if (String.IsNullOrEmpty(POSITION_EMPLOYEE) == false
                                && POSITION_EMPLOYEE.Contains(".") == true
                                && POSITION_EMPLOYEE.Split('.')[0] != null)
                            {
                                string employeePositionShortName = POSITION_EMPLOYEE.Split('.')[0].Trim().ToString();
                                EmployeePosition employeePosition = _employeePositionService.Get(x =>
                                        x.Where(ep => ep.ShortName == employeePositionShortName).ToList())
                                    .FirstOrDefault();

                                if (employeePosition != null)
                                {
                                    employee.EmployeePositionID = employeePosition.ID;
                                    employee.EmployeePositionTitle = employeePosition.Title;
                                }
                            }
                        }

                        if (bitrixReqEmployeeEnrollment.TERRITORIAL_LOCATION != null
                            && String.IsNullOrEmpty(bitrixReqEmployeeEnrollment.TERRITORIAL_LOCATION?.FirstOrDefault().Value) == false)
                        {
                            string TERRITORIAL_LOCATION = BitrixHelper.ParseBitrixListPropertyDisplayValueByID(TERRITORIAL_LOCATION_DisplayValues, bitrixReqEmployeeEnrollment.TERRITORIAL_LOCATION?.FirstOrDefault().Value);

                            if (String.IsNullOrEmpty(TERRITORIAL_LOCATION) == false
                                && TERRITORIAL_LOCATION.Contains(".") == true
                                && TERRITORIAL_LOCATION.Split('.')[0] != null)
                            {
                                string employeeLocationShortName = TERRITORIAL_LOCATION.Split('.')[0].Trim().ToString();
                                EmployeeLocation employeeLocation = _employeeLocationService.Get(x =>
                                        x.Where(el => el.ShortName == employeeLocationShortName)
                                            .ToList())
                                    .FirstOrDefault();

                                if (employeeLocation != null)
                                {
                                    employee.EmployeeLocationID = employeeLocation.ID;
                                }
                            }
                        }

                        if (bitrixReqEmployeeEnrollment.OFFICIAL_REGISTRATION_IN_COMPANY != null
                            && String.IsNullOrEmpty(bitrixReqEmployeeEnrollment.OFFICIAL_REGISTRATION_IN_COMPANY?.FirstOrDefault().Value) == false)
                        {
                            string OFFICIAL_REGISTRATION_IN_COMPANY = BitrixHelper.ParseBitrixListPropertyDisplayValueByID(OFFICIAL_REGISTRATION_IN_COMPANY_DisplayValues, bitrixReqEmployeeEnrollment.OFFICIAL_REGISTRATION_IN_COMPANY?.FirstOrDefault().Value);

                            if (String.IsNullOrEmpty(OFFICIAL_REGISTRATION_IN_COMPANY) == false
                                && OFFICIAL_REGISTRATION_IN_COMPANY.Contains(".") == true
                                && OFFICIAL_REGISTRATION_IN_COMPANY.Split('.')[0] != null)
                            {
                                string organisationShortName = OFFICIAL_REGISTRATION_IN_COMPANY.Split('.')[0].Trim().ToString();
                                Organisation organisation = _organisationService.Get(x =>
                                    x.Where(o => o.ShortName == organisationShortName)
                                        .ToList()).FirstOrDefault();

                                if (organisation != null)
                                {
                                    employee.OrganisationID = organisation.ID;
                                }
                            }
                        }

                        if (bitrixReqEmployeeEnrollment.PHONE_NUMBER != null)
                        {
                            employee.PersonalMobilePhoneNumber = bitrixReqEmployeeEnrollment.PHONE_NUMBER.FirstOrDefault().Value;
                        }

                        _employeeService.Add(employee);
                    }
                }

                return RedirectToAction("EmployeeBitrixReqEnrollment", new { bitrixReqEnrollmentID = bitrixReqEnrollmentID });
            }
            else
            {
                return StatusCode(StatusCodes.Status400BadRequest);
            }

        }

        [OperationActionFilter(nameof(Operation.EmployeeView))]
        [HttpGet]
        public ActionResult ViewVersion(int? id)
        {
            Employee employee = null;

            if (id == null)
            {
                return StatusCode(StatusCodes.Status400BadRequest);
            }
            employee = _employeeService.Get(x => x.Where(e => e.ID == id.Value).ToList(), GetEntityMode.VersionAndOther).FirstOrDefault();

            if (employee == null)
            {
                return StatusCode(StatusCodes.Status404NotFound);
            }

            if (!employee.IsVersion)
                return StatusCode(StatusCodes.Status403Forbidden);

            // ReSharper disable once Mvc.ViewNotResolved
            return View(employee);

        }
        
        [HttpGet]
        [OperationActionFilter(nameof(Operation.OOAccessAllow))]
        public ActionResult EmployeePayrollChange(EmployeePayrollChangeParametersViewModel viewModel) => RedirectToAction("DetailsEdit", "EmployeePayrollChange", viewModel);

        [OperationActionFilter(nameof(Operation.EmployeeIdentityDocsView))]
        public ActionResult EmployeeIdentityDocs(int bitrixUserID)
        {
            ApplicationUser user = _applicationUserService.GetUser();
            Employee currentUserEmployee = _userService.GetEmployeeForCurrentUser();

            var bitrixHelper = new BitrixHelper(_bitrixConfigOptions);
            BitrixUser bitrixUser = bitrixHelper.GetBitrixUserByID(bitrixUserID.ToString());

            Employee employee = null;
            if (bitrixUser != null)
            {
                employee = _employeeService.Get(x => x
                    .Where(e => e.Email != null && e.Email.ToLower().Equals(bitrixUser.EMAIL.Trim(), StringComparison.OrdinalIgnoreCase))
                    .ToList()).FirstOrDefault();

                if (employee != null)
                    ViewBag.Title = "Карточка сотрудника:" + employee.FullName;
                else
                    ViewBag.Title = "Карточка сотрудника: не найдено";
            }
            else
            {
                ViewBag.Title = "Карточка сотрудника: не найдено";
            }
            return View(employee);
        }
    }
}
