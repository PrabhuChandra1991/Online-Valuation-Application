using AutoMapper;
using SKCE.Examination.Models.DbModels.QPSettings;
using SKCE.Examination.Services.ViewModels.QPSettings;

namespace SKCE.Examination.Services.MappingProfiles
{
    public class QPTemplateMappingProfile : Profile
    {
        public QPTemplateMappingProfile()
        {
            CreateMap<QPTemplateVM, QPTemplate>()
                .ForMember(dest => dest.QPTemplateId, opt => opt.Ignore()) // Ignore Id for entity auto-generation
                .ReverseMap();
        }
    }
}
