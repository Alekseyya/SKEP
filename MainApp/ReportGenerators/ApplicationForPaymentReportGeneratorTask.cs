using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using Core.BL.Interfaces;
using Core.Common;
using Core.Config;
using Core.Helpers;
using Core.Models;
using MainApp.BitrixSync;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;







namespace MainApp.ReportGenerators
{
    public class ApplicationForPaymentReportGeneratorTask : LongRunningTaskBase
    {
        private readonly IProjectService _projectService;
        private readonly IDepartmentService _departmentService;
        private readonly ICostSubItemService _costSubItemService;
        private readonly IOptions<BitrixConfig> _bitrixConfigOptions;

        public ApplicationForPaymentReportGeneratorTask(IProjectService projectService, IDepartmentService departmentService, ICostSubItemService costSubItemService, IOptions<BitrixConfig> bitrixConfigOptions)
            : base()
        {
            _projectService = projectService;
            _departmentService = departmentService;
            _costSubItemService = costSubItemService;
            _bitrixConfigOptions = bitrixConfigOptions;
        }


        public ReportGeneratorResult ProcessLongRunningAction(string userIdentityName, string id,
            DateTime periodStart, DateTime periodEnd)
        {
            var htmlErrorReport = string.Empty;

            taskId = id;

            byte[] binData = null;

            try
            {

                SetStatus(0, "Старт формирования отчета...");

                DataTable dataTable = new DataTable();

                dataTable.Columns.Add("ID", typeof(string)).Caption = "ID";
                dataTable.Columns["ID"].ExtendedProperties["Width"] = (double)6;
                dataTable.Columns.Add("URegNum", typeof(string)).Caption = "Ун. № заявки";
                dataTable.Columns["URegNum"].ExtendedProperties["Width"] = (double)8;
                dataTable.Columns.Add("Url", typeof(string)).Caption = "Гиперссылка на карточку заявки";
                dataTable.Columns["Url"].ExtendedProperties["Width"] = (double)63;
                dataTable.Columns.Add("Type", typeof(string)).Caption = "Вид заявки";
                dataTable.Columns["Type"].ExtendedProperties["Width"] = (double)8;
                dataTable.Columns.Add("FinResponsibilityCenter_ShortName", typeof(string)).Caption = "Код ЦФО";
                dataTable.Columns["FinResponsibilityCenter_ShortName"].ExtendedProperties["Width"] = (double)8;
                dataTable.Columns.Add("FinResponsibilityCenter_ShortTitle", typeof(string)).Caption = "ЦФО";
                dataTable.Columns["FinResponsibilityCenter_ShortTitle"].ExtendedProperties["Width"] = (double)8;
                dataTable.Columns.Add("FinResponsibilityCenter_Title", typeof(string)).Caption = "Название ЦФО (полное)";
                dataTable.Columns["FinResponsibilityCenter_Title"].ExtendedProperties["Width"] = (double)40;
                dataTable.Columns.Add("CostSubitem_ShortName", typeof(string)).Caption = "Код подстатьи затрат";
                dataTable.Columns["CostSubitem_ShortName"].ExtendedProperties["Width"] = (double)10;
                dataTable.Columns.Add("CostSubitem_Title", typeof(string)).Caption = "Название подстатьи затрат";
                dataTable.Columns["CostSubitem_Title"].ExtendedProperties["Width"] = (double)50;
                dataTable.Columns.Add("ProjectShortName", typeof(string)).Caption = "Код проекта";
                dataTable.Columns["ProjectShortName"].ExtendedProperties["Width"] = (double)25;
                dataTable.Columns.Add("Subject", typeof(string)).Caption = "Предмет заявки";
                dataTable.Columns["Subject"].ExtendedProperties["Width"] = (double)50;
                dataTable.Columns.Add("Created", typeof(DateTime)).Caption = "Дата создания";
                dataTable.Columns["Created"].ExtendedProperties["Width"] = (double)17;
                dataTable.Columns.Add("DeliveryDate", typeof(DateTime)).Caption = "Дата поставки";
                dataTable.Columns["DeliveryDate"].ExtendedProperties["Width"] = (double)11;

                dataTable.Columns.Add("PaymentForm", typeof(string)).Caption = "Способ оплаты";
                dataTable.Columns["PaymentForm"].ExtendedProperties["Width"] = (double)8;

                dataTable.Columns.Add("Organisation", typeof(string)).Caption = "Юридическое лицо-плательщик";
                dataTable.Columns["Organisation"].ExtendedProperties["Width"] = (double)18;

                dataTable.Columns.Add("Amount", typeof(double)).Caption = "Сумма заявки";
                dataTable.Columns["Amount"].ExtendedProperties["Width"] = (double)17;
                dataTable.Columns.Add("AmountCurrency", typeof(string)).Caption = "Валюта";
                dataTable.Columns["AmountCurrency"].ExtendedProperties["Width"] = (double)11;
                dataTable.Columns.Add("RateVAT", typeof(string)).Caption = "Ставка НДС";
                dataTable.Columns["RateVAT"].ExtendedProperties["Width"] = (double)11;
                dataTable.Columns.Add("AmountWithoutVAT", typeof(double)).Caption = "Сумма заявки, без НДС";
                dataTable.Columns["AmountWithoutVAT"].ExtendedProperties["Width"] = (double)17;
                dataTable.Columns.Add("AmountVAT", typeof(double)).Caption = "Сумма НДС";
                dataTable.Columns["AmountVAT"].ExtendedProperties["Width"] = (double)17;

                dataTable.Columns.Add("Contragent", typeof(string)).Caption = "Получатель средств";
                dataTable.Columns["Contragent"].ExtendedProperties["Width"] = (double)40;

                dataTable.Columns.Add("DocumentState", typeof(string)).Caption = "Статус";
                dataTable.Columns["DocumentState"].ExtendedProperties["Width"] = (double)16;
                dataTable.Columns.Add("ApproveDate", typeof(DateTime)).Caption = "Дата финального согласования";
                dataTable.Columns["ApproveDate"].ExtendedProperties["Width"] = (double)17;

                dataTable.Columns.Add("FinancialYear", typeof(string)).Caption = "Финансовый год";
                dataTable.Columns["FinancialYear"].ExtendedProperties["Width"] = (double)13;

                dataTable.Columns.Add("ActualPaymentDate", typeof(DateTime)).Caption = "Дата факт. оплаты";
                dataTable.Columns["ActualPaymentDate"].ExtendedProperties["Width"] = (double)11;
                dataTable.Columns.Add("ActualPaymentAmount", typeof(double)).Caption = "Сумма факт. оплаты";
                dataTable.Columns["ActualPaymentAmount"].ExtendedProperties["Width"] = (double)17;
                dataTable.Columns.Add("ActualPaymentAmountWithoutVAT", typeof(double)).Caption = "Сумма факт. оплаты, без НДС";
                dataTable.Columns["ActualPaymentAmountWithoutVAT"].ExtendedProperties["Width"] = (double)17;
                dataTable.Columns.Add("ActualPaymentVAT", typeof(double)).Caption = "Сумма факт. НДС";
                dataTable.Columns["ActualPaymentVAT"].ExtendedProperties["Width"] = (double)17;

                dataTable.Columns.Add("ActualPaymentAmountCurrency", typeof(string)).Caption = "Валюта факт. оплаты";
                dataTable.Columns["ActualPaymentAmountCurrency"].ExtendedProperties["Width"] = (double)11;
                dataTable.Columns.Add("PaymentCompletedDate", typeof(DateTime)).Caption = "Дата финальной оплаты";
                dataTable.Columns["PaymentCompletedDate"].ExtendedProperties["Width"] = (double)17;
                dataTable.Columns.Add("Author", typeof(string)).Caption = "Инициатор";
                dataTable.Columns["Author"].ExtendedProperties["Width"] = (double)33;

                BitrixHelper bitrixHelper = new BitrixHelper(_bitrixConfigOptions);

                List<BitrixApplicationForPayment> bitrixApplicationForPaymentList = new List<BitrixApplicationForPayment>();

                string bitrixAFPListIDs = bitrixHelper.GetBitrixAFPListIDs();
                string bitrixRFTListID = bitrixHelper.GetBitrixRFTListID();

                Hashtable fieldDisplayValuesForListIDs = new Hashtable();

                if (String.IsNullOrEmpty(bitrixAFPListIDs) == false)
                {
                    string[] listIDs = bitrixAFPListIDs.Split(',');

                    int k = 1;
                    foreach (string listID in listIDs)
                    {
                        SetStatus(1, "Экспорт заявок - шаг " + k.ToString() + " из " + listIDs.Length);
                        bitrixApplicationForPaymentList.AddRange(bitrixHelper.GetBitrixAFPList(listID));
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


                SetStatus(1, "Получение списка пользователей Б24");
                List<BitrixUser> bitrixUserList = bitrixHelper.GetBitrixUserList();

                SetStatus(2, "Получение списка подстатей затрат");
                List<BitrixCSI> bitrixCSIList = bitrixHelper.GetBitrixCSIList();

                SetStatus(3, "Получение списка ЦФО");
                List<BitrixFRC> bitrixFRCList = bitrixHelper.GetBitrixFRCList();

                SetStatus(4, "Получение списка кодов проектов");
                List<BitrixProject> bitrixProjectList = bitrixHelper.GetBitrixProjectList();

                SetStatus(4, "Получение списка юридических лиц");
                List<BitrixOrganisation> bitrixOrganisationList = bitrixHelper.GetBitrixOrganisationList();


                SetStatus(4, "Получение списка компаний из CRM Б24");
                List<BitrixCRMCompany> bitrixCRMCompanyList = bitrixHelper.GetBitrixCRMCompanyList();

                if (String.IsNullOrEmpty(bitrixRFTListID) == false)
                {
                    SetStatus(4, "Экспорт заявок на командировки");
                    List<BitrixRequestForTrip> bitrixRFTList = bitrixHelper.GetBitrixRequestForTrip().ToList();

                    Dictionary<string, string> documentState_DisplayValues = bitrixHelper.GetBitrixListPropertyDisplayValuesForm(bitrixRFTListID, "DOCUMENT_STATE");
                    if (documentState_DisplayValues != null)
                    {
                        fieldDisplayValuesForListIDs.Add(bitrixRFTListID + "_DOCUMENT_STATE", documentState_DisplayValues);
                    }

                    foreach (var bitrixRFTListItem in bitrixRFTList)
                    {
                        try
                        {
                            if (bitrixRFTListItem != null)
                            {
                                var request_status = bitrixRFTListItem.DISPLAY_STATUS?.FirstOrDefault().Value;
                                /*if (request_status == null || !(request_status == "Командировка отменена" || request_status == "Командировка закрыта"))
                                    continue;*/

                                bitrixApplicationForPaymentList.AddRange(ExtractApplicationsForPaymentFromBitrixRequestForTrip(bitrixRFTListItem,
                                    bitrixCSIList,
                                    bitrixFRCList,
                                    bitrixProjectList).ToList());

                            }
                        }
                        catch (Exception)
                        {

                        }
                    }

                }

                SetStatus(5, "Фильтрация выборки заявок...");
                periodStart = periodStart.Date;
                periodEnd = new DateTime(periodEnd.Year, periodEnd.Month, periodEnd.Day, 23, 59, 59);
                List<BitrixApplicationForPayment> bitrixApplicationForPaymentListSorted = bitrixApplicationForPaymentList.Where(e => e.Created >= periodStart && e.Created <= periodEnd).OrderBy(e => e.Created).ToList();

                int j = 1;
                foreach (BitrixApplicationForPayment bitrixApplicationForPayment in bitrixApplicationForPaymentListSorted)
                {
                    string uRegNum = (bitrixApplicationForPayment.URegNum != null) ? bitrixApplicationForPayment.URegNum.FirstOrDefault().Value : "";
                    string urlCellValue = bitrixHelper.GetBitrixListElementUrl(bitrixApplicationForPayment.ID, bitrixApplicationForPayment.IBLOCK_ID);// "=ГИПЕРССЫЛКА(\"" + bitrixHelper.GetListElementUrl(bitrixApplicationForPayment.ID, bitrixApplicationForPayment.IBLOCK_ID) + "\")"; 

                    string typeName = (uRegNum.Contains("-")) ? uRegNum.Split('-')[0] : "";

                    string paymentForm = "";
                    if (fieldDisplayValuesForListIDs[bitrixApplicationForPayment.IBLOCK_ID + "_PaymentForm"] != null)
                    {
                        paymentForm = BitrixHelper.ParseBitrixListPropertyDisplayValueByID((Dictionary<string, string>)fieldDisplayValuesForListIDs[bitrixApplicationForPayment.IBLOCK_ID + "_PaymentForm"], bitrixApplicationForPayment.PaymentForm?.FirstOrDefault().Value);
                    }

                    double amount = 0;
                    string amountCurrency = "";
                    if (bitrixApplicationForPayment.AmountRuble != null
                        && String.IsNullOrEmpty(bitrixApplicationForPayment.AmountRuble.FirstOrDefault().Value) == false)
                    {
                        if (bitrixApplicationForPayment.AmountRuble.FirstOrDefault().Value.Contains("|"))
                        {
                            amount = Convert.ToDouble(bitrixApplicationForPayment.AmountRuble.FirstOrDefault().Value.Split('|')[0].Replace(".", ","));
                            amountCurrency = bitrixApplicationForPayment.AmountRuble.FirstOrDefault().Value.Split('|')[1];
                        }
                        else
                        {
                            amount = Convert.ToDouble(bitrixApplicationForPayment.AmountRuble.FirstOrDefault().Value.Replace("руб.", "").Replace(".", ","));
                            amountCurrency = "RUB";
                        }
                    }
                    else if (bitrixApplicationForPayment.Amount != null
                        && String.IsNullOrEmpty(bitrixApplicationForPayment.Amount.FirstOrDefault().Value) == false)
                    {
                        if (bitrixApplicationForPayment.Amount.FirstOrDefault().Value.Contains("|"))
                        {
                            amount = Convert.ToDouble(bitrixApplicationForPayment.Amount.FirstOrDefault().Value.Split('|')[0].Replace(".", ","));
                            amountCurrency = bitrixApplicationForPayment.Amount.FirstOrDefault().Value.Split('|')[1];
                        }
                        else
                        {
                            amount = Convert.ToDouble(bitrixApplicationForPayment.Amount.FirstOrDefault().Value.Replace("руб.", "").Replace(".", ","));
                            amountCurrency = "RUB";
                        }
                    }

                    string rateVAT = "";
                    if (fieldDisplayValuesForListIDs[bitrixApplicationForPayment.IBLOCK_ID + "_RateVAT"] != null)
                    {
                        rateVAT = BitrixHelper.ParseBitrixListPropertyDisplayValueByID((Dictionary<string, string>)fieldDisplayValuesForListIDs[bitrixApplicationForPayment.IBLOCK_ID + "_RateVAT"], bitrixApplicationForPayment.RateVAT?.FirstOrDefault().Value);
                    }

                    double amountWithoutVAT = 0;
                    double amountVAT = 0;


                    if (String.IsNullOrEmpty(paymentForm) == false && paymentForm.Equals("ОПЕР") == true)
                    {
                        rateVAT = "0%";

                        amountVAT = 0;

                        amountWithoutVAT = amount;
                    }
                    else
                    {
                        if (bitrixApplicationForPayment.AmountVAT != null
                            && String.IsNullOrEmpty(bitrixApplicationForPayment.AmountVAT.FirstOrDefault().Value) == false)
                        {
                            amountVAT = BitrixHelper.ParseBitrixListElementCurrencyOrNumberFieldValue(bitrixApplicationForPayment.AmountVAT?.FirstOrDefault().Value);

                            amountWithoutVAT = amount - amountVAT;

                            if (Math.Round(amountVAT, 2) == Math.Round(amount * 20 / 120, 2))
                            {
                                rateVAT = "20%";
                            }
                            else if (Math.Round(amountVAT, 2) == Math.Round(amount * 18 / 118, 2))
                            {
                                rateVAT = "18%";
                            }
                            else if (Math.Round(amountVAT, 2) == Math.Round(amount * 10 / 110, 2))
                            {
                                rateVAT = "10%";
                            }
                            else if (amountVAT == 0)
                            {
                                rateVAT = "0%";
                            }
                            else
                            {
                                rateVAT = "";
                            }
                        }
                        else
                        {
                            amountWithoutVAT = ((bitrixApplicationForPayment.AmountWithoutVAT != null) ? BitrixHelper.ParseBitrixListElementCurrencyOrNumberFieldValue(bitrixApplicationForPayment.AmountWithoutVAT?.FirstOrDefault().Value) : 0);

                            if (amountWithoutVAT != 0)
                            {
                                amountVAT = amount - amountWithoutVAT;
                            }
                        }
                    }

                    string documentState = "";
                    if (fieldDisplayValuesForListIDs[bitrixApplicationForPayment.IBLOCK_ID + "_DOCUMENT_STATE"] != null)
                    {
                        documentState = BitrixHelper.ParseBitrixListPropertyDisplayValueByID((Dictionary<string, string>)fieldDisplayValuesForListIDs[bitrixApplicationForPayment.IBLOCK_ID + "_DOCUMENT_STATE"], bitrixApplicationForPayment.DOCUMENT_STATE?.FirstOrDefault().Value);
                    }

                    string financialYear = "";
                    if (fieldDisplayValuesForListIDs[bitrixApplicationForPayment.IBLOCK_ID + "_FinancialYear"] != null)
                    {
                        financialYear = BitrixHelper.ParseBitrixListPropertyDisplayValueByID((Dictionary<string, string>)fieldDisplayValuesForListIDs[bitrixApplicationForPayment.IBLOCK_ID + "_FinancialYear"], bitrixApplicationForPayment.FinancialYear?.FirstOrDefault().Value);
                    }

                    string finResponsibilityCenter_ShortName = "";
                    string finResponsibilityCenter_ShortTitle = "";
                    string finResponsibilityCenter_Title = "";

                    if (bitrixApplicationForPayment.FinResponsibilityCenter != null
                        && String.IsNullOrEmpty(bitrixApplicationForPayment.FinResponsibilityCenter.FirstOrDefault().Value) == false)
                    {
                        string finResponsibilityCenterName = bitrixFRCList.Where(x => x.ID == bitrixApplicationForPayment.FinResponsibilityCenter.FirstOrDefault().Value).FirstOrDefault().NAME;  //bitrixHelper.GetBitrixFRCListElementNameByID(bitrixApplicationForPayment.FinResponsibilityCenter.FirstOrDefault().Value);
                        if (finResponsibilityCenterName != null &&
                            finResponsibilityCenterName.Contains("."))
                        {
                            string[] tokens = finResponsibilityCenterName.Split('.');
                            if (tokens.Length == 3)
                            {
                                finResponsibilityCenter_ShortName = tokens[0].Trim();
                                finResponsibilityCenter_ShortTitle = tokens[1].Trim();
                                finResponsibilityCenter_Title = tokens[2].Trim();
                            }
                        }
                    }

                    string costSubitem_ShortName = "";
                    string costSubitem_Title = "";

                    if (bitrixApplicationForPayment.CostSubitem != null
                        && String.IsNullOrEmpty(bitrixApplicationForPayment.CostSubitem.FirstOrDefault().Value) == false)
                    {
                        string costSubitemName = bitrixCSIList.Where(x => x.ID == bitrixApplicationForPayment.CostSubitem.FirstOrDefault().Value).FirstOrDefault().NAME; //bitrixHelper.GetBitrixCSIListElementNameByID(bitrixApplicationForPayment.CostSubitem.FirstOrDefault().Value);
                        if (String.IsNullOrEmpty(costSubitemName) == false
                            && costSubitemName.Length >= 9)
                        {
                            costSubitem_ShortName = costSubitemName.Substring(0, 7);
                            costSubitem_Title = costSubitemName.Substring(9).Trim();
                        }
                    }

                    string projectShortName = "";

                    if (bitrixApplicationForPayment.Project != null
                        && String.IsNullOrEmpty(bitrixApplicationForPayment.Project.FirstOrDefault().Value) == false)
                    {
                        projectShortName = bitrixProjectList.Where(x => x.ID == bitrixApplicationForPayment.Project.FirstOrDefault().Value).FirstOrDefault().NAME; //bitrixHelper.GetBitrixProjectListElementNameByID(bitrixApplicationForPayment.Project.FirstOrDefault().Value);
                    }
                    else if (bitrixApplicationForPayment.ProjectShortName != null
                        && String.IsNullOrEmpty(bitrixApplicationForPayment.ProjectShortName.FirstOrDefault().Value) == false)
                    {
                        projectShortName = bitrixApplicationForPayment.ProjectShortName.FirstOrDefault().Value;
                    }

                    string organisationTitle = "";

                    if (bitrixApplicationForPayment.Organisation != null
                        && String.IsNullOrEmpty(bitrixApplicationForPayment.Organisation.FirstOrDefault().Value) == false)
                    {
                        BitrixOrganisation bitrixOrganisation = bitrixOrganisationList.Where(x => x.ID == bitrixApplicationForPayment.Organisation.FirstOrDefault().Value).FirstOrDefault();
                        if (bitrixOrganisation != null)
                        {
                            organisationTitle = bitrixOrganisation.NAME;
                        }
                    }


                    string contragentTitle = "";

                    if (bitrixApplicationForPayment.Contragent != null
                        && String.IsNullOrEmpty(bitrixApplicationForPayment.Contragent.FirstOrDefault().Value) == false)
                    {
                        BitrixCRMCompany bitrixCRMCompany = bitrixCRMCompanyList.Where(x => x.ID == bitrixApplicationForPayment.Contragent.FirstOrDefault().Value).FirstOrDefault(); //bitrixHelper.GetBitrixCRMCompanyByID(bitrixApplicationForPayment.Contragent.FirstOrDefault().Value);
                        if (bitrixCRMCompany != null)
                        {
                            contragentTitle = bitrixCRMCompany.TITLE;
                        }
                    }
                    else if (bitrixApplicationForPayment.ContragentText != null
                        && String.IsNullOrEmpty(bitrixApplicationForPayment.ContragentText.FirstOrDefault().Value) == false)
                    {
                        contragentTitle = bitrixApplicationForPayment.ContragentText.FirstOrDefault().Value;
                    }

                    double actualPaymentAmount = 0;
                    double actualPaymentAmountWithoutVAT = 0;
                    double actualPaymentVAT = 0;
                    string actualPaymentAmountCurrency = "";
                    if (bitrixApplicationForPayment.OverallPaymentAmount != null
                        && String.IsNullOrEmpty(bitrixApplicationForPayment.OverallPaymentAmount.FirstOrDefault().Value) == false)
                    {
                        if (bitrixApplicationForPayment.OverallPaymentAmount.FirstOrDefault().Value.Contains("|"))
                        {
                            actualPaymentAmount = Convert.ToDouble(bitrixApplicationForPayment.OverallPaymentAmount.FirstOrDefault().Value.Split('|')[0].Replace(".", ","));
                            actualPaymentAmountCurrency = bitrixApplicationForPayment.OverallPaymentAmount.FirstOrDefault().Value.Split('|')[1];
                        }
                        else
                        {
                            actualPaymentAmount = Convert.ToDouble(bitrixApplicationForPayment.OverallPaymentAmount.FirstOrDefault().Value.Replace("руб.", "").Replace(".", ","));
                            actualPaymentAmountCurrency = "RUB";
                        }

                        if (bitrixApplicationForPayment.OverallPaymentAmountWithoutVAT != null
                            && String.IsNullOrEmpty(bitrixApplicationForPayment.OverallPaymentAmountWithoutVAT.FirstOrDefault().Value) == false)
                        {
                            if (bitrixApplicationForPayment.OverallPaymentAmountWithoutVAT.FirstOrDefault().Value.Contains("|"))
                            {
                                actualPaymentAmountWithoutVAT = Convert.ToDouble(bitrixApplicationForPayment.OverallPaymentAmountWithoutVAT.FirstOrDefault().Value.Split('|')[0].Replace(".", ","));
                            }
                            else
                            {
                                actualPaymentAmountWithoutVAT = Convert.ToDouble(bitrixApplicationForPayment.OverallPaymentAmountWithoutVAT.FirstOrDefault().Value.Replace("руб.", "").Replace(".", ","));
                            }
                        }

                        if (actualPaymentAmountWithoutVAT != 0)
                        {
                            actualPaymentVAT = actualPaymentAmount - actualPaymentAmountWithoutVAT;
                        }
                    }
                    else if (bitrixApplicationForPayment.ActualPaymentAmount != null
                        && String.IsNullOrEmpty(bitrixApplicationForPayment.ActualPaymentAmount.FirstOrDefault().Value) == false)
                    {
                        if (bitrixApplicationForPayment.ActualPaymentAmount.FirstOrDefault().Value.Contains("|"))
                        {
                            actualPaymentAmount = Convert.ToDouble(bitrixApplicationForPayment.ActualPaymentAmount.FirstOrDefault().Value.Split('|')[0].Replace(".", ","));
                            actualPaymentAmountCurrency = bitrixApplicationForPayment.ActualPaymentAmount.FirstOrDefault().Value.Split('|')[1];
                        }
                        else
                        {
                            actualPaymentAmount = Convert.ToDouble(bitrixApplicationForPayment.ActualPaymentAmount.FirstOrDefault().Value.Replace("руб.", "").Replace(".", ","));
                            actualPaymentAmountCurrency = "RUB";
                        }

                        if (bitrixApplicationForPayment.ActualPaymentAmountWithoutVAT != null
                            && String.IsNullOrEmpty(bitrixApplicationForPayment.ActualPaymentAmountWithoutVAT.FirstOrDefault().Value) == false)
                        {
                            if (bitrixApplicationForPayment.ActualPaymentAmountWithoutVAT.FirstOrDefault().Value.Contains("|"))
                            {
                                actualPaymentAmountWithoutVAT = Convert.ToDouble(bitrixApplicationForPayment.ActualPaymentAmountWithoutVAT.FirstOrDefault().Value.Split('|')[0].Replace(".", ","));
                            }
                            else
                            {
                                actualPaymentAmountWithoutVAT = Convert.ToDouble(bitrixApplicationForPayment.ActualPaymentAmountWithoutVAT.FirstOrDefault().Value.Replace("руб.", "").Replace(".", ","));
                            }
                        }

                        if (actualPaymentAmountWithoutVAT != 0)
                        {
                            actualPaymentVAT = actualPaymentAmount - actualPaymentAmountWithoutVAT;
                        }
                    }

                    string author = "";
                    if (String.IsNullOrEmpty(bitrixApplicationForPayment.CREATED_BY) == false)
                    {
                        BitrixUser bitrixUser = bitrixUserList.Where(x => x.ID == bitrixApplicationForPayment.CREATED_BY).FirstOrDefault(); //bitrixHelper.GetBitrixUserByID(bitrixApplicationForPayment.CREATED_BY);
                        if (bitrixUser != null)
                        {
                            author = bitrixUser.LAST_NAME + " " + bitrixUser.NAME;
                        }
                    }

                    dataTable.Rows.Add(bitrixApplicationForPayment.ID,
                        uRegNum,
                        urlCellValue,
                        typeName,
                        finResponsibilityCenter_ShortName,
                        finResponsibilityCenter_ShortTitle,
                        finResponsibilityCenter_Title,
                        costSubitem_ShortName,
                        costSubitem_Title,
                        projectShortName,
                        bitrixApplicationForPayment.NAME,
                        (String.IsNullOrEmpty(bitrixApplicationForPayment.DATE_CREATE) == false) ? Convert.ToDateTime(bitrixApplicationForPayment.DATE_CREATE) as object : null,
                        (bitrixApplicationForPayment.DeliveryDate != null) ? Convert.ToDateTime(bitrixApplicationForPayment.DeliveryDate.FirstOrDefault().Value) as object : null,
                        paymentForm,
                        organisationTitle,
                        amount,
                        amountCurrency,
                        rateVAT,
                        amountWithoutVAT,
                        amountVAT,
                        contragentTitle,
                        documentState,
                        (bitrixApplicationForPayment.APPROVE_DATE != null) ? Convert.ToDateTime(bitrixApplicationForPayment.APPROVE_DATE.FirstOrDefault().Value) as object : null,
                        (String.IsNullOrEmpty(financialYear) == false) ? financialYear : ((String.IsNullOrEmpty(bitrixApplicationForPayment.DATE_CREATE) == false) ? Convert.ToDateTime(bitrixApplicationForPayment.DATE_CREATE).Year.ToString() : ""),
                        (bitrixApplicationForPayment.ActualPaymentDate != null) ? Convert.ToDateTime(bitrixApplicationForPayment.ActualPaymentDate.FirstOrDefault().Value) as object : null,
                        actualPaymentAmount,
                        actualPaymentAmountWithoutVAT,
                        actualPaymentVAT,
                        actualPaymentAmountCurrency,
                        (bitrixApplicationForPayment.PAYMENT_COMPLETED_DATE != null) ? Convert.ToDateTime(bitrixApplicationForPayment.PAYMENT_COMPLETED_DATE.FirstOrDefault().Value) as object : null,
                        author);

                    SetStatus(5 + 93 * j / bitrixApplicationForPaymentListSorted.Count, "Добавление заявки в выборку: "
                        + ((String.IsNullOrEmpty(uRegNum) == false) ? uRegNum : "ID: " + bitrixApplicationForPayment.ID.ToString()));
                    j++;
                }

                SetStatus(98, "Формирование файла MS Excel...");

                string reportTitle = "Выгрузка данных о расходах из Б24 за период: " + periodStart.ToString("yyyy-MM-dd") + " - " + periodEnd.ToString("yyyy-MM-dd");

                using (MemoryStream stream = new MemoryStream())
                {
                    using (SpreadsheetDocument doc = SpreadsheetDocument.Create(stream, SpreadsheetDocumentType.Workbook))
                    {
                        WorkbookPart workbookPart = ExcelHelper.CreateWorkbookPart(doc, "Отчет");

                        WorksheetPart rId1 = ExcelHelper.CreateWorksheetPartAndImportDataTable(workbookPart, "rId1", 1, 1, (uint)dataTable.Columns.Count,
                            reportTitle, dataTable, 3, 1);

                        doc.WorkbookPart.Workbook.Save();
                    }

                    stream.Position = 0;
                    BinaryReader b = new BinaryReader(stream);
                    binData = b.ReadBytes((int)stream.Length);


                }

                SetStatus(100, "Отчет сформирован");
            }
            catch (Exception e)
            {
                SetStatus(-1, "Ошибка: " + e.Message.Replace("\r", "").Replace("\n", " "));
                htmlErrorReport += "<br>" + e.Message + "<br>" + e.StackTrace + "<br>" + e.TargetSite.ToString();
            }

            return new ReportGeneratorResult() { fileId = id, fileBinData = binData, htmlErrorReport = htmlErrorReport };
        }

        private List<BitrixApplicationForPayment> ExtractApplicationsForPaymentFromBitrixRequestForTrip(BitrixRequestForTrip bitrixRFTListItem,
            List<BitrixCSI> bitrixCSIList,
            List<BitrixFRC> bitrixFRCList,
            List<BitrixProject> bitrixProjectList)
        {
            var result = new List<BitrixApplicationForPayment>();
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
                            project = _projectService.Get(p => p.Where(x => x.ShortName == projectTitle).ToList()).FirstOrDefault();
                        }
                        if (project == null)
                            continue;

                        if (!project.DepartmentID.HasValue)
                            continue;

                        Department department = _departmentService.GetById(project.DepartmentID.Value);

                        if (!project.ProjectTypeID.HasValue)
                            continue;

                        int? costSubItemID = project.ProjectType.BusinessTripCostSubItemID;
                        if (costSubItemID == null)
                            continue;

                        CostSubItem costSubItem = _costSubItemService.GetById(costSubItemID.Value);

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

                        BitrixCSI bitrixCSI = bitrixCSIList.Where(x => x.NAME.StartsWith(costSubItem.ShortName)).FirstOrDefault();
                        BitrixFRC bitrixFRC = bitrixFRCList.Where(x => x.NAME.StartsWith(department.ShortName)).FirstOrDefault();

                        var bitrixApplicationForPayment = new BitrixApplicationForPayment
                        {

                            ID = bitrixRFTListItem.ID,
                            IBLOCK_ID = bitrixRFTListItem.IBLOCK_ID,
                            NAME = expensesRecordName,
                            IBLOCK_SECTION_ID = bitrixRFTListItem.IBLOCK_SECTION_ID,
                            CREATED_BY = bitrixRFTListItem.CREATED_BY,
                            BP_PUBLISHED = bitrixRFTListItem.BP_PUBLISHED,
                            CODE = bitrixRFTListItem.CODE,
                            DATE_CREATE = bitrixRFTListItem.DATE_CREATE,

                            URegNum = bitrixRFTListItem.URegNum,
                            FinResponsibilityCenter = (bitrixFRC != null) ? new Dictionary<string, string>() { { "FinResponsibilityCenter", bitrixFRC.ID } } : null,
                            CostSubitem = (bitrixCSI != null) ? new Dictionary<string, string>() { { "CostSubitem", bitrixCSI.ID } } : null,
                            DeliveryDate = null,

                            Amount = new Dictionary<string, string>() { { "Amount", (total_planned_amount * load / 100).ToString() } },
                            AmountRuble = null,
                            RateVAT = null,
                            AmountWithoutVAT = new Dictionary<string, string>() { { "AmountWithoutVAT", (total_planned_amount * load / 100).ToString() } },
                            AmountVAT = null,

                            PaymentForm = null,

                            Contragent = null,
                            ContragentText = null,
                            ProjectShortName = new Dictionary<string, string>() { { "ProjectShortName", project.ShortName } },
                            Project = null,
                            ActualPaymentDate = (total_actual_amount != 0) ? new Dictionary<string, string>() { { "ActualPaymentDate", bitrixRFTListItem.Updated.Value.ToString() } } : null,

                            ActualPaymentAmount = new Dictionary<string, string>() { { "ActualPaymentAmount", (total_actual_amount * load / 100).ToString() } },
                            ActualPaymentAmountWithoutVAT = new Dictionary<string, string>() { { "ActualPaymentAmountWithoutVAT", (total_actual_amount * load / 100).ToString() } },
                            ActualPaymentVAT = null,

                            OverallPaymentAmount = new Dictionary<string, string>() { { "OverallPaymentAmount", (total_actual_amount * load / 100).ToString() } },
                            OverallPaymentAmountWithoutVAT = new Dictionary<string, string>() { { "OverallPaymentAmountWithoutVAT", (total_actual_amount * load / 100).ToString() } },
                            OverallPaymentAmountVAT = null,

                            FinancialYear = null,

                            DOCUMENT_STATE = bitrixRFTListItem.DOCUMENT_STATE,
                            APPROVE_DATE = bitrixRFTListItem.APPROVE_DATE, //(total_actual_amount != 0) ? new Dictionary<string, string>() { { "APPROVE_DATE", bitrixRFTListItem.Updated.Value.ToString() } } : null,
                            PAYMENT_COMPLETED_DATE = bitrixRFTListItem.PAYMENT_COMPLETED_DATE,// //(total_actual_amount != 0) ? new Dictionary<string, string>() { { "PAYMENT_COMPLETED_DATE", bitrixRFTListItem.Updated.Value.ToString() } } : null,
                            DISPLAY_STATUS = bitrixRFTListItem.DISPLAY_STATUS
                        };
                        result.Add(bitrixApplicationForPayment);
                    }
                }
            }

            return result;
        }
    }
}