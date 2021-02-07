using System;
using System.Collections.Generic;
using System.Linq;
using Core.BL;
using Core.BL.Interfaces;
using Core.Data;
using Core.Data.Interfaces;
using Core.Models;

namespace BL.Implementation
{
    public class DepartmentService : RepositoryAwareServiceBase<Department, int, IDepartmentRepository>, IDepartmentService
    {
        private readonly (string, string) _user;

        public DepartmentService(IRepositoryFactory repositoryFactory, IUserService userService) : base(repositoryFactory)
        {
            _user = userService.GetUserDataForVersion();
        }

        public IList<Department> GetChildDepartments(int departmentId, bool includeChildDepratments)
        {
            var repository = RepositoryFactory.GetRepository<IDepartmentRepository>();
            IList<Department> childDepartmentList = null;
            if (includeChildDepratments)
            {
                childDepartmentList = GetChildDepartmentsHierarchy(repository, departmentId);
            }
            else
            {
                childDepartmentList = repository.GetAll(d => d.ParentDepartmentID.Value == departmentId);
            }

            return childDepartmentList;
        }

        private IList<Department> GetChildDepartmentsHierarchy(IDepartmentRepository departmentRepository, int departmentId)
        {
            List<Department> result = new List<Department>();
            var childDepartmentList = departmentRepository.GetQueryable().Where(d => d.ParentDepartmentID.Value == departmentId).ToList();
            if (childDepartmentList.Count > 0)
            {
                result.AddRange(childDepartmentList);
                foreach (var childDepartment in childDepartmentList)
                {
                    result.AddRange(GetChildDepartmentsHierarchy(departmentRepository, childDepartment.ID));
                }
            }
            return result;
        }

        public Department GetDepartmentForManager(int managerEmployeeId)
        {
            var repository = RepositoryFactory.GetRepository<IDepartmentRepository>();
            var departments = repository.GetAll(d => d.DepartmentManagerID.Value == managerEmployeeId);
            if (departments.Count > 0)
                return departments[0];
            return null;
        }
        public IList<Department> GetDepartmentsForManager(int managerEmployeeId)
        {
            var repository = RepositoryFactory.GetRepository<IDepartmentRepository>();
            var departments = repository.GetAll(d => d.DepartmentManagerID.Value == managerEmployeeId);
            departments = GetChildDepartmentsRecursive(departments);
            if (departments.Count > 0)
                return departments;
            return null;
        }

        //TODO не понятно нужен ли этот метод или нет
        private IList<Department> GetChildDepartmentsRecursive(IList<Department> departments)
        {
            var repository = RepositoryFactory.GetRepository<IDepartmentRepository>();
            List<Department> newListDepartments = new List<Department>();
            List<Department> workingCopy = departments.ToList();
            foreach (var item in workingCopy)
            {
                var department = item;
                newListDepartments.Add(department);

                //var childDeparts = _db.Departments.Where(d => d.ParentDepartmentID != null && d.ParentDepartmentID.Value == dId);
                var childDeparts = repository.GetAll(d => d.ParentDepartmentID != null && d.ParentDepartmentID.Value == department.ID);

                if (childDeparts != null && childDeparts.Any())
                    newListDepartments.AddRange(GetChildDepartmentsRecursive(childDeparts));
            }
            return newListDepartments;
        }

        public override Department Add(Department department)
        {
            if (department == null)
                throw new ArgumentNullException();

            var departmentRepository = RepositoryFactory.GetRepository<IDepartmentRepository>();
            department.InitBaseFields(Tuple.Create(_user.Item1, _user.Item2));
            return departmentRepository.Add(department);
        }

        public override Department Update(Department department)
        {
            if (department == null) throw new ArgumentNullException(nameof(department));
            var departmentRepository = RepositoryFactory.GetRepository<IDepartmentRepository>();

            var originalItem = departmentRepository.FindNoTracking(department.ID);

            department.UpdateBaseFields(Tuple.Create(_user.Item1, _user.Item2), originalItem.ID, originalItem);
            originalItem.FreeseVersion(originalItem.ID);

            departmentRepository.Add(originalItem);
            return departmentRepository.Update(department);
        }
    }
}