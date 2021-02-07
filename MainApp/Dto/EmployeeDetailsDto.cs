using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Core.Models;


namespace MainApp.Dto
{
    public class EmployeeDetailsDto : BasicEmployeeDto
    {
        public string ADLogin { get; set; }
        public string ADEmployeeID { get; set; }

        public DateTime? EnrollmentDate { get; set; }
        public DateTime? ProbationEndDate { get; set; }
        public DateTime? DismissalDate { get; set; }

        public int? EmployeeGradID { get; set; }
        public virtual EmployeeGradDto EmployeeGrad { get; set; }

        public EmployeeCategoryType? CategoryType { get; set; }
        public int? EmployeeLocationID { get; set; }
        public virtual EmployeeLocationDto EmployeeLocation { get; set; }

        public EmployeeDetailsDto() { }

        public EmployeeDetailsDto(Employee employee, EmployeeCategory category) : base(employee)
        {
            Id = employee.ID;
            ADLogin = employee.ADLogin;
            ADEmployeeID = employee.ADEmployeeID;
            EnrollmentDate = employee.EnrollmentDate;
            ProbationEndDate = employee.ProbationEndDate;
            DismissalDate = employee.DismissalDate;

            if (category != null)
                CategoryType = category.CategoryType;
            else
                CategoryType = null;
        }
    }
}
