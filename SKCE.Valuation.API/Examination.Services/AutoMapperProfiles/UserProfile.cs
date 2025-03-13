using AutoMapper;
using SKCE.Examination.Models.DbModels.Common;
using SKCE.Examination.Services.ViewModels.Common;

namespace SKCE.Examination.Services.AutoMapperProfiles
{
    public class UserProfile : Profile
    {
        public UserProfile()
        {
            CreateMap<User, UserDto>();

            // Mapping for related entities
            CreateMap<UserCourse, UserCourseDto>();
            CreateMap<UserAreaOfSpecialization, UserSpecializationDto>();
            CreateMap<UserDesignation, UserDesignationDto>();
            CreateMap<UserQualification, UserQualificationsDto>();
        }
    }
}
