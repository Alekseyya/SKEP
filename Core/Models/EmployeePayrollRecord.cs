using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Core.Models;


namespace Core.Models
{
    public enum EmployeePayrollRecordType
    {
        [Display(Name = "-", ShortName = "-")] None = 0,
        [Display(Name = "ПРИЕМ", ShortName = "ПРИЕМ")] Enrollment = 10,
        [Display(Name = "ИЗПГ", ShortName = "ИЗПГ")] PayrollChange = 20,
        [Display(Name = "Руководитель подразделения", ShortName = "ИЗПГ.РукПод")] PayrollChangeHD = 30,
        [Display(Name = "Куратор подразделения", ShortName = "ИЗПГ.КураторЦФО")] PayrollChangeHDCurator = 40,
        [Display(Name = "HR-директор", ShortName = "ИЗПГ.HR")] PayrollChangeHR = 50,
        [Display(Name = "ВП ФиБ", ShortName = "ИЗПГ.ФиБ")] PayrollChangeFin = 60,
        [Display(Name = "Президент ГК", ShortName = "ИЗПГ.ПрезидентГК")] PayrollChangeCEO = 70
    }

    public static class EmployeePayrollRecordTypeHelper
    {
        public static EmployeePayrollRecordType GetByDisplayShortName(string shortName)
        {
            var enums = Enum.GetValues(typeof(EmployeePayrollRecordType)).Cast<EmployeePayrollRecordType>();
            foreach (var value in enums)
            {
                var displayName = value.GetType().GetMember(value.ToString()).First().GetCustomAttribute<DisplayAttribute>();
                if (displayName.ShortName.Equals(shortName.Trim(), StringComparison.OrdinalIgnoreCase))
                    return value;
            }

            return EmployeePayrollRecordType.None;
        }

        public static string GetDisplayShortNameFor(EmployeePayrollRecordType type)
        {
            return type.GetType().GetMember(type.ToString()).First().GetCustomAttribute<DisplayAttribute>().ShortName;
        }

        public static string GetDisplayNameFor(EmployeePayrollRecordType type)
        {
            return type.GetType().GetMember(type.ToString()).First().GetCustomAttribute<DisplayAttribute>().Name;
        }

        public static int GetIndexForType(EmployeePayrollRecordType type)
        {
            var enums = Enum.GetValues(typeof(EmployeePayrollRecordType)).Cast<EmployeePayrollRecordType>();
            for (int i = 0; i < enums.Count(); i++)
            {
                if (type == enums.ElementAt(i))
                    return i;
            }
            return -1;
        }
    }

    public enum EmployeePayrollRecordActionFormMode
    {
        /*
        [Display(Name = "Согласование изменений")] Approve = 10,
        [Display(Name = "Предложить корректировки")] ApproveWithSuggestions = 20,
        [Display(Name = "Отклонение изменений")] Reject = 30
        */

        [Display(Name = "")] None = 0,
        [Display(Name = "Внести предложение")] InputSuggestion = 5,
        [Display(Name = "Согласовать")] Approve = 10,
        [Display(Name = "Утвердить версию Руководителя подразделения")] FinalApproveHDVersion = 20,
        [Display(Name = "Утвердить версию Куратора ЦФО")] FinalApproveHDCuratorVersion = 30,
        [Display(Name = "Предложить корректировки")] ApproveWithSuggestions = 40,
        [Display(Name = "Утвердить со своими корректировками")] FinalApproveWithSuggestions = 50,
        [Display(Name = "Отклонить")] Reject = 60,
    }

    public static class EmployeePayrollRecordActionFormModeHelper
    {
        public static List<KeyValuePair<int, string>> GetNamesForActionFormMode()
        {
            var values = Enum.GetValues(typeof(EmployeePayrollRecordActionFormMode)).Cast<EmployeePayrollRecordActionFormMode>().OrderBy(v => v);
            var list = new List<KeyValuePair<int, string>>();
            foreach (var value in values)
            {
                var name = value.GetType().GetMember(value.ToString()).First().GetCustomAttribute<DisplayAttribute>().Name;
                list.Add(new KeyValuePair<int, string>((int)value, name));
            }
            return list;
        }

        public static int GetActionFormModeIndex(EmployeePayrollRecordActionFormMode formMode)
        {
            var values = Enum.GetValues(typeof(EmployeePayrollRecordActionFormMode)).Cast<EmployeePayrollRecordActionFormMode>().OrderBy(v => v);
            for (int i = 0; i < values.Count(); i++)
            {
                if (values.ElementAt(i) == formMode)
                    return i;
            }
            return -1;
        }

        public static EmployeePayrollRecordActionFormMode? GetByValue(int value)
        {
            try
            {
                return (EmployeePayrollRecordActionFormMode)value;
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        public static EmployeePayrollRecordResult MapToRecordResult(EmployeePayrollRecordActionFormMode formMode)
        {
            switch (formMode)
            {
                case EmployeePayrollRecordActionFormMode.InputSuggestion:
                    return EmployeePayrollRecordResult.ApprovedWithSuggestions;
                case EmployeePayrollRecordActionFormMode.Approve:
                    return EmployeePayrollRecordResult.Approved;
                case EmployeePayrollRecordActionFormMode.ApproveWithSuggestions:
                    return EmployeePayrollRecordResult.ApprovedWithSuggestions;
                case EmployeePayrollRecordActionFormMode.FinalApproveHDCuratorVersion:
                    return EmployeePayrollRecordResult.FinalApprovedHDCuratorVersion;
                case EmployeePayrollRecordActionFormMode.FinalApproveHDVersion:
                    return EmployeePayrollRecordResult.FinalApprovedHDVersion;
                case EmployeePayrollRecordActionFormMode.FinalApproveWithSuggestions:
                    return EmployeePayrollRecordResult.FinalApprovedWithSuggestions;
                case EmployeePayrollRecordActionFormMode.Reject:
                    return EmployeePayrollRecordResult.Rejected;
                default:
                    return EmployeePayrollRecordResult.None;
            }
        }
    }

    public enum EmployeePayrollRecordResult
    {
        [Display(Name = "-", ShortName = "-")] None = 0,
        [Display(Name = "Согласовано", ShortName = "Согласовано")] Approved = 30,
        [Display(Name = "Согласовано с корректировками", ShortName = "Согласовано.Корр")] ApprovedWithSuggestions = 40,
        [Display(Name = "Отклонено", ShortName = "Отклонено")] Rejected = 50,
        [Display(Name = "Утвеждена версия Руководителя подразделения", ShortName = "Утверждено.Вер.РукПод")] FinalApprovedHDVersion = 60,
        [Display(Name = "Утвеждена версия Куратора", ShortName = "Утверждено.Вер.Куратора")] FinalApprovedHDCuratorVersion = 70,
        [Display(Name = "Утвеждено с корректировками", ShortName = "Утверждено.Корр")] FinalApprovedWithSuggestions = 80
    }

    public static class EmployeePayrollRecordResultHelper
    {
        public static EmployeePayrollRecordResult GetByDisplayShortName(string shortName)
        {
            var enums = Enum.GetValues(typeof(EmployeePayrollRecordResult)).Cast<EmployeePayrollRecordResult>();
            foreach (var value in enums)
            {
                var displayName = value.GetType().GetMember(value.ToString()).First().GetCustomAttribute<DisplayAttribute>();
                if (displayName.ShortName.Equals(shortName.Trim(), StringComparison.OrdinalIgnoreCase))
                    return value;
            }

            return EmployeePayrollRecordResult.None;
        }

        public static string GetDisplayShortNameFor(EmployeePayrollRecordResult type)
        {
            return type.GetType().GetMember(type.ToString()).First().GetCustomAttribute<DisplayAttribute>().ShortName;
        }

        public static EmployeePayrollRecordActionFormMode MapToFormMode(EmployeePayrollRecordResult recordResult)
        {
            switch (recordResult)
            {
                case EmployeePayrollRecordResult.Approved:
                    return EmployeePayrollRecordActionFormMode.Approve;
                case EmployeePayrollRecordResult.ApprovedWithSuggestions:
                    return EmployeePayrollRecordActionFormMode.ApproveWithSuggestions;
                case EmployeePayrollRecordResult.FinalApprovedHDCuratorVersion:
                    return EmployeePayrollRecordActionFormMode.FinalApproveHDCuratorVersion;
                case EmployeePayrollRecordResult.FinalApprovedHDVersion:
                    return EmployeePayrollRecordActionFormMode.FinalApproveHDVersion;
                case EmployeePayrollRecordResult.FinalApprovedWithSuggestions:
                    return EmployeePayrollRecordActionFormMode.FinalApproveWithSuggestions;
                case EmployeePayrollRecordResult.Rejected:
                    return EmployeePayrollRecordActionFormMode.Reject;
                default:
                    return EmployeePayrollRecordActionFormMode.Approve;
            }
        }
    }
}

    public class EmployeePayrollRecord
    {
        [Display(Name = "ИД")]
        public int ID { get; set; }
        [Display(Name = "ИД Сотрудника")]
        public int EmployeeID { get; set; }
        public Employee Employee { get; set; }

        [Display(Name = "Дата последнего изменения")]
        [DisplayFormat(DataFormatString = "{0:dd.MM.yyyy}")]
        public DateTime? PayrollChangeDate { get; set; }
        public DateTime? Created { get; set; }

        [Display(Name = "Подразделение")]
        public string DepartmentName { get; set; }

        [Display(Name = "ID подразделения")]
        public int? DepartmentID { get; set; }
    
        [Display(Name = "Ежемесячная выплата")]
        public double? PayrollValue { get; set; }
        [Display(Name = "Ежеквартальная выплата")]
        public double? PayrollQuarterValue { get; set; }
        [Display(Name = "Полугодовая выплата")]
        public double? PayrollHalfYearValue { get; set; }
        [Display(Name = "Годовая выплата")]
        public double? PayrollYearValue { get; set; }

        public string Comments { get; set; }
        public string SourceElementID { get; set; }
        public string URegNum { get; set; }
        [Display(Name = "Грейд")]
        public int? EmployeeGrad { get; set; }
        //public string EmployeeGradName { get; set; }
        [Display(Name = "Комментарий")]
        public string UserComment { get; set; }
        [Display(Name = "Особое мнение")]
        public string UserSpecialComment { get; set; }
        public string AuthorFullName { get; set; }
        public EmployeePayrollRecordType RecordType { get; set; }
        public EmployeePayrollRecordResult RecordResult { get; set; }

        public string PayrollTypeAutoComments
        {
            get
            {
                return (PayrollValue >= 0) ? "Оклад" : "Ставка в час";
            }
        }

        public string GetRecordTypeName()
        {
            return EmployeePayrollRecordTypeHelper.GetDisplayNameFor(RecordType);
        }

        public string GetDisplayNameForField<TValue>(Expression<Func<EmployeePayrollRecord, TValue>> expression, string defaultName = "")
        {
            string exprName = expression.Parameters.FirstOrDefault().Name;
            string exprBody = expression.Body.ToString();

            string fieldName = exprBody.Replace(exprName + ".", "");
            var prop = GetType().GetProperty(fieldName);
            if (Attribute.IsDefined(prop, typeof(DisplayAttribute)) && string.IsNullOrEmpty(defaultName))
                return GetType().GetProperty(fieldName).GetCustomAttribute<DisplayAttribute>().Name;
            else
                return defaultName;
        }

        //public string PaymentMethodProbation { get; set; }
        public string PaymentMethod { get; set; }
        public string AdditionallyInfo { get; set; }

        private const int maxCountFirstKeySegmentSymbols = 16;
        private const string stubValue = "0";

        private string GetFormatValue(string value)
        {
            value = value == null ? string.Empty : value;
            if (value.Length < maxCountFirstKeySegmentSymbols)
            {
                int additionalSymbolCount = maxCountFirstKeySegmentSymbols - value.Length;
                var builder = new System.Text.StringBuilder();
                builder.Append(stubValue[0], additionalSymbolCount);
                builder.Append(value);
                return builder.ToString();
            }
            return value;
        }

        public string RecordGroupKey
        {
            get
            {
                if (RecordType == EmployeePayrollRecordType.PayrollChange)
                {
                    return GetFormatValue(SourceElementID) + "." + ((int)RecordType).ToString() + "." + PayrollChangeDate.Value.ToString("yyyyMMdd");
                }
                return GetFormatValue(SourceElementID) + ".";
            }
        }

        public string RecordSortKey
        {
            get
            {
                return GetFormatValue(SourceElementID) + "." + ((int)RecordType).ToString() + "." + PayrollChangeDate.Value.ToString("yyyyMMdd");
            }
        }

        public enum ReqPayrollChangeStatus
        {
            [Display(Name = "Утверждено")] Approved,
            [Display(Name = "На согласовании")] OnAgreement,
            [Display(Name = "Отменено")] Rejected
        }

        public class EmployeeReqPayrollChange
        {
            public EmployeePayrollRecord Record { get; private set; }
            public Department Department { get; private set; }
            public double EmployeePayrollChange { get; private set; }
            public ReqPayrollChangeStatus Status { get; private set; }

            public EmployeeReqPayrollChange(EmployeePayrollRecord record, Department department, double employeePayrollChange, ReqPayrollChangeStatus status)
            {
                Record = record;
                Department = department;
                EmployeePayrollChange = employeePayrollChange;
                Status = status;
            }

            public string GetStatusDisplayName() => Status.GetType().GetMember(Status.ToString()).First().GetCustomAttribute<DisplayAttribute>().Name;
        }
}
