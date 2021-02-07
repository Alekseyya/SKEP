using AutoMapper;
using BL.Implementation;
using Core.BL.Interfaces;
using Core.Data;
using Data;
using Data.Implementation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Server.IISIntegration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace WebApi
{
    public class Startup
    {
        public IConfigurationRoot Configuration { get; set; }

        public Startup(IHostingEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
            Configuration = builder.Build();
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            //Строки подключения бд
            services.AddScoped<DbContextOptions<RPCSContext>>(provider =>
            {
                var ob = new DbContextOptionsBuilder<RPCSContext>();
                //ob.UseLazyLoadingProxies();
                ob.UseNpgsql(Configuration["MyConfig:DbConnectionString"]);
                return ob.Options;
            });

            services.AddScoped<DbContextOptions<RPCSContextMysql>>(provider =>
            {
                var ob = new DbContextOptionsBuilder<RPCSContextMysql>();
                ob.UseMySql(Configuration["MyConfig:DbConnectionStringMysql"]);
                return ob.Options;
            });

            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_2);

            //автомаппер
            services.AddAutoMapper();

            //Настройка https
            //services.AddHsts(options =>
            //{
            //    options.Preload = true;
            //    options.IncludeSubDomains = true;
            //    options.MaxAge = TimeSpan.FromDays(60);
            //    options.ExcludedHosts.Add("example.com");
            //    options.ExcludedHosts.Add("www.example.com");
            //});
            //services.AddHttpsRedirection(options =>
            //{
            //    options.RedirectStatusCode = StatusCodes.Status307TemporaryRedirect;
            //    options.HttpsPort = 5001;
            //});

            services.AddAuthentication(IISDefaults.AuthenticationScheme);

            #region Регистрация сервисов

            services.AddScoped<IRPCSDbAccessor, RPCSSingletonDbAccessor>();
            services.AddScoped<IRepositoryFactory, RPCSRepositoryFactory>();
            services.AddScoped<IUnitOfWork, RPCSUnitOfWork>();

            services.AddTransient<IAppPropertyService, AppPropertyService>();
            services.AddTransient<IBudgetLimitService, BudgetLimitService>();
            services.AddTransient<IEmployeeService, EmployeeService>();
            services.AddTransient<IDepartmentService, DepartmentService>();
            services.AddTransient<IProductionCalendarService, ProductionCalendarService>();
            services.AddTransient<IProjectMembershipService, ProjectMembershipService>();
            services.AddTransient<IProjectStatusRecordService, ProjectStatusRecordService>();
            services.AddTransient<IProjectScheduleEntryService, ProjectScheduleEntryService>();
            services.AddTransient<IProjectService, ProjectService>();
            services.AddTransient<IEmployeeWorkloadService, EmployeeWorkloadService>();
            services.AddTransient<IUserService, UserService>();
            services.AddTransient<ITSHoursRecordService, TSHoursRecordService>();
            services.AddTransient<ITSAutoHoursRecordService, TSAutoHoursRecordService>();
            services.AddTransient<IReportingPeriodService, ReportingPeriodService>();
            services.AddTransient<IVacationRecordService, VacationRecordService>();
            services.AddTransient<IFinanceService, FinanceService>();
            services.AddTransient<IQualifyingRoleService, QualifyingRoleService>();
            services.AddTransient<IEmployeeCategoryService, EmployeeCategoryService>();
            services.AddTransient<IEmployeeGradService, EmployeeGradService>();
            services.AddTransient<IEmployeeQualifyingRoleService, EmployeeQualifyingRoleService>();
            services.AddTransient<IQualifyingRoleRateService, QualifyingRoleRateService>();
            services.AddTransient<IProjectReportRecordService, ProjectReportRecordService>();
            services.AddTransient<IExpensesRecordService, ExpensesRecordService>();
            services.AddTransient<ICostSubItemService, CostSubItemService>();
            services.AddTransient<ICostItemService, CostItemService>();
            services.AddTransient<IEmployeeDepartmentAssignmentService, EmployeeDepartmentAssignmentService>();
            services.AddTransient<IEmployeeGradAssignmentService, EmployeeGradAssignmentService>();
            services.AddTransient<IEmployeeLocationService, EmployeeLocationService>();
            services.AddTransient<IEmployeePositionAssignmentService, EmployeePositionAssignmentService>();
            services.AddTransient<IEmployeePositionOfficialAssignmentService, EmployeePositionOfficialAssignmentService>();
            services.AddTransient<IEmployeePositionOfficialService, EmployeePositionOfficialService>();
            services.AddTransient<IEmployeePositionService, EmployeePositionService>();
            services.AddTransient<IJiraService, JiraService>();
            services.AddTransient<ITimesheetService, TimesheetService>();
            services.AddTransient<IOOService, OOService>();
            services.AddTransient<IProjectTypeService, ProjectTypeService>();
            services.AddTransient<IOrganisationService, OrganisationService>();
            services.AddTransient<IProjectScheduleEntryTypeService, ProjectScheduleEntryTypeService>();
            services.AddTransient<IProjectRoleService, ProjectRoleService>();

            #endregion

            //Контекст акцессор
            services.AddHttpContextAccessor();

        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseBrowserLink();
            }
            else
            {
                app.UseHsts();
            }

            app.UseStatusCodePages();
            app.UseHttpsRedirection();
            app.UseMvcWithDefaultRoute();
        }
    }
}
