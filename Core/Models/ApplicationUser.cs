using System;
using System.Collections.Generic;
using System.Text;
using Core.Models.RBAC;


namespace Core.Models
{
    public class ApplicationUser
    {
        public int UserId { get; set; }
        public string UserLogin { get; set; }
        public string OOLogin { get; set; }
        public string OOPassword { get; set; }

        public IList<Department> ManagedDepartments { get; set; }

        public Role Role { get; set; }

        public void InitRoles(RPCSUser dbUser)
        {
            Role = new Role();

            //роли добавляются в порядке повышения уровня доступа
            if (dbUser.IsEmployee)
                Role = Role | new RoleEmployee();

            if (dbUser.IsAdAdmin)
                Role = Role | new RoleAdAdmin();
            if (dbUser.IsHR)
                Role = Role | new RoleHumanResources();
            if (dbUser.IsPM)
                Role = Role | new RoleProjectManager();
            if (dbUser.IsPMOAdmin)
                Role = Role | new RolePMOAdmin();
            if (dbUser.IsDepartmentManager)
                Role = Role | new RoleDepartmentManager();
            if (dbUser.IsFin)
                Role = Role | new RoleFin();
            if (dbUser.IsDirector)
                Role = Role | new RoleDirector();
            if (dbUser.IsPMOChief)
                Role = Role | new RolePMOChief();
            if (dbUser.IsPayrollAdmin)
                Role = Role | new RolePayrollAdmin();
            if (dbUser.IsPayrollFullRead)
                Role = Role | new RolePayrollFullRead();
            if (dbUser.IsDataAdmin)
                Role = Role | new RoleDataAdmin();
            if (dbUser.IsTSAdmin)
                Role = Role | new RoleTSAdmin();
            if (dbUser.IsIDDocsAdmin)
                Role = Role | new RoleIDDocsAdmin();

            if (dbUser.IsApiAccess)
                Role = Role | new RoleApiAccess();

            if (dbUser.IsAdmin)
                Role = Role | new RoleAdmin();

        }
    }
}
