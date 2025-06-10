using SKCE.Examination.Models.DbModels.Common;
using SKCE.Examination.Services.ViewModels.Report;

namespace SKCE.Examination.Services.Common
{
    public class ReportService
    {
        private readonly ExaminationDbContext _context;

        public ReportService(ExaminationDbContext context )
        {
            _context = context; 
        }

        public async Task<IEnumerable<ConsolidatedMarkReportDto>> GetConsolidatedMarkReportData()
        {
            return new List<ConsolidatedMarkReportDto>();

        }

        public async Task<IEnumerable<PassAnalysisReportDto>> GetPassAnalysisReportData()
        {
            return new List<PassAnalysisReportDto>();

        }
         
        public async Task<IEnumerable<FailAnalysisReportDto>> GetFailAnalysisReportData()
        {
            return new List<FailAnalysisReportDto>();
        }

    }
}
