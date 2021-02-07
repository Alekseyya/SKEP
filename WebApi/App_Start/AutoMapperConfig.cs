using AutoMapper;
using Core.Models;
using WebApi.Dto;

namespace WebApi
{
    public class AutoMapperConfig
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

    }
}
