using System;
using Core.Data;
using Core.Data.Interfaces;
using Z.EntityFramework.Plus;

namespace Data.Implementation
{
    public class RPCSRepositoryFactory : IRepositoryFactory
    {
        protected IRPCSDbAccessor DbAccessor { get; }

        public RPCSRepositoryFactory(IRPCSDbAccessor dbAccessor)
        {
            if (dbAccessor == null)
                throw new ArgumentNullException(nameof(dbAccessor));

            DbAccessor = dbAccessor;
        }

        public TRepository GetRepository<TRepository>()
        {
            // TODO: примитивная реализация, потом стоит подумать об улучшении
            var repositoryType = typeof(TRepository);
            object result;
            if (repositoryType == typeof(IEmployeeRepository))
                result = new EmployeeRepository(DbAccessor.GetDbContext());
            else if (repositoryType == typeof(IDepartmentRepository))
                result = new DepartmentRepository(DbAccessor.GetDbContext());
            else if (repositoryType == typeof(IProductionCalendarRepository))
                result = new ProductionCalendarRepository(DbAccessor.GetDbContext());
            else if (repositoryType == typeof(IProjectMembersRepository))
                result = new ProjectMembersRepository(DbAccessor.GetDbContext());
            else if (repositoryType == typeof(IProjectRolesRepository))
                result = new ProjectRolesRepository(DbAccessor.GetDbContext());
            else if (repositoryType == typeof(IProjectStatusRecordRepository))
                result = new ProjectStatusRecordRepository(DbAccessor.GetDbContext());
            else if (repositoryType == typeof(IProjectReportRecordsRepository))
                result = new ProjectReportRecordsRepository(DbAccessor.GetDbContext());
            else if (repositoryType == typeof(IProjectRepository))
                result = new ProjectRepository(DbAccessor.GetDbContext());
            else if (repositoryType == typeof(IUserRepository))
                result = new UserRepository(DbAccessor.GetDbContext());
            else if (repositoryType == typeof(IBudgetLimitRepository))
                result = new BudgetLimitRepository(DbAccessor.GetDbContext());
            else if (repositoryType == typeof(IExpensesRecordRepository))
                result = new ExpensesRecordRepository(DbAccessor.GetDbContext());
            else if (repositoryType == typeof(ITSHoursRecordRepository))
                result = new TSHoursRecordRepository(DbAccessor.GetDbContext());
            else if (repositoryType == typeof(ITSAutoHoursRecordRepository))
                result = new TSAutoHoursRecordRepository(DbAccessor.GetDbContext());
            else if (repositoryType == typeof(IReportingPeriodRepository))
                result = new ReportingPeriodRepository(DbAccessor.GetDbContext());
            else if (repositoryType == typeof(IVacationRecordRepository))
                result = new VacationRecordRepository(DbAccessor.GetDbContext());
            else if (repositoryType == typeof(IProjectScheduleEntryRepository))
                result = new ProjectScheduleEntryRepository(DbAccessor.GetDbContext());
            else if (repositoryType == typeof(ICostSubItemRepository))
                result = new CostSubItemRepository(DbAccessor.GetDbContext());
            else if (repositoryType == typeof(IProjectScheduleEntryTypeRepository))
                result = new ProjectScheduleEntryTypeRepository(DbAccessor.GetDbContext());
            else if (repositoryType == typeof(IEmployeeCategoryRepository))
                result = new EmployeeCategoryRepository(DbAccessor.GetDbContext());
            else if (repositoryType == typeof(IEmployeeQualifyingRoleRepository))
                result = new EmployeeQualifyingRoleRepository(DbAccessor.GetDbContext());
            else if (repositoryType == typeof(IQualifyingRoleRepository))
                result = new QualifyingRoleRepository(DbAccessor.GetDbContext());
            else if (repositoryType == typeof(IProjectTypeRepository))
                result = new ProjectTypeRepository(DbAccessor.GetDbContext());
            else if (repositoryType == typeof(IQualifyingRoleRateRepository))
                result = new QualifyingRoleRateRepository(DbAccessor.GetDbContext());
            else if (repositoryType == typeof(IAppPropertyRepository))
                result = new AppPropertyRepository(DbAccessor.GetDbContext());
            else if (repositoryType == typeof(IEmployeeDepartmentAssignmentRepository))
                result = new EmployeeDepartmentAssignmentRepository(DbAccessor.GetDbContext());
            else if (repositoryType == typeof(IEmployeeGradAssignmentRepository))
                result = new EmployeeGradAssignmentRepository(DbAccessor.GetDbContext());
            else if (repositoryType == typeof(IEmployeeGradRepository))
                result = new EmployeeGradRepository(DbAccessor.GetDbContext());
            else if (repositoryType == typeof(IEmployeeLocationRepository))
                result = new EmployeeLocationRepository(DbAccessor.GetDbContext());
            else if (repositoryType == typeof(IEmployeePositionAssignmentRepository))
                result = new EmployeePositionAssignmentRepository(DbAccessor.GetDbContext());
            else if (repositoryType == typeof(IEmployeePositionRepository))
                result = new EmployeePositionRepository(DbAccessor.GetDbContext());
            else if (repositoryType == typeof(IEmployeePositionOfficialRepository))
                result = new EmployeePositionOfficialRepository(DbAccessor.GetDbContext());
            else if (repositoryType == typeof(IOrganisationRepository))
                result = new OrganisationRepository(DbAccessor.GetDbContext());
            else if (repositoryType == typeof(IProjectExternalWorkspaceRepository))
                result = new ProjectExternalWorkspaceRepository(DbAccessor.GetDbContext());
            else if (repositoryType == typeof(ICostItemRepository))
                result = new CostItemRepository(DbAccessor.GetDbContext());
            else if (repositoryType == typeof(IEmployeeOrganisationRepository))
                result = new EmployeeOrganisationRepository(DbAccessor.GetDbContext());
            else if (repositoryType == typeof(IProjectStatusRecordEntryRepository))
                result = new ProjectStatusRecordEntryRepository(DbAccessor.GetDbContext());
            else if (repositoryType == typeof(IEmployeeGradParamRepository))
                result = new EmployeeGradParamRepository(DbAccessor.GetDbContext());
            else
                throw new RepositoryNotFoundException(repositoryType);
            return (TRepository)result;
        }

        public void EnableDeletedFilter()
        {
            DbAccessor.GetDbContext().Filter("IsDeleted").Enable();
        }

        public void EnableVersionsFilter()
        {
            DbAccessor.GetDbContext().Filter("IsVersion").Enable();
        }

        public void DisableDeletedFilter()
        {
            DbAccessor.GetDbContext().Filter("IsDeleted").Disable();
        }

        public void DisableVersionsFilter()
        {
            DbAccessor.GetDbContext().Filter("IsVersion").Disable();
        }
    }
}