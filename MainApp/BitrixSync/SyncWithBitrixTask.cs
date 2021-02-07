using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Core.BL.Interfaces;
using Core.Common;
using Core.Config;
using Core.Extensions;
using Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;






namespace MainApp.BitrixSync
{
    public class SyncWithBitrixTask : LongRunningTaskBase
    {
        protected string currentProjectShortName = "";
        private List<BitrixUser> bitrixUserList = null;
        BitrixHelper bitrixHelper = null;
        IBudgetLimitService _budgetLimitService;
        private readonly IOptions<BitrixConfig> _bitrixOptions;
        private readonly IEmployeeService _employeeService;
        private readonly IProjectService _projectService;
        private readonly IDepartmentService _departmentService;
        private readonly ICostSubItemService _costSubItemService;
        private readonly IExpensesRecordService _expensesRecordService;
        private readonly IProjectRoleService _projectRoleService;

        public SyncWithBitrixTask(IBudgetLimitService budgetLimitService, IOptions<BitrixConfig> bitrixOptions,
        IEmployeeService employeeService,
            IProjectService projectService,
        IDepartmentService departmentService,
            ICostSubItemService costSubItemService,
        IExpensesRecordService expensesRecordService, IProjectRoleService projectRoleService)
            : base()
        {
            _budgetLimitService = budgetLimitService;
            _bitrixOptions = bitrixOptions;
            _employeeService = employeeService;
            _projectService = projectService;
            _departmentService = departmentService;
            _costSubItemService = costSubItemService;
            _expensesRecordService = expensesRecordService;
            _projectRoleService = projectRoleService;
        }



        private void SyncBitrixProjects()
        {
            if (bitrixHelper.IsSyncBitrixProjectsEnabled() == false)
                return;

            var projectList = _projectService.Get(x => x.Include(p => p.EmployeePM).Include(p => p.EmployeeCAM)
                .Include(p => p.Department)
                .Where(p => String.IsNullOrEmpty(p.ShortName) == false
                            && String.IsNullOrEmpty(p.Title) == false
                            && p.ShortName.Contains(".") == true).ToList().OrderBy(p => p.ShortName).ToList());

            SetStatus(1, "Получение списка кодов проектов", true);
            List<BitrixProject> bitrixProjects = bitrixHelper.GetBitrixProjectList();
            if (bitrixProjects == null)
                return;

            SetStatus(1, "Получение списка ЦФО", true);
            List<BitrixFRC> bitrixFRCList = bitrixHelper.GetBitrixFRCList();

            SetStatus(1, "Получение списка типов проекта", true);
            List<BitrixProjectType> bitrixProjectTypeList = bitrixHelper.GetBitrixProjectTypeList();

            SetStatus(1, "Получение списка статусов проекта", true);
            List<BitrixProjectStatus> bitrixProjectStatusList = bitrixHelper.GetBitrixProjectStatusList();


            int j = 0;
            foreach (Project project in projectList)
            {
                int progress = 1 + j * 49 / projectList.Count;
                SetStatus(progress, "Синхронизация проекта: " + project.ShortName);
                currentProjectShortName = project.ShortName;

                bool skipProjectExistsInBitrix = false;

                foreach (BitrixProject bitrixProject in bitrixProjects)
                {
                    Dictionary<string, string> skipr_id = bitrixProject.SKIPR_ID;
                    if (skipr_id == null)
                        continue;

                    if (project.ID.ToString() != skipr_id.First().Value)
                        continue;

                    skipProjectExistsInBitrix = true;
                    if (IsNeedUpdateProject(progress, project, bitrixProject,
                        bitrixFRCList,
                        bitrixProjectTypeList,
                        bitrixProjectStatusList))
                    {
                        // Обновим проект
                        bitrixHelper.UpdateBitrixProject(project, bitrixProject.ID);
                    }
                    break;
                }

                if (!skipProjectExistsInBitrix)
                {
                    if (project.EndDate == null
                        || (project.EndDate != null && project.EndDate.HasValue == true && project.EndDate >= DateTime.Today))
                    {
                        // Проекта нет на сервере, добавим
                        bitrixHelper.AddBitrixProject(project);
                    }
                }

                j++;
            }
        }

        private void SyncBitrixFRC()
        {
            var bitrixFRCList = bitrixHelper.GetBitrixFRCList();
            if (bitrixFRCList == null)
                return;

            var bitrixDepatments = bitrixFRCList.Where(x => x.SKIPR_ID != null).ToDictionary(x => x.SKIPR_ID?.First().Value, x => x);
            var departments = _departmentService.Get(x => x.Where(record => record.IsFinancialCentre).ToList());
            int j = 0;
            foreach (var department in departments)
            {
                SetStatus(j * 98 / departments.Count, "Синхронизация ЦФО: " + department.ShortName);
                j++;
                var departmentId = department.ID.ToString();
                if (bitrixDepatments.ContainsKey(departmentId))
                {
                    var bitrixDepatment = bitrixDepatments[departmentId];
                    if (IsNeedUpdateDepartment(department, bitrixDepatment))
                        bitrixHelper.UpdateBitrixFRC(department, bitrixDepatments[departmentId].ID);
                    continue;

                }

                bitrixHelper.AddBitrixFRC(department);
            }
        }

        private void SyncBitrixCSI()
        {
            var bitrixCSIList = bitrixHelper.GetBitrixCSIList();
            if (bitrixCSIList == null)
                return;

            var bitrixCSIs = bitrixCSIList.Where(x => x.SKIPR_ID != null).ToDictionary(x => x.SKIPR_ID?.First().Value, x => x);
            var items = _costSubItemService.Get(x => x.ToList());
            int j = 0;
            foreach (var item in items)
            {
                SetStatus(j * 98 / items.Count, "Синхронизация подстатьи затрат: " + item.ShortName);
                j++;
                var itemId = item.ID.ToString();
                if (bitrixCSIs.ContainsKey(itemId))
                {
                    var bitrixCSI = bitrixCSIs[itemId];
                    if (IsNeedUpdateCSI(item, bitrixCSI))
                        bitrixHelper.UpdateBitrixCSI(item, bitrixCSIs[itemId]);
                    continue;

                }

                bitrixHelper.AddBitrixCSI(item);
            }
        }

        private bool IsNeedUpdateCSI(CostSubItem costSubItem, BitrixCSI bitrixCSI)
        {
            if (costSubItem.FullName != bitrixCSI.NAME)
                return true;

            if (costSubItem.CostItem?.Title != bitrixCSI.CostItemTitle?.First().Value)
                return true;

            if (costSubItem.Title != bitrixCSI.CostSubitemTitle?.First().Value)
                return true;

            if (costSubItem.Description != bitrixCSI.CostSubitemDescription?.First().Value)
                return true;


            return false;
        }

        private bool IsNeedUpdateDepartment(Department department, BitrixFRC bitrixDepartment)
        {
            var departmentName = String.Join(". ", department.ShortName, department.ShortTitle, department.Title);
            if (departmentName != bitrixDepartment.NAME)
                return true;

            if (department.ShortTitle != bitrixDepartment.FinResponsibilityCenterItem?.First().Value)
                return true;

            if (department.Title != bitrixDepartment.DepartmentTitle?.First().Value)
                return true;

            var bitrixDM = bitrixHelper.GetBitrixUserByID(bitrixDepartment.FinResponsibilityCenterHead?.First().Value);
            if (department.DepartmentManager?.Email != bitrixDM?.EMAIL)
                return true;

            return false;
        }

        private bool IsNeedUpdateDepartment(Department department, BitrixDepartment bitrixDepartment, string parentDeptId)
        {
            var departmentName = String.Join(". ", department.ShortName, department.Title);
            if (departmentName != bitrixDepartment.NAME)
                return true;

            if (department.ShortName != bitrixDepartment.SHORT_NAME?.First().Value)
                return true;

            if (department.ShortTitle != bitrixDepartment.SHORT_TITLE?.First().Value)
                return true;

            if (parentDeptId != bitrixDepartment.PARENT_DEPARTMENT?.First().Value)
                return true;

            var bitrixDM = bitrixHelper.GetBitrixUserByEmail(department.DepartmentManager?.Email);
            if (department.DepartmentManager?.Email != bitrixDM?.EMAIL)
                return true;

            if (department.IsFinancialCentre.BoolToYOrN() != bitrixDepartment.IS_FINANCIAL_CENTRE?.First().Value)
                return true;

            if (department.IsAutonomous.BoolToYOrN() != bitrixDepartment.IS_AUTONOMOUS?.First().Value)
                return true;

            return false;
        }


        private bool IsNeedUpdateProject(int progress, Project project, BitrixProject bitrixProject,
            List<BitrixFRC> bitrixFRCList,
            List<BitrixProjectType> bitrixProjectTypeList,
            List<BitrixProjectStatus> bitrixProjectStatusList)
        {
            if (project.Title != null
                && project.Title.Replace("\"", "").Replace("&", "_").Replace("+", "_") != bitrixProject.ProjectTitle?.First().Value)
            {
                SetStatus(progress, "Не совпадает название проекта: " + project.ShortName /*+ ", Б24: " 
                    + ((bitrixProject.ProjectTitle != null) ? (bitrixProject.ProjectTitle?.First().Value) : "")*/, true);
                return true;
            }

            if (project.ShortName != bitrixProject.NAME)
            {
                SetStatus(progress, "Не совпадает код проекта: " + project.ShortName, true);
                return true;
            }

            var bitrixPM = bitrixUserList.Where(x => x.ID == bitrixProject.ProjectPM?.First().Value).FirstOrDefault(); //bitrixHelper.GetBitrixUserByID(bitrixProject.ProjectPM?.First().Value);            
            if (bitrixPM != null && project.EmployeePM?.Email != bitrixPM?.EMAIL)
            {
                SetStatus(progress, "Не совпадает РП проекта: " + project.ShortName, true);
                return true;
            }

            var bitrixCAM = bitrixUserList.Where(x => x.ID == bitrixProject.ProjectCAM?.First().Value).FirstOrDefault(); //bitrixHelper.GetBitrixUserByID(bitrixProject.ProjectCAM?.First().Value);            
            if (bitrixCAM != null && project.EmployeeCAM?.Email != bitrixCAM?.EMAIL)
            {
                SetStatus(progress, "Не совпадает КАМ проекта: " + project.ShortName, true);
                return true;
            }

            var bitrixFRC = bitrixFRCList.Where(x => x.ID == bitrixProject.FinResponsibilityCenter?.First().Value).FirstOrDefault(); //bitrixHelper.GetBitrixFRCById(bitrixProject.FinResponsibilityCenter?.First().Value);
            var projectFRC = bitrixFRCList.Where(x => x.NAME.Split('.').FirstOrDefault() == project.Department?.ShortName).FirstOrDefault(); //bitrixHelper.GetBitrixFRCByDepartmentShortName(project.Department?.ShortName);            
            if (projectFRC?.ID != bitrixFRC?.ID)
            {
                SetStatus(progress, "Не совпадает ЦФО проекта: " + project.ShortName, true);
                return true;
            }

            var bitrixProjectStatus = bitrixProjectStatusList.Where(x => x.ID == bitrixProject.ProjectStatus?.First().Value).FirstOrDefault(); //bitrixHelper.GetBitrixProjectStatusById(bitrixProject.ProjectStatus?.First().Value);
            var projectStatusValue = ((int)project.Status).ToString();
            if (projectStatusValue != bitrixProjectStatus?.Value.FirstOrDefault().Value)
            {
                SetStatus(progress, "Не совпадает статус проекта: " + project.ShortName, true);
                return true;
            }

            var bitrixProjectType = bitrixProjectTypeList.Where(x => x.ID == bitrixProject.ProjectType?.First().Value).FirstOrDefault(); //bitrixHelper.GetBitrixProjectTypeById(bitrixProject.ProjectType?.First().Value);
            var projectTypeShortName = project.ProjectType?.ShortName;
            if (projectTypeShortName != bitrixProjectType?.ShortName.FirstOrDefault().Value)
            {
                SetStatus(progress, "Не совпадает тип проекта: " + project.ShortName, true);
                return true;
            }

            return false;
        }

        private bool IsNeedUpdateProjectRole(ProjectRole projectRole, BitrixProjectRole bitrixProjectRole)
        {
            if (projectRole.Title != bitrixProjectRole.NAME)
                return true;

            var bitrixSHORTNAME = bitrixProjectRole.SHORT_NAME?.FirstOrDefault().Value;
            if (projectRole.ShortName != bitrixSHORTNAME)
                return true;

            return false;
        }

        private void SyncBitrixExpensesRecord()
        {
            if (bitrixHelper.IsSyncBitrixExpensesRecordEnabled() == false)
                return;

            List<BitrixApplicationForPayment> bitrixAFPList = new List<BitrixApplicationForPayment>();

            string bitrixAFPListIDs = bitrixHelper.GetBitrixAFPListIDs();

            Hashtable fieldDisplayValuesForListIDs = new Hashtable();

            if (String.IsNullOrEmpty(bitrixAFPListIDs) == false)
            {
                string[] listIDs = bitrixAFPListIDs.Split(',');

                int k = 1;
                foreach (string listID in listIDs)
                {
                    SetStatus(50, "Экспорт заявок на затраты - шаг " + k.ToString() + " из " + listIDs.Length, true);
                    bitrixAFPList.AddRange(bitrixHelper.GetBitrixAFPList(listID));
                    k++;

                    Dictionary<string, string> rateVAT_DisplayValues = bitrixHelper.GetBitrixListPropertyDisplayValuesForm(listID, "RateVAT");
                    if (rateVAT_DisplayValues != null)
                    {
                        fieldDisplayValuesForListIDs.Add(listID + "_RateVAT", rateVAT_DisplayValues);
                    }

                    Dictionary<string, string> documentState_DisplayValues = bitrixHelper.GetBitrixListPropertyDisplayValuesForm(listID, "DOCUMENT_STATE");
                    if (documentState_DisplayValues != null)
                    {
                        fieldDisplayValuesForListIDs.Add(listID + "_DOCUMENT_STATE", documentState_DisplayValues);
                    }

                    Dictionary<string, string> financialYear_DisplayValues = bitrixHelper.GetBitrixListPropertyDisplayValuesForm(listID, "FinancialYear");
                    if (financialYear_DisplayValues != null)
                    {
                        fieldDisplayValuesForListIDs.Add(listID + "_FinancialYear", financialYear_DisplayValues);
                    }

                    Dictionary<string, string> paymentForm_DisplayValues = bitrixHelper.GetBitrixListPropertyDisplayValuesForm(listID, "PaymentForm");
                    if (paymentForm_DisplayValues != null)
                    {
                        fieldDisplayValuesForListIDs.Add(listID + "_PaymentForm", paymentForm_DisplayValues);
                    }
                }
            }

            SetStatus(50, "Получение списка подстатей затрат", true);
            List<BitrixCSI> bitrixCSIList = bitrixHelper.GetBitrixCSIList();

            SetStatus(50, "Получение списка ЦФО", true);
            List<BitrixFRC> bitrixFRCList = bitrixHelper.GetBitrixFRCList();

            SetStatus(50, "Получение списка кодов проектов", true);
            List<BitrixProject> bitrixProjectList = bitrixHelper.GetBitrixProjectList();

            if (bitrixAFPList != null)
            {
                UpdateExpensesRecords(bitrixAFPList,
                    bitrixCSIList,
                    bitrixFRCList,
                    bitrixProjectList,
                    fieldDisplayValuesForListIDs);
            }

            SetStatus(70, "Экспорт заявок на командировки", true);

            string bitrixRFTListID = bitrixHelper.GetBitrixRFTListID();

            var bitrixRFTList = bitrixHelper.GetBitrixRequestForTrip();

            if (bitrixRFTList != null)
            {
                Dictionary<string, string> documentState_DisplayValues = bitrixHelper.GetBitrixListPropertyDisplayValuesForm(bitrixRFTListID, "DOCUMENT_STATE");
                if (documentState_DisplayValues != null)
                {
                    fieldDisplayValuesForListIDs.Add(bitrixRFTListID + "_DOCUMENT_STATE", documentState_DisplayValues);
                }

                UpdateBitrixRequestForTripRecords(bitrixRFTList,
                    bitrixProjectList,
                    fieldDisplayValuesForListIDs);
            }

            //db.SaveChanges();
        }

        private void UpdateExpensesRecords(List<BitrixApplicationForPayment> bitrixAFPList,
            List<BitrixCSI> bitrixCSIList,
            List<BitrixFRC> bitrixFRCList,
            List<BitrixProject> bitrixProjectList,
            Hashtable fieldDisplayValuesForListIDs)
        {
            int j = 0;
            foreach (var bitrixAFPListItem in bitrixAFPList)
            {
                SetStatus(50 + j * 20 / bitrixAFPList.Count, "Синхронизация затрат старт: ID = " + bitrixAFPListItem.ID + ", " + bitrixAFPListItem.NAME);
                j++;

                DateTime? approveDate = null;
                DateTime? amountReservedApprovedActualDate = null;
                double amount = 0;
                string amountCurrency = "";
                string rateVAT = "";
                double amountWithoutVAT = 0;
                double amountVAT = 0;
                string documentState = "";
                string financialYear = "";

                try
                {
                    string paymentForm = "";
                    if (fieldDisplayValuesForListIDs[bitrixAFPListItem.IBLOCK_ID + "_PaymentForm"] != null)
                    {
                        paymentForm = BitrixHelper.ParseBitrixListPropertyDisplayValueByID((Dictionary<string, string>)fieldDisplayValuesForListIDs[bitrixAFPListItem.IBLOCK_ID + "_PaymentForm"], bitrixAFPListItem.PaymentForm?.FirstOrDefault().Value);
                    }

                    if (bitrixAFPListItem.AmountRuble != null
                        && String.IsNullOrEmpty(bitrixAFPListItem.AmountRuble.FirstOrDefault().Value) == false)
                    {
                        if (bitrixAFPListItem.AmountRuble.FirstOrDefault().Value.Contains("|"))
                        {
                            amount = Convert.ToDouble(bitrixAFPListItem.AmountRuble.FirstOrDefault().Value.Split('|')[0].Replace(".", ","));
                            amountCurrency = bitrixAFPListItem.AmountRuble.FirstOrDefault().Value.Split('|')[1];
                        }
                        else
                        {
                            amount = Convert.ToDouble(bitrixAFPListItem.AmountRuble.FirstOrDefault().Value.Replace("руб.", "").Replace(".", ","));
                            amountCurrency = "RUB";
                        }
                    }
                    else if (bitrixAFPListItem.Amount != null
                       && String.IsNullOrEmpty(bitrixAFPListItem.Amount.FirstOrDefault().Value) == false)
                    {
                        if (bitrixAFPListItem.Amount.FirstOrDefault().Value.Contains("|"))
                        {
                            amount = Convert.ToDouble(bitrixAFPListItem.Amount.FirstOrDefault().Value.Split('|')[0].Replace(".", ","));
                            amountCurrency = bitrixAFPListItem.Amount.FirstOrDefault().Value.Split('|')[1];
                        }
                        else
                        {
                            amount = Convert.ToDouble(bitrixAFPListItem.Amount.FirstOrDefault().Value.Replace("руб.", "").Replace(".", ","));
                            amountCurrency = "RUB";
                        }
                    }

                    if (fieldDisplayValuesForListIDs[bitrixAFPListItem.IBLOCK_ID + "_RateVAT"] != null)
                    {
                        rateVAT = BitrixHelper.ParseBitrixListPropertyDisplayValueByID((Dictionary<string, string>)fieldDisplayValuesForListIDs[bitrixAFPListItem.IBLOCK_ID + "_RateVAT"], bitrixAFPListItem.RateVAT?.FirstOrDefault().Value);
                    }

                    if (String.IsNullOrEmpty(paymentForm) == false && paymentForm.Equals("ОПЕР") == true)
                    {
                        amountVAT = 0;
                        amountWithoutVAT = amount;
                    }
                    else
                    {

                        if (bitrixAFPListItem.AmountVAT != null
                            && String.IsNullOrEmpty(bitrixAFPListItem.AmountVAT.FirstOrDefault().Value) == false)
                        {
                            amountVAT = BitrixHelper.ParseBitrixListElementCurrencyOrNumberFieldValue(bitrixAFPListItem.AmountVAT?.FirstOrDefault().Value);

                            amountWithoutVAT = amount - amountVAT;
                        }
                        else
                        {
                            amountWithoutVAT = ((bitrixAFPListItem.AmountWithoutVAT != null) ? BitrixHelper.ParseBitrixListElementCurrencyOrNumberFieldValue(bitrixAFPListItem.AmountWithoutVAT?.FirstOrDefault().Value) : 0);

                            if (amountWithoutVAT != 0)
                            {
                                amountVAT = amount - amountWithoutVAT;
                            }
                        }
                    }

                    documentState = "";
                    if (fieldDisplayValuesForListIDs[bitrixAFPListItem.IBLOCK_ID + "_DOCUMENT_STATE"] != null)
                    {
                        documentState = BitrixHelper.ParseBitrixListPropertyDisplayValueByID((Dictionary<string, string>)fieldDisplayValuesForListIDs[bitrixAFPListItem.IBLOCK_ID + "_DOCUMENT_STATE"], bitrixAFPListItem.DOCUMENT_STATE?.FirstOrDefault().Value);
                    }

                    financialYear = "";
                    if (fieldDisplayValuesForListIDs[bitrixAFPListItem.IBLOCK_ID + "_FinancialYear"] != null)
                    {
                        financialYear = BitrixHelper.ParseBitrixListPropertyDisplayValueByID((Dictionary<string, string>)fieldDisplayValuesForListIDs[bitrixAFPListItem.IBLOCK_ID + "_FinancialYear"], bitrixAFPListItem.FinancialYear?.FirstOrDefault().Value);
                    }

                    approveDate = (bitrixAFPListItem.APPROVE_DATE != null) ? Convert.ToDateTime(bitrixAFPListItem.APPROVE_DATE.FirstOrDefault().Value) : (DateTime?)null;
                }
                catch (Exception)
                {
                    amount = 0;
                    amountCurrency = "";
                    documentState = "";
                }

                //var payment = ParceAFPAmount(bitrixAFPListItem.OverallPaymentAmount?.FirstOrDefault().Value);
                double actualPaymentAmount = 0;
                double actualPaymentAmountWithoutVAT = 0;
                double actualPaymentVAT = 0;
                string actualPaymentAmountCurrency = "";
                DateTime? paymentCompletedActualDate = null;

                try
                {
                    if (bitrixAFPListItem.OverallPaymentAmount != null
                        && String.IsNullOrEmpty(bitrixAFPListItem.OverallPaymentAmount.FirstOrDefault().Value) == false)
                    {
                        if (bitrixAFPListItem.OverallPaymentAmount.FirstOrDefault().Value.Contains("|"))
                        {
                            actualPaymentAmount = Convert.ToDouble(bitrixAFPListItem.OverallPaymentAmount.FirstOrDefault().Value.Split('|')[0].Replace(".", ","));
                            actualPaymentAmountCurrency = bitrixAFPListItem.OverallPaymentAmount.FirstOrDefault().Value.Split('|')[1];
                        }
                        else
                        {
                            actualPaymentAmount = Convert.ToDouble(bitrixAFPListItem.OverallPaymentAmount.FirstOrDefault().Value.Replace("руб.", "").Replace(".", ","));
                            actualPaymentAmountCurrency = "RUB";
                        }

                        if (bitrixAFPListItem.OverallPaymentAmountWithoutVAT != null
                        && String.IsNullOrEmpty(bitrixAFPListItem.OverallPaymentAmountWithoutVAT.FirstOrDefault().Value) == false)
                        {
                            if (bitrixAFPListItem.OverallPaymentAmountWithoutVAT.FirstOrDefault().Value.Contains("|"))
                            {
                                actualPaymentAmountWithoutVAT = Convert.ToDouble(bitrixAFPListItem.OverallPaymentAmountWithoutVAT.FirstOrDefault().Value.Split('|')[0].Replace(".", ","));
                            }
                            else
                            {
                                actualPaymentAmountWithoutVAT = Convert.ToDouble(bitrixAFPListItem.OverallPaymentAmountWithoutVAT.FirstOrDefault().Value.Replace("руб.", "").Replace(".", ","));
                            }
                        }

                        if (actualPaymentAmountWithoutVAT == 0 && actualPaymentAmount != 0)
                        {
                            actualPaymentAmountWithoutVAT = actualPaymentAmount;
                        }

                        if (actualPaymentAmountWithoutVAT != 0)
                        {
                            actualPaymentVAT = actualPaymentAmount - actualPaymentAmountWithoutVAT;
                        }
                    }
                    else if (bitrixAFPListItem.ActualPaymentAmount != null
                        && String.IsNullOrEmpty(bitrixAFPListItem.ActualPaymentAmount.FirstOrDefault().Value) == false)
                    {
                        if (bitrixAFPListItem.ActualPaymentAmount.FirstOrDefault().Value.Contains("|"))
                        {
                            actualPaymentAmount = Convert.ToDouble(bitrixAFPListItem.ActualPaymentAmount.FirstOrDefault().Value.Split('|')[0].Replace(".", ","));
                            actualPaymentAmountCurrency = bitrixAFPListItem.ActualPaymentAmount.FirstOrDefault().Value.Split('|')[1];
                        }
                        else
                        {
                            actualPaymentAmount = Convert.ToDouble(bitrixAFPListItem.ActualPaymentAmount.FirstOrDefault().Value.Replace("руб.", "").Replace(".", ","));
                            actualPaymentAmountCurrency = "RUB";
                        }

                        if (bitrixAFPListItem.ActualPaymentAmountWithoutVAT != null
                        && String.IsNullOrEmpty(bitrixAFPListItem.ActualPaymentAmountWithoutVAT.FirstOrDefault().Value) == false)
                        {
                            if (bitrixAFPListItem.ActualPaymentAmountWithoutVAT.FirstOrDefault().Value.Contains("|"))
                            {
                                actualPaymentAmountWithoutVAT = Convert.ToDouble(bitrixAFPListItem.ActualPaymentAmountWithoutVAT.FirstOrDefault().Value.Split('|')[0].Replace(".", ","));
                            }
                            else
                            {
                                actualPaymentAmountWithoutVAT = Convert.ToDouble(bitrixAFPListItem.ActualPaymentAmountWithoutVAT.FirstOrDefault().Value.Replace("руб.", "").Replace(".", ","));
                            }
                        }

                        if (actualPaymentAmountWithoutVAT == 0 && actualPaymentAmount != 0)
                        {
                            actualPaymentAmountWithoutVAT = actualPaymentAmount;
                        }

                        if (actualPaymentAmountWithoutVAT != 0)
                        {
                            actualPaymentVAT = actualPaymentAmount - actualPaymentAmountWithoutVAT;
                        }
                    }

                    paymentCompletedActualDate = (bitrixAFPListItem.PAYMENT_COMPLETED_DATE != null) ? Convert.ToDateTime(bitrixAFPListItem.PAYMENT_COMPLETED_DATE.FirstOrDefault().Value) : (DateTime?)null;
                }
                catch (Exception)
                {
                    actualPaymentAmount = 0;
                    actualPaymentAmountCurrency = "";
                }

                int costSubItemID = 0;

                try
                {
                    if (bitrixAFPListItem.CostSubitem != null
                    && String.IsNullOrEmpty(bitrixAFPListItem.CostSubitem?.FirstOrDefault().Value) == false)
                    {
                        var bitrixCSI = bitrixCSIList.Where(x => x.ID == bitrixAFPListItem.CostSubitem?.FirstOrDefault().Value).FirstOrDefault();  //bitrixHelper.GetBitrixCSIById(bitrixAFPListItem.CostSubitem?.FirstOrDefault().Value);
                        if (bitrixCSI != null)
                        {
                            int.TryParse(bitrixCSI?.SKIPR_ID?.First().Value, out costSubItemID);
                        }
                    }
                }
                catch (Exception)
                {

                }

                int projectID = 0;

                try
                {
                    if (bitrixAFPListItem.Project != null
                        && String.IsNullOrEmpty(bitrixAFPListItem.Project?.FirstOrDefault().Value) == false)
                    {
                        var bitrixProject = bitrixProjectList.Where(x => x.ID == bitrixAFPListItem.Project.FirstOrDefault().Value).FirstOrDefault();//bitrixHelper.GetBitrixProjectById(bitrixAFPListItem.Project?.FirstOrDefault().Value);
                        if (bitrixProject != null)
                        {
                            int.TryParse(bitrixProject?.SKIPR_ID?.FirstOrDefault().Value, out projectID);
                        }
                    }
                }
                catch (Exception)
                {
                }

                if (String.IsNullOrEmpty(documentState) == true
                    || documentState.Equals("Утверждено") == false
                    || amount <= 0)
                {
                    SetStatus(50 + j * 20 / bitrixAFPList.Count, "Синхронизация заявки на затраты пропущена: ID = " + bitrixAFPListItem.ID + ", сумма: " + actualPaymentAmount.ToString());
                    continue;
                }

                string uregNum = "";
                try
                {
                    if (bitrixAFPListItem.URegNum != null)
                    {
                        uregNum = bitrixAFPListItem.URegNum.FirstOrDefault().Value;
                    }
                }
                catch (Exception)
                {
                    uregNum = "";
                }

                SetStatus(50 + j * 20 / bitrixAFPList.Count, "Синхронизация заявки на затраты: ID = " + bitrixAFPListItem.ID + ", Ун. №: " + uregNum);

                if (String.IsNullOrEmpty(financialYear) == true)
                {
                    amountReservedApprovedActualDate = approveDate;
                }
                else
                {
                    try
                    {
                        int financialYearIntValue = Convert.ToInt32(financialYear);

                        if (approveDate.Value.Year == financialYearIntValue)
                        {
                            amountReservedApprovedActualDate = approveDate;
                        }
                        else if (approveDate.Value.Year > financialYearIntValue)
                        {
                            amountReservedApprovedActualDate = new DateTime(financialYearIntValue, 12, 31);
                        }
                        else
                        {
                            amountReservedApprovedActualDate = new DateTime(financialYearIntValue, 1, 1);
                        }
                    }
                    catch (Exception)
                    {
                        amountReservedApprovedActualDate = approveDate;
                    }
                }

                ExpensesRecord exist_record = null;
                if (String.IsNullOrEmpty(uregNum) == false)
                {
                    exist_record = _expensesRecordService.Get(e => e.Where(x => x.SourceElementID == bitrixAFPListItem.ID && x.SourceDB == SourceDB.Bitrix).ToList()).FirstOrDefault();
                }

                if (exist_record == null)
                {
                    var record = new ExpensesRecord();

                    try
                    {
                        record = ExtractExpensesRecordFromBitrix(bitrixAFPListItem,
                            bitrixCSIList,
                            bitrixFRCList,
                            bitrixProjectList,
                            amountReservedApprovedActualDate,
                            amount,
                            amountWithoutVAT,
                            actualPaymentAmount,
                            actualPaymentAmountWithoutVAT,
                            paymentCompletedActualDate,
                            record);
                    }
                    catch (Exception)
                    {
                        record = null;
                    }

                    if (record != null)
                    {
                        _expensesRecordService.Add(record);
                    }
                    SetStatus(50 + j * 20 / bitrixAFPList.Count, "Синхронизация заявки на затраты - добавлено: ID = " + bitrixAFPListItem.ID);
                    continue;
                }

                if (exist_record?.Amount == (decimal)actualPaymentAmount
                    && exist_record?.AmountNoVAT == (decimal)actualPaymentAmountWithoutVAT
                    && exist_record?.AmountReservedApprovedActualDate == amountReservedApprovedActualDate
                    && exist_record?.PaymentCompletedActualDate == paymentCompletedActualDate
                    && exist_record.CostSubItemID == costSubItemID
                    && exist_record.ProjectID == projectID
                    && exist_record.Project != null && exist_record.DepartmentID == exist_record.Project.DepartmentID)
                {
                    SetStatus(50 + j * 20 / bitrixAFPList.Count, "Синхронизация заявки на затраты пропущена: ID = " + bitrixAFPListItem.ID + ", сумма: " + actualPaymentAmount.ToString());
                    continue;
                }

                try
                {
                    exist_record = ExtractExpensesRecordFromBitrix(bitrixAFPListItem,
                        bitrixCSIList,
                        bitrixFRCList,
                        bitrixProjectList,
                        amountReservedApprovedActualDate,
                        amount,
                        amountWithoutVAT,
                        actualPaymentAmount,
                        actualPaymentAmountWithoutVAT,
                        paymentCompletedActualDate,
                        exist_record);
                }
                catch (Exception)
                {
                    exist_record = null;
                }

                if (exist_record != null)
                {
                    _expensesRecordService.Update(exist_record);
                    SetStatus(50 + j * 20 / bitrixAFPList.Count, "Синхронизация заявки на затраты - обновлено: ID = " + bitrixAFPListItem.ID);
                }
            }
        }

        private void UpdateBitrixRequestForTripRecords(List<BitrixRequestForTrip> bitrixRFTList,
            List<BitrixProject> bitrixProjectList,
            Hashtable fieldDisplayValuesForListIDs)
        {
            int j = 0;
            foreach (var bitrixRFTListItem in bitrixRFTList)
            {
                try
                {
                    SetStatus(70 + j * 10 / bitrixRFTList.Count, "Синхронизация затрат на командировку: " + bitrixRFTListItem.NAME);
                    j++;


                    string documentState = "";
                    DateTime? approveDate = null;
                    DateTime? amountReservedApprovedActualDate = null;

                    DateTime? paymentCompletedActualDate = null;

                    if (fieldDisplayValuesForListIDs[bitrixRFTListItem.IBLOCK_ID + "_DOCUMENT_STATE"] != null)
                    {
                        documentState = BitrixHelper.ParseBitrixListPropertyDisplayValueByID((Dictionary<string, string>)fieldDisplayValuesForListIDs[bitrixRFTListItem.IBLOCK_ID + "_DOCUMENT_STATE"], bitrixRFTListItem.DOCUMENT_STATE?.FirstOrDefault().Value);
                    }

                    approveDate = (bitrixRFTListItem.APPROVE_DATE != null) ? Convert.ToDateTime(bitrixRFTListItem.APPROVE_DATE.FirstOrDefault().Value) : (DateTime?)null;


                    /*var request_status = bitrixRFTListItem.DISPLAY_STATUS?.FirstOrDefault().Value;
                    if (!(request_status == "Командировка отменена" || request_status == "Командировка закрыта"))
                        continue;*/

                    if (String.IsNullOrEmpty(documentState) == true
                        || documentState.Equals("Утверждено") == false)
                    {
                        SetStatus(70 + j * 10 / bitrixRFTList.Count, "Синхронизация заявки на командировку пропущена: ID = " + bitrixRFTListItem.ID);
                        continue;
                    }

                    paymentCompletedActualDate = (bitrixRFTListItem.PAYMENT_COMPLETED_DATE != null) ? Convert.ToDateTime(bitrixRFTListItem.PAYMENT_COMPLETED_DATE.FirstOrDefault().Value) : (DateTime?)null;

                    amountReservedApprovedActualDate = approveDate;

                    var expensesRecords = ExtractExpensesRecordsFromBitrixRequestForTrip(bitrixRFTListItem, bitrixProjectList,
                        amountReservedApprovedActualDate,
                        paymentCompletedActualDate);

                    foreach (var record in expensesRecords)
                    {
                        if (record == null || record.Amount == null || record.Amount == 0)
                            continue;

                        var exist_record = _expensesRecordService.Get(r => r.Where(x =>
                            x.SourceElementID == bitrixRFTListItem.ID
                            && x.SourceDB == SourceDB.Bitrix
                            && x.ProjectID == record.ProjectID).ToList()).FirstOrDefault();
                        if (exist_record == null)
                        {
                            _expensesRecordService.Add(record);
                            SetStatus(70 + j * 10 / bitrixRFTList.Count, "Синхронизация заявки на командировку - добавлено: ID = " + bitrixRFTListItem.ID);
                            continue;
                        }
                        else
                        {

                            exist_record.RecordStatus = record.RecordStatus;

                            exist_record.AmountReservedApprovedActualDate = record.AmountReservedApprovedActualDate;
                            exist_record.AmountReserved = record.AmountReserved;
                            exist_record.AmountReservedNoVAT = record.AmountReservedNoVAT;

                            exist_record.PaymentCompletedActualDate = record.PaymentCompletedActualDate;
                            exist_record.Amount = record.Amount;
                            exist_record.AmountNoVAT = record.AmountNoVAT;

                            exist_record.SourceDB = SourceDB.Bitrix;
                            exist_record.SourceElementID = record.SourceElementID;
                            exist_record.SourceListID = record.SourceListID;
                            exist_record.ExpensesRecordName = record.ExpensesRecordName;
                            exist_record.ExpensesDate = record.ExpensesDate;

                            _expensesRecordService.Update(exist_record);
                            SetStatus(70 + j * 10 / bitrixRFTList.Count, "Синхронизация затрат обновлено: ID = " + bitrixRFTListItem.ID);
                        }
                    }
                }
                catch (Exception e)
                {

                }
            }
        }

        private ExpensesRecord ExtractExpensesRecordFromBitrix(BitrixApplicationForPayment bitrixAFPListItem,
            List<BitrixCSI> bitrixCSIList,
            List<BitrixFRC> bitrixFRCList,
            List<BitrixProject> bitrixProjectList,
            DateTime? amountReservedApprovedActualDate,
            double amountReserved,
            double amountReservedNoVAT,
            double amount,
            double amountNoVAT,
            DateTime? paymentCompletedActualDate,
            ExpensesRecord expensesRecord)
        {
            int costSubItemID = 0;
            CostSubItem costSubItem = null;
            var bitrixCSI = bitrixCSIList.Where(x => x.ID == bitrixAFPListItem.CostSubitem?.FirstOrDefault().Value).FirstOrDefault();  //bitrixHelper.GetBitrixCSIById(bitrixAFPListItem.CostSubitem?.FirstOrDefault().Value);
            int.TryParse(bitrixCSI?.SKIPR_ID?.First().Value, out costSubItemID);
            if (costSubItemID != 0)
            {
                costSubItem = _costSubItemService.GetById(costSubItemID);
            }

            if (costSubItem == null && bitrixCSI != null && String.IsNullOrEmpty(bitrixCSI.NAME) == false)
            {
                try
                {
                    string code = bitrixCSI.NAME.Substring(0, 7);
                    costSubItem = _costSubItemService.Get(x => x.Where(csi => csi.ShortName == code).ToList())
                        .FirstOrDefault();
                }
                catch (Exception)
                {
                    costSubItem = null;
                }
            }

            int departmentID = 0;
            var bitrixFRC = bitrixFRCList.Where(x => x.ID == bitrixAFPListItem.FinResponsibilityCenter.FirstOrDefault().Value).FirstOrDefault(); //bitrixHelper.GetBitrixFRCById(bitrixAFPListItem.FinResponsibilityCenter?.FirstOrDefault().Value);
            int.TryParse(bitrixFRC?.SKIPR_ID?.FirstOrDefault().Value, out departmentID);

            int projectID = 0;
            if (bitrixAFPListItem.Project != null
                && String.IsNullOrEmpty(bitrixAFPListItem.Project?.FirstOrDefault().Value) == false)
            {
                var bitrixProject = bitrixProjectList.Where(x => x.ID == bitrixAFPListItem.Project.FirstOrDefault().Value).FirstOrDefault();//bitrixHelper.GetBitrixProjectById(bitrixAFPListItem.Project?.FirstOrDefault().Value);
                if (bitrixProject != null)
                {
                    int.TryParse(bitrixProject?.SKIPR_ID?.FirstOrDefault().Value, out projectID);
                }
            }

            Project project = null;

            if (projectID != 0)
            {
                project = _projectService.GetById(projectID);
            }

            if (project != null
                && project.Department != null
                && costSubItem != null)
            {
                expensesRecord.ExpensesDate = BitrixHelper.ParseBitrixListElementDateFieldValue(bitrixAFPListItem.ActualPaymentDate?.FirstOrDefault().Value);
                expensesRecord.CostSubItemID = costSubItem.ID;
                expensesRecord.DepartmentID = project.Department.ID;
                expensesRecord.ProjectID = project.ID;
                expensesRecord.BitrixURegNum = bitrixAFPListItem.URegNum?.FirstOrDefault().Value;

                expensesRecord.AmountReservedApprovedActualDate = amountReservedApprovedActualDate;
                expensesRecord.AmountReserved = (decimal)amountReserved;
                expensesRecord.AmountReservedNoVAT = (decimal)amountReservedNoVAT;

                expensesRecord.Amount = (decimal)amount;
                expensesRecord.AmountNoVAT = (decimal)amountNoVAT;
                expensesRecord.PaymentCompletedActualDate = paymentCompletedActualDate;

                if (amount != 0)
                {
                    expensesRecord.RecordStatus = ExpensesRecordStatus.ActuallySpent;
                }
                else
                {
                    expensesRecord.RecordStatus = ExpensesRecordStatus.Reserved;
                }

                expensesRecord.ExpensesRecordName = bitrixAFPListItem.NAME;
                expensesRecord.SourceListID = bitrixAFPListItem.IBLOCK_ID;
                expensesRecord.SourceElementID = bitrixAFPListItem.ID;
                expensesRecord.SourceDB = SourceDB.Bitrix;
            }
            else
            {
                expensesRecord = null;
            }

            return expensesRecord;
        }

        private List<ExpensesRecord> ExtractExpensesRecordsFromBitrixRequestForTrip(BitrixRequestForTrip bitrixRFTListItem,
            List<BitrixProject> bitrixProjectList,
            DateTime? amountReservedApprovedActualDate,
            DateTime? paymentCompletedActualDate)
        {
            var result = new List<ExpensesRecord>();
            if (bitrixRFTListItem.Project != null)
            {
                foreach (var rftProject in bitrixRFTListItem.Project)
                {
                    int projectID = 0;
                    var bitrixProject = bitrixProjectList.Where(x => x.ID == rftProject.Value).FirstOrDefault();
                    if (bitrixProject != null)
                    {
                        int.TryParse(bitrixProject?.SKIPR_ID?.FirstOrDefault().Value, out projectID);
                    }


                    if (bitrixProject != null)
                    {
                        var load = 100;

                        if (bitrixRFTListItem.BlockProjects != null)
                        {
                            foreach (var project_data in bitrixRFTListItem.BlockProjects)
                            {
                                var project_params = JsonConvert.DeserializeObject<Dictionary<string, string>>(project_data.Value);
                                if (project_params != null && String.IsNullOrEmpty(project_params["CODE"]) == false
                                    && bitrixProject.ID == project_params["CODE"])
                                {
                                    Int32.TryParse(project_params["LOAD"], out load);
                                    break;
                                }
                            }
                        }

                        Project project = null;
                        if (projectID != 0)
                        {
                            project = _projectService.GetById(projectID);
                        }
                        else
                        {
                            var projectTitle = bitrixProject.ProjectTitle?.FirstOrDefault().Value;
                            project = _projectService.Get(p => p.Where(x => x.ShortName == projectTitle).ToList())
                                .FirstOrDefault();
                        }
                        if (project == null)
                            continue;

                        if (!project.DepartmentID.HasValue)
                            continue;

                        if (!project.ProjectTypeID.HasValue)
                            continue;

                        int? costSubItemID = project.ProjectType.BusinessTripCostSubItemID;
                        if (costSubItemID == null)
                            continue;

                        double total_planned_amount = 0;

                        if (bitrixRFTListItem.CostAmountTripInfo != null)
                        {
                            if (bitrixRFTListItem.CostAmountTripInfo.FirstOrDefault().Value.Contains("|"))
                            {
                                total_planned_amount = Convert.ToDouble(bitrixRFTListItem.CostAmountTripInfo.FirstOrDefault().Value.Split('|')[0].Replace(".", ","));
                            }
                            else
                            {
                                total_planned_amount = Convert.ToDouble(bitrixRFTListItem.CostAmountTripInfo.FirstOrDefault().Value.Replace("руб.", "").Replace(".", ","));
                            }
                        }

                        double total_actual_amount = 0;

                        if (bitrixRFTListItem.TOTAL_ACTUAL_COSTS_AMOUNT != null)
                        {
                            if (bitrixRFTListItem.TOTAL_ACTUAL_COSTS_AMOUNT.FirstOrDefault().Value.Contains("|"))
                            {
                                total_actual_amount = Convert.ToDouble(bitrixRFTListItem.TOTAL_ACTUAL_COSTS_AMOUNT.FirstOrDefault().Value.Split('|')[0].Replace(".", ","));
                            }
                            else
                            {
                                total_actual_amount = Convert.ToDouble(bitrixRFTListItem.TOTAL_ACTUAL_COSTS_AMOUNT.FirstOrDefault().Value.Replace("руб.", "").Replace(".", ","));
                            }
                        }

                        string expensesRecordName = bitrixRFTListItem.NAME;

                        try
                        {
                            expensesRecordName = (String.IsNullOrEmpty(bitrixRFTListItem.CommittedNotDirectory?.FirstOrDefault().Value) == false) ? bitrixRFTListItem.CommittedNotDirectory?.FirstOrDefault().Value : "";
                            expensesRecordName += ": " + ((bitrixRFTListItem.StartDateTrip != null) ? Convert.ToDateTime(bitrixRFTListItem.StartDateTrip?.FirstOrDefault().Value).ToShortDateString() : "")
                                + " - " + ((bitrixRFTListItem.EndDateTrip != null) ? Convert.ToDateTime(bitrixRFTListItem.EndDateTrip?.FirstOrDefault().Value).ToShortDateString() : "");

                        }
                        catch (Exception)
                        {

                        }

                        var record = new ExpensesRecord
                        {
                            ExpensesDate = (amountReservedApprovedActualDate != null) ? amountReservedApprovedActualDate.Value : bitrixRFTListItem.Updated.Value,

                            ProjectID = project.ID,
                            DepartmentID = project.DepartmentID.Value,
                            CostSubItemID = costSubItemID.Value,
                            BitrixURegNum = bitrixRFTListItem.URegNum?.FirstOrDefault().Value,
                            RecordStatus = (total_actual_amount != 0) ? ExpensesRecordStatus.ActuallySpent : ExpensesRecordStatus.Reserved,

                            AmountReservedApprovedActualDate = amountReservedApprovedActualDate,
                            AmountReserved = Convert.ToDecimal(total_planned_amount * load / 100),
                            AmountReservedNoVAT = Convert.ToDecimal(total_planned_amount * load / 100),

                            PaymentCompletedActualDate = paymentCompletedActualDate,
                            Amount = Convert.ToDecimal(total_actual_amount * load / 100),
                            AmountNoVAT = Convert.ToDecimal(total_actual_amount * load / 100),

                            ExpensesRecordName = expensesRecordName,
                            SourceListID = bitrixRFTListItem.IBLOCK_ID,
                            SourceElementID = bitrixRFTListItem.ID,
                            SourceDB = SourceDB.Bitrix
                        };
                        result.Add(record);
                    }
                }
            }

            return result;
        }

        private void SyncBitrixEmployeeGrad()
        {
            if (bitrixHelper.IsSyncBitrixEmployeeGradEnabled() == false)
                return;

            SetStatus(80, "Экспорт грейдов сотрудников - старт", true);

            var employeeList = _employeeService.Get(x => x.Where(e => String.IsNullOrEmpty(e.Email) == false
                                                                      && e.EmployeeGrad != null)
                .Include(e => e.EmployeeGrad).ToList().OrderBy(e => e.FullName).ToList());

            int j = 0;
            foreach (Employee employee in employeeList)
            {
                SetStatus(80 + j * 15 / employeeList.Count, "Экспорт грейда сотрудника: " + employee.FullName);
                j++;

                BitrixUser bitrixUser = bitrixUserList.Where(x => x.EMAIL == employee.Email).FirstOrDefault(); //bitrixHelper.GetBitrixUserByEmail(employee.Email);

                if (bitrixUser != null)
                {
                    try
                    {
                        bitrixHelper.UpdateBitrixUserEmployeeGrad(bitrixUser, employee.EmployeeGrad.Title);
                    }
                    catch (Exception e)
                    {

                    }
                }
            }

            SetStatus(95, "Экспорт грейдов сотрудников - завершено", true);
        }

        public void CalcAndUpdateLimitAmounts()
        {
            SetStatus(80, "Обновление данных о лимитах - старт", true);


            foreach (var budgetLimit in _budgetLimitService.Get(blList => blList.ToList()))
            {
                // если значений нет, то что передавать
                // обернуть в try catch
                try
                {
                    SetStatus(80, "Обновление данных о лимитах: "
                        + budgetLimit.Department.ShortName + " - " + budgetLimit.CostSubItem.ShortName + " - " + budgetLimit.Year + "." + budgetLimit.Month);

                    var limitDto = _budgetLimitService.GetLimitData(budgetLimit.CostSubItemID.Value, budgetLimit.DepartmentID.Value, budgetLimit.ProjectID, budgetLimit.Year.Value, budgetLimit.Month.Value);

                    if (budgetLimit.LimitAmountApproved != limitDto.LimitAmountReserved
                        || budgetLimit.FundsExpendedAmount != limitDto.LimitAmountActuallySpent)
                    {
                        budgetLimit.LimitAmountApproved = limitDto.LimitAmountReserved;
                        budgetLimit.FundsExpendedAmount = limitDto.LimitAmountActuallySpent;
                        _budgetLimitService.UpdateWithoutVersion(budgetLimit);
                    }
                }
                catch (Exception ex)
                {

                }
            }

            SetStatus(80, "Обновление данных о лимитах - завершено", true);
        }

        private void SyncBitrixProjectRoles()
        {
            if (!bitrixHelper.IsSyncBitrixProjectRolesEnabled())
                return;

            SetStatus(1, "Обновление данных проектных ролей в Б24 - старт", true);

            var projectRoleList = _projectRoleService.Get(pr => pr.ToList());

            List<BitrixProjectRole> bitrixProjects = bitrixHelper.GetBitrixProjectRoles();
            if (bitrixProjects == null)
                return;

            SetStatus(1, "Получен список проектных ролей из Б24", true);

            for (int i = 0; i < projectRoleList.Count; i++)
            {
                var projectRole = projectRoleList.ElementAt(i);
                SetStatus(1, "Обновление данных проектной роли: " + projectRole.FullName, true);

                var bitrixProjectRole = bitrixProjects.FirstOrDefault(p => p.SKIPR_ID?.FirstOrDefault().Value == projectRole.ID.ToString());
                // bitrixProjectRole.ID Element_id 
                if (bitrixProjectRole == null)
                {
                    // add
                    bitrixHelper.AddBitrixProjectRole(projectRole);
                }
                else if (IsNeedUpdateProjectRole(projectRole, bitrixProjectRole))
                {
                    // update                 
                    bitrixHelper.UpdateBitrixProjectRole(projectRole, bitrixProjectRole.ID);
                }
            }

            SetStatus(1, "Обновление данных проектных ролей в Б24 - завершено", true);
        }

        private void SyncBitrixDepartment()
        {
            if (!bitrixHelper.IsSyncBitrixDepartmentsEnabled())
                return;
            try
            {
                SetStatus(1, "Обновление данных о подразделениях в Б24 - старт", true);

                var departments = _departmentService.Get(d => d.ToList());

                List<BitrixDepartment> bitrixDepartments = bitrixHelper.GetBitrixDepartments();
                if (bitrixDepartments == null)
                    return;

                SetStatus(1, "Получен список подразделений из Б24", true);

                Func<Department, List<BitrixDepartment>, string> getBitrixParentDepartmentID =
                    (department, bitrixDeptList) =>
                    {
                        string bitrixParentDepartmentID = string.Empty;
                        if (department.ParentDepartmentID.HasValue)
                        {
                            var bitrixParentDepartment = bitrixDeptList.FirstOrDefault(d =>
                                d.SKIPR_ID?.FirstOrDefault().Value == department.ParentDepartmentID.ToString());
                            if (bitrixParentDepartment != null)
                            {
                                bitrixParentDepartmentID = bitrixParentDepartment.ID;
                            }
                        }

                        return bitrixParentDepartmentID;
                    };

                for (int i = 0; i < departments.Count; i++)
                {
                    var department = departments.ElementAt(i);
                    SetStatus(1, "Обновление данных о подразделении: " + department.FullName, true);

                    var bitrixDepartment = bitrixDepartments.FirstOrDefault(d =>
                        d.SKIPR_ID?.FirstOrDefault().Value == department.ID.ToString());
                    if (bitrixDepartment == null)
                    {
                        // add
                        var bitrixParentDepartmentID = getBitrixParentDepartmentID(department, bitrixDepartments);
                        bitrixHelper.AddBitrixDepartment(department, bitrixParentDepartmentID);
                    }
                    else
                    {
                        // update
                        var bitrixParentDepartmentID = getBitrixParentDepartmentID(department, bitrixDepartments);
                        if (IsNeedUpdateDepartment(department, bitrixDepartment, bitrixParentDepartmentID))
                            bitrixHelper.UpdateBitrixDepartment(department, bitrixDepartment.ID,
                                bitrixParentDepartmentID);
                    }
                }

                SetStatus(1, "Обновление данных о подразделениях в Б24 - завершено", true);
            }
            catch (Exception e)
            {
                SetStatus(1, "Ошибка: " + e.Message.Replace("\r", "").Replace("\n", " "), true);
            }
        }


        public BitrixSyncResult ProcessLongRunningAction(string userIdentityName, string id)
        {
            var htmlReport = string.Empty;
            var htmlErrorReport = string.Empty;
            taskId = id;
            taskReport = new LongRunningTaskReport("Отчет о синхронизации с Б24", "");

            try
            {
                SetStatus(0, "Старт синхронизации...", true);

                bitrixHelper = new BitrixHelper(_bitrixOptions);

                SetStatus(1, "Получение списка пользователей Б24", true);
                bitrixUserList = bitrixHelper.GetBitrixUserList();

                SyncBitrixDepartment();

                //временно отключена синхронизация ЦФО и Статей затрат
                //SyncBitrixFRC();
                //SyncBitrixCSI();
                SyncBitrixProjectRoles(); // отестировать


                SyncBitrixProjects();
                SyncBitrixExpensesRecord();
                CalcAndUpdateLimitAmounts();
                SyncBitrixEmployeeGrad();


                SetStatus(100, "Синхронизация завершена", true);

                try
                {
                    if (taskReport != null)
                        htmlReport = taskReport.GenerateHtmlReport();
                }
                catch (Exception e)
                {
                    SetStatus(-1, "Ошибка: " + e.Message);
                    htmlErrorReport += "<br>" + e.Message + "<br>" + e.StackTrace + "<br>" + e.TargetSite.ToString();
                }
            }
            catch (Exception e)
            {
                SetStatus(-1, "Ошибка: " + e.Message.Replace("\r", "").Replace("\n", " ") + ", проект: " + currentProjectShortName);
                htmlErrorReport += "<br>" + e.Message + "<br>" + e.StackTrace + "<br>" + e.TargetSite.ToString() + "<br> Проект: " + currentProjectShortName;
            }

            return new BitrixSyncResult() { fileId = id, fileHtmlReport = new List<string>() { htmlReport, htmlErrorReport } };
        }
    }
}