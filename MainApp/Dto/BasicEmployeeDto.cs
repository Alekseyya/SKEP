using System;
using Core.Models;


namespace MainApp.Dto
{
    public class BasicEmployeeDto
    {
        public int Id { get; set; }

        public string LastName { get; set; }

        public string FirstName { get; set; }

        public string MidName { get; set; }

        public string FullName { get; set; }

        public string Email { get; set; }

        public int? EmployeePositionID { get; set; }
        public virtual EmployeePositionDto EmployeePosition { get; set; }

        public string EmployeePositionTitle { get; set; }
        /*public string Login { get; set; }*/ //Имя учетной записи является конфиденциальной информацией и не включается в BasicEmployeeDto

        public int? DepartmentId { get; set; }

        public BasicEmployeeDto()
        { }

        public BasicEmployeeDto(Employee employee)
        {
            if (employee == null)
                throw new ArgumentNullException(nameof(employee));

            Id = employee.ID;
            LastName = employee.LastName;
            FirstName = employee.FirstName;
            MidName = employee.MidName;
            FullName = employee.FullName;
            Email = employee.Email;
            /*Login = employee.ADLogin;*/
            EmployeePositionTitle = employee.EmployeePositionTitle;
            DepartmentId = employee.DepartmentID;
        }
    }
}