using System.Collections.Generic;

namespace MainApp.BitrixSync
{
    public class BitrixReqPayrollChange : BitrixListElement
    {
        public Dictionary<string, string> RegNum;
        public Dictionary<string, string> EMPLOYEE;
        public Dictionary<string, string> AUTHOR_COMMENT;
        public Dictionary<string, string> COMMENT_DEPARTMENT_HEAD;
        public Dictionary<string, string> DOCUMENT_STATE;
        public Dictionary<string, string> DISPLAY_STATUS;
        public Dictionary<string, string> DEPARTMENT_HEAD_APPROVED;
        public Dictionary<string, string> CURATOR_FRC_APPROVED;
        public Dictionary<string, string> HR_HEAD_APPROVED;
        public Dictionary<string, string> FINANCE_AND_ACCOUNTING_APPROVED;
        public Dictionary<string, string> CEO_APPROVED;


        public Dictionary<string, string> DEPARTMENT_HEAD_APPROVED_BP_TASK_COMPLETED;
        public Dictionary<string, string> CURATOR_FRC_APPROVED_BP_TASK_COMPLETED;
        public Dictionary<string, string> HR_HEAD_APPROVED_BP_TASK_COMPLETED;
        public Dictionary<string, string> FINANCE_AND_ACCOUNTING_APPROVED_BP_TASK_COMPLETED;
        public Dictionary<string, string> CEO_APPROVED_BP_TASK_COMPLETED;
    }
}
