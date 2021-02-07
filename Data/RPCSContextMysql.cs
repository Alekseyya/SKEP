using Core.Extensions;
using Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Data
{
    public class RPCSContextMysql : DbContext
    {
        public RPCSContextMysql(DbContextOptions<RPCSContextMysql> options) : base(options)
        {}

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

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            //TODO Включение плюрализации
            //https://stackoverflow.com/questions/37493095/entity-framework-core-rc2-table-name-pluralization
            //modelBuilder.Conventions.Remove<PluralizingTableNameConvention>();
            modelBuilder.RemovePluralizingTableNameConvention();

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


            //////Todo не уврен что это правильно
            modelBuilder.Entity<Project>().Ignore(x => x.ParentProject);
            modelBuilder.Entity<Project>().Ignore(x => x.ChildProjects);
            modelBuilder.Entity<Project>().Ignore(x => x.Versions);
            modelBuilder.Entity<CostItem>().Ignore(x => x.Versions);
            modelBuilder.Entity<CostSubItem>().Ignore(x => x.Versions);
            modelBuilder.Entity<BudgetLimit>().Ignore(x => x.Versions);
            modelBuilder.Entity<Department>().Ignore(x => x.Versions);
            modelBuilder.Entity<Employee>().Ignore(x => x.Versions);
            modelBuilder.Entity<ProjectScheduleEntry>().Ignore(x => x.Versions);
            modelBuilder.Entity<ProjectStatusRecord>().Ignore(x => x.Versions);
            modelBuilder.Entity<TSAutoHoursRecord>().Ignore(x => x.Versions);
            modelBuilder.Entity<TSHoursRecord>().Ignore(x => x.Versions);
            modelBuilder.Entity<VacationRecord>().Ignore(x => x.Versions);

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
        }
    }

    public class RPCSContextMysqlDbFactory : IDesignTimeDbContextFactory<RPCSContextMysql>
    {
        RPCSContextMysql IDesignTimeDbContextFactory<RPCSContextMysql>.CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<RPCSContextMysql>();
            optionsBuilder.UseMySql<RPCSContextMysql>("server=localhost;port=3306;database=RPCS;uid=root;CharSet=utf8;");

            return new RPCSContextMysql(optionsBuilder.Options);
        }
    }
}
