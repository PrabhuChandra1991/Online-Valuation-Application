using SKCE.Examination.Models.DbModels.Common;
using SKCE.Examination.Services.EntityReportHelpers;
using SKCE.Examination.Services.ViewModels.Report;

namespace SKCE.Examination.Services.Common
{
    public class ReportService
    {
        private readonly ExaminationDbContext _context;

        public ReportService(ExaminationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<ConsolidatedMarkReportDto>> GetConsolidatedMarkReportData()
        {
            var helper = new ConsolidatedMarkReportHelper(this._context);
            return await helper.GetConsolidatedMarkReportData();
        }

        public async Task<IEnumerable<PassAnalysisReportDto>> GetPassAnalysisReportData()
        {
            var helper = new PassAnalysisReportHelper(this._context);
            return await helper.GetPassAnalysisReportData();
        }

        public async Task<IEnumerable<FailAnalysisReportDto>> GetFailAnalysisReportData()
        {
            var helper = new FailAnalysisReportHelper(this._context);
            return await helper.GetFailAnalysisReportData();
        }

    }
}
