using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Models;
using SKCE.Examination.Models.DbModels.Common;
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

        [HttpGet("GetAnswersheetImports")]
        public async Task<List<AnswersheetImportDto>> GetAnswersheetImports(long examinationId)
        {
            return await this._answersheetImportService.GetAnswersheetImports(examinationId);
        }

        [HttpGet("GetAnswersheetImportDetails")]
        public async Task<List<AnswersheetImportDetail>> GetAnswersheetImportDetails(long answersheetImportId)
        {
            return await this._answersheetImportService.GetAnswersheetImportDetails(answersheetImportId);
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

        /// <summary>
        /// Upload an Excel file and import QP data.
        /// </summary>
        [HttpPost("ImportDummyNoFromExcelByCourse")]
        public async Task<IActionResult> ImportDummyNoFromExcelByCourse(IFormFile file)
        {
            string examinationId = Request.Headers["examinationId"].ToString();

            if (examinationId == "" || examinationId == null)
                return BadRequest("Examination data not selected.");


            if (file == null || file.Length == 0)
                return BadRequest("No file uploaded.");


            using var stream = file.OpenReadStream();
            var importedInfo = await _answersheetImportService.ImportDummyNoFromExcelByCourse(stream, long.Parse(examinationId));

            return Ok(new { Message = importedInfo });
        }

        [HttpGet("ReviewedAndApproveDummyNumbers")]
        public async Task<IActionResult> ReviewedAndApproveDummyNumbers(long answersheetImportId)
        {
            try
            {
                long loggedInUserId = long.Parse(Request.Headers["loggedInUserId"].ToString());

                var result = await _answersheetImportService
                    .CreateAnswerSheetsAndApproveImportedData(answersheetImportId, loggedInUserId);

                return Ok(result);
            }
            catch (Exception ex)
            {
                if (ex.InnerException.ToString().Contains("UC_Answersheet"))
                {
                    return BadRequest(new ResultModel() { Message = "UNIQUE-KEY-VIOLATION" });
                }
                else{
                    return BadRequest(new ResultModel() { Message = "Error on ReviewedAndApproveDummyNumbers" });
                }
                
            }
        }


    }
}
