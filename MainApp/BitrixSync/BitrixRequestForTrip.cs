using System;
using System.Collections.Generic;

namespace MainApp.BitrixSync
{
    public class BitrixRequestForTrip : BitrixListElement
    {
        public Dictionary<string, string> URegNum;
        public Dictionary<string, string> BlockProjects;
        public Dictionary<string, string> Project;
        public Dictionary<string, string> CommittedGeneral;
        public Dictionary<string, string> CommittedNotDirectory;
        public Dictionary<string, string> StartDateTrip;
        public Dictionary<string, string> EndDateTrip;
        public string TIMESTAMP_X;
        public Dictionary<string, string> AssignmentPercentage;
        public Dictionary<string, string> CostAmountTripInfo;
        public Dictionary<string, string> TOTAL_ACTUAL_COSTS_AMOUNT;
        public Dictionary<string, string> DOCUMENT_STATE;
        public Dictionary<string, string> APPROVE_DATE;
        public Dictionary<string, string> PAYMENT_COMPLETED_DATE;
        public Dictionary<string, string> DISPLAY_STATUS;

        public DateTime? Updated
        {
            get
            {
                if (String.IsNullOrEmpty(TIMESTAMP_X) == false)
                {
                    try
                    {
                        return Convert.ToDateTime(TIMESTAMP_X);
                    }
                    catch (Exception)
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