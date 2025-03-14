using Aspose.Words.Saving;
using AutoMapper;
using SKCE.Examination.Models.DbModels.QPSettings;
using SKCE.Examination.Services.ViewModels.QPSettings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SKCE.Examination.Services.MappingProfiles
{
    public class QPTemplateMappingProfile : Profile
    {
        public QPTemplateMappingProfile()
        {
            CreateMap<QPTemplateVM, QPTemplate>()
                .ForMember(dest => dest.QPTemplateId, opt => opt.Ignore()) // Ignore Id for entity auto-generation
                .ReverseMap();
            CreateMap<QPTemplateVM, QPTemplateDocument>()
                .ForMember(dest => dest.QPTemplateDocumentId, opt => opt.Ignore()) // Ignore Id for entity auto-generation
                .ReverseMap();
            CreateMap<QPTemplateVM, QPTemplateInstitution>()
               .ForMember(dest => dest.QPTemplateInstitutionId, opt => opt.Ignore()) // Ignore Id for entity auto-generation
               .ReverseMap();
            CreateMap<QPTemplateVM, QPTemplateInstitutionDocument>()
              .ForMember(dest => dest.QPTemplateInstitutionDocumentId, opt => opt.Ignore()) // Ignore Id for entity auto-generation
              .ReverseMap();
            CreateMap<QPTemplateVM, QPTemplateInstitutionDepartment>()
             .ForMember(dest => dest.QPTemplateInstitutionDepartmentId, opt => opt.Ignore()) // Ignore Id for entity auto-generation
             .ReverseMap();
        }
    }
}
