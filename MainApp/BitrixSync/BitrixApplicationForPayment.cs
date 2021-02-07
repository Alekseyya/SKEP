using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace MainApp.BitrixSync
{
    public class BitrixApplicationForPayment : BitrixListElement
    {
        public Dictionary<string, string> URegNum;
        public Dictionary<string, string> FinResponsibilityCenter;
        public Dictionary<string, string> CostSubitem;
        public Dictionary<string, string> DeliveryDate;

        public Dictionary<string, string> Amount;
        public Dictionary<string, string> AmountRuble;
        public Dictionary<string, string> RateVAT;
        public Dictionary<string, string> AmountWithoutVAT;
        public Dictionary<string, string> AmountVAT;

        public Dictionary<string, string> PaymentForm;

        public Dictionary<string, string> Organisation;
        public Dictionary<string, string> Contragent;
        public Dictionary<string, string> ContragentText;
        public Dictionary<string, string> ProjectShortName;
        public Dictionary<string, string> Project;
        public Dictionary<string, string> ActualPaymentDate;
        public Dictionary<string, string> ActualPaymentAmount;
        public Dictionary<string, string> ActualPaymentAmountWithoutVAT;
        public Dictionary<string, string> ActualPaymentVAT;

        public Dictionary<string, string> OverallPaymentAmount;
        public Dictionary<string, string> OverallPaymentAmountWithoutVAT;
        public Dictionary<string, string> OverallPaymentAmountVAT;

        public Dictionary<string, string> FinancialYear;

        public Dictionary<string, string> DOCUMENT_STATE;
        public Dictionary<string, string> APPROVE_DATE;
        public Dictionary<string, string> PAYMENT_COMPLETED_DATE;
        public Dictionary<string, string> DISPLAY_STATUS;


        public DateTime? Created
        {
            get
            {
                if (String.IsNullOrEmpty(DATE_CREATE) == false)
                {
                    try
                    {
                        return Convert.ToDateTime(DATE_CREATE);
                    }
                    catch(Exception)
                    {
                        return null;
                    }
                }
                else
                {
                    return null;
                }
            }
        }
    }
}