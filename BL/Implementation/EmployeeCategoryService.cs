using System;
using System.Linq;
using Core.BL;
using Core.BL.Interfaces;
using Core.Data;
using Core.Data.Interfaces;
using Core.Models;
using Core.Validation;

namespace BL.Implementation
{
    public class EmployeeCategoryService : RepositoryAwareServiceBase<EmployeeCategory, int, IEmployeeCategoryRepository>, IEmployeeCategoryService
    {
        public EmployeeCategoryService(IRepositoryFactory repositoryFactory) : base(repositoryFactory)
        {
        }

        public void Validate(EmployeeCategory entity, IValidationRecipient validationRecipient)
        {
            throw new NotImplementedException();
        }


        public EmployeeCategory GetEmployeeCategoryForDate(int employeeId, DateTime date)
        {
            //var employeeCategory = RepositoryFactory.GetRepository<IEmployeeCategoryRepository>()
            //    .GetQueryable().Where(ec => ec.EmployeeID == employeeId).OrderBy(ec => ec.CategoryDateEnd).FirstOrDefault(ec => ec.CategoryDateBegin != null
            //                                                                                                                    && ec.CategoryDateBegin <= date
            //                                                                                                                    && (ec.CategoryDateEnd == null || ec.CategoryDateEnd >= date));
            //if (employeeCategory != null)
            //    return ((DisplayAttribute) (employeeCategory.CategoryType.GetType().GetMember(employeeCategory.CategoryType.ToString()).First().GetCustomAttributes(true)[0])).Name;
            //return null;
            return RepositoryFactory.GetRepository<IEmployeeCategoryRepository>()
                .GetQueryable().Where(ec => ec.EmployeeID == employeeId).OrderBy(ec => ec.CategoryDateEnd).FirstOrDefault(ec => ec.CategoryDateBegin != null
                                                                                                                                && ec.CategoryDateBegin <= date
                                                                                                                                && (ec.CategoryDateEnd == null || ec.CategoryDateEnd >= date));
        }
    }
}
