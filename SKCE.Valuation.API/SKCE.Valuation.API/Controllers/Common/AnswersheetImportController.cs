using Microsoft.AspNetCore.Mvc;
using SKCE.Examination.Services.Common;
using SKCE.Examination.Services.ViewModels.Common;

namespace SKCE.Examination.API.Controllers.Common
{
    [Route("api/[controller]")]
    [ApiController]
    public class AnswersheetImportController : ControllerBase
    {
        private readonly AnswersheetImportService _answersheetImportService;

        public AnswersheetImportController(AnswersheetImportService answersheetImportService)
        {
            _answersheetImportService = answersheetImportService;
        }

        [HttpGet("GetExaminationInfo")]
        public async Task<ActionResult<IEnumerable<AnswersheetImportCourseDptDto>>>
            GetExaminationInfo([FromQuery] long institutionId,
            [FromQuery] string examYear, [FromQuery] string examMonth)
        {
            var result = await this._answersheetImportService
                .GetExaminationInfoAsync(institutionId, examYear, examMonth);
            return Ok(result);
        }

    }
}
