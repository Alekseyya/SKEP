using System;
using System.Security.Principal;
using Core.BL.Interfaces;
using Core.Models;
using Core.Models.RBAC;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;

namespace MainApp.RBAC.Attributes
{
    [AttributeUsage(AttributeTargets.Method, Inherited = true, AllowMultiple = false)]
    public class AProjectDetailsView : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            try
            {
                var permissionValidator = filterContext.HttpContext.RequestServices.GetService<IPermissionValidatorService>();
                var projectService = filterContext.HttpContext.RequestServices.GetService<IProjectService>();
                var applicationUserService = filterContext.HttpContext.RequestServices.GetService<IApplicationUserService>();

                IPrincipal user = filterContext.HttpContext.User;
                
                if (!permissionValidator.HasAccess(user, Operation.ProjectView | Operation.ProjectMyProjectView | Operation.ProjectMyDepartmentProjectView))
                {
                    filterContext.Result = NoPermissionResult.Generate();
                }
                else if (permissionValidator.HasAccess(user, Operation.ProjectMyProjectView | Operation.ProjectMyDepartmentProjectView)
                    && !permissionValidator.HasAccess(user, Operation.ProjectView))
                {
                    ApplicationUser applicationUser = applicationUserService.GetUser();
                    int? projectId = null;
                    try
                    {
                        projectId = filterContext.ActionArguments["id"] as int?;
                    }
                    catch (Exception)
                    {
                    }

                    if (projectId == null)
                    {
                        try
                        {
                            projectId = filterContext.ActionArguments["projectID"] as int?;
                        }
                        catch (Exception)
                        {
                        }

                        if (projectId == null)
                        {
                            try
                            {
                                projectId = filterContext.ActionArguments["projectid"] as int?;
                            }
                            catch (Exception)
                            {
                            }
                        }
                    }

                    if (projectId != null)
                    {
                        Project project = projectService.GetById(projectId.Value); 

                        if (project == null)
                        {
                            filterContext.Result = NoPermissionResult.Generate();
                        }
                        else if (applicationUserService.IsMyProject(project) == false)
                        {
                            filterContext.Result = NoPermissionResult.Generate();
                        }
                    }
                    else
                    {
                        filterContext.Result = NoPermissionResult.Generate();
                    }
                }
            }
            catch (Exception)
            {
                filterContext.Result = NoPermissionResult.Generate();
            }
        }

    }

    [AttributeUsage(AttributeTargets.Method, Inherited = true, AllowMultiple = false)]
    public class AProjectsHoursReportView : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            try
            {
                var permissionValidator = filterContext.HttpContext.RequestServices.GetService<IPermissionValidatorService>();
                IPrincipal user = filterContext.HttpContext.User;
                if (!permissionValidator.HasAccess(user, Operation.ProjectsHoursReportView | Operation.ProjectsHoursReportViewForManagedEmployees))
                    filterContext.Result = NoPermissionResult.Generate();
            }
            catch
            {
                filterContext.Result = NoPermissionResult.Generate();
            }
        }

    }

    [AttributeUsage(AttributeTargets.Method, Inherited = true, AllowMultiple = false)]
    public class AProjectStatusRecordDetailsView : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            try
            {
                var permissionValidator = filterContext.HttpContext.RequestServices.GetService<IPermissionValidatorService>();
                var projectStatusRecordService = filterContext.HttpContext.RequestServices.GetService<IProjectStatusRecordService>();
                var applicationUserService = filterContext.HttpContext.RequestServices.GetService<IApplicationUserService>();
                IPrincipal user = filterContext.HttpContext.User;

                if (!permissionValidator.HasAccess(user, Operation.ProjectView | Operation.ProjectStatusRecordView | Operation.ProjectMyProjectView | Operation.ProjectMyDepartmentProjectView))
                {
                    filterContext.Result = NoPermissionResult.Generate();
                }
                else if (permissionValidator.HasAccess(user, Operation.ProjectMyProjectView | Operation.ProjectMyDepartmentProjectView)
                         && !permissionValidator.HasAccess(user, Operation.ProjectView | Operation.ProjectStatusRecordView)) 
                { 
                    ApplicationUser applicationUser = applicationUserService.GetUser();
                    int? id = null;
                    try
                    {
                        id = filterContext.ActionArguments["id"] as int?;
                    }
                    catch (Exception)
                    {
                    }

                    if (id != null)
                    {
                        ProjectStatusRecord projectStatusRecord =  projectStatusRecordService.GetById(id.Value);
                        Project project = projectStatusRecord.Project;

                        if (project == null)
                        {
                            filterContext.Result = NoPermissionResult.Generate();
                        }
                        else if (applicationUserService.IsMyProject(project) == false)
                        {
                            filterContext.Result = NoPermissionResult.Generate();
                        }
                    }
                    else
                    {
                        filterContext.Result = NoPermissionResult.Generate();
                    }
                }
            }
            catch (Exception)
            {
                filterContext.Result = NoPermissionResult.Generate();
            }
        }

    }

    [AttributeUsage(AttributeTargets.Method, Inherited = true, AllowMultiple = false)]
    public class AProjectStatusRecordCreateUpdate : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            try
            {
                var permissionValidator = filterContext.HttpContext.RequestServices.GetService<IPermissionValidatorService>();
                var projectService = filterContext.HttpContext.RequestServices.GetService<IProjectService>();
                var applicationUserService = filterContext.HttpContext.RequestServices.GetService<IApplicationUserService>();
                var projectStatusRecordService = filterContext.HttpContext.RequestServices.GetService<IProjectStatusRecordService>();
                
                IPrincipal user = filterContext.HttpContext.User;

                if (!permissionValidator.HasAccess(user, Operation.ProjectCreateUpdate | Operation.ProjectStatusRecordCreateUpdate | Operation.ProjectMyProjectView | Operation.ProjectMyDepartmentProjectView))
                {
                    filterContext.Result = NoPermissionResult.Generate();
                }
                else if (permissionValidator.HasAccess(user, Operation.ProjectMyProjectView | Operation.ProjectMyDepartmentProjectView)
                         && !permissionValidator.HasAccess(user, Operation.ProjectCreateUpdate | Operation.ProjectStatusRecordCreateUpdate))
                {
                    ApplicationUser applicationUser = applicationUserService.GetUser();
                    int? id = null;
                    int? projectId = null;
                    try
                    {
                        id = filterContext.ActionArguments["id"] as int?;
                    }
                    catch (Exception)
                    {
                    }

                    if (id == null)
                    {
                        try
                        {
                            projectId = filterContext.ActionArguments["projectID"] as int?;
                        }
                        catch (Exception)
                        {
                        }

                        if (projectId == null)
                        {
                            try
                            {
                                projectId = filterContext.ActionArguments["projectid"] as int?;
                            }
                            catch (Exception)
                            {
                            }
                        }
                    }

                    if (id != null || projectId != null)
                    {
                        Project project = null;

                        if (id != null)
                        {
                            ProjectStatusRecord projectStatusRecord = projectStatusRecordService.GetById(id.Value);
                            project = projectStatusRecord.Project;
                        }
                        else
                        {
                            project = projectService.GetById(projectId.Value);
                        }

                        if (project == null)
                        {
                            filterContext.Result = NoPermissionResult.Generate();
                        }
                        else if (applicationUserService.IsMyProject(project) == false)
                        {
                            filterContext.Result = NoPermissionResult.Generate();
                        }
                    }
                    else
                    {
                        filterContext.Result = NoPermissionResult.Generate();
                    }
                }
            }
            catch (Exception)
            {
                filterContext.Result = NoPermissionResult.Generate();
            }
        }

    }

    [AttributeUsage(AttributeTargets.Method, Inherited = true, AllowMultiple = false)]
    public class AProjectScheduleEntryDetailsView : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            try
            {
                var permissionValidator = filterContext.HttpContext.RequestServices.GetService<IPermissionValidatorService>();
                var applicationUserService = filterContext.HttpContext.RequestServices.GetService<IApplicationUserService>();
                var projectScheduleEntryService = filterContext.HttpContext.RequestServices.GetService<IProjectScheduleEntryService>();
                IPrincipal user = filterContext.HttpContext.User;

                if (!permissionValidator.HasAccess(user, Operation.ProjectView | Operation.ProjectScheduleEntryView | Operation.ProjectMyProjectView | Operation.ProjectMyDepartmentProjectView))
                {
                    filterContext.Result = NoPermissionResult.Generate();
                }
                else if (permissionValidator.HasAccess(user, Operation.ProjectMyProjectView | Operation.ProjectMyDepartmentProjectView)
                    && !permissionValidator.HasAccess(user, Operation.ProjectView | Operation.ProjectScheduleEntryView))
                {
                    ApplicationUser applicationUser = applicationUserService.GetUser();
                    int? id = null;
                    try
                    {
                        id = filterContext.ActionArguments["id"] as int?;
                    }
                    catch (Exception)
                    {
                    }

                    if (id != null)
                    {
                        ProjectScheduleEntry projectScheduleEntry = projectScheduleEntryService.GetById(id.Value);
                        Project project = projectScheduleEntry.Project;

                        if (project == null)
                        {
                            filterContext.Result = NoPermissionResult.Generate();
                        }
                        else if (applicationUserService.IsMyProject(project) == false)
                        {
                            filterContext.Result = NoPermissionResult.Generate();
                        }
                    }
                    else
                    {
                        filterContext.Result = NoPermissionResult.Generate();
                    }
                }
            }
            catch (Exception)
            {
                filterContext.Result = NoPermissionResult.Generate();
            }
        }

    }

    [AttributeUsage(AttributeTargets.Method, Inherited = true, AllowMultiple = false)]
    public class AProjectScheduleEntryCreateUpdate : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            try
            {
                var permissionValidator = filterContext.HttpContext.RequestServices.GetService<IPermissionValidatorService>();
                var projectService = filterContext.HttpContext.RequestServices.GetService<IProjectService>();
                var applicationUserService = filterContext.HttpContext.RequestServices.GetService<IApplicationUserService>();
                var projectScheduleEntryService = filterContext.HttpContext.RequestServices.GetService<IProjectScheduleEntryService>();
                IPrincipal user = filterContext.HttpContext.User;

                if (!permissionValidator.HasAccess(user, Operation.ProjectCreateUpdate | Operation.ProjectScheduleEntryCreateUpdate | Operation.ProjectMyProjectView | Operation.ProjectMyDepartmentProjectView))
                {
                    filterContext.Result = NoPermissionResult.Generate();
                }
                else if (permissionValidator.HasAccess(user, Operation.ProjectMyProjectView | Operation.ProjectMyDepartmentProjectView)
                    && !permissionValidator.HasAccess(user, Operation.ProjectCreateUpdate | Operation.ProjectScheduleEntryCreateUpdate))
                {
                    ApplicationUser applicationUser = applicationUserService.GetUser();
                    int? id = null;
                    int? projectId = null;
                    try
                    {
                        id = filterContext.ActionArguments["id"] as int?;
                    }
                    catch (Exception)
                    {
                    }

                    if (id == null)
                    {
                        try
                        {
                            projectId = filterContext.ActionArguments["projectID"] as int?;
                        }
                        catch (Exception)
                        {
                        }

                        if (projectId == null)
                        {
                            try
                            {
                                projectId = filterContext.ActionArguments["projectid"] as int?;
                            }
                            catch (Exception)
                            {
                            }
                        }
                    }

                    if (id != null || projectId != null)
                    {
                        Project project = null;

                        if (id != null)
                        {
                            ProjectScheduleEntry projectScheduleEntry = projectScheduleEntryService.GetById(id.Value);
                            project = projectScheduleEntry.Project;
                        }
                        else
                        {
                            project = projectService.GetById(projectId.Value);
                        }

                        if (project == null)
                        {
                            filterContext.Result = NoPermissionResult.Generate();
                        }
                        else if (applicationUserService.IsMyProject(project) == false)
                        {
                            filterContext.Result = NoPermissionResult.Generate();
                        }
                    }
                    else
                    {
                        filterContext.Result = NoPermissionResult.Generate();
                    }
                }
            }
            catch (Exception)
            {
                filterContext.Result = NoPermissionResult.Generate();
            }
        }

    }


    [AttributeUsage(AttributeTargets.Method, Inherited = true, AllowMultiple = false)]
    public class AProjectExternalWorkspaceDetailsView : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            try
            {
                var permissionValidator = filterContext.HttpContext.RequestServices.GetService<IPermissionValidatorService>();
                var applicationUserService = filterContext.HttpContext.RequestServices.GetService<IApplicationUserService>();
                var projectExternalWorkspaceService = filterContext.HttpContext.RequestServices.GetService<IProjectExternalWorkspaceService>();
                IPrincipal user = filterContext.HttpContext.User;

                if (!permissionValidator.HasAccess(user, Operation.ProjectView | Operation.ProjectExternalWorkspaceView | Operation.ProjectMyProjectView | Operation.ProjectMyDepartmentProjectView))
                {
                    filterContext.Result = NoPermissionResult.Generate();
                }
                else if (permissionValidator.HasAccess(user, Operation.ProjectMyProjectView | Operation.ProjectMyDepartmentProjectView)
                         && !permissionValidator.HasAccess(user, Operation.ProjectView | Operation.ProjectExternalWorkspaceView))
                {
                    ApplicationUser applicationUser = applicationUserService.GetUser();
                    int? id = null;
                    try
                    {
                        id = filterContext.ActionArguments["id"] as int?;
                    }
                    catch (Exception)
                    {
                    }

                    if (id != null)
                    {
                        ProjectExternalWorkspace projectExternalWorkspace = projectExternalWorkspaceService.GetById((int)id);
                        Project project = projectExternalWorkspace.Project;

                        if (project == null)
                        {
                            filterContext.Result = NoPermissionResult.Generate();
                        }
                        else if (applicationUserService.IsMyProject(project) == false)
                        {
                            filterContext.Result = NoPermissionResult.Generate();
                        }
                    }
                    else
                    {
                        filterContext.Result = NoPermissionResult.Generate();
                    }
                }
            }
            catch (Exception)
            {
                filterContext.Result = NoPermissionResult.Generate();
            }
        }

    }

    [AttributeUsage(AttributeTargets.Method, Inherited = true, AllowMultiple = false)]
    public class AProjectExternalWorkspaceCreateUpdate : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            try
            {
                var permissionValidator = filterContext.HttpContext.RequestServices.GetService<IPermissionValidatorService>();
                var applicationUserService = filterContext.HttpContext.RequestServices.GetService<IApplicationUserService>();
                var projectExternalWorkspaceService = filterContext.HttpContext.RequestServices.GetService<IProjectExternalWorkspaceService>();
                var projectService = filterContext.HttpContext.RequestServices.GetService<IProjectService>();
                IPrincipal user = filterContext.HttpContext.User;

                if (!permissionValidator.HasAccess(user, Operation.ProjectCreateUpdate | Operation.ProjectExternalWorkspaceCreateUpdate | Operation.ProjectMyProjectView | Operation.ProjectMyDepartmentProjectView))
                {
                    filterContext.Result = NoPermissionResult.Generate();
                }
                else if (permissionValidator.HasAccess(user, Operation.ProjectMyProjectView | Operation.ProjectMyDepartmentProjectView)
                         && !permissionValidator.HasAccess(user, Operation.ProjectCreateUpdate | Operation.ProjectExternalWorkspaceCreateUpdate))
                {
                    int? id = null;
                    int? projectId = null;
                    try
                    {
                        id = filterContext.ActionArguments["id"] as int?;
                    }
                    catch (Exception)
                    {
                    }

                    if (id == null)
                    {
                        try
                        {
                            projectId = filterContext.ActionArguments["projectID"] as int?;
                        }
                        catch (Exception)
                        {
                        }

                        if (projectId == null)
                        {
                            try
                            {
                                projectId = filterContext.ActionArguments["projectid"] as int?;
                            }
                            catch (Exception)
                            {
                            }
                        }
                    }

                    if (id != null || projectId != null)
                    {
                        Project project = null;

                        if (id != null)
                        {
                            ProjectExternalWorkspace projectExternalWorkspace = projectExternalWorkspaceService.GetById((int) id);
                            project = projectExternalWorkspace.Project;
                        }
                        else
                        {
                            project = projectService.GetById((int)projectId);
                        }

                        if (project == null)
                        {
                            filterContext.Result = NoPermissionResult.Generate();
                        }
                        else if (applicationUserService.IsMyProject(project) == false)
                        {
                            filterContext.Result = NoPermissionResult.Generate();
                        }
                    }
                    else
                    {
                        filterContext.Result = NoPermissionResult.Generate();
                    }
                }
            }
            catch (Exception)
            {
                filterContext.Result = NoPermissionResult.Generate();
            }
        }

    }


}