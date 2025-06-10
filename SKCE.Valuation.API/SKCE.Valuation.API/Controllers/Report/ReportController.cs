using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SKCE.Examination.Services.Common;
using SKCE.Examination.Services.ViewModels.Report;

namespace SKCE.Examination.API.Controllers.Report
{
    [Route("api/[controller]")]
    [ApiController]
    public class ReportController : ControllerBase
    {
        private readonly ReportService _reportService;

        public ReportController(ReportService reportService)
        {
            _reportService = reportService;
        }

        [HttpGet("GetConsolidatedMarkReport")]
        public async Task<ActionResult<IEnumerable<ConsolidatedMarkReportDto>>> GetConsolidatedMarkReportData()
        {
            var resultItems = _reportService.GetConsolidatedMarkReportData();
            return Ok(resultItems);
        }         

        [HttpGet("GetPassAnalysisReport")]
        public async Task<ActionResult<IEnumerable<PassAnalysisReportDto>>> GetPassAnalysisReportData()
        {
            var resultItems = _reportService.GetPassAnalysisReportData();
            return Ok(resultItems);
        }

        [HttpGet("GetFailAnalysisReport")]
        public async Task<ActionResult<IEnumerable<FailAnalysisReportDto>>> GetFailAnalysisReportData()
        {
            var resultItems = _reportService.GetFailAnalysisReportData();
            return Ok(resultItems);
        }

    }
}
