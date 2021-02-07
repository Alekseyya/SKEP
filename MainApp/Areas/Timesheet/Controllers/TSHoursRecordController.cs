using Core.BL.Interfaces;
using Core.Config;
using MainApp.Common;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;




namespace MainApp.Areas.Timesheet.Controllers
{
    [Area("Timesheet")]
    public class TSHoursRecordController : BaseTimesheetController
    {
        public TSHoursRecordController(IEmployeeService employeeService, ITSHoursRecordService tsHoursRecordService,
            IProjectService projectService,
            IProjectMembershipService projectMembershipService,
            IUserService userService,
            ITSAutoHoursRecordService autoHoursRecordService,
            IVacationRecordService vacationRecordService,
            IReportingPeriodService reportingPeriodService,
            IDepartmentService departmentService,
            IProductionCalendarService productionCalendarService,
            IEmployeeCategoryService employeeCategoryService, IJiraService jiraService, IOptions<JiraConfig> jiraOptions, IApplicationUserService applicationUserService) : base(employeeService,
            tsHoursRecordService, projectService, projectMembershipService, userService,
            autoHoursRecordService, vacationRecordService, reportingPeriodService, departmentService, productionCalendarService, employeeCategoryService, jiraService, jiraOptions, applicationUserService)
        { }
    }
}