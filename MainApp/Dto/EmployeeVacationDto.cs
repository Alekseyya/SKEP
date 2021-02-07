using System;

namespace MainApp.Dto
{
    public class EmployeeVacationDto
    {
        public int ID { get; set; } //EmployeeID

        public string LastName { get; set; }
        public string FirstName { get; set; }
        public string MidName { get; set; }
        public string FullName { get; set; }

        public DateTime? EnrollmentDate { get; set; }

        public int VacationPaidDaysUsed { get; set; }
        public int VacationNoPaidDaysUsed { get; set; }
        public int MonthCount { get; set; }
        public int SubtractDays { get; set; }
        public int AvailableVacationDays { get; set; }
    }
}
