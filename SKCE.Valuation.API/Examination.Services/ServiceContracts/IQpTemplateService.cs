using SKCE.Examination.Models.DbModels.QPSettings;
using SKCE.Examination.Services.ViewModels.QPSettings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SKCE.Examination.Services.ServiceContracts
{
   public interface IQpTemplateService
    {
        Task<QPTemplateVM?> GetQPTemplateByCourseIdAsync(long courseId);
        Task<QPTemplate> CreateQpTemplateAsync(QPTemplateVM viewModel);
        Task<List<QPTemplateVM>> GetQPTemplatesAsync();
        Task<QPTemplateVM?> GetQPTemplateAsync(long qpTemplateId);
        Task<List<QPTemplateVM>> GetQPTemplatesByUserIdAsync(long userId);
    }
}
