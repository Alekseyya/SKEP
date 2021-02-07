using System;


namespace Core.Models.RBAC
{
    /// <summary>
    /// Базовый класс ролей пользователя
    /// </summary>
    public class Role
    {
        //не должен быть static, так как в классах, наследованных от Role, различный набор Operations
        public OperationSet Operations = new OperationSet();

        //сделан не static, так как в будущем возможен различный набор PayrollAccessOperations в классах, наследованных от Role
        public string CurrentOOLoginConfigPropertyName = "";
        public OperationSet PayrollAccessOperations = new OperationSet();

        public static Role operator |(Role x, Role y)
        {
            Role z = new Role();
            z.Operations = x.Operations | y.Operations;
            if (!string.IsNullOrEmpty(y.GetOOLoginConfigPropertyName()))
            {
                z.CurrentOOLoginConfigPropertyName = y.GetOOLoginConfigPropertyName();
            }
            else if (!string.IsNullOrEmpty(x.GetOOLoginConfigPropertyName()))
            {
                z.CurrentOOLoginConfigPropertyName = x.GetOOLoginConfigPropertyName();
            }
            return z;
        }

        protected virtual string GetOOLoginConfigPropertyName()
        {
            return "";
        }
        public Role()
        {
            PayrollAccessOperations.SetPayrollAccessOperations();
        }
    }

    /// <summary>
    /// Роль "Администратор" - имеет все права
    /// </summary>
    public class RoleAdmin : Role
    {
        public RoleAdmin()
            : base()
        {
            Operations.SetFullMask();
        }

        protected override string GetOOLoginConfigPropertyName()
        {
            return "OOLoginRoleAdmin";
        }
    }

    /// <summary>
    /// Роль "Администратор Active Directory" - имеет все базовые права HR и права на изменение некоторых полей Employee
    /// </summary>
    public class RoleAdAdmin : Role
    {
        public RoleAdAdmin()
            : base()
        {
            Operations.SetBasicReadonlyOperations();

            Operations.Add(Operation.EmployeeView);
            Operations.Add(Operation.EmployeeFullListView);
            Operations.Add(Operation.EmployeeExcelExport);
            Operations.Add(Operation.OrgChartView);
            Operations.Add(Operation.DepartmentListView);
            Operations.Add(Operation.DepartmentView);
            Operations.Add(Operation.PositionView);
            Operations.Add(Operation.OrganizationView);
            //Operations.Add(Operation.EmployeeADUpdate);
            Operations.Add(Operation.EmployeeLocationView);
            Operations.Add(Operation.EmployeeIDServiceAccess);
            Operations.Add(Operation.ADSyncAccess);
        }
    }

    /// <summary>
    /// Роль Project Manager (PM) - имеет базовые права "все только на чтение" и полные права на раздел "Проекты"
    /// </summary>
    public class RoleProjectManager : Role
    {
        public RoleProjectManager()
            : base()
        {
            Operations.SetBasicReadonlyOperations();

            Operations.Add(Operation.ProjectListView);
            Operations.Add(Operation.ProjectMyProjectView);

            //Operations.Add(Operation.ProjectMemberMyPeopleView); //временно скрыто представление для РП
            Operations.Add(Operation.TSHoursRecordPMApproveHours);

        }

        protected override string GetOOLoginConfigPropertyName()
        {
            return "OOLoginRoleProjectManager";
        }
    }

    /// <summary>
    /// Роль HumanResources (HR) - имеет базовые права "все только на чтение", полные права на разделы "Сотрудники", "Подразделения", "Должности" и т.д., а также на просмотр "Служебных таблиц"
    /// Не имеет прав на работу с проектами
    /// </summary>
    public class RoleHumanResources : Role
    {

        public RoleHumanResources()
            : base()
        {
            Operations.SetBasicReadonlyOperations();

            Operations.Add(Operation.EmployeePersonalDataView);
            Operations.Add(Operation.EmployeeIdentityDocsView);
            Operations.Add(Operation.EmployeeCreateUpdate);
            Operations.Add(Operation.EmployeeIdentityDocsUpdate);
            Operations.Add(Operation.EmployeeView);
            Operations.Add(Operation.EmployeeDelete);
            Operations.Add(Operation.EmployeeFullListView);
            Operations.Add(Operation.EmployeeExcelExport);

            Operations.Add(Operation.OrgChartView);

            Operations.Add(Operation.DepartmentCreateUpdate);
            Operations.Add(Operation.DepartmentListView);
            Operations.Add(Operation.DepartmentView);

            Operations.Add(Operation.PositionCreateUpdate);
            Operations.Add(Operation.PositionView);

            Operations.Add(Operation.GradView);

            Operations.Add(Operation.OrganizationView);

            Operations.Add(Operation.EmployeeLocationView);

            Operations.Add(Operation.VacationRecordCreateUpdate);
            Operations.Add(Operation.VacationRecordView);
            Operations.Add(Operation.VacationRecordDelete);

            Operations.Add(Operation.TSAutoHoursRecordCreateUpdate);
            Operations.Add(Operation.TSAutoHoursRecordView);
            Operations.Add(Operation.TSAutoHoursRecordDelete);

            Operations.Add(Operation.QualifyingRoleView);
            Operations.Add(Operation.EmployeeQualifyingRoleCreateUpdate);
            Operations.Add(Operation.EmployeeQualifyingRoleView);
            Operations.Add(Operation.EmployeeQualifyingRoleDelete);

            Operations.Add(Operation.EmployeeCategoryCreateUpdate);
            Operations.Add(Operation.EmployeeCategoryView);
            Operations.Add(Operation.EmployeeCategoryDelete);

            Operations.Add(Operation.EmployeeOrganisationCreateUpdate);
            Operations.Add(Operation.EmployeeOrganisationView);
        }
    }

    public class RolePMOAdmin : Role
    {

        public RolePMOAdmin()
            : base()
        {
            Operations.SetBasicReadonlyOperations();

            Operations.Add(Operation.EmployeeView);
            Operations.Add(Operation.EmployeeFullListView);
            Operations.Add(Operation.EmployeeExcelExport);

            Operations.Add(Operation.OrgChartView);

            Operations.Add(Operation.DepartmentView);
            Operations.Add(Operation.DepartmentListView);

            Operations.Add(Operation.PositionView);

            Operations.Add(Operation.OrganizationView);

            Operations.Add(Operation.ProjectCreateUpdate);
            Operations.Add(Operation.ProjectDelete);
            Operations.Add(Operation.ProjectListView);
            Operations.Add(Operation.ProjectView);
            Operations.Add(Operation.ProjectExcelExport);
            Operations.Add(Operation.ProjectMemberView);
            Operations.Add(Operation.ProjectMemberCreateUpdate);
            Operations.Add(Operation.ProjectMemberMyPeopleView);
            Operations.Add(Operation.ProjectsHoursReportView);
            Operations.Add(Operation.ProjectsCostsReportView);
            Operations.Add(Operation.ProjectExternalWorkspaceView);
            Operations.Add(Operation.ProjectExternalWorkspaceCreateUpdate);

            Operations.Add(Operation.ProjectRoleView);
            Operations.Add(Operation.ProjectTypeView);
            Operations.Add(Operation.ProjectScheduleEntryTypeView);
            Operations.Add(Operation.ProjectScheduleEntryDelete);

            Operations.Add(Operation.ProjectScheduleEntryCreateUpdate);
            Operations.Add(Operation.ProjectScheduleEntryView);

            Operations.Add(Operation.ProjectStatusRecordCreateUpdate);
            Operations.Add(Operation.ProjectStatusRecordDelete);
            Operations.Add(Operation.ProjectStatusRecordView);

            Operations.Add(Operation.TSCompletenessReportView);
            Operations.Add(Operation.TSCompletenessReportViewForManagedEmployees);

            Operations.Add(Operation.TSApproveHoursReportView);
            Operations.Add(Operation.TSApproveHoursReportViewForManagedEmployees);

            Operations.Add(Operation.TSHoursUtilizationReportView);

            Operations.Add(Operation.TSAutoHoursRecordCreateUpdate);
            Operations.Add(Operation.TSAutoHoursRecordView);
            Operations.Add(Operation.TSAutoHoursRecordDelete);

            Operations.Add(Operation.OOAccessAllow);

            Operations.Add(Operation.ReportingPeriodCreateUpdate);
            Operations.Add(Operation.ReportingPeriodView);

            Operations.Add(Operation.EmployeeQualifyingRoleView);
            Operations.Add(Operation.EmployeeCategoryView);

        }

        protected override string GetOOLoginConfigPropertyName()
        {
            return "OOLoginRolePMOAdmin";
        }
    }

    public class RolePMOChief : Role
    {

        public RolePMOChief()
            : base()
        {
            Operations.SetBasicReadonlyOperations();

            Operations.Add(Operation.EmployeeView);
            Operations.Add(Operation.EmployeeFullListView);
            Operations.Add(Operation.EmployeeExcelExport);
            Operations.Add(Operation.OrgChartView);
            Operations.Add(Operation.DepartmentView);
            Operations.Add(Operation.DepartmentListView);
            Operations.Add(Operation.PositionView);
            Operations.Add(Operation.GradView);
            Operations.Add(Operation.OrganizationView);

            Operations.Add(Operation.ProjectCreateUpdate);
            Operations.Add(Operation.ProjectDelete);
            Operations.Add(Operation.ProjectListView);
            Operations.Add(Operation.ProjectView);
            Operations.Add(Operation.ProjectExcelExport);
            Operations.Add(Operation.ProjectMemberView);
            Operations.Add(Operation.ProjectMemberMyPeopleView);
            Operations.Add(Operation.ProjectsHoursReportView);
            Operations.Add(Operation.ProjectsCostsReportView);
            Operations.Add(Operation.ProjectExternalWorkspaceView);

            Operations.Add(Operation.ProjectRoleView);
            Operations.Add(Operation.ProjectTypeView);
            Operations.Add(Operation.ProjectScheduleEntryTypeView);

            Operations.Add(Operation.ProjectScheduleEntryView);
            Operations.Add(Operation.ProjectScheduleEntryCreateUpdate);
            Operations.Add(Operation.ProjectScheduleEntryDelete);

            Operations.Add(Operation.ProjectStatusRecordView);
            Operations.Add(Operation.ProjectStatusRecordCreateUpdate);
            Operations.Add(Operation.ProjectScheduleEntryDelete);

            Operations.Add(Operation.OOAccessAllow);
            Operations.Add(Operation.AppPropertiesAccess);

            Operations.Add(Operation.QualifyingRoleCreateUpdate);
            Operations.Add(Operation.QualifyingRoleView);
            Operations.Add(Operation.EmployeeQualifyingRoleCreateUpdate);
            Operations.Add(Operation.EmployeeQualifyingRoleView);
            Operations.Add(Operation.EmployeeQualifyingRoleDelete);

            Operations.Add(Operation.QualifyingRoleRateCreateUpdate);
            Operations.Add(Operation.QualifyingRoleRateView);

            Operations.Add(Operation.EmployeeCategoryCreateUpdate);
            Operations.Add(Operation.EmployeeCategoryView);
            Operations.Add(Operation.EmployeeCategoryDelete);

            Operations.Add(Operation.TSCompletenessReportView);
            Operations.Add(Operation.TSCompletenessReportViewForManagedEmployees);

            Operations.Add(Operation.TSApproveHoursReportView);
            Operations.Add(Operation.TSApproveHoursReportViewForManagedEmployees);

            Operations.Add(Operation.TSHoursUtilizationReportView);

            Operations.Add(Operation.EmployeeOrganisationCreateUpdate);
            Operations.Add(Operation.EmployeeOrganisationView);
        }

        protected override string GetOOLoginConfigPropertyName()
        {
            return "OOLoginRolePMOChief";
        }
    }

    public class RoleFin : Role
    {

        public RoleFin()
            : base()
        {
            Operations.SetBasicReadonlyOperations();

            //Operations.Add(Operation.EmployeePersonalDataView);
            Operations.Add(Operation.EmployeeView);
            Operations.Add(Operation.EmployeeFullListView);
            //Operations.Add(Operation.EmployeeExcelExport);
            Operations.Add(Operation.OrgChartView);
            Operations.Add(Operation.DepartmentView);
            Operations.Add(Operation.DepartmentListView);
            Operations.Add(Operation.PositionView);
            Operations.Add(Operation.GradView);

            Operations.Add(Operation.OrganizationView);

            Operations.Add(Operation.ProjectCreateUpdate);
            Operations.Add(Operation.ProjectDelete);
            Operations.Add(Operation.ProjectListView);
            Operations.Add(Operation.ProjectView);
            Operations.Add(Operation.ProjectExcelExport);
            Operations.Add(Operation.ProjectsHoursReportView);
            Operations.Add(Operation.ProjectsCostsReportView);

            Operations.Add(Operation.ProjectRoleView);
            Operations.Add(Operation.ProjectTypeView);
            Operations.Add(Operation.ProjectScheduleEntryTypeView);

            Operations.Add(Operation.ProjectScheduleEntryView);
            Operations.Add(Operation.ProjectScheduleEntryCreateUpdate);
            Operations.Add(Operation.ProjectScheduleEntryDelete);

            Operations.Add(Operation.ProjectStatusRecordView);
            Operations.Add(Operation.ProjectStatusRecordCreateUpdate);
            Operations.Add(Operation.ProjectStatusRecordDelete);

            Operations.Add(Operation.FinReportView);
            Operations.Add(Operation.FinDataView);
            Operations.Add(Operation.FinDataViewForMyDepartments);
            Operations.Add(Operation.FinDataCreateUpdate);
            Operations.Add(Operation.OOAccessAllow);

            Operations.Add(Operation.QualifyingRoleCreateUpdate);
            Operations.Add(Operation.QualifyingRoleView);
            Operations.Add(Operation.EmployeeQualifyingRoleCreateUpdate);
            Operations.Add(Operation.EmployeeQualifyingRoleView);
            Operations.Add(Operation.EmployeeQualifyingRoleDelete);
            Operations.Add(Operation.QualifyingRoleRateCreateUpdate);
            Operations.Add(Operation.QualifyingRoleRateView);

            Operations.Add(Operation.EmployeeCategoryView);

            Operations.Add(Operation.TSHoursUtilizationReportView);

        }

        protected override string GetOOLoginConfigPropertyName()
        {
            return "OOLoginRoleFin";
        }
    }

    public class RoleDirector : Role
    {

        public RoleDirector()
            : base()
        {
            Operations.SetBasicReadonlyOperations();

            Operations.Add(Operation.EmployeeView);
            Operations.Add(Operation.EmployeeFullListView);
            Operations.Add(Operation.EmployeeExcelExport);
            Operations.Add(Operation.EmployeePersonalDataView);

            Operations.Add(Operation.OrgChartView);

            Operations.Add(Operation.DepartmentView);
            Operations.Add(Operation.DepartmentListView);
            Operations.Add(Operation.PositionView);
            Operations.Add(Operation.GradView);
            Operations.Add(Operation.OrganizationView);

            Operations.Add(Operation.ProjectListView);

            Operations.Add(Operation.QualifyingRoleView);
            Operations.Add(Operation.EmployeeQualifyingRoleView);
            Operations.Add(Operation.EmployeeCategoryView);

            Operations.Add(Operation.TSCompletenessReportView);
            Operations.Add(Operation.TSCompletenessReportViewForManagedEmployees);

            Operations.Add(Operation.TSApproveHoursReportView);
            Operations.Add(Operation.TSApproveHoursReportViewForManagedEmployees);
        }

        protected override string GetOOLoginConfigPropertyName()
        {
            return "OOLoginRoleDirector";
        }
    }

    /// <summary>
    /// Роль "Руководитель подразделения"
    /// </summary>
    public class RoleDepartmentManager : Role
    {
        public RoleDepartmentManager()
            : base()
        {
            Operations.SetBasicReadonlyOperations();

            Operations.Add(Operation.EmployeeView);
            Operations.Add(Operation.EmployeeExcelExport);
            Operations.Add(Operation.EmployeeSubEmplPersonalDataView);
            Operations.Add(Operation.DepartmentView);

            Operations.Add(Operation.ProjectListView);
            Operations.Add(Operation.ProjectMyDepartmentProjectView);
            //Operations.Add(Operation.ProjectsHoursReportViewForManagedEmployees);

            Operations.Add(Operation.EmployeePayrollReportView);

            Operations.Add(Operation.OOAccessAllow);
            Operations.Add(Operation.OOAccessSubEmplReadPayrollAccess);
            Operations.Add(Operation.OOAccessSubEmplPayrollChangeAccess);

            Operations.Add(Operation.TSCompletenessReportViewForManagedEmployees);
            
            Operations.Add(Operation.FinDataViewForMyDepartments);
        }

        protected override string GetOOLoginConfigPropertyName()
        {
            return "OOLoginRoleDepartmentManager";
        }
    }

    public class RolePayrollAdmin : Role
    {

        public RolePayrollAdmin()
            : base()
        {
            Operations.SetBasicReadonlyOperations();

            Operations.Add(Operation.EmployeeView);
            Operations.Add(Operation.EmployeeFullListView);
            Operations.Add(Operation.EmployeeExcelExport);
            Operations.Add(Operation.EmployeePersonalDataView);

            Operations.Add(Operation.OrgChartView);

            Operations.Add(Operation.DepartmentView);
            Operations.Add(Operation.DepartmentListView);
            Operations.Add(Operation.PositionView);
            Operations.Add(Operation.GradView);
            Operations.Add(Operation.OrganizationView);

            Operations.Add(Operation.EmployeeIDServiceAccess);
            Operations.Add(Operation.EmployeePayrollReportView);

            Operations.Add(Operation.OOAccessAllow);
            Operations.Add(Operation.OOAccessFullReadPayrollAccess);
            Operations.Add(Operation.OOAccessFullPayrollAccess);
            Operations.Add(Operation.OOAccessFullPayrollChangeAccess);
            Operations.Add(Operation.OOAccessFullReadPayrollChangeAccess);
        }

        protected override string GetOOLoginConfigPropertyName()
        {
            return "OOLoginPayrollAdmin";
        }
    }

    public class RoleDepartmentPayrollRead : Role
    {

        public RoleDepartmentPayrollRead()
            : base()
        {
            Operations.SetBasicReadonlyOperations();

            Operations.Add(Operation.EmployeeView);
            Operations.Add(Operation.EmployeeSubEmplPersonalDataView);
            Operations.Add(Operation.DepartmentView);
            Operations.Add(Operation.EmployeePayrollReportView);
            Operations.Add(Operation.OOAccessAllow);
            Operations.Add(Operation.OOAccessSubEmplReadPayrollAccess);
        }

        protected override string GetOOLoginConfigPropertyName()
        {
            return "OOLoginDepartmentPayrollAdmin";
        }
    }

    public class RolePayrollFullRead : Role
    {

        public RolePayrollFullRead()
            : base()
        {
            Operations.SetBasicReadonlyOperations();

            Operations.Add(Operation.EmployeeView);
            Operations.Add(Operation.EmployeeFullListView);
            Operations.Add(Operation.EmployeeExcelExport);
            Operations.Add(Operation.EmployeePersonalDataView);

            Operations.Add(Operation.OrgChartView);

            Operations.Add(Operation.DepartmentView);
            Operations.Add(Operation.DepartmentListView);
            Operations.Add(Operation.PositionView);
            Operations.Add(Operation.GradView);
            Operations.Add(Operation.OrganizationView);

            Operations.Add(Operation.EmployeeIDServiceAccess);
            Operations.Add(Operation.EmployeePayrollReportView);

            Operations.Add(Operation.OOAccessAllow);
            Operations.Add(Operation.OOAccessFullReadPayrollAccess);
            Operations.Add(Operation.OOAccessFullReadPayrollChangeAccess);
        }

        protected override string GetOOLoginConfigPropertyName()
        {
            return "OOLoginPayrollFullRead";
        }
    }

    public class RoleTSAdmin : Role
    {
        public RoleTSAdmin() : base()
        {
            Operations.SetBasicReadonlyOperations();

            Operations.Add(Operation.EmployeeView);
            Operations.Add(Operation.EmployeeFullListView);

            Operations.Add(Operation.TSHoursRecordCreateUpdate);
            Operations.Add(Operation.TSHoursRecordView);

            Operations.Add(Operation.ProjectListView);
            Operations.Add(Operation.ProjectsHoursReportView);
            Operations.Add(Operation.ProjectExcelExport);

            Operations.Add(Operation.VacationRecordCreateUpdate);
            Operations.Add(Operation.VacationRecordView);
            Operations.Add(Operation.VacationRecordDelete);

            Operations.Add(Operation.ReportingPeriodCreateUpdate);
            Operations.Add(Operation.ReportingPeriodView);

            Operations.Add(Operation.TimesheetProcessingAccess);

            Operations.Add(Operation.TSCompletenessReportView);
            Operations.Add(Operation.TSCompletenessReportViewForManagedEmployees);

            Operations.Add(Operation.TSApproveHoursReportView);
            Operations.Add(Operation.TSApproveHoursReportViewForManagedEmployees);


            Operations.Add(Operation.TSAutoHoursRecordCreateUpdate);
            Operations.Add(Operation.TSAutoHoursRecordView);
            Operations.Add(Operation.TSAutoHoursRecordDelete);

            Operations.Add(Operation.TSHoursUtilizationReportView);
            Operations.Add(Operation.TSHoursRecordDelete);
        }
        protected override string GetOOLoginConfigPropertyName()
        {
            return "OOLoginRoleTSAdmin";
        }
    }

    public class RoleDataAdmin : Role
    {

        public RoleDataAdmin()
            : base()
        {
            Operations.SetBasicReadonlyOperations();

            Operations.Add(Operation.ApiAccessDataView);

            Operations.Add(Operation.EmployeeView);
            Operations.Add(Operation.EmployeeFullListView);
            Operations.Add(Operation.EmployeeExcelExport);

            Operations.Add(Operation.EmployeeLocationCreateUpdate);
            Operations.Add(Operation.EmployeeLocationView);

            Operations.Add(Operation.OrgChartView);

            Operations.Add(Operation.DepartmentCreateUpdate);
            Operations.Add(Operation.DepartmentView);
            Operations.Add(Operation.DepartmentListView);

            Operations.Add(Operation.PositionCreateUpdate);
            Operations.Add(Operation.PositionView);

            Operations.Add(Operation.OrganizationCreateUpdate);
            Operations.Add(Operation.OrganizationView);

            Operations.Add(Operation.ProjectCreateUpdate);
            Operations.Add(Operation.ProjectDelete);
            Operations.Add(Operation.ProjectView);
            Operations.Add(Operation.ProjectListView);
            Operations.Add(Operation.ProjectExcelExport);

            Operations.Add(Operation.ProjectRoleCreateUpdate);
            Operations.Add(Operation.ProjectRoleView);
            Operations.Add(Operation.ProjectTypeCreateUpdate);
            Operations.Add(Operation.ProjectTypeView);

            Operations.Add(Operation.ProjectScheduleEntryCreateUpdate);
            Operations.Add(Operation.ProjectScheduleEntryView);
            Operations.Add(Operation.ProjectScheduleEntryDelete);

            Operations.Add(Operation.ProjectStatusRecordCreateUpdate);
            Operations.Add(Operation.ProjectStatusRecordDelete);
            Operations.Add(Operation.ProjectStatusRecordView);

            Operations.Add(Operation.ProjectScheduleEntryTypeView);
            Operations.Add(Operation.ProjectScheduleEntryTypeCreateUpdate);

            Operations.Add(Operation.ProjectMemberView);
            Operations.Add(Operation.ProjectMemberCreateUpdate);
            Operations.Add(Operation.ProjectMemberMyPeopleView);

            Operations.Add(Operation.ProjectExternalWorkspaceView);
            Operations.Add(Operation.ProjectExternalWorkspaceCreateUpdate);

            Operations.Add(Operation.ProjectsHoursReportView);

            Operations.Add(Operation.ProductionCalendarRecordCreateUpdate);

            Operations.Add(Operation.QualifyingRoleCreateUpdate);
            Operations.Add(Operation.QualifyingRoleView);
            Operations.Add(Operation.EmployeeQualifyingRoleCreateUpdate);
            Operations.Add(Operation.EmployeeQualifyingRoleView);
            Operations.Add(Operation.EmployeeQualifyingRoleDelete);

            Operations.Add(Operation.EmployeeCategoryCreateUpdate);
            Operations.Add(Operation.EmployeeCategoryView);
            Operations.Add(Operation.EmployeeCategoryDelete);

            Operations.Add(Operation.TSCompletenessReportView);
            Operations.Add(Operation.TSCompletenessReportViewForManagedEmployees);

            Operations.Add(Operation.TSApproveHoursReportView);
            Operations.Add(Operation.TSApproveHoursReportViewForManagedEmployees);

            Operations.Add(Operation.EmployeeOrganisationCreateUpdate);
            Operations.Add(Operation.EmployeeOrganisationView);
        }

        protected override string GetOOLoginConfigPropertyName()
        {
            return "";
        }
    }

    public class RoleIDDocsAdmin : Role
    {

        public RoleIDDocsAdmin()
            : base()
        {
            Operations.SetBasicReadonlyOperations();

            Operations.Add(Operation.EmployeeView);
            Operations.Add(Operation.EmployeeFullListView);
            Operations.Add(Operation.EmployeeIdentityDocsView);
            Operations.Add(Operation.EmployeeIdentityDocsUpdate);

            Operations.Add(Operation.OrgChartView);

            Operations.Add(Operation.DepartmentListView);


        }

        protected override string GetOOLoginConfigPropertyName()
        {
            return "";
        }
    }

    /// <summary>
    /// Роль Employee - имеет базовые права "все только на чтение", 
    /// а также можем изменять некоторые поля своей записи в Employee (своя запись определяется по совпадению Employee.ADLogin с @User.Identity.Name текущего пользователя портала)
    /// </summary>
    public class RoleEmployee : Role
    {
        public RoleEmployee()
            : base()
        {
            Operations.SetBasicReadonlyOperations();

            Operations.Add(Operation.TSHoursRecordCreateUpdateMyHours);
            Operations.Add(Operation.TSHoursRecordDeleteMyHours);


        }
    }

    /// <summary>
    /// Роль ApiAccess - роль для системных учетных записей, которым требуется доступ только к API без доступа в систему
    /// </summary>
    public class RoleApiAccess : Role
    {
        public RoleApiAccess()
            : base()
        {

            Operations.Add(Operation.ApiAccessDataView);
        }
    }
}
