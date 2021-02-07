using System;
using Core.Models;


namespace Core.BL.Interfaces
{
   public interface IEmployeeCategoryService : IEntityValidatingService<EmployeeCategory>, IServiceBase<EmployeeCategory, int>
   {
       EmployeeCategory GetEmployeeCategoryForDate(int employeeId, DateTime date);
   }
}
