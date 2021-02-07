using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Core.Models.Attributes;
using Core.Models.RBAC;


namespace Core.Models
{
    [AllowRecycleBin]
    [DisplayTableName("Подразделения")]
    public class Department : BaseModel
    {
        [Display(Name = "ИД")]
        public int ID { get; set; }

        [Required]
        [Display(Name = "Код")]
        public string ShortName { get; set; }

        [Display(Name = "Сокращение")]
        public string ShortTitle { get; set; }

        [Required]
        [Display(Name = "Наименование")]
        public string Title { get; set; }

        [Display(Name = "Вышестоящее подразделение")]
        public int? ParentDepartmentID { get; set; }
        public virtual Department ParentDepartment { get; set; }

        [Display(Name = "Организация")]
        public int? OrganisationID { get; set; }
        public virtual Organisation Organisation { get; set; }

        [Display(Name = "Примечание")]
        [DataType(DataType.MultilineText)]
        public string Comments { get; set; }

        [Display(Name = "Руководитель")]
        public int? DepartmentManagerID { get; set; }
        public virtual Employee DepartmentManager { get; set; }

        [Display(Name = "Администратор подразделения")]
        public int? DepartmentManagerAssistantID { get; set; }
        public virtual Employee DepartmentManagerAssistant { get; set; }

        [Display(Name = "Администратор проектов подразделения")]
        public int? DepartmentPAID { get; set; }
        public virtual Employee DepartmentPA { get; set; }

        [Required]
        [Display(Name = "Является ЦФО")]
        public bool IsFinancialCentre { get; set; }

        [Required]
        [Display(Name = "Наличие ежеквартальных выплат")]
        public bool UsePayrollQuarterValue { get; set; }

        [Required]
        [Display(Name = "Наличие полугодовых выплат")]
        public bool UsePayrollHalfYearValue { get; set; }

        [Required]
        [Display(Name = "Наличие годовых выплат")]
        public bool UsePayrollYearValue { get; set; }

        [Required]
        [Display(Name = "Является самостоятельным структурным подразделением")]
        public bool IsAutonomous { get; set; }

        [Display(Name = "Полное наименование")]
        public string FullName
        {
            get
            {
                return (((String.IsNullOrEmpty(ShortName) == false) ? ShortName.Trim() : "") + ((String.IsNullOrEmpty(ShortTitle) == false) ? "." + ShortTitle.Trim() : "") + ((Title != null) ? " - " + Title.Trim() : ""));
            }
        }

        [Display(Name = "Код")]
        public string DisplayShortName
        {
            get
            {
                if (String.IsNullOrEmpty(ShortName) == false
                    && String.IsNullOrEmpty(ShortName.Trim()) == false)
                {
                    if (ShortName.Contains("-") == true)
                    {
                        return ShortName.Substring(0, ShortName.IndexOf("-")).Trim();
                    }
                    else
                    {
                        return ShortName.Trim();
                    }
                }
                else
                {
                    return ShortName;
                }
            }
        }

        [Display(Name = "Сокращение")]
        public string DisplayShortTitle
        {
            get
            {
                if (String.IsNullOrEmpty(ShortTitle) == false
                    && String.IsNullOrEmpty(ShortTitle.Trim()) == false)
                {

                    return ShortTitle.Trim();
                }
                else
                {
                    return ShortName;
                }
            }
        }

        public ICollection<Employee> EmployeesInDepartment { get; set; }

        public IEnumerable<Department> Versions { get; set; }
    }
}