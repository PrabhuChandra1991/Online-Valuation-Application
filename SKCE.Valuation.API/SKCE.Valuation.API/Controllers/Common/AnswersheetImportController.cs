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
        public async Task<List<AnswersheetImportDto>> GetAnswersheetImports()
        {
            long institutionId = long.Parse(Request.Headers["institutionId"].ToString());
            string examYear = Request.Headers["examYear"].ToString();
            string examMonth = Request.Headers["examMonth"].ToString();
            long courseId = long.Parse(Request.Headers["courseId"].ToString());

            return await this._answersheetImportService
                .GetAnswersheetImports(institutionId, examYear, examMonth, courseId);

        }

        [HttpGet("GetAnswersheetImportDetails")]
        public async Task<List<AnswersheetImportDetailDto>> GetAnswersheetImportDetails(long answersheetImportId)
        {
            return await this._answersheetImportService.GetAnswersheetImportDetails(answersheetImportId);
        }

        [HttpGet("GetExaminationCourseInfo")]
        public async Task<ActionResult<IEnumerable<AnswersheetImportCourseDto>>>
            GetExaminationCourseInfo([FromQuery] long institutionId,
            [FromQuery] string examYear, [FromQuery] string examMonth)
        {
            var result = await this._answersheetImportService
                .GetExaminationCourseInfoAsync(institutionId, examYear, examMonth);
            return Ok(result);
        }

        /// <summary>
        /// Upload an Excel file and import QP data.
        /// </summary>
        [HttpPost("ImportDummyNoFromExcelByCourse")]
        public async Task<IActionResult> ImportDummyNoFromExcelByCourse(IFormFile file)
        {
            long institutionId = long.Parse(Request.Headers["institutionId"].ToString());
            string examYear = Request.Headers["examYear"].ToString();
            string examMonth = Request.Headers["examMonth"].ToString();
            long courseId = long.Parse(Request.Headers["courseId"].ToString());

            if (institutionId == 0)
                return BadRequest("Invalid institutionId");

            if (courseId == 0)
                return BadRequest("Invalid course Id");


            if (file == null || file.Length == 0)
                return BadRequest("No file uploaded.");


            using var stream = file.OpenReadStream();

            var importedInfo =
                await _answersheetImportService
                .ImportDummyNoFromExcelByCourse(stream, institutionId, examYear, examMonth, courseId);


            return Ok(new { Message = importedInfo });
        }

        [HttpGet("ReviewedAndApproveDummyNumbers")]
        public async Task<IActionResult> ReviewedAndApproveDummyNumbers(long answersheetImportId, int absentCount)
        {
            try
            {
                long loggedInUserId = long.Parse(Request.Headers["loggedInUserId"].ToString());

                var result = await _answersheetImportService
                    .CreateAnswerSheetsAndApproveImportedData(answersheetImportId, absentCount, loggedInUserId);

                return Ok(result);
            }
            catch (Exception ex)
            {
                if (ex.InnerException.ToString().Contains("UC_Answersheet"))
                {
                    return BadRequest(new ResultModel() { Message = "UNIQUE-KEY-VIOLATION" });
                }
                else
                {
                    return BadRequest(new ResultModel() { Message = "Error on ReviewedAndApproveDummyNumbers" });
                }

            }
        }

        [HttpGet("DeleteAnswersheetImport")]
        public async Task<IActionResult> DeleteAnswersheetImport(long answersheetImportId)
        {
            try
            {
                long loggedInUserId = long.Parse(Request.Headers["loggedInUserId"].ToString());
                var result = await _answersheetImportService.DeleteAnswersheetImport(answersheetImportId, loggedInUserId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        [HttpPost("UploadAnswerSheetData/{courseCode}/{dummyNumber}")]
        public async Task<IActionResult> UploadAnswerSheetData(
            string courseCode, string dummyNumber, IFormFile file)
        {
            try
            {

                using var stream = file.OpenReadStream();
                var result = await _answersheetImportService.UploadAnswersheetAsync(courseCode, dummyNumber, stream);
                return Ok(result);
            }
            catch (Exception ex)
            {
                throw;
            }
        }


    }
}
