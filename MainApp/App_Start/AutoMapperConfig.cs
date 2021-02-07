using AutoMapper;
using Core.Models;
using MainApp.Dto;
using MainApp.ViewModels.ProjectStatusRecord;



namespace MainApp.App_Start
{
    public static class AutoMapperConfig
    {
        public static void Configure()
        {
            Mapper.Initialize(cfg =>
            {
                //cfg.AddProfile<ProjectProfile>();

            });

        }

        #region Employee
        public class EmployeeGetActualEmployeeDetailsListProfile : Profile
        {
            public EmployeeGetActualEmployeeDetailsListProfile()
            {
                CreateMap<BaseModel, BasicEmployeeDto>()
                    .Include<Employee, EmployeeDetailsDto>();
                CreateMap<Employee, EmployeeDetailsDto>();
                CreateMap<EmployeeGrad, EmployeeGradDto>();
                CreateMap<EmployeePosition, EmployeePositionDto>();
                CreateMap<Department, BasicDepartmentDto>();
                CreateMap<EmployeeLocation, EmployeeLocationDto>();
            }
        }
        #endregion

        public class HoursRecordIndexProfile : Profile
        {
            public HoursRecordIndexProfile()
            {
                CreateMap<Project, Project>()
                    .ForMember(recordDTO => recordDTO.ShortName, project => project.MapFrom(prop => prop.ShortName))
                    .ForAllOtherMembers(opt => opt.Ignore());
                CreateMap<Employee, Employee>()
                    .ForMember(employeeDTO => employeeDTO.LastName, record => record.MapFrom(prop => prop.LastName))
                    .ForAllOtherMembers(otp => otp.Ignore());
                CreateMap<Department, BasicDepartmentDto>();
                CreateMap<TSHoursRecord, TSHoursRecord>()
                    .ForMember(recordDTO => recordDTO.ID, record => record.MapFrom(prop => prop.ID))
                    .ForMember(recordDTO => recordDTO.Employee, record => record.MapFrom(prop => prop.Employee))
                    .ForMember(recordDTO => recordDTO.Project, record => record.MapFrom(prop => prop.Project))
                    .ForMember(recordDTO => recordDTO.RecordDate, record => record.MapFrom(prop => prop.RecordDate))
                    .ForMember(recordDTO => recordDTO.Hours, record => record.MapFrom(prop => prop.Hours))
                    .ForMember(recordDTO => recordDTO.Description, record => record.MapFrom(prop => prop.Description))
                    .ForMember(recordDTO => recordDTO.RecordStatus, record => record.MapFrom(prop => prop.RecordStatus))
                    .ForMember(recordDTO => recordDTO.Created, record => record.MapFrom(prop => prop.Created))
                    .ForAllOtherMembers(opt => opt.Ignore());
            }
        }

        #region BudgetLimit
        public class BudgetLimitSetViewBag : Profile
        {
            public BudgetLimitSetViewBag()
            {
                CreateMap<Project, Project>()
                    .ForMember(recordDto => recordDto.ID, record => record.MapFrom(p => p.ID))
                    .ForMember(recordDto => recordDto.ShortName, record => record.MapFrom(p => p.ShortName))
                    .ForAllOtherMembers(opt => opt.Ignore());
            }
        }


        #endregion

        #region Projet
        public class ProjectSetViewBag : Profile
        {
            public ProjectSetViewBag()
            {
                CreateMap<Project, Project>()
                    .ForMember(recordDto => recordDto.ID, record => record.MapFrom(p => p.ID))
                    .ForMember(recordDto => recordDto.ShortName, record => record.MapFrom(p => p.ShortName))
                    .ForAllOtherMembers(opt => opt.Ignore());

            }
        }

        #endregion
        public class ProjectProfile : Profile
        {
            public ProjectProfile()
            {
                CreateMap<Project, BasicProjectDto>();
            }
        }

        #region Профили для ProjectStatusRecord
        public class ProjectStatusRecordGetStatusRecordsProfile : Profile
        {
            public ProjectStatusRecordGetStatusRecordsProfile()
            {
                CreateMap<ProjectStatusRecord, ProjectStatusRecordDto>()
                    .ForMember(recordDTO => recordDTO.Project, record => record.Ignore())
                    .ForMember(recordDTO => recordDTO.Author, record => record.MapFrom(x => x.Author))
                    .ForMember(recordDTO => recordDTO.SupervisorComments, record => record.MapFrom(x => x.SupervisorComments))
                    .ForMember(recordDTO => recordDTO.ProblemsText, record => record.MapFrom(x => x.ProblemsText))
                    .ForMember(recordDTO => recordDTO.ProposedSolutionText, record => record.MapFrom(x => x.ProposedSolutionText))
                    .ForMember(recordDTO => recordDTO.StatusInfoText, record => record.MapFrom(x => x.StatusInfoText))
                    .ForMember(recordDTO => recordDTO.StatusInfoHtml, record => record.MapFrom(x => x.StatusInfoHtml));
            }
        }

        #endregion


        #region Профили для ProjectReportRecord
        public class ProjectReportRecordGetReportRecordsProfile : Profile
        {
            public ProjectReportRecordGetReportRecordsProfile()
            {
                CreateMap<ProjectReportRecord, ProjectReportRecordDto>()
                    .ForMember(recordDTO => recordDTO.Employee, record => record.Ignore())
                    .ForMember(recordDTO => recordDTO.Project, record => record.Ignore())
                    .ForMember(recordDTO => recordDTO.RecordSortKey, record => record.Ignore());
            }
        }

        public class ProjectReportRecordGetReportRecordsByEmployeeProfile : Profile
        {
            public ProjectReportRecordGetReportRecordsByEmployeeProfile()
            {
                CreateMap<EmployeePosition, EmployeePositionDto>()
                    .ForMember(recordDTO => recordDTO.Title, record => record.MapFrom(prop => prop.Title))
                    .ForAllOtherMembers(opt => opt.Ignore());
                CreateMap<Employee, BasicEmployeeDto>()
                    .ForMember(recordDTO => recordDTO.FullName, record => record.MapFrom(prop => prop.FullName))
                    .ForMember(recordDTO => recordDTO.EmployeePosition, record => record.MapFrom(prop => prop.EmployeePosition))
                    .ForMember(recordDTO => recordDTO.EmployeePositionTitle, record => record.MapFrom(prop => prop.EmployeePositionTitle))
                    .ForAllOtherMembers(opt => opt.Ignore());
                CreateMap<Department, BasicDepartmentDto>();

                CreateMap<ProjectReportRecord, ProjectReportRecordDto>()
                    .ForMember(recordDTO => recordDTO.Employee, record => record.MapFrom(prop => prop.Employee))
                    .ForMember(recordDTO => recordDTO.ID, record => record.MapFrom(prop => prop.ID))
                    .ForMember(recordDTO => recordDTO.ReportPeriodName, record => record.MapFrom(prop => prop.ReportPeriodName))
                    .ForMember(recordDTO => recordDTO.CalcDate, record => record.MapFrom(prop => prop.CalcDate))
                    .ForMember(recordDTO => recordDTO.Hours, record => record.MapFrom(prop => prop.Hours))
                    .ForMember(recordDTO => recordDTO.Comments, record => record.MapFrom(prop => prop.Comments))
                    .ForAllOtherMembers(opt => opt.Ignore());
            }
        }

        #endregion


        #region Профили для ExpensesRecord
        public class ExpensesRecordGetExpensesRecordsProfile : Profile
        {
            public ExpensesRecordGetExpensesRecordsProfile()
            {
                CreateMap<CostSubItem, CostSubItemDto>()
                    .ForMember(recordDTO => recordDTO.ShortName, record => record.MapFrom(prop => prop.ShortName))
                    .ForMember(recordDTO => recordDTO.Title, record => record.MapFrom(prop => prop.Title))
                    .ForMember(recordDTO => recordDTO.FullName, record => record.MapFrom(prop => prop.FullName))
                    .ForAllOtherMembers(opt => opt.Ignore());
                CreateMap<ExpensesRecord, ExpensesRecordDto>()
                    .ForMember(recordDTO => recordDTO.CostSubItem, record => record.MapFrom(prop => prop.CostSubItem))
                    .ForMember(recordDTO => recordDTO.Department, record => record.Ignore())
                    .ForMember(recordDTO => recordDTO.Project, record => record.Ignore())
                    .ForMember(recordDTO => recordDTO.RecordStatus, record => record.Ignore())
                    .ForMember(recordDTO => recordDTO.SourceElementID, record => record.Ignore())
                    .ForMember(recordDTO => recordDTO.SourceDB, record => record.Ignore());
            }
        }

        #endregion


        #region Профили для TSHoursRecord
        //Для MyHours
        public class GetMyHoursDataProfile : Profile
        {
            public GetMyHoursDataProfile()
            {
                CreateMap<Employee, BasicEmployeeDto>()
                    .ForMember(employeeDTO => employeeDTO.FullName, employee => employee.MapFrom(prop => prop.FullName))
                    .ForAllOtherMembers(opt => opt.Ignore());
                CreateMap<Department, BasicDepartmentDto>();
                CreateMap<Project, BasicProjectDto>()
                    .ForMember(projectDTO => projectDTO.ShortName, project => project.MapFrom(prop => prop.ShortName))
                    .ForMember(projectDTO => projectDTO.EmployeePMName, project => project.MapFrom(prop => prop.EmployeePM.FullName))
                    .ForMember(projectDTO => projectDTO.ApproveHoursEmployeeName, project => project.MapFrom(prop => prop.ApproveHoursEmployee.FullName))
                    .ForAllOtherMembers(opt => opt.Ignore());
                CreateMap<TSHoursRecord, TSHoursRecordDTO>()
                    .ForMember(projectDTO => projectDTO.Project, project => project.MapFrom(prop => prop.Project))
                    .ForMember(projectDTO => projectDTO.Hours, project => project.MapFrom(prop => prop.Hours))
                    .ForMember(projectDTO => projectDTO.Description, project => project.MapFrom(prop => prop.Description))
                    .ForMember(projectDTO => projectDTO.ProjectShortName, project => project.MapFrom(prop => prop.Project.ShortName))
                    .ForMember(projectDTO => projectDTO.ExternalSourceElementID, project => project.MapFrom(prop => prop.ExternalSourceElementID))
                    .ForMember(projectDTO => projectDTO.RecordSource, project => project.MapFrom(prop => prop.RecordSource))
                    .ForMember(recordDTO => recordDTO.Employee, record => record.Ignore())
                    .ForMember(recordDTO => recordDTO.Created, record => record.Ignore());
            }
        }

        //Для метода GetMyProjects
        public class GetMyProjectProfile : Profile
        {
            public GetMyProjectProfile()
            {
                CreateMap<Project, Project>()
                    .ForMember(projectDTO => projectDTO.ShortName, project => project.MapFrom(prop => prop.ShortName))
                    .ForMember(projectDTO => projectDTO.ID, project => project.MapFrom(prop => prop.ID))
                    .ForAllOtherMembers(opt => opt.Ignore());
            }
        }

        //Для MyHoursDeclined
        public class GetEmployeeDeclinedDataProfile : Profile
        {
            public GetEmployeeDeclinedDataProfile()
            {
                CreateMap<TSHoursRecord, TSHoursRecordDTO>()
                    .ForMember(recordDTO => recordDTO.ID, record => record.MapFrom(prop => prop.ID))
                    .ForMember(recordDTO => recordDTO.RecordDate, record => record.MapFrom(prop => prop.RecordDate))
                    .ForMember(recordDTO => recordDTO.Hours, record => record.MapFrom(prop => prop.Hours))
                    .ForMember(recordDTO => recordDTO.Description, record => record.MapFrom(prop => prop.Description))
                    .ForAllOtherMembers(opt => opt.Ignore());
            }
        }

        public class GetEmployeesApproveProfile : Profile
        {
            public GetEmployeesApproveProfile()
            {
                CreateMap<Project, BasicProjectDto>()
                    .ForMember(recordDTO => recordDTO.ShortName, record => record.MapFrom(prop => prop.ShortName))
                    .ForAllOtherMembers(opt => opt.Ignore());
                CreateMap<Employee, BasicEmployeeDto>()
                    .ForMember(recordDTO => recordDTO.FullName, record => record.MapFrom(prop => prop.FullName));
                CreateMap<Department, BasicDepartmentDto>();
                CreateMap<TSHoursRecord, TSHoursRecordDTO>()
                    .ForMember(recordDTO => recordDTO.ID, record => record.MapFrom(prop => prop.ID))
                    .ForMember(recordDTO => recordDTO.EmployeeID, record => record.MapFrom(prop => prop.EmployeeID))
                    .ForMember(recordDTO => recordDTO.Employee, record => record.MapFrom(prop => prop.Employee))
                    .ForMember(recordDTO => recordDTO.Project, record => record.MapFrom(prop => prop.Project))
                    .ForMember(recordDTO => recordDTO.ProjectShortName, record => record.MapFrom(prop => prop.Project.ShortName))
                    .ForMember(recordDTO => recordDTO.RecordDate, record => record.MapFrom(prop => prop.RecordDate))
                    .ForMember(recordDTO => recordDTO.Hours, record => record.MapFrom(prop => prop.Hours))
                    .ForMember(recordDTO => recordDTO.Description, record => record.MapFrom(prop => prop.Description))
                    .ForMember(recordDTO => recordDTO.RecordStatus, record => record.MapFrom(prop => prop.RecordStatus))
                    .ForMember(recordDTO => recordDTO.VersionNumber, record => record.MapFrom(prop => prop.VersionNumber))
                    .ForMember(projectDTO => projectDTO.ExternalSourceElementID, project => project.MapFrom(prop => prop.ExternalSourceElementID))
                    .ForMember(projectDTO => projectDTO.RecordSource, project => project.MapFrom(prop => prop.RecordSource))
                    .ForAllOtherMembers(opt => opt.Ignore());
            }
        }
        #endregion

        #region Профили  для TSAutoHoursRecord

        public class GetAutoHoursDataProfile : Profile
        {
            public GetAutoHoursDataProfile()
            {
                CreateMap<Employee, BasicEmployeeDto>()
                    .ForMember(employeeDTO => employeeDTO.FullName,
                        employee => employee.MapFrom(prop => prop.FullName));
                CreateMap<Department, BasicDepartmentDto>();
                CreateMap<Project, BasicProjectDto>()
                    .ForMember(projectDTO => projectDTO.ShortName, project => project.MapFrom(prop => prop.ShortName))
                    .ForAllOtherMembers(opt => opt.Ignore());
                CreateMap<TSAutoHoursRecord, TSAutoHoursRecordDTO>();
            }
        }

        #endregion

        #region Профили для ProjectStatusRecod

        public class CreateProjectStatusProfile : Profile
        {
            public CreateProjectStatusProfile()
            {
                CreateMap<Project, Project>()
                    .ForMember(projectViewModel => projectViewModel.ID, projectDomainModel => projectDomainModel.MapFrom(prop => prop.ID))
                    .ForMember(projectViewModel => projectViewModel.ShortName, projectDomainModel => projectDomainModel.MapFrom(prop => prop.ShortName))
                    .ForAllOtherMembers(opt => opt.Ignore());
                CreateMap<ProjectStatusRecord, ProjectStatusRecord>();
                CreateMap<ProjectStatusRecordEntryViewModel, ProjectStatusRecordEntryViewModel>();
                CreateMap<ProjectScheduleEntry, ProjectScheduleEntry>()
                    .ForMember(projectViewModel => projectViewModel.Project, projectDomainModel => projectDomainModel.Ignore());
            }
        }

        public class UpdateProjectStatusProfile : Profile
        {
            public UpdateProjectStatusProfile()
            {
                CreateMap<Project, Project>()
                    .ForMember(projectDTO => projectDTO.ShortName, project => project.MapFrom(p => p.ShortName))
                    .ForAllOtherMembers(opt => opt.Ignore());
                CreateMap<ProjectStatusRecord, ProjectStatusRecord>();
                CreateMap<ProjectScheduleEntry, ProjectScheduleEntry>()
                    .ForMember(projectViewModel => projectViewModel.Project, projectDomainModel => projectDomainModel.Ignore());
            }
        }



        public class ProjectStatusRecordAllProjectLastStatus : Profile
        {
            public ProjectStatusRecordAllProjectLastStatus()
            {
                CreateMap<ProjectStatusRecord, ProjectStatusRecord>();
                CreateMap<ProjectMember, ProjectMember>();
            }
        }

        #endregion
    }
}