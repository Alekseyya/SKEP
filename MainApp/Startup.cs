using System;
using AutoMapper;
using BL.Implementation;
using Core.BL.Interfaces;
using Core.Config;
using Core.Data;
using Core.Helpers;
using Core.Models;
using Data;
using Data.Implementation;
using FluentValidation;
using FluentValidation.AspNetCore;
using MainApp.Common;
using MainApp.Helpers;
using MainApp.HtmlControls;
using MainApp.Quartz;
using MainApp.RBAC.Attributes;
using MainApp.ReportGenerators;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Server.IISIntegration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Serialization;
using Quartz;
using Quartz.Impl;
using Quartz.Spi;



namespace MainApp
{
    public class Startup
    {

        private readonly ILoggerFactory _factory;

        public IConfigurationRoot Configuration { get; set; }

        public Startup(IHostingEnvironment env, ILoggerFactory factory)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile("externalsettings.json", optional: false, reloadOnChange: true);

            Configuration = builder.Build();
            _factory = factory;
        }
        //Метод регистрации сервисов в приложении
        public IServiceProvider ConfigureServices(IServiceCollection services)
        {
            //Строки подключения бд
            services.AddScoped<DbContextOptions<RPCSContext>>(provider =>
            {
                var ob = new DbContextOptionsBuilder<RPCSContext>();
                //ob.UseLazyLoadingProxies();
                ob.UseNpgsql(Configuration["MyConfig:DbConnectionString"]);
                //TODO добавлен логгер
                ob.UseLoggerFactory(_factory);
                return ob.Options;
            });

            services.AddScoped<DbContextOptions<RPCSContextMysql>>(provider =>
            {
                var ob = new DbContextOptionsBuilder<RPCSContextMysql>();
                ob.UseMySql(Configuration["MyConfig:DbConnectionStringMysql"]);
                return ob.Options;
            });

            //TODO Добавлены сессии
            services.AddSession();

            //TODO Добавлен Kestrel
            //services.Configure<KestrelServerOptions>(
            //    Configuration.GetSection("Kestrel"));

            //для того чтобы работал атрибут [ApiController]
            services.AddMvc(config =>
                {
                    //Глобальный

                    #region Добавление экш фильров

                    config.Filters.Add(typeof(ReturnUrlController));


                    /*config.Filters.Add(typeof(AEmployeeAnyUpdate));

                    config.Filters.Add(typeof(AProjectDetailsView));
                    config.Filters.Add(typeof(AProjectsHoursReportView));

                    config.Filters.Add(typeof(ATSHoursRecordCreateUpdateMyHours));
                    config.Filters.Add(typeof(ATSHoursRecordPMApproveHours));*/ // - для чего это???

                    #endregion

                    //todo https://github.com/aspnet/AspNetCore/issues/5135 - UrlHelper полагается IRouterи не будет работать,
                    //когда включена маршрутизация конечной точки. Но лучше использовать LinkGenerator
                    config.EnableEndpointRouting = false;

                    config.ModelBinderProviders.Insert(0, new InvariantDoubleModelBinderProvider());
                    config.ModelBinderProviders.Insert(1, new InvariantDecimalModelBinderProvider());

                    //Todo для того, чтобы Json имел верблюжью нотацию!
                }).AddJsonOptions(options =>
                {
                    options.SerializerSettings.ContractResolver = new DefaultContractResolver(); 
                })
                .SetCompatibilityVersion(CompatibilityVersion.Version_2_2)
                .AddFluentValidation();
            
            //Todo возможро авторизация через куки нужна
            services.AddAuthentication(IISDefaults.AuthenticationScheme);
            services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie(options =>
            {
                options.LoginPath = "/Home/Index";
                options.LogoutPath = "/Home/LogOut";
            });

            //настройки проверки подленности Windows
            //services.Configure<IISServerOptions>(options =>
            //{
            //    options.AutomaticAuthentication = false;
            //});

            //автомаппер
            services.AddAutoMapper();
            
            //Регистрация Fluent валидатора
            services.AddTransient<IValidator<Project>, ProjectFluentValidator>();

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
            //    options.HttpsPort = 9999;
            //});


            #region Регистрация фильтров

            services.AddScoped<ReturnUrlController>();

            services.AddScoped<OperationActionFilter>();

            services.AddScoped<AEmployeeAnyUpdate>();
            services.AddScoped<AProjectDetailsView>();
            services.AddScoped<AProjectsHoursReportView>();
            services.AddScoped<ATSHoursRecordCreateUpdateMyHours>();
            services.AddScoped<ATSHoursRecordPMApproveHours>();

            #endregion

            #region Регистрация сервисов

            services.AddScoped<IRPCSDbAccessor, RPCSSingletonDbAccessor>();
            services.AddScoped<IRepositoryFactory, RPCSRepositoryFactory>();
            services.AddScoped<IUnitOfWork, RPCSUnitOfWork>();
            
            services.AddTransient<IAppPropertyService, AppPropertyService>();
            services.AddTransient<IBudgetLimitService, BudgetLimitService>();
            services.AddTransient<IEmployeeService, EmployeeService>();
            services.AddTransient<IDepartmentService, DepartmentService>();
            services.AddTransient<IProductionCalendarService, ProductionCalendarService>();
            services.AddTransient<IEmployeeOrganisationService, EmployeeOrganisationService>();
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
            services.AddTransient<IExcelService, ExcelService>();
            services.AddTransient<IReflectionService, ReflectionService>();
            services.AddTransient<IProjectExternalWorkspaceService, ProjectExternalWorkspaceService>();
            services.AddTransient<IProjectStatusRecordEntryService, ProjectStatusRecordEntryService>();
            services.AddTransient<IServiceService, ServiceService>();
            services.AddTransient<IEmployeeGradParamService, EmployeeGradParamService>();

            //TODO посмотреть как будет с новой конфигурацией, ниже строчку не убирать
            services.AddScoped<IApplicationUserService, ApplicationUserService>();
            services.AddScoped<IPermissionValidatorService, PermissionValidatorService>();

            #endregion
            
            //Контекст акцессор
            services.AddHttpContextAccessor();

            //Сервисы для хелперов
            services.AddTransient<IGenerateAnchorService, GenerateAnchorService>();

            //Регистрация опций
            services.AddOptions();

            services.AddSingleton<IActionContextAccessor, ActionContextAccessor>();
            services.AddScoped<IUrlHelper>(x => {

                var actionContext = x.GetRequiredService<IActionContextAccessor>().ActionContext;
                var factory = x.GetRequiredService<IUrlHelperFactory>();
                return factory.GetUrlHelper(actionContext);
            });

            services.AddMemoryCache();

            #region ConfigRegisstration
            services.AddSingleton<SMTPConfig>();
            services.AddSingleton<CommonConfig>();
            services.AddSingleton<DaykassaConfig>();
            services.AddSingleton<JiraConfig>();
            services.AddSingleton<DBDataProcessingConfig>();

            services.Configure<MyConfig>(Configuration.GetSection("MyConfig"));
            services.Configure<OnlyOfficeConfig>(Configuration.GetSection("OnlyOffice"));
            services.Configure<ADConfig>(Configuration.GetSection("AD"));
            services.Configure<TimesheetConfig>(Configuration.GetSection("Timesheet"));
            services.Configure<BitrixConfig>(Configuration.GetSection("Bitrix"));
            services.Configure<SMTPConfig>(Configuration.GetSection("SMTP"));
            services.Configure<CommonConfig>(Configuration.GetSection("Common"));
            services.Configure<DaykassaConfig>(Configuration.GetSection("Daykassa"));
            services.Configure<JiraConfig>(Configuration.GetSection("Jira"));
            services.Configure<DBDataProcessingConfig>(Configuration.GetSection("DBDataProcessing"));
            #endregion


            #region Quartz
            services.AddSingleton<IJobFactory, JobFactory>();
            services.AddSingleton<ISchedulerFactory, StdSchedulerFactory>();
            services.AddHostedService<QuartzHostedService>();

            services.AddSingleton<ADSyncJob>();
            services.AddSingleton<BitrixSyncJob>();
            services.AddSingleton<TimesheetJob>();
            services.AddSingleton<DBDataProcessingJob>();
            #endregion

            return services.BuildServiceProvider();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        //Как приложение будет обрабатывать запрос
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, IApplicationLifetime lifetime,
         IOptions<ADConfig> adOptions, IOptions<BitrixConfig> bitrixConfig, IOptions<OnlyOfficeConfig> onlyOfficeOptions,
         IOptions<TimesheetConfig> timesheetOptions, IOptions<SMTPConfig> smtpOptions, IOptions<CommonConfig> commonOptions,
            IServiceProvider serviceProvider)
        {

            InitializeDatabase(app);
            if (env.IsDevelopment())
            {
                //Раскоментить, когда сделаем кастомные ошибки!!!
                //app.UseDeveloperExceptionPage();
                app.UseDatabaseErrorPage();
                app.UseBrowserLink();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                //Todo отображение ошибок в ProductionEnvironment - позже убрать!
                //app.UseDeveloperExceptionPage();
                //app.UseHsts();
            }
            //TODO В будущем убрать из продакшн версии отображение страницы девелопмент ошибок
            app.UseDeveloperExceptionPage();

            //Страницы ошибок
            app.UseStatusCodePages(async context =>
            {
                context.HttpContext.Response.ContentType = "text/plain";

                await context.HttpContext.Response.WriteAsync(
                    "Status code page, status code: " +
                    context.HttpContext.Response.StatusCode);
            });

            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseCookiePolicy();
            app.UseAuthentication();
            app.UseSession();

            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "timesheetRoute",
                    template: "{area:exists}/{controller=Timesheet}/{action=Index}/{id?}"
                );

                routes.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=Index}/{id?}");
            });


            //Quartz 
            //var quartz = new QuartZStartup(adOptions, bitrixConfig, onlyOfficeOptions, timesheetOptions);
            //lifetime.ApplicationStarted.Register(quartz.Start);
            //lifetime.ApplicationStopped.Register(quartz.Stop);
            //QuartzHelper.Configure(quartz.scheduler);



            RpcsControls.Configure(app.ApplicationServices.GetService<IServiceProvider>());
            AutocompleteControls.Configure(app.ApplicationServices.GetService<IServiceProvider>());
            PermissionControls.Configure(app.ApplicationServices.GetService<IServiceProvider>());
            RPCSHelper.Configure(app.ApplicationServices.GetService<IServiceProvider>());
            RPCSEmailHelper.Configure(app.ApplicationServices.GetService<IServiceProvider>());
            Daykassa.Configure(app.ApplicationServices.GetService<IServiceProvider>());

        }
        //см. http://jaliyaudagedara.blogspot.com/2017/10/ef-core-automatic-migrations.html
        private void InitializeDatabase(IApplicationBuilder app)
        {
            
            using (var scope = app.ApplicationServices.GetService<IServiceScopeFactory>().CreateScope())
            {
                //var options = scope.ServiceProvider.GetRequiredService<DbContextOptions<RPCSContext>>();
                using (RPCSContext contex = scope.ServiceProvider.GetRequiredService<IRPCSDbAccessor>().GetDbContext())
                {
                    contex.Database.Migrate();
                }
            }
        }
    }
}
