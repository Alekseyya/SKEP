using System.Linq;
using Core.Extensions;
using Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Z.EntityFramework.Plus;

namespace Data
{
    //закомментировать [DbConfigurationType(typeof(MySql.Data.Entity.MySqlEFConfiguration))] при расширении модели и добавлении Scaffolded item,
    //затем перед сборкой проекта раскомментировать
    //[DbConfigurationType(typeof(MySql.Data.Entity.MySqlEFConfiguration))]
    public class RPCSContext : DbContext
    {
        public RPCSContext(DbContextOptions<RPCSContext> options) : base(options)
        {
            QueryFilterManager.Filter<BaseModel>("IsVersion", m => m.Where(x => !x.IsVersion), true);
            QueryFilterManager.Filter<BaseModel>("IsDeleted", m => m.Where(x => !x.IsDeleted), true);
            QueryFilterManager.InitilizeGlobalFilter(this);
        }

        public virtual DbSet<Employee> Employees { get; set; }
        public virtual DbSet<EmployeePosition> EmployeePositions { get; set; }
        public virtual DbSet<Department> Departments { get; set; }
        public virtual DbSet<Organisation> Organisations { get; set; }
        public virtual DbSet<Project> Projects { get; set; }
        public virtual DbSet<EmployeeGrad> EmployeeGrads { get; set; }
        public virtual DbSet<EmployeeGradAssignment> EmployeeGradAssignments { get; set; }
        public virtual DbSet<EmployeeDepartmentAssignment> EmployeeDepartmentAssignments { get; set; }
        public virtual DbSet<EmployeePositionAssignment> EmployeePositionAssignments { get; set; }
        public virtual DbSet<EmployeeOrganisation> EmployeeOrganisations { get; set; }
        public virtual DbSet<RPCSUser> RPCSUsers { get; set; }
        public virtual DbSet<ProjectReportRecord> ProjectReportRecords { get; set; }
        public virtual DbSet<ProjectStatusRecord> ProjectStatusRecords { get; set; }
        public virtual DbSet<EmployeeLocation> EmployeeLocations { get; set; }
        public virtual DbSet<ProjectRole> ProjectRoles { get; set; }
        public virtual DbSet<ProjectMember> ProjectMembers { get; set; }
        public virtual DbSet<ProductionCalendarRecord> ProductionCalendarRecords { get; set; }
        public virtual DbSet<EmployeePositionOfficial> EmployeePositionOfficials { get; set; }
        public virtual DbSet<TSHoursRecord> TSHoursRecords { get; set; }
        public virtual DbSet<EmployeePositionOfficialAssignment> EmployeePositionOfficialAssignments { get; set; }
        public virtual DbSet<ProjectType> ProjectTypes { get; set; }
        public virtual DbSet<AppProperty> AppProperties { get; set; }
        public virtual DbSet<EmployeeCategory> EmployeeCategories { get; set; }
        public virtual DbSet<TSAutoHoursRecord> TSAutoHoursRecords { get; set; }
        public virtual DbSet<CostItem> CostItems { get; set; }
        public virtual DbSet<CostSubItem> CostSubItems { get; set; }
        public virtual DbSet<BudgetLimit> BudgetLimits { get; set; }
        public virtual DbSet<ExpensesRecord> ExpensesRecords { get; set; }
        public virtual DbSet<VacationRecord> VacationRecords { get; set; }
        public virtual DbSet<ReportingPeriod> ReportingPeriods { get; set; }
        public virtual DbSet<QualifyingRole> QualifyingRoles { get; set; }
        public virtual DbSet<QualifyingRoleRate> QualifyingRoleRates { get; set; }
        public virtual DbSet<EmployeeQualifyingRole> EmployeeQualifyingRoles { get; set; }
        public virtual DbSet<ProjectScheduleEntry> ProjectScheduleEntries { get; set; }
        public virtual DbSet<ProjectScheduleEntryType> ProjectScheduleEntryTypes { get; set; }
        public virtual DbSet<ProjectExternalWorkspace> ProjectExternalWorkspaces { get; set; }
        public virtual DbSet<ProjectStatusRecordEntry> ProjectStatusRecordEntries { get; set; }
        public virtual DbSet<EmployeeGradParam> EmployeeGradParams { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            //TODO Включение плюрализации
            //https://stackoverflow.com/questions/37493095/entity-framework-core-rc2-table-name-pluralization
            //modelBuilder.Conventions.Remove<PluralizingTableNameConvention>();
            modelBuilder.RemovePluralizingTableNameConvention();
            

            //TODo для того чтобы в запросах не писить Select "ID" from "Project" - без скобочек
            //TODO смотреть сюда https://andrewlock.net/customising-asp-net-core-identity-ef-core-naming-conventions-for-postgresql/ и
            //TODO сюда https://stackoverflow.com/questions/6304268/postgresql-query-syntax-without-quotes
            //modelBuilder.NamesToSnakeCase();
            
            
            //modelBuilder.Entity<Project>().Property(p => p.ContractAmount).HasPrecision(32, 2);
            modelBuilder.Entity<Project>().Property(p => p.ContractAmount).HasColumnType("decimal(32,2)");

            //modelBuilder.Entity<Project>().Property(p => p.SubcontractorsAmountBudget).HasPrecision(32, 2);
            modelBuilder.Entity<Project>().Property(p => p.SubcontractorsAmountBudget).HasColumnType("decimal(32,2)");

            //modelBuilder.Entity<Project>().Property(p => p.OrganisationAmountBudget).HasPrecision(32, 2);
            modelBuilder.Entity<Project>().Property(p => p.OrganisationAmountBudget).HasColumnType("decimal(32,2)");

            //modelBuilder.Entity<Project>().Property(p => p.EmployeePayrollBudget).HasPrecision(32, 2);
            modelBuilder.Entity<Project>().Property(p => p.EmployeePayrollBudget).HasColumnType("decimal(32,2)");


            //modelBuilder.Entity<Project>().Property(p => p.OtherCostsBudget).HasPrecision(32, 2);
            modelBuilder.Entity<Project>().Property(p => p.OtherCostsBudget).HasColumnType("decimal(32,2)");


            //modelBuilder.Entity<Project>().Property(p => p.EmployeePayrollTotalAmountActual).HasPrecision(32, 2);
            modelBuilder.Entity<Project>().Property(p => p.EmployeePayrollTotalAmountActual).HasColumnType("decimal(32,2)");
            
            ////Todo не уврен что это правильно
            modelBuilder.Entity<Project>().Ignore(x => x.ParentProject);
            modelBuilder.Entity<Project>().Ignore(x => x.ChildProjects);
            modelBuilder.Entity<Project>().Ignore(x => x.Versions);

            //TODo для того чтобы не создавалась лишняя таблица в бд
            //modelBuilder.Ignore<ChangeInfoRecord>();
            

            //modelBuilder.Entity<ProjectStatusRecord>().Property(p => p.ContractReceivedMoneyAmountActual).HasPrecision(32, 2);
            modelBuilder.Entity<ProjectStatusRecord>().Property(p => p.ContractReceivedMoneyAmountActual).HasColumnType("decimal(32,2)");


            //modelBuilder.Entity<ProjectStatusRecord>().Property(p => p.PaidToSubcontractorsAmountActual).HasPrecision(32, 2);
            modelBuilder.Entity<ProjectStatusRecord>().Property(p => p.PaidToSubcontractorsAmountActual).HasColumnType("decimal(32,2)");

            //modelBuilder.Entity<ProjectStatusRecord>().Property(p => p.EmployeePayrollAmountActual).HasPrecision(32, 2);
            modelBuilder.Entity<ProjectStatusRecord>().Property(p => p.EmployeePayrollAmountActual).HasColumnType("decimal(32,2)");

            //modelBuilder.Entity<ProjectStatusRecord>().Property(p => p.OtherCostsAmountActual).HasPrecision(32, 2);
            modelBuilder.Entity<ProjectStatusRecord>().Property(p => p.OtherCostsAmountActual).HasColumnType("decimal(32,2)");

            //modelBuilder.Entity<ProjectReportRecord>().Property(p => p.EmployeePayroll).HasPrecision(32, 2);
            modelBuilder.Entity<ProjectReportRecord>().Property(p => p.EmployeePayroll).HasColumnType("decimal(32,2)");


            //modelBuilder.Entity<ProjectReportRecord>().Property(p => p.EmployeeOvertimePayroll).HasPrecision(32, 2);
            modelBuilder.Entity<ProjectReportRecord>().Property(p => p.EmployeeOvertimePayroll).HasColumnType("decimal(32,2)");

            //modelBuilder.Entity<ProjectReportRecord>().Property(p => p.EmployeePerformanceBonus).HasPrecision(32, 2);
            modelBuilder.Entity<ProjectReportRecord>().Property(p => p.EmployeePerformanceBonus).HasColumnType("decimal(32,2)");


            //modelBuilder.Entity<ProjectReportRecord>().Property(p => p.EmployeePerformanceBonus).HasPrecision(32, 2);
            modelBuilder.Entity<ProjectReportRecord>().Property(p => p.EmployeePerformanceBonus).HasColumnType("decimal(32,2)");

            //modelBuilder.Entity<ProjectReportRecord>().Property(p => p.OtherCosts).HasPrecision(32, 2);
            modelBuilder.Entity<ProjectReportRecord>().Property(p => p.OtherCosts).HasColumnType("decimal(32,2)");

            //modelBuilder.Entity<ProjectReportRecord>().Property(p => p.TotalCosts).HasPrecision(32, 2);
            modelBuilder.Entity<ProjectReportRecord>().Property(p => p.TotalCosts).HasColumnType("decimal(32,2)");

            // modelBuilder.Entity<Employee>()
            //.HasOptional(x => x.Department)
            //.WithMany(e => e.EmployeesInDepartment)
            //.HasForeignKey(x => x.DepartmentID);

            modelBuilder.Entity<Employee>()
                .HasOne(s => s.Department)
                .WithMany(e => e.EmployeesInDepartment)
                .HasForeignKey(x => x.DepartmentID);

            //modelBuilder.Entity<Department>()
            //.HasOptional(x => x.DepartmentManager)
            //.WithMany()
            //.HasForeignKey(x => x.DepartmentManagerID);

            modelBuilder.Entity<Department>()
                .HasOne(x => x.DepartmentManager)
                .WithMany()
                .HasForeignKey(x => x.DepartmentManagerID);


            //TODO Что-то с этим сделать
            //modelBuilder.Filter("IsVersion", (BaseModel p) => p.IsVersion, false);
        }
    }

    //TODO Нужно для того, чтобы работала инициализация первой миграции
    //https://stackoverflow.com/questions/45782446/unable-to-create-migrations-after-upgrading-to-asp-net-core-2-0
    public class RPCSContextDbFactory : IDesignTimeDbContextFactory<RPCSContext>
    {
        RPCSContext IDesignTimeDbContextFactory<RPCSContext>.CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<RPCSContext>();
            //optionsBuilder.UseNpgsql<RPCSContext>("Server=localhost;Port=5432;Database=rpcs;Username=postgres;Password=***;");
            //TODO Надо добавлять строку подключения каждый раз перед созанием миграции
            //optionsBuilder.UseNpgsql<RPCSContext>("Server=localhost;Port=5432;Database=rpcs;Username=postgres;Password=jfgSvSD@gM;");

            return new RPCSContext(optionsBuilder.Options);
        }
    }
}