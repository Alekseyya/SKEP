using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Core.Models.Attributes;
using Core.Models.RBAC;


namespace Core.Models
{
    [AllowRecycleBin]
    [DisplayTableName("Сотрудники")]
    public class Employee : BaseModel
    {
        [Display(Name = "ИД")]
        public int ID { get; set; }


        [Display(Name = "Фамилия")]
        public string LastName { get; set; }

        [Display(Name = "Имя")]
        public string FirstName { get; set; }

        [Display(Name = "Отчество")]
        public string MidName { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Дата рождения")]
        public DateTime? BirthdayDate { get; set; }

        [Display(Name = "E-mail")]
        public string Email { get; set; }

        [Display(Name = "Имя учетной записи в AD")]
        public string ADLogin { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Принят")]
        public DateTime? EnrollmentDate { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Дата завершения испытательного срока")]
        public DateTime? ProbationEndDate { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Уволен")]
        public DateTime? DismissalDate { get; set; }

        [Display(Name = "Причина увольнения")]
        [DataType(DataType.MultilineText)]
        public string DismissalReason { get; set; }

        [Display(Name = "Подразделение")]
        public int? DepartmentID { get; set; }
        public virtual Department Department { get; set; }

        [Display(Name = "Должность в структуре ГК (из справочника)")]
        public int? EmployeePositionID { get; set; }
        public virtual EmployeePosition EmployeePosition { get; set; }

        [Display(Name = "Должность по трудовой книжке")]
        public int? EmployeePositionOfficialID { get; set; }
        public virtual EmployeePositionOfficial EmployeePositionOfficial { get; set; }

        [Display(Name = "Должность в структуре ГК")]
        public string EmployeePositionTitle { get; set; }

        [Display(Name = "Организация")]
        public int? OrganisationID { get; set; }
        public virtual Organisation Organisation { get; set; }

        [Display(Name = "Территориальное расположение")]
        public int? EmployeeLocationID { get; set; }
        public virtual EmployeeLocation EmployeeLocation { get; set; }

        [Display(Name = "Офис (№ кабинета)")]
        public string OfficeName { get; set; }

        [Display(Name = "Рабочий телефон")]
        public string WorkPhoneNumber { get; set; }

        [Display(Name = "Мобильный телефон личный")]
        public string PersonalMobilePhoneNumber { get; set; }

        [Display(Name = "Мобильный телефон общедоступный")]
        public string PublicMobilePhoneNumber { get; set; }

        [Display(Name = "Skype")]
        public string SkypeLogin { get; set; }

        [Display(Name = "Специализация")]
        [DataType(DataType.MultilineText)]
        public string Specialization { get; set; }

        [Display(Name = "Примечание")]
        [DataType(DataType.MultilineText)]
        public string Comments { get; set; }

        [Display(Name = "Грейд")]
        public int? EmployeeGradID { get; set; }
        public virtual EmployeeGrad EmployeeGrad { get; set; }

        [Display(Name = "ADEmployeeID")]
        public string ADEmployeeID { get; set; }

        [Display(Name = "Признак вакансии")]
        public bool IsVacancy { get; set; }

        [Display(Name = "ID вакансии")]
        public string VacancyID { get; set; }

        [Display(Name = "Данные медстраховки (ДМС и/или ОМС)")]
        public string MedicalInsuranceInfo { get; set; }

        [Display(Name = "Фактический домашний адрес")]
        public string HomeAddress { get; set; }

        [Display(Name = "ФИО контактного лица для связи в случае чрезвычайных ситуаций")]
        public string EmergencyContactName { get; set; }

        [Display(Name = "Мобильный телефон контактного лица")]
        public string EmergencyContactMobilePhoneNumber { get; set; }

        [Display(Name = "Серия и номер паспорта РФ")]
        public string PassportNumber { get; set; }

        [Display(Name = "ФИО в загранпаспорте")]
        public string InternationalPassportName { get; set; }

        [Display(Name = "Серия и номер загранпаспорта")]
        public string InternationalPassportNumber { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Срок действия загранпаспорта")]
        public DateTime? InternationalPassportDueDate { get; set; }

        [Display(Name = "Серия и номер иностранного паспорта")]
        public string ForeignPassportNumber { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Срок действия иностранного паспорта")]
        public DateTime? ForeignPassportDueDate { get; set; }

        [Display(Name = "Номер карты авиакомпании")]
        public string AirlineCardInfo { get; set; }

        
        [Display(Name = "ФИО")]
        public string FullName
        {
            get
            {
                if (IsVacancy)
                {
                    //return $"Вакансия: {VacancyID}";
                    return "-Вакансия: " + ((VacancyID != null) ? VacancyID : "");

                }
                else
                {
                    return (((LastName != null) ? LastName.Trim() + " " : "") + ((FirstName != null) ? FirstName.Trim() + " " : "") + ((MidName != null) ? MidName.Trim() : "")).Trim();
                }
            }
        }

        public IEnumerable<EmployeeCategory> EmployeeCategories { get; set; }
        public IEnumerable<EmployeeQualifyingRole> EmployeeQualifyingRoles { get; set; }
        public IEnumerable<EmployeeOrganisation> EmployeeOrganisation { get; set; }

        public IEnumerable<EmployeeDepartmentAssignment> EmployeeDepartmentAssignments { get; set; }
        public IEnumerable<EmployeePositionAssignment> EmployeePositionAssignments { get; set; }
        public IEnumerable<EmployeeGradAssignment> EmployeeGradAssignments { get; set; }
        public IEnumerable<EmployeePositionOfficialAssignment> EmployeePositionOfficialAssignments { get; set; }
        public IEnumerable<VacationRecord> VacationRecords { get; set; }
        public IEnumerable<Employee> Versions { get; set; }

    }
}