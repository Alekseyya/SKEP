using System;
using System.ComponentModel.DataAnnotations;
using Core.Models.RBAC;


namespace Core.Models
{
    [DisplayTableName("Трудоустройство сотрудника")]
    public class EmployeeOrganisation
    {
        [Display(Name = "ИД")]
        public int ID { get; set; }

        [Required]
        [Display(Name = "Сотрудник")]
        public int? EmployeeID { get; set; }
        [Display(Name = "Сотрудник")]
        public virtual Employee Employee { get; set; }

        [Required]
        [Display(Name = "Должность по трудовой книжке")]
        public int? EmployeePositionOfficialID { get; set; }
        [Display(Name = "Должность по трудовой книжке")]
        public virtual EmployeePositionOfficial EmployeePositionOfficial { get; set; }

        [Required]
        [Display(Name = "Организация")]
        public int? OrganisationID { get; set; }
        [Display(Name = "Организация")]
        public virtual Organisation Organisation { get; set; }

        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "Дата начала")]
        public DateTime? OrganisationDateBegin { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Дата окончания")]
        public DateTime? OrganisationDateEnd { get; set; }

        [Required]
        [Display(Name = "Основное место работы")]
        public bool IsMainPlaceWork { get; set; }

        [Display(Name = "Примечание")]
        [DataType(DataType.MultilineText)]
        public string Comments { get; set; }

        public bool CheckDateTransection(DateTime date)
        {
            if (this.OrganisationDateEnd.HasValue)
            {
                return this.OrganisationDateBegin <= date && this.OrganisationDateBegin >= date;
            }
            return false;
        }

        public bool CollisionByDate(EmployeeOrganisation employee)
        {
            if (employee.OrganisationDateEnd.HasValue)
            {
                bool collissionByBeginDate = this.OrganisationDateBegin >= employee.OrganisationDateBegin && this.OrganisationDateBegin <= employee.OrganisationDateEnd;
                if (this.OrganisationDateEnd.HasValue && !collissionByBeginDate)
                    return this.OrganisationDateEnd >= employee.OrganisationDateBegin && this.OrganisationDateEnd <= employee.OrganisationDateEnd;
                return collissionByBeginDate;
            }
            else if (this.OrganisationDateEnd.HasValue)
                return employee.OrganisationDateBegin >= this.OrganisationDateBegin && employee.OrganisationDateBegin <= this.OrganisationDateEnd;
            return false; // в том случае когда есть у сравниваемых объектов есть только OrganisationDateBegin
        }
    }
}
