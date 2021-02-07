using System;
using System.Collections.Generic;
using System.Data;
using System.DirectoryServices.ActiveDirectory;
using System.Linq;
using System.Net;
using Core.BL.Interfaces;
using Core.Config;
using Core.Extensions;

using Core.Helpers;
using Core.Models;
using Core.Models.RBAC;
using MainApp.BitrixSync;
using MainApp.RBAC.Attributes;
using MainApp.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;


namespace MainApp.Controllers
{
    public class EmployeePayrollChangeController : Controller
    {
        private readonly IEmployeeService _employeeService;
        private readonly IEmployeeGradService _employeeGradService;
        private readonly IEmployeeGradAssignmentService _employeeGradAssignmentService;
        private readonly IApplicationUserService _applicationUserService;
        private readonly IFinanceService _financeService;
        private readonly IUserService _userService;
        private readonly IOOService _OOService;
        private readonly IBudgetLimitService _budgetLimitService;
        private readonly IDepartmentService _departmentService;
        private readonly IOptions<BitrixConfig> _bitrixOptions;
        private readonly IEmployeeDepartmentAssignmentService _employeeDepartmentAssignmentService;
        private readonly IEmployeeGradParamService _employeeGradParamService;

        public EmployeePayrollChangeController(IServiceProvider serviceProvider)
        {
            _employeeService = serviceProvider.GetService<IEmployeeService>();
            _employeeGradService = serviceProvider.GetService<IEmployeeGradService>();
            _employeeGradAssignmentService = serviceProvider.GetService<IEmployeeGradAssignmentService>();
            _applicationUserService = serviceProvider.GetService<IApplicationUserService>();
            _financeService = serviceProvider.GetService<IFinanceService>();
            _userService = serviceProvider.GetService<IUserService>();
            _OOService = serviceProvider.GetService<IOOService>();
            _bitrixOptions = serviceProvider.GetService<IOptions<BitrixConfig>>();
            _budgetLimitService = serviceProvider.GetService<IBudgetLimitService>();
            _departmentService = serviceProvider.GetService<IDepartmentService>();
            _employeeDepartmentAssignmentService = serviceProvider.GetService<IEmployeeDepartmentAssignmentService>();
            _employeeGradParamService = serviceProvider.GetService<IEmployeeGradParamService>();
        }
        // GET: EmployeePayrollChange
        public ActionResult Index() => Content("Working");

        [HttpGet]
        [OperationActionFilter(nameof(Operation.OOAccessAllow))]
        public ActionResult DetailsEdit(EmployeePayrollChangeParametersViewModel viewModel) => View(viewModel);

        [HttpPost]
        [OperationActionFilter(nameof(Operation.OOAccessAllow))]
        public ActionResult DetailsEditInternal(string bitrixUserLogin, int? bitrixUserID, int? bitrixReqPayrollChangeID, int? recordType, int? actionModeForm, bool? forceEdit, bool? disableReject, string userComment = null, string userSpecialComment = null)
        {
            if (!_OOService.CheckPayrollAccess())
                return PartialView(new EmployeePayrollChangeRecordViewModel());

            if (!recordType.HasValue)
                return StatusCode(StatusCodes.Status400BadRequest);
            else if (recordType.Value < (int)EmployeePayrollRecordType.PayrollChangeHD)
                return StatusCode(StatusCodes.Status400BadRequest);

            var currentRecordType = (EmployeePayrollRecordType)recordType;
            ViewBag.RecordTypeID = recordType;
            ApplicationUser user = _applicationUserService.GetUser();
            Employee currentUserEmployee = _userService.GetEmployeeForCurrentUser();
            Employee employee = null;
            var bitrixHelper = new BitrixHelper(_bitrixOptions);

            if (currentUserEmployee == null)
            {
                return StatusCode(StatusCodes.Status403Forbidden);
            }
            else if (bitrixUserID.HasValue && bitrixReqPayrollChangeID.HasValue || !string.IsNullOrEmpty(bitrixUserLogin))
            {
                // Логика подгрузки сотрудника, ужасна!
                BitrixUser bitrixUser = null;
                if (string.IsNullOrEmpty(bitrixUserLogin))
                    bitrixUser = bitrixHelper.GetBitrixUserByID(bitrixUserID.Value.ToString());
                if (bitrixUser != null && !string.IsNullOrEmpty(bitrixUser.EMAIL) || !string.IsNullOrEmpty(bitrixUserLogin))
                {
                    var grads = _employeeGradService.Get(x => x.ToList().OrderBy(e => e.ShortName).ToList());
                    ViewBag.EmployeeGradID = new SelectList(grads, "ID", "ShortName");

                    if (!string.IsNullOrEmpty(bitrixUserLogin))
                    {
                        string domainName = Domain.GetCurrentDomain().Name;
                        string domainNetbiosName = ADHelper.GetDomainNetbiosName(Domain.GetCurrentDomain());
                        string employeeADLogin = domainNetbiosName + "\\" + bitrixUserLogin;

                        employee = _employeeService.GetEmployeeByLogin(employeeADLogin);
                        ViewBag.BitrixUserLogin = bitrixUserLogin;
                    }
                    else
                        employee = GetEmployeeByBitrixUser(bitrixUser);

                    if (employee != null)
                    {
                        if (CheckEmployeePayrollChangeAccessForUser(employee, user, currentRecordType) == false)
                        {
                            return StatusCode(StatusCodes.Status403Forbidden);
                        }
                    }

                    if (employee != null)
                    {
                        DataTable employeePayrollSheetDataTable = _financeService.GetEmployeePayrollSheetDataTableFromOO(user, false);
                        EmployeePayrollRecord record = _financeService.GetEmployeePayrollRecordsFromDataTable(employeePayrollSheetDataTable, employee)
                            .OrderByDescending(r => r.PayrollChangeDate)
                            .Where(r => r.PayrollChangeDate.Value.Date <= DateTime.Today)
                            .FirstOrDefault();

                        if (record != null
                            && (record.EmployeeGrad == null || record.EmployeeGrad == 0)
                            && employee.EmployeeGrad != null
                            && String.IsNullOrEmpty(employee.EmployeeGrad.ShortName) == false)
                        {
                            try
                            {
                                int grad;
                                if (int.TryParse(employee.EmployeeGrad.ShortName, out grad))
                                    record.EmployeeGrad = grad;
                                else
                                    record.EmployeeGrad = 0;
                            }
                            catch (Exception)
                            {
                                record.EmployeeGrad = 0;
                            }
                        }

                        DataTable employeePayrollSheetDataTableTmp = _financeService.GetEmployeePayrollSheetDataTableFromOO(user, true);
                        var tmpEmployeePayrollRecordList = _financeService.GetEmployeePayrollRecordsFromDataTable(employeePayrollSheetDataTableTmp, employee).ToList();

                        EmployeePayrollRecord tmpRecord = null;
                        List<EmployeePayrollRecord> tmpRecordList = null;
                        int rowId = -1;
                        tmpRecordList = GetEmployeePayrollRecords(tmpEmployeePayrollRecordList, bitrixReqPayrollChangeID.Value.ToString());
                        if (tmpRecordList.Count > 0)
                        {
                            tmpRecord = tmpRecordList.Last();
                            rowId = tmpRecord.ID - 1;
                        }
                        else
                            tmpRecord = null;


                        var filteredTmpEmployees = tmpEmployeePayrollRecordList.OrderBy(r => r.PayrollChangeDate).Where(rec => !string.IsNullOrEmpty(rec.SourceElementID))
                            .Where(rec => !rec.SourceElementID.Trim().Equals(bitrixReqPayrollChangeID.Value.ToString(), StringComparison.OrdinalIgnoreCase))
                            .GroupBy(rec => rec.SourceElementID)
                            .Where(g => g.FirstOrDefault(rec => rec.RecordType == EmployeePayrollRecordType.PayrollChangeCEO) == null)
                            .Where(g => g.FirstOrDefault(rec => rec.RecordResult == EmployeePayrollRecordResult.Rejected) == null);

                        BitrixReqPayrollChange bitrixReqPayrollChange = bitrixHelper.GetBitrixReqPayrollChangeById(bitrixReqPayrollChangeID.Value.ToString());
                        if (IsTaskCompleted(currentRecordType, bitrixReqPayrollChange, bitrixHelper))
                        {
                            ViewBag.ToEdit = false;
                            ViewBag.ActionModeFormList = null;
                            ViewBag.ActionModeFormItem = null;
                        }
                        else if (tmpRecord == null && filteredTmpEmployees.Count() > 0 && currentRecordType == EmployeePayrollRecordType.PayrollChangeHD)
                        {
                            ViewBag.ProcessHasNotComplete = true;
                            ViewBag.ProcessesNotCompleted = string.Join(", ", filteredTmpEmployees.Select(g => g.FirstOrDefault().URegNum).Where(s => !string.IsNullOrEmpty(s)));
                            ViewBag.ToEdit = false;
                            ViewBag.ActionModeFormList = null;
                            ViewBag.ActionModeFormItem = null;
                        }
                        else if (tmpRecord == null)
                        {
                            ViewBag.ToEdit = true;
                            if (!disableReject.NullableBoolToBool())
                            {
                                if (!actionModeForm.HasValue)
                                    ViewBag.ActionModeFormItem = (int)EmployeePayrollRecordActionFormMode.InputSuggestion;
                                else
                                    ViewBag.ActionModeFormItem = actionModeForm.Value;
                                var actionModeNames = EmployeePayrollRecordActionFormModeHelper.GetNamesForActionFormMode();
                                ViewBag.ActionModeFormList = new SelectList(actionModeNames.Where(an => an.Key == (int)EmployeePayrollRecordActionFormMode.InputSuggestion
                                                || an.Key == (int)EmployeePayrollRecordActionFormMode.Reject), "Key", "Value");
                            }
                            else
                            {
                                ViewBag.ActionModeFormList = null;
                                ViewBag.ActionModeFormItem = null;
                            }
                            if (currentRecordType != EmployeePayrollRecordType.PayrollChangeHD)
                                return StatusCode(StatusCodes.Status403Forbidden);
                        }
                        else
                        {
                            var tmpResult = tmpRecordList;
                            if (tmpResult.Count() == 0) // скорее всего никогда не отработает
                            {
                                ViewBag.ToEdit = true;
                                ViewBag.ActionModeFormList = null;
                                ViewBag.ActionModeFormItem = null;
                                // отрабатываем на первое добавление
                                // на редактирование
                            }
                            else if (tmpResult.Count() > 0)
                            {
                                // проверка на бизнес процесс
                                var tmpLast = tmpRecordList.Last();
                                int tmpLastRecordTypeIndex = EmployeePayrollRecordTypeHelper.GetIndexForType(tmpLast.RecordType);
                                int currentRecordTypeIndex = EmployeePayrollRecordTypeHelper.GetIndexForType(currentRecordType);
                                if ((tmpLastRecordTypeIndex == currentRecordTypeIndex || tmpLastRecordTypeIndex > currentRecordTypeIndex) && currentRecordTypeIndex != -1 && !forceEdit.NullableBoolToBool())
                                {
                                    // только на чтение
                                    ViewBag.ToEdit = false;
                                    ViewBag.ActionModeFormList = null;
                                    ViewBag.ActionModeFormItem = null;
                                    ViewBag.ViewForceEditLink = tmpLastRecordTypeIndex == currentRecordTypeIndex && !forceEdit.NullableBoolToBool();
                                }
                                else if (tmpLastRecordTypeIndex == currentRecordTypeIndex - 1 && currentRecordTypeIndex != -1
                                    || tmpLastRecordTypeIndex == currentRecordTypeIndex && forceEdit.NullableBoolToBool())
                                {
                                    ViewBag.ViewForceEditLink = tmpLastRecordTypeIndex == currentRecordTypeIndex && forceEdit.NullableBoolToBool();
                                    // на редактирование
                                    if (currentRecordType != EmployeePayrollRecordType.PayrollChangeHD || forceEdit.NullableBoolToBool())
                                    {
                                        if (currentRecordType != EmployeePayrollRecordType.PayrollChangeFin && currentRecordType != EmployeePayrollRecordType.PayrollChangeHR)
                                        {
                                            ViewBag.ToEdit = true;
                                            var actionModeNames = EmployeePayrollRecordActionFormModeHelper.GetNamesForActionFormMode();
                                            if (currentRecordType == EmployeePayrollRecordType.PayrollChangeHDCurator)
                                            {
                                                if (!actionModeForm.HasValue)
                                                    actionModeForm = (int)EmployeePayrollRecordActionFormMode.Approve;
                                                ViewBag.ActionModeFormItem = actionModeForm.Value;

                                                ViewBag.ActionModeFormList = new SelectList(actionModeNames.Where(an => an.Key == (int)EmployeePayrollRecordActionFormMode.Approve
                                                || an.Key == (int)EmployeePayrollRecordActionFormMode.ApproveWithSuggestions
                                                || (an.Key == (int)EmployeePayrollRecordActionFormMode.Reject && !disableReject.NullableBoolToBool())), "Key", "Value");
                                            }
                                            else if (currentRecordType == EmployeePayrollRecordType.PayrollChangeCEO)
                                            {
                                                // нужно ли делать проверку на корректный параметр actionModeForm
                                                if (!actionModeForm.HasValue)
                                                    actionModeForm = (int)EmployeePayrollRecordActionFormMode.FinalApproveHDVersion;
                                                ViewBag.ActionModeFormItem = actionModeForm.Value;

                                                ViewBag.ActionModeFormList = new SelectList(actionModeNames.Where(an => an.Key == (int)EmployeePayrollRecordActionFormMode.FinalApproveHDVersion
                                                || an.Key == (int)EmployeePayrollRecordActionFormMode.FinalApproveHDCuratorVersion
                                                || an.Key == (int)EmployeePayrollRecordActionFormMode.FinalApproveWithSuggestions
                                                || (an.Key == (int)EmployeePayrollRecordActionFormMode.Reject && !disableReject.NullableBoolToBool())), "Key", "Value");
                                            }
                                            else if (currentRecordType == EmployeePayrollRecordType.PayrollChangeHD && forceEdit.NullableBoolToBool())
                                            {
                                                ViewBag.ToEdit = true;
                                                if (disableReject.NullableBoolToBool())
                                                {
                                                    ViewBag.ActionModeFormList = null;
                                                    ViewBag.ActionModeFormItem = null;
                                                }
                                                else
                                                {
                                                    if (!actionModeForm.HasValue)
                                                        ViewBag.ActionModeFormItem = (int)EmployeePayrollRecordActionFormMode.InputSuggestion;
                                                    else
                                                        ViewBag.ActionModeFormItem = actionModeForm.Value;
                                                    ViewBag.ActionModeFormList = new SelectList(actionModeNames.Where(an =>
                                                    an.Key == (int)EmployeePayrollRecordActionFormMode.InputSuggestion
                                                    || an.Key == (int)EmployeePayrollRecordActionFormMode.Reject), "Key", "Value");
                                                }
                                            }
                                            else
                                            {
                                                ViewBag.ToEdit = false;
                                                ViewBag.ActionModeFormList = null;
                                                ViewBag.ActionModeFormItem = null;
                                            }
                                        }
                                        else
                                        {
                                            ViewBag.ToEdit = true;
                                            ViewBag.ActionModeFormList = null;
                                            ViewBag.ActionModeFormItem = null;
                                        }

                                    }
                                    else
                                    {
                                        ViewBag.ToEdit = false;
                                        ViewBag.ActionModeFormItem = (int)EmployeePayrollRecordActionFormMode.Approve;
                                    }
                                }
                                else
                                {
                                    // просто так
                                    return StatusCode(StatusCodes.Status403Forbidden);
                                }
                            }
                        }

                        SetViewBag(tmpRecord, ViewBag.ViewForceEditLink == true);

                        ViewBag.RecordType = (EmployeePayrollRecordType)recordType;

                        ViewBag.RecordTMPRowId = rowId;
                        ViewBag.BitrixUserID = bitrixUserID;
                        ViewBag.BitrixReqPayrollChangeID = bitrixReqPayrollChangeID;
                        ViewBag.DisableReject = disableReject.NullableBoolToBool();

                        if (ViewBag.ToEdit == true)
                        {
                            ViewBag.UserComment = userComment != null ? userComment : ViewBag.UserComment;
                            ViewBag.UserSpecialComment = userSpecialComment != null ? userSpecialComment : ViewBag.UserSpecialComment;
                        }

                        if (ViewBag.ViewForceEditLink == true)
                        {
                            var recordFormMode = EmployeePayrollRecordResultHelper.MapToFormMode(tmpRecord.RecordResult);
                            if (ViewBag.RecordType == EmployeePayrollRecordType.PayrollChangeHD)
                            {
                                ViewBag.ActionModeForm = (int)(recordFormMode == EmployeePayrollRecordActionFormMode.ApproveWithSuggestions ?
                                    EmployeePayrollRecordActionFormMode.InputSuggestion : recordFormMode);
                            }
                            else
                                ViewBag.ActionModeForm = (int)recordFormMode;
                        }

                        Department department = employee.Department;
                        if (department != null)
                        {
                            while (department.ParentDepartment != null && !department.IsFinancialCentre)
                            {
                                department = department.ParentDepartment;
                            }
                        }

                        //string depName = string.Empty;
                        //if (department != null)
                        //    depName = department.FullName;

                        if (record != null)
                        {
                            if (department != null)
                            {
                                record.DepartmentName = department.FullName;
                                record.DepartmentID = department.ID;
                            }
                            else
                                return StatusCode(StatusCodes.Status400BadRequest);

                            record.Employee = employee;
                            return PartialView(new EmployeePayrollChangeRecordViewModel(tmpRecordList, record));
                        }
                        else
                        {
                            var viewModel = new EmployeePayrollChangeRecordViewModel(tmpRecordList, record: new EmployeePayrollRecord() { Employee = employee, DepartmentName = department?.FullName, DepartmentID = department?.ID });
                            return PartialView(viewModel);
                        }
                    }
                    else
                    {
                        return StatusCode(StatusCodes.Status400BadRequest);
                    }
                }
                else
                {
                    return StatusCode(StatusCodes.Status400BadRequest);
                }
            }
            else
            {
                return StatusCode(StatusCodes.Status400BadRequest);
            }
        }

        [HttpPost]
        [OperationActionFilter(nameof(Operation.OOAccessAllow))]
        public JsonResult Update(EmployeePayrollRecord employeePayrollRecord, string bitrixUserLogin, int itemId, int? bitrixUserID, int? bitrixReqPayrollChangeID, int? recordType, int? actionModeForm, bool? forceEdit)
        {
            // нет проверки на соответсвие recordType и actionModeForm
            if (!recordType.HasValue)
                return Json(new { Error = "ErrorRecordType" });
            else if (recordType.Value < (int)EmployeePayrollRecordType.PayrollChangeHD)
                return Json(new { Error = "ErrorRecordType" });

            var recType = EmployeePayrollRecordType.PayrollChangeHD;
            try
            {
                recType = (EmployeePayrollRecordType)recordType;
            }
            catch (Exception ex)
            {
                return Json(new { Error = "ErrorRecordType" });
            }

            ApplicationUser user = _applicationUserService.GetUser();
            Employee currentUserEmployee = _userService.GetEmployeeForCurrentUser();
            var bitrixHelper = new BitrixHelper(_bitrixOptions);
            if (currentUserEmployee == null)
            {
                return Json(new { Error = "ErrorUser" });
            }
            else if ((bitrixUserID.HasValue && bitrixReqPayrollChangeID.HasValue || !string.IsNullOrEmpty(bitrixUserLogin)) && recordType.HasValue)
            {
                Employee employee = null;
                if (string.IsNullOrEmpty(bitrixUserLogin))
                {
                    BitrixUser bitrixUser = bitrixHelper.GetBitrixUserByID(bitrixUserID.Value.ToString());
                    employee = GetEmployeeByBitrixUser(bitrixUser);
                }
                else
                {
                    string domainName = Domain.GetCurrentDomain().Name;
                    string domainNetbiosName = ADHelper.GetDomainNetbiosName(Domain.GetCurrentDomain());
                    string employeeADLogin = domainNetbiosName + "\\" + bitrixUserLogin;

                    employee = _employeeService.GetEmployeeByLogin(employeeADLogin);
                }
                if (employee != null)
                {
                    if (CheckEmployeePayrollChangeAccessForUser(employee, user, recType) == false)
                    {
                        return Json(new { Error = "ErrorAccess" });
                    }
                }
                employeePayrollRecord.Employee = employee;

                DataTable employeePayrollSheetDataTableTmp = _financeService.GetEmployeePayrollSheetDataTableFromOO(user, true);
                var tmpEmployeePayrollRecordList = _financeService.GetEmployeePayrollRecordsFromDataTable(employeePayrollSheetDataTableTmp, employee);
                EmployeePayrollRecord tmpRecord = null;
                List<EmployeePayrollRecord> tmpRecordList = null;
                int rowId = -1;
                if (bitrixReqPayrollChangeID.HasValue)
                {
                    tmpRecordList = GetEmployeePayrollRecords(tmpEmployeePayrollRecordList, bitrixReqPayrollChangeID.Value.ToString());
                    if (tmpRecordList.Count > 0)
                    {
                        tmpRecord = tmpRecordList.Last();
                        rowId = tmpRecord.ID - 1;
                    }
                    else
                        tmpRecord = null;
                }

                // логика первого добавления
                if (rowId < 0)
                {
                    // добавление
                    try
                    {
                        employeePayrollRecord.SourceElementID = bitrixReqPayrollChangeID.Value.ToString();
                        employeePayrollRecord.RecordType = EmployeePayrollRecordType.PayrollChangeHD;
                        employeePayrollRecord.RecordResult = EmployeePayrollRecordActionFormModeHelper.MapToRecordResult(EmployeePayrollRecordActionFormMode.ApproveWithSuggestions);

                        if (actionModeForm.HasValue && actionModeForm == (int)EmployeePayrollRecordActionFormMode.Reject)
                        {
                            employeePayrollRecord.PayrollChangeDate = null;
                            employeePayrollRecord.PayrollHalfYearValue = 0;
                            employeePayrollRecord.PayrollQuarterValue = 0;
                            employeePayrollRecord.PayrollValue = 0;
                            employeePayrollRecord.PayrollYearValue = 0;
                            employeePayrollRecord.EmployeeGrad = 0;
                            employeePayrollRecord.RecordResult = EmployeePayrollRecordResult.Rejected;
                        }

                        string nonValidFieldName = ValidateFieldValues(employeePayrollRecord, recType,
                            actionModeForm.HasValue ? (EmployeePayrollRecordActionFormMode?)actionModeForm : null);
                        if (!string.IsNullOrEmpty(nonValidFieldName))
                            return Json(new { Error = /*"Незаполнено обязательное поле - " + */nonValidFieldName });

                        BitrixReqPayrollChange bitrixReqPayrollChange = bitrixHelper.GetBitrixReqPayrollChangeById(bitrixReqPayrollChangeID.Value.ToString());
                        if (IsTaskCompleted(recType, bitrixReqPayrollChange, bitrixHelper))
                            return Json(new { Error = "ErrorBitrixTaskComplete" });

                        employeePayrollRecord.URegNum = bitrixReqPayrollChange.NAME;
                        employeePayrollRecord.Created = DateTime.Now;

                        _financeService.UpdateEmployeePayrollTableRecords(employeePayrollSheetDataTableTmp, User.Identity.Name, currentUserEmployee, employee, employeePayrollRecord, true, rowId);
                        bool result = _financeService.PutEmployeePayrollSheetDataTableToOO(user, employeePayrollSheetDataTableTmp, true);

                        if (!result)
                            return Json(new { Error = "Произошла ошибка при записи данных КОТ. Пожалуйста, попробуйте выполнить операцию позже или обратитесь к администратору системы." });

                        if (!actionModeForm.HasValue)
                            actionModeForm = (int)EmployeePayrollRecordActionFormMode.InputSuggestion;

                        if (SetBitrixTaskParameter((EmployeePayrollRecordActionFormMode)actionModeForm, recType, bitrixHelper, bitrixReqPayrollChange))
                            return Json(new { Status = "Added" });
                        else
                            return Json(new { Error = "ErrorBitrixAdd" });
                    }
                    catch (Exception e)
                    {
                        return Json(new { Error = "ErrorAdd: " + "<br>" + e.Message + "<br>" + e.StackTrace + "<br>" + e.TargetSite.ToString() });
                    }
                }
                else // для последующего добавление, больше не обновляем, только добавляем
                {

                    if (recType == EmployeePayrollRecordType.PayrollChangeFin || recType == EmployeePayrollRecordType.PayrollChangeHR)
                        actionModeForm = (int)EmployeePayrollRecordActionFormMode.Approve;
                    else if (recType == EmployeePayrollRecordType.PayrollChangeHD && forceEdit.NullableBoolToBool())
                    {
                        if (!actionModeForm.HasValue)
                            actionModeForm = (int)EmployeePayrollRecordActionFormMode.InputSuggestion;
                    }
                    // последующее добавление происходит только с параметром actionModeForm
                    if (!actionModeForm.HasValue)
                    {
                        return Json(new { Error = "ErrorModeForm" });
                    }
                    else if (rowId != itemId)
                    {
                        return Json(new { Error = "ItemIdError" });
                    }
                    else
                    {
                        if (forceEdit.NullableBoolToBool() && tmpRecordList.Count > 1 && (int)tmpRecord.RecordType == recordType.Value)
                            tmpRecord = tmpRecordList.ElementAt(tmpRecordList.Count - 2);

                        var mode = recType == EmployeePayrollRecordType.PayrollChangeFin || recType == EmployeePayrollRecordType.PayrollChangeHR ?
                            EmployeePayrollRecordActionFormMode.Approve : (EmployeePayrollRecordActionFormMode)actionModeForm.Value;
                        if (mode == EmployeePayrollRecordActionFormMode.Approve)
                        {
                            tmpRecord.UserComment = employeePayrollRecord.UserComment;
                            tmpRecord.UserSpecialComment = employeePayrollRecord.UserSpecialComment;
                            employeePayrollRecord = tmpRecord;
                        }
                        else if (mode == EmployeePayrollRecordActionFormMode.ApproveWithSuggestions || mode == EmployeePayrollRecordActionFormMode.FinalApproveWithSuggestions || mode == EmployeePayrollRecordActionFormMode.InputSuggestion)
                        {
                            // оставляем как есть
                        }
                        else if (mode == EmployeePayrollRecordActionFormMode.Reject)
                        {
                            tmpRecord.UserComment = employeePayrollRecord.UserComment;
                            tmpRecord.UserSpecialComment = employeePayrollRecord.UserSpecialComment;
                            employeePayrollRecord = tmpRecord;
                            employeePayrollRecord.PayrollHalfYearValue = 0;
                            employeePayrollRecord.PayrollQuarterValue = 0;
                            employeePayrollRecord.PayrollValue = 0;
                            employeePayrollRecord.PayrollYearValue = 0;
                            employeePayrollRecord.EmployeeGrad = 0;
                            if (recType == EmployeePayrollRecordType.PayrollChangeHD)
                                employeePayrollRecord.PayrollChangeDate = null;
                        }
                        else if (recType == EmployeePayrollRecordType.PayrollChangeCEO)
                        {
                            if (mode == EmployeePayrollRecordActionFormMode.FinalApproveHDVersion)
                                tmpRecord = tmpRecordList.FirstOrDefault(rec => rec.RecordType == EmployeePayrollRecordType.PayrollChangeHD);
                            else if (mode == EmployeePayrollRecordActionFormMode.FinalApproveHDCuratorVersion)
                                tmpRecord = tmpRecordList.FirstOrDefault(rec => rec.RecordType == EmployeePayrollRecordType.PayrollChangeHDCurator);
                            else
                                tmpRecord = null;
                            if (tmpRecord != null)
                            {
                                tmpRecord.UserComment = employeePayrollRecord.UserComment;
                                tmpRecord.UserSpecialComment = employeePayrollRecord.UserSpecialComment;
                                employeePayrollRecord = tmpRecord;
                            }
                            else
                                return Json(new { Error = "ErrorModeForm" });
                        }
                        else
                            return Json(new { Error = "ErrorModeForm" });
                        // обновление
                        // обработать режим обновления
                        try
                        {
                            employeePayrollRecord.SourceElementID = tmpRecord.SourceElementID;
                            employeePayrollRecord.RecordType = recType;
                            employeePayrollRecord.RecordResult = EmployeePayrollRecordActionFormModeHelper.MapToRecordResult(mode);

                            string nonValidFieldName = ValidateFieldValues(employeePayrollRecord, recType, (EmployeePayrollRecordActionFormMode?)mode);
                            if (!string.IsNullOrEmpty(nonValidFieldName))
                                return Json(new { Error = /*"Незаполнено обязательное поле - " + */ nonValidFieldName });

                            BitrixReqPayrollChange bitrixReqPayrollChange = bitrixHelper.GetBitrixReqPayrollChangeById(bitrixReqPayrollChangeID.Value.ToString());
                            if (IsTaskCompleted(recType, bitrixReqPayrollChange, bitrixHelper))
                                return Json(new { Error = "ErrorBitrixTaskComplete" });

                            employeePayrollRecord.URegNum = bitrixReqPayrollChange.NAME;
                            employeePayrollRecord.Created = DateTime.Now;

                            int recordRowId = forceEdit.NullableBoolToBool() ? rowId : -1;

                            // -1
                            _financeService.UpdateEmployeePayrollTableRecords(employeePayrollSheetDataTableTmp, User.Identity.Name, currentUserEmployee, employee, employeePayrollRecord, true, recordRowId);
                            bool result = _financeService.PutEmployeePayrollSheetDataTableToOO(user, employeePayrollSheetDataTableTmp, true);

                            if (!result)
                                return Json(new { Error = "Произошла ошибка при записи данных КОТ. Пожалуйста, попробуйте выполнить операцию позже или обратитесь к администратору системы." });

                            if (recType == EmployeePayrollRecordType.PayrollChangeCEO && (mode != EmployeePayrollRecordActionFormMode.Reject || forceEdit.NullableBoolToBool()))
                            {
                                DataTable employeePayrollSheetDataTable = _financeService.GetEmployeePayrollSheetDataTableFromOO(user, false);
                                // -1
                                // нужно передавать другой id для директора из записи КОТ
                                EmployeePayrollRecord employeePayrollRecordCEO = null;
                                if (forceEdit.NullableBoolToBool())
                                {
                                    var employeePayrollRecordList = _financeService.GetEmployeePayrollRecordsFromDataTable(employeePayrollSheetDataTable, employee);
                                    var recordList = employeePayrollRecordList.Where(emp => emp.SourceElementID.Equals(bitrixReqPayrollChangeID.Value.ToString(), StringComparison.OrdinalIgnoreCase))
                                        .OrderBy(emp => emp.Created);
                                    employeePayrollRecordCEO = recordList.Count() > 0 ? recordList.Last() : null;
                                }

                                employeePayrollRecord.RecordType = EmployeePayrollRecordType.PayrollChange;

                                int recordCEORowId = employeePayrollRecordCEO != null ? employeePayrollRecordCEO.ID - 1 : -1; // обновляем или добавляем
                                if (mode != EmployeePayrollRecordActionFormMode.Reject)
                                {
                                    _financeService.UpdateEmployeePayrollTableRecords(employeePayrollSheetDataTable, User.Identity.Name, currentUserEmployee, employee, employeePayrollRecord, false, recordCEORowId);
                                }
                                else if (recordCEORowId != -1)
                                {
                                    _financeService.ClearRecord(employeePayrollSheetDataTable, recordCEORowId, false);
                                }

                                if (_financeService.PutEmployeePayrollSheetDataTableToOO(user, employeePayrollSheetDataTable, false))
                                {
                                    if (employeePayrollRecord.EmployeeGrad != null && employeePayrollRecord.PayrollChangeDate != null
                                        && mode != EmployeePayrollRecordActionFormMode.Reject)
                                    {
                                        EmployeeGrad employeeGrad = _employeeGradService.Get(x => x.Where(eg => eg.ShortName == employeePayrollRecord.EmployeeGrad.Value.ToString()).ToList()).FirstOrDefault();

                                        if (employeeGrad != null)
                                        {
                                            if (employee.EmployeeGradID == null
                                                 || employee.EmployeeGradID.HasValue == false
                                                 || employee.EmployeeGradID != employeeGrad.ID)
                                            {

                                                // Проверим, есть ли запись об изменении грейда в истории и если нет, то добавим
                                                if (employee.EmployeeGradID.HasValue == true)
                                                {
                                                    if (_employeeGradAssignmentService.Get(x => x.Where(e => (e.EmployeeID == employee.ID)).ToList()).Count == 0)
                                                    {
                                                        EmployeeGradAssignment employeeGradAssignmentPrev = new EmployeeGradAssignment();

                                                        employeeGradAssignmentPrev.EmployeeID = employee.ID;
                                                        employeeGradAssignmentPrev.EmployeeGradID = employee.EmployeeGradID;
                                                        employeeGradAssignmentPrev.Comments = "Изменено в карточке сотрудника пользователем: " + User.Identity.Name;

                                                        _employeeGradAssignmentService.Add(employeeGradAssignmentPrev);
                                                    }
                                                }

                                                EmployeeGradAssignment employeeGradAssignment = null;

                                                employeeGradAssignment = _employeeGradAssignmentService.Get(x => x.Where(e => (e.EmployeeID == employee.ID && e.ExternalSourceElementID == employeePayrollRecord.SourceElementID)).ToList()).FirstOrDefault();

                                                if (employeeGradAssignment == null)
                                                {
                                                    employeeGradAssignment = new EmployeeGradAssignment();

                                                    employeeGradAssignment.EmployeeID = employee.ID;
                                                    employeeGradAssignment.EmployeeGradID = employeeGrad.ID;
                                                    employeeGradAssignment.BeginDate = employeePayrollRecord.PayrollChangeDate.Value;
                                                    employeeGradAssignment.Comments = "Изменено в карточке сотрудника пользователем: " + User.Identity.Name;
                                                    employeeGradAssignment.ExternalSourceElementID = employeePayrollRecord.SourceElementID;

                                                    _employeeGradAssignmentService.Add(employeeGradAssignment);
                                                }
                                                else
                                                {
                                                    employeeGradAssignment.EmployeeGradID = employeeGrad.ID;
                                                    employeeGradAssignment.BeginDate = employeePayrollRecord.PayrollChangeDate.Value;
                                                    employeeGradAssignment.Comments = "Изменено в карточке сотрудника пользователем: " + User.Identity.Name;

                                                    _employeeGradAssignmentService.Update(employeeGradAssignment);
                                                }

                                                employee.EmployeeGradID = employeeGrad.ID;

                                                _employeeService.Update(employee);
                                            }
                                        }
                                    }
                                    else if (mode == EmployeePayrollRecordActionFormMode.Reject)
                                    {
                                        EmployeeGradAssignment employeeGradAssignment = _employeeGradAssignmentService.Get(x => x.Where(e => (e.EmployeeID == employee.ID && e.ExternalSourceElementID == employeePayrollRecord.SourceElementID)).ToList()).FirstOrDefault();

                                        if (employeeGradAssignment != null)
                                        {
                                            EmployeeGradAssignment employeeGradAssignmentPrev = _employeeGradAssignmentService.Get(x => x.Where(e => (e.EmployeeID == employee.ID
                                                && e.ID != employeeGradAssignment.ID
                                                && e.BeginDate <= employeeGradAssignment.BeginDate)).OrderByDescending(e => e.BeginDate).ToList()).FirstOrDefault();

                                            if (employeeGradAssignmentPrev != null)
                                            {
                                                employee.EmployeeGradID = employeeGradAssignmentPrev.EmployeeGradID;

                                                _employeeService.Update(employee);

                                                _employeeGradAssignmentService.Delete(employeeGradAssignment.ID);
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    return Json(new { Error = "Произошла ошибка при записи данных КОТ. Пожалуйста, попробуйте выполнить операцию позже или обратитесь к администратору системы." });
                                }
                            }

                            if (SetBitrixTaskParameter((EmployeePayrollRecordActionFormMode)actionModeForm.Value, recType, bitrixHelper, bitrixReqPayrollChange))
                                return Json(new { Status = "Updated" });
                            else
                                return Json(new { Error = "ErrorUpdate" });
                        }
                        catch (Exception e)
                        {
                            return Json(new { Error = "ErrorUpdate: " + "<br>" + e.Message + "<br>" + e.StackTrace + "<br>" + e.TargetSite.ToString() });
                        }
                    }
                }

            }
            return Json(new { Error = "ErrorData" });
        }

        [OperationActionFilter(nameof(Operation.OOAccessAllow))]
        public ActionResult History(int? employeeID, int? bitrixReqPayrollChangeID, int? recordType) // передавать employee.ID
        {
            if (!employeeID.HasValue)
                return StatusCode(StatusCodes.Status400BadRequest);

            ApplicationUser user = _applicationUserService.GetUser();
            Employee employee = _employeeService.GetById(employeeID.Value);

            if (employee != null)
            {
                var currentRecordType = (EmployeePayrollRecordType)recordType;
                if (CheckEmployeePayrollChangeAccessForUser(employee, user, currentRecordType) == false)
                {
                    return StatusCode(StatusCodes.Status403Forbidden);
                }
            }
            else
            {
                return StatusCode(StatusCodes.Status400BadRequest);
            }

            DataTable employeePayrollSheetDataTable = _financeService.GetEmployeePayrollSheetDataTableFromOO(user, false);
            List<EmployeePayrollRecord> records = _financeService.GetEmployeePayrollRecordsFromDataTable(employeePayrollSheetDataTable, employee).OrderBy(r => r.PayrollChangeDate).ToList();

            DataTable employeePayrollSheetDataTableTmp = _financeService.GetEmployeePayrollSheetDataTableFromOO(user, true);
            List<EmployeePayrollRecord> recordsTmp = _financeService.GetEmployeePayrollRecordsFromDataTable(employeePayrollSheetDataTableTmp, employee).OrderBy(r => r.PayrollChangeDate).ToList();

            if (bitrixReqPayrollChangeID.HasValue)
                recordsTmp = recordsTmp.Where(rec => rec.SourceElementID != bitrixReqPayrollChangeID.Value.ToString()).ToList();

            var emplRecords = records.Where(r => r.RecordType == EmployeePayrollRecordType.None).ToList(); // получаем только старые записи
            var gradAssignments = _employeeGradAssignmentService.Get(grads => grads.Where(ga => ga.EmployeeID == employee.ID).ToList());
            var additionalGradAssignments = new List<EmployeeGradAssignment>();
            foreach (var emplRecord in emplRecords)
            {
                emplRecord.RecordType = EmployeePayrollRecordType.PayrollChange;
                if (emplRecord.PayrollChangeDate.HasValue)
                {
                    var gradAssignment = gradAssignments.FirstOrDefault(ga => ga.
                    BeginDate.Date == emplRecord.PayrollChangeDate.Value.Date);
                    if (gradAssignment != null)
                    {
                        int grad;
                        if (int.TryParse(gradAssignment.EmployeeGrad.ShortName, out grad))
                            emplRecord.EmployeeGrad = grad;
                        else
                            emplRecord.EmployeeGrad = null;

                        if (emplRecord.EmployeeGrad == 0)
                            emplRecord.EmployeeGrad = null;

                        additionalGradAssignments.Add(gradAssignment);
                    }
                }
            }

            foreach (var gradAssigment in gradAssignments) // добавляем остальные грейды для которых не были найдены записи из КОТ
            {
                if (additionalGradAssignments.Count == 0 || additionalGradAssignments.FirstOrDefault(aga => aga.ID == gradAssigment.ID) == null)
                {
                    int grad = 0;
                    int.TryParse(gradAssigment.EmployeeGrad.ShortName, out grad);

                    DateTime gradAssigmentBeginDate = DateTime.MinValue;

                    if (gradAssigment.BeginDate != DateTime.MinValue)
                        gradAssigmentBeginDate = gradAssigment.BeginDate;
                    else if (employee.EnrollmentDate != null)
                        gradAssigmentBeginDate = employee.EnrollmentDate.Value;

                    emplRecords.Add(new EmployeePayrollRecord()
                    {
                        RecordType = EmployeePayrollRecordType.PayrollChange,
                        EmployeeGrad = grad,
                        PayrollChangeDate = gradAssigmentBeginDate
                    });
                }
            }

            // добавляем из КОТ'
            emplRecords.AddRange(recordsTmp
                .Where(rec => rec.RecordType == EmployeePayrollRecordType.PayrollChangeHD
                    || rec.RecordType == EmployeePayrollRecordType.PayrollChangeHDCurator
                    || rec.RecordType == EmployeePayrollRecordType.PayrollChangeCEO));

            Department department = employee.Department;
            if (department != null)
            {
                while (department.ParentDepartment != null && !department.IsFinancialCentre)
                    department = department.ParentDepartment;
            }

            // в обратном порядке по Дате решения
            ViewBag.Department = department;
            var emplGroupedList = emplRecords
                .OrderBy(rec => rec.RecordSortKey)
                .GroupBy(rec => rec.RecordGroupKey)
                .OrderByDescending(g => g.Key);

            var emplViewModel = new List<GroupDictInfoList<string, EmployeePayrollRecord>>();
            foreach (var item in emplGroupedList)
            {

                var emplCEO = item.FirstOrDefault(rec => rec.RecordType == EmployeePayrollRecordType.PayrollChangeCEO);
                var uRegNum = string.IsNullOrEmpty(item.ElementAt(0).URegNum) ? "-" : item.ElementAt(0).URegNum;
                string dateDecision;
                if (emplCEO != null)
                    dateDecision = emplCEO.Created.HasValue ? emplCEO.Created.Value.ToString("dd.MM.yyyy") : "-";
                else if (item.Count() == 1 && item.First().RecordType == EmployeePayrollRecordType.PayrollChange)
                {
                    dateDecision = item.First().PayrollChangeDate.HasValue ?
                    item.First().PayrollChangeDate.Value.ToString("dd.MM.yyyy") : "-";
                }
                else
                {
                    var emplCur = item.FirstOrDefault(rec =>
                    rec.RecordType == EmployeePayrollRecordType.PayrollChangeHDCurator
                    && rec.RecordResult == EmployeePayrollRecordResult.Rejected);

                    if (emplCur != null)
                        dateDecision = emplCur.Created.HasValue ? emplCur.Created.Value.ToString("dd.MM.yyyy") : "-";
                    else
                    {
                        emplCur = item.FirstOrDefault(rec =>
                        rec.RecordType == EmployeePayrollRecordType.PayrollChangeHD
                        && rec.RecordResult == EmployeePayrollRecordResult.Rejected);
                        if (emplCur != null)
                            dateDecision = emplCur.Created.HasValue ? emplCur.Created.Value.ToString("dd.MM.yyyy") : "-";
                        else
                            dateDecision = "-";
                    }
                }

                var groupInfoList = new GroupDictInfoList<string, EmployeePayrollRecord>(new Dictionary<string, string>() {
                    { "URegNum", uRegNum },
                    { "DateDecision", dateDecision },
                });
                groupInfoList.AddRange(item);
                emplViewModel.Add(groupInfoList);
            }
            return View(emplViewModel);
        }

        private Employee GetEmployeeByBitrixUser(BitrixUser bitrixUser)
        {
            return _employeeService.Get(x => x
                    .Where(e => e.Email != null && e.Email.ToLower().Equals(bitrixUser.EMAIL.Trim(), StringComparison.OrdinalIgnoreCase)).ToList())
                .FirstOrDefault();
        }

        private bool CheckEmployeePayrollChangeAccessForUser(Employee employee, ApplicationUser user, EmployeePayrollRecordType recordType)
        {
            if (recordType != EmployeePayrollRecordType.PayrollChangeCEO)
            {
                if (_applicationUserService.HasAccess(Operation.OOAccessFullPayrollChangeAccess) || _applicationUserService.HasAccess(Operation.OOAccessFullReadPayrollChangeAccess))
                    return true;
                else if (_applicationUserService.HasAccess(Operation.OOAccessSubEmplPayrollChangeAccess))
                    return (_employeeService.GetAllManagedEmployees(_applicationUserService.GetUser().ManagedDepartments)
                        .Where(e => e.ID == employee.ID)
                        .FirstOrDefault() != null);
                else
                    return false;
            }
            else if (recordType == EmployeePayrollRecordType.PayrollChangeCEO && _applicationUserService.HasAccess(Operation.OOAccessFullPayrollAccess))
                return true;
            else
                return false;
        }

        private List<EmployeePayrollRecord> GetEmployeePayrollRecords(List<EmployeePayrollRecord> records, string bitrixReqPayrollChangeID)
        {
            var lst = new List<EmployeePayrollRecord>();
            for (int i = 0; i < records.Count; i++)
            {
                var r = records[i];
                if (r.SourceElementID != null && r.SourceElementID.Trim().Equals(bitrixReqPayrollChangeID.Trim(), StringComparison.OrdinalIgnoreCase))
                    lst.Add(r);

            }
            return lst;
        }

        private bool IsTaskCompleted(EmployeePayrollRecordType recordType, BitrixReqPayrollChange bitrixReqPayrollChange, BitrixHelper bitrixHelper)
        {
            string completeMark = "y";
            Func<string, bool> checkValue = s => string.IsNullOrEmpty(s) ? false : s.Trim().Equals(completeMark, StringComparison.OrdinalIgnoreCase);

            switch (recordType)
            {
                case EmployeePayrollRecordType.PayrollChangeHD:
                    return bitrixReqPayrollChange.DEPARTMENT_HEAD_APPROVED_BP_TASK_COMPLETED != null ? checkValue(bitrixReqPayrollChange.DEPARTMENT_HEAD_APPROVED_BP_TASK_COMPLETED.FirstOrDefault().Value) : false;
                case EmployeePayrollRecordType.PayrollChangeHDCurator:
                    return bitrixReqPayrollChange.CURATOR_FRC_APPROVED_BP_TASK_COMPLETED != null ? checkValue(bitrixReqPayrollChange.CURATOR_FRC_APPROVED_BP_TASK_COMPLETED.FirstOrDefault().Value) : false;
                case EmployeePayrollRecordType.PayrollChangeHR:
                    return bitrixReqPayrollChange.HR_HEAD_APPROVED_BP_TASK_COMPLETED != null ? checkValue(bitrixReqPayrollChange.HR_HEAD_APPROVED_BP_TASK_COMPLETED.FirstOrDefault().Value) : false;
                case EmployeePayrollRecordType.PayrollChangeFin:
                    return bitrixReqPayrollChange.FINANCE_AND_ACCOUNTING_APPROVED_BP_TASK_COMPLETED != null ? checkValue(bitrixReqPayrollChange.FINANCE_AND_ACCOUNTING_APPROVED_BP_TASK_COMPLETED.FirstOrDefault().Value) : false;
                case EmployeePayrollRecordType.PayrollChangeCEO:
                    return bitrixReqPayrollChange.CEO_APPROVED_BP_TASK_COMPLETED != null ? checkValue(bitrixReqPayrollChange.CEO_APPROVED_BP_TASK_COMPLETED.FirstOrDefault().Value) : false;
                default:
                    return true;
            }
        }

        private string ValidateFieldValues(EmployeePayrollRecord tmpRecord, EmployeePayrollRecordType type, EmployeePayrollRecordActionFormMode? mode = null)
        {
            Func<EmployeePayrollRecordType, EmployeePayrollRecordActionFormMode?, bool> validateField = (t, m) =>
            {
                if (t == EmployeePayrollRecordType.PayrollChangeHD && m.HasValue)
                    return m.Value == EmployeePayrollRecordActionFormMode.InputSuggestion;
                else
                    return true;
            };
            if (!tmpRecord.EmployeeGrad.HasValue && validateField(type, mode)) // грейд
                return "Некорректно заполнено поле - " + tmpRecord.GetDisplayNameForField(r => r.EmployeeGrad, "Предлагаемый грейд");

            if (!tmpRecord.PayrollChangeDate.HasValue && validateField(type, mode)) // Планируемая дата изменений
                return "Некорректно заполнено поле - " + tmpRecord.GetDisplayNameForField(r => r.PayrollChangeDate, "Планируемая дата изменения");
            if (tmpRecord.PayrollChangeDate.HasValue && validateField(type, mode))
            {
                if ((type == EmployeePayrollRecordType.PayrollChangeHD || type == EmployeePayrollRecordType.PayrollChangeHDCurator) && tmpRecord.PayrollChangeDate.Value.Day > 1)
                    return "Некорректно заполнено поле - " + tmpRecord.GetDisplayNameForField(r => r.PayrollChangeDate, "Планируемая дата изменения");
            }

            if (!tmpRecord.PayrollValue.HasValue && validateField(type, mode)) // Ежемесячная выплата
                return "Некорректно заполнено поле - " + tmpRecord.GetDisplayNameForField(r => r.PayrollValue);

            if (!tmpRecord.PayrollQuarterValue.HasValue && tmpRecord.Employee.Department.UsePayrollQuarterValue && validateField(type, mode)) // Ежеквартальная выплата
                return "Не заполнено обязательное поле - " + tmpRecord.GetDisplayNameForField(r => r.PayrollQuarterValue);
            if (!tmpRecord.PayrollHalfYearValue.HasValue && tmpRecord.Employee.Department.UsePayrollHalfYearValue && validateField(type, mode)) // Полугодовая выплата
                return "Не заполнено обязательное поле - " + tmpRecord.GetDisplayNameForField(r => r.PayrollQuarterValue);

            if (string.IsNullOrEmpty(tmpRecord.UserComment?.Trim())) // Комментарий
            {
                if (mode.HasValue && type == EmployeePayrollRecordType.PayrollChangeCEO)
                {
                    if (mode.Value == EmployeePayrollRecordActionFormMode.Reject)
                        return "Не заполнено обязательное поле - " + tmpRecord.GetDisplayNameForField(r => r.UserComment);
                }
                else
                {
                    return "Не заполнено обязательное поле - " + tmpRecord.GetDisplayNameForField(r => r.UserComment);
                }
            }

            return null;
        }

        private bool SetBitrixTaskParameter(EmployeePayrollRecordActionFormMode mode, EmployeePayrollRecordType type, BitrixHelper bitrixHelper, BitrixReqPayrollChange bitrixReqPayrollChange)
        {
            Func<EmployeePayrollRecordActionFormMode, string> modeToString = (m) =>
            {
                if (m == EmployeePayrollRecordActionFormMode.Approve
                || m == EmployeePayrollRecordActionFormMode.InputSuggestion
                || m == EmployeePayrollRecordActionFormMode.ApproveWithSuggestions
                || m == EmployeePayrollRecordActionFormMode.FinalApproveHDCuratorVersion
                || m == EmployeePayrollRecordActionFormMode.FinalApproveHDVersion
                || m == EmployeePayrollRecordActionFormMode.FinalApproveWithSuggestions)
                    return EmployeePayrollRecordActionFormMode.Approve.ToString();
                else if (m == EmployeePayrollRecordActionFormMode.Reject)
                    return EmployeePayrollRecordActionFormMode.Reject.ToString();
                else
                    return string.Empty;
            };
            try
            {
                switch (type)
                {
                    case EmployeePayrollRecordType.PayrollChangeHD:
                        bitrixHelper.UpdateBitrixListElement(bitrixReqPayrollChange, nameof(bitrixReqPayrollChange.DEPARTMENT_HEAD_APPROVED), modeToString(mode));
                        break;
                    case EmployeePayrollRecordType.PayrollChangeHDCurator:
                        bitrixHelper.UpdateBitrixListElement(bitrixReqPayrollChange, nameof(bitrixReqPayrollChange.CURATOR_FRC_APPROVED), modeToString(mode));
                        break;
                    case EmployeePayrollRecordType.PayrollChangeHR:
                        bitrixHelper.UpdateBitrixListElement(bitrixReqPayrollChange, nameof(bitrixReqPayrollChange.HR_HEAD_APPROVED), "Y");
                        break;
                    case EmployeePayrollRecordType.PayrollChangeFin:
                        bitrixHelper.UpdateBitrixListElement(bitrixReqPayrollChange, nameof(bitrixReqPayrollChange.FINANCE_AND_ACCOUNTING_APPROVED), "Y");
                        break;
                    case EmployeePayrollRecordType.PayrollChangeCEO:
                        bitrixHelper.UpdateBitrixListElement(bitrixReqPayrollChange, nameof(bitrixReqPayrollChange.CEO_APPROVED), modeToString(mode));
                        break;
                    default:
                        return true;
                }
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        [OperationActionFilter(nameof(Operation.OOAccessAllow))]
        [HttpGet]
        public ActionResult ExportToExcel(int? employeeID, int? bitrixReqPayrollChangeID, int? recordType)
        {
            ApplicationUser user = _applicationUserService.GetUser();
            Employee employee = _employeeService.GetById(employeeID.Value);
            Byte[] binData;
            if (employee != null)
            {
                var currentRecordType = (EmployeePayrollRecordType)recordType;
                if (!CheckEmployeePayrollChangeAccessForUser(employee, user, currentRecordType))
                {
                    return StatusCode(StatusCodes.Status403Forbidden);
                }
            }
            else
            {
                return StatusCode(StatusCodes.Status400BadRequest);
            }

            if (bitrixReqPayrollChangeID.HasValue && recordType.HasValue)
            {
                DataTable employeePayrollSheetDataTable = _financeService.GetEmployeePayrollSheetDataTableFromOO(user, false);
                EmployeePayrollRecord record = _financeService.GetEmployeePayrollRecordsFromDataTable(employeePayrollSheetDataTable, employee)
                    .OrderByDescending(r => r.PayrollChangeDate)
                    .Where(r => r.PayrollChangeDate.Value.Date <= DateTime.Today)
                    .FirstOrDefault();

                if (record != null
                    && (record.EmployeeGrad == null || record.EmployeeGrad == 0)
                    && employee.EmployeeGrad != null
                    && String.IsNullOrEmpty(employee.EmployeeGrad.ShortName) == false)
                {
                    try
                    {
                        int grad;
                        if (int.TryParse(employee.EmployeeGrad.ShortName, out grad))
                            record.EmployeeGrad = grad;
                        else
                            record.EmployeeGrad = 0;
                    }
                    catch (Exception)
                    {
                        record.EmployeeGrad = 0;
                    }
                }

                DataTable employeePayrollSheetDataTableTmp = _financeService.GetEmployeePayrollSheetDataTableFromOO(user, true);
                var tmpEmployeePayrollRecordList = _financeService.GetEmployeePayrollRecordsFromDataTable(employeePayrollSheetDataTableTmp, employee).ToList();

                EmployeePayrollRecord tmpRecord = null;
                List<EmployeePayrollRecord> tmpRecordList = null;
                int rowId = -1;
                tmpRecordList = GetEmployeePayrollRecords(tmpEmployeePayrollRecordList, bitrixReqPayrollChangeID.Value.ToString());
                var curRecordType = (EmployeePayrollRecordType)recordType.Value;
                EmployeePayrollRecordType prevRecordType = curRecordType == EmployeePayrollRecordType.PayrollChangeHDCurator ? EmployeePayrollRecordType.PayrollChangeHD : EmployeePayrollRecordType.PayrollChangeHDCurator;
                if (tmpRecordList.Count > 0)
                {
                    // tmpRecord = tmpRecordList.LastOrDefault(r => r.RecordResult != EmployeePayrollRecordResult.Rejected);
                    tmpRecord = tmpRecordList.FirstOrDefault(r => r.RecordResult != EmployeePayrollRecordResult.Rejected && r.RecordType == prevRecordType);
                    rowId = tmpRecord.ID - 1;
                }
                else
                    tmpRecord = null;

                string comment = tmpRecordList.FirstOrDefault(r => r.RecordType == EmployeePayrollRecordType.PayrollChangeHD)?.UserComment;

                Department department = employee.Department;
                if (department != null)
                {
                    while (department.ParentDepartment != null && !department.IsFinancialCentre)
                        department = department.ParentDepartment;
                }

                if (record != null)
                {
                    record.Employee = employee;
                    record.Employee.Department = department;
                }

                binData = _financeService.GetEmployeePayrollChangeReport(record, tmpRecord,
                    (double)GetYearPayrollRatioByGrad(record?.EmployeeGrad, tmpRecord?.PayrollChangeDate),
                    (double)GetYearPayrollRatioByGrad(tmpRecord?.EmployeeGrad, tmpRecord?.PayrollChangeDate), comment);
                //if (tmpRecord != null)
                //{
                //    binData = _financeService.CreateWorksheetPartAndImportDataTable(tmpRecord);
                //}
                //else
                //    return StatusCode(StatusCodes.Status400BadRequest);
            }
            else
            {
                return StatusCode(StatusCodes.Status400BadRequest);
            }

            return File(binData, ExcelHelper.ExcelContentType, "ReportEmployeePayrollChange" + DateTime.Now.ToString("ddMMyyHHmmss") + ".xlsx");
        }

        [OperationActionFilter(nameof(Operation.OOAccessAllow))]
        [HttpGet]
        public ActionResult ExportDepartmentPayrollChangeToExcel(int? employeeID, int? bitrixReqPayrollChangeID, int? recordType)
        {
            ApplicationUser user = _applicationUserService.GetUser();
            Employee employee = _employeeService.GetById(employeeID.Value);


            if (employee != null) // по факту проверку нужно делать в списке для каждого сотрудника
            {
                var currentRecordType = (EmployeePayrollRecordType)recordType;
                if (CheckEmployeePayrollChangeAccessForUser(employee, user, currentRecordType) == false)
                {
                    return StatusCode(StatusCodes.Status403Forbidden);
                }
            }
            else
            {
                return StatusCode(StatusCodes.Status400BadRequest);
            }

            Department frcDepartment = employee.Department;
            if (frcDepartment != null)
            {
                while (frcDepartment.ParentDepartment != null && !frcDepartment.IsFinancialCentre)
                    frcDepartment = frcDepartment.ParentDepartment;
            }
            else
                return StatusCode(StatusCodes.Status400BadRequest);

            // var childDept = _departmentService.GetChildDepartments(frcDepartment.ID, false);


            DataTable employeePayrollSheetDataTableTmp = _financeService.GetEmployeePayrollSheetDataTableFromOO(user, true);
            var tmpEmployeePayrollRecordList = _financeService.GetEmployeePayrollRecordsFromDataTable(employeePayrollSheetDataTableTmp, employee).ToList();

            EmployeePayrollRecord tmpRecord = null;
            List<EmployeePayrollRecord> tmpRecordList = null;
            int rowId = -1;
            tmpRecordList = GetEmployeePayrollRecords(tmpEmployeePayrollRecordList, bitrixReqPayrollChangeID.Value.ToString());
            var curRecordType = (EmployeePayrollRecordType)recordType.Value;
            EmployeePayrollRecordType prevRecordType = curRecordType == EmployeePayrollRecordType.PayrollChangeHDCurator ? EmployeePayrollRecordType.PayrollChangeHD : EmployeePayrollRecordType.PayrollChangeHDCurator;
            if (tmpRecordList.Count > 0)
            {
                tmpRecord = tmpRecordList.FirstOrDefault(r => r.RecordResult != EmployeePayrollRecordResult.Rejected && r.RecordType == prevRecordType);
                rowId = tmpRecord.ID - 1;
            }

            if (tmpRecord == null)
                return StatusCode(StatusCodes.Status400BadRequest);

            var employees = _employeeService.GetEmployeesInDepartment(frcDepartment.ID, true).ToList();

            int year = tmpRecord.PayrollChangeDate.Value.Year;

            // насыщаем выборку сотрудниками переведёнными из подразделения на данный момент
            // не найденных в текущее время как сотрудник данного подразделения
            var departments = _departmentService.GetChildDepartments(frcDepartment.ID, true);

            var departmentIds = departments.Select(d => d.ID);
            var employeeIds = employees.Select(e => e.ID);
            //var employeeAssignmentIdList = _employeeDepartmentAssignmentService.Get(edas => edas.Where(eda => 
            //    departmentIds.Any(dId => dId == eda.DepartmentID) && !employeeIds.Any(eId => eId == eda.EmployeeID))
            //    .ToList()).Select(eda => eda.EmployeeID).Distinct();
            // оптимизация ))
            var employeeAssignmentIdList = _employeeDepartmentAssignmentService.Get(edas => edas.ToList())
                .Where(eda => departmentIds.Any(dId => dId == eda.DepartmentID) && !employeeIds.Any(eId => eId == eda.EmployeeID))
                .Select(eda => eda.EmployeeID).Distinct();

            //var employeeAssignmentList = _employeeDepartmentAssignmentService.Get(edas => edas.Where(eda => 
            //    employeeAssignmentIdList.Any(_id => _id == eda.EmployeeID)).ToList())
            //    .OrderBy(eda => eda.BeginDate).GroupBy(eda => eda.EmployeeID);

            var employeeAssignmentList = _employeeDepartmentAssignmentService.Get(edas => edas.ToList())
                .Where(eda => employeeAssignmentIdList.Any(_id => _id == eda.EmployeeID))
                .OrderBy(eda => eda.BeginDate).GroupBy(eda => eda.EmployeeID);


            var addtionalEmployeeIdList = new List<int>();
            foreach (var emplAssing in employeeAssignmentList)
            {
                var emplList = emplAssing.OrderBy(ea => ea.BeginDate);
                if (emplList.Count() > 1)
                {
                    for (int i = 1; i < emplList.Count(); i++)
                    {
                        var datePrev = emplList.ElementAt(i - 1).BeginDate;
                        var dateNext = emplList.ElementAt(i).BeginDate;
                        if (datePrev <= tmpRecord.PayrollChangeDate.Value && tmpRecord.PayrollChangeDate.Value <= dateNext)
                        {
                            addtionalEmployeeIdList.Add(emplList.ElementAt(i).EmployeeID.Value);
                            break;
                        }
                    }
                }
            }

            var addtionalEmployeeList = _employeeService.Get(es => es.Where(e => employeeAssignmentIdList.Any(eaid => eaid == e.ID)).ToList());
            employees.AddRange(addtionalEmployeeList);

            // var employeeAssignmentList = _employeeDepartmentAssignmentService.Get(edas => edas.Where(eda => eda.BeginDate.Year == year).ToList());
            //var employeeAssignmentIdList = employeeAssignmentList.Where(eda => departments.Any(d => d.ID == eda.DepartmentID) && !employees.Any(e => e.ID == eda.EmployeeID))
            //    .Select(eda => eda.ID).Distinct();

            //var employeeAssignmentList = _employeeDepartmentAssignmentService.Get(edas => edas.Where(eda => eda.BeginDate.Year == year && 
            //departments.Any(d => d.ID == eda.DepartmentID)
            //&& !employees.Any(e => e.ID == eda.EmployeeID)).ToList());

            // var addtionalEmployeeList = _employeeService.Get(es => es.Where(e => employeeAssignmentIdList.Any(eaid => eaid == e.ID)).ToList());
            // employees.AddRange(addtionalEmployeeList);

            DataTable employeePayrollSheetDataTable = _financeService.GetEmployeePayrollSheetDataTableFromOO(user, false);
            var employeeReportPayrollRecordList = _financeService.GetFullListEmployeePayrollRecordsFromDataTable(employeePayrollSheetDataTable, employees)
                .OrderByDescending(r => r.PayrollChangeDate)
                .Where(r => r.PayrollChangeDate.Value.Date <= DateTime.Today)
                .Where(record => record.RecordType != EmployeePayrollRecordType.PayrollChangeCEO);

            var tmpEmployeeReportPayrollRecordList = _financeService.GetFullListEmployeePayrollRecordsFromDataTable(employeePayrollSheetDataTableTmp, employees);
            var emplGroupedList = tmpEmployeeReportPayrollRecordList.Where(r => r.PayrollChangeDate.Value.Year == year)
                .OrderBy(rec => rec.RecordSortKey)
                .GroupBy(rec => rec.RecordGroupKey)
                .OrderBy(g => g.Key);

            var emplViewModel = new List<EmployeePayrollRecord.EmployeeReqPayrollChange>();
            EmployeePayrollRecord prevRecord = null;
            foreach (var item in emplGroupedList)
            {
                if (item.Any(rec => rec.RecordResult == EmployeePayrollRecordResult.Rejected))
                    continue;
                EmployeePayrollRecord record = item.FirstOrDefault(rec => rec.RecordType == EmployeePayrollRecordType.PayrollChangeCEO);
                // EmployeePayrollRecord record = item.FirstOrDefault(rec => rec.RecordType == EmployeePayrollRecordType.PayrollChangeHDCurator);
                if (record == null)
                    record = item.FirstOrDefault(rec => rec.RecordType == EmployeePayrollRecordType.PayrollChangeHDCurator);
                if (record == null)
                    record = item.FirstOrDefault(rec => rec.RecordType == EmployeePayrollRecordType.PayrollChangeHD);
                if (record != null)
                {
                    var status = item.FirstOrDefault(rec => rec.RecordType == EmployeePayrollRecordType.PayrollChangeCEO) != null ? EmployeePayrollRecord.ReqPayrollChangeStatus.Approved : EmployeePayrollRecord.ReqPayrollChangeStatus.OnAgreement;
                    double payrolChange;
                    if (prevRecord == null || record.EmployeeID != prevRecord.EmployeeID)
                    {
                        var mainEmplRecord = employeeReportPayrollRecordList.FirstOrDefault(r => r.Employee.ID == record.EmployeeID);
                        prevRecord = record;
                        payrolChange = mainEmplRecord != null ? (record.PayrollValue - mainEmplRecord.PayrollValue) ?? 0 : 0;
                    }
                    else
                    {
                        payrolChange = (record.PayrollValue - prevRecord.PayrollValue) ?? 0;
                        prevRecord = record;
                    }
                    Department rootDept = record.Employee.Department;
                    while (rootDept.ParentDepartment != null && !rootDept.IsFinancialCentre)
                        rootDept = frcDepartment.ParentDepartment;
                    emplViewModel.Add(new EmployeePayrollRecord.EmployeeReqPayrollChange(record, rootDept, payrolChange, status));
                }
            }

            var limits = _budgetLimitService.Get(ls => ls.Where(l => l.DepartmentID == frcDepartment.ID && l.Year == year && l.CostSubItem.IsEmployeePayrollCosts).ToList());
            var binData = _financeService.GetDepartmentPayrollChangeReport(limits, employeeReportPayrollRecordList, emplViewModel, frcDepartment /*?.ShortTitle*/, year);

            return File(binData, ExcelHelper.ExcelContentType, "ExportDepartmentPayrollChangeToExcel" + DateTime.Now.ToString("ddMMyyHHmmss") + ".xlsx");
        }

        public decimal GetYearPayrollRatioByGrad(int? grad, DateTime? atDate)
        {
            if (grad.HasValue && atDate.HasValue)
                return _employeeGradParamService.Get(egps => egps
                    .Where(egp => egp.EmployeeGrad.ShortName == grad.Value.ToString()
                                  && egp.BeginDate <= atDate.Value).ToList()).OrderByDescending(egp => egp.BeginDate).FirstOrDefault()?.EmployeeYearPayrollRatio ?? 0;
            else
                return 0;
        }

        private void SetViewBag(EmployeePayrollRecord record, bool isForceEdit)
        {
            if (record != null)
            {
                if (record.RecordResult == EmployeePayrollRecordResult.Rejected)
                {
                    ViewBag.NewGrad = string.Empty;
                    ViewBag.NewPayrollValue = string.Empty;
                    ViewBag.NewPayrollQuarterValue = string.Empty;
                    ViewBag.NewPayrollHalfYearValue = string.Empty;
                    ViewBag.NewPayrollYearValue = string.Empty;
                    ViewBag.NewDateChange = string.Empty;
                }
                else
                {
                    ViewBag.NewGrad = record.EmployeeGrad;
                    ViewBag.NewPayrollValue = record.PayrollValue;
                    ViewBag.NewPayrollQuarterValue = record.PayrollQuarterValue;
                    ViewBag.NewPayrollHalfYearValue = record.PayrollHalfYearValue;
                    ViewBag.NewPayrollYearValue = record.PayrollYearValue;
                    ViewBag.NewDateChange = record.PayrollChangeDate.Value.ToString("dd.MM.yyyy");
                }
                if (isForceEdit)
                {
                    ViewBag.UserComment = record.UserComment;
                    ViewBag.UserSpecialComment = record.UserSpecialComment;
                }
                else
                {
                    ViewBag.UserComment = string.Empty;
                    ViewBag.UserSpecialComment = string.Empty;
                }
            }
            else
            {
                ViewBag.NewGrad = string.Empty;
                ViewBag.NewPayrollValue = string.Empty;
                ViewBag.NewPayrollQuarterValue = string.Empty;
                ViewBag.NewPayrollHalfYearValue = string.Empty;
                ViewBag.NewPayrollYearValue = string.Empty;
                ViewBag.NewDateChange = string.Empty;
                ViewBag.UserComment = string.Empty;
                ViewBag.UserSpecialComment = string.Empty;
            }
        }

    }
}
