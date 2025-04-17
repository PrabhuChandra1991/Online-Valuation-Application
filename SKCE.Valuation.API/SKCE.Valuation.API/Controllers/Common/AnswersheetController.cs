using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SKCE.Examination.Models.DbModels.Common;
using SKCE.Examination.Services.Common;
using SKCE.Examination.Services.ViewModels.Common;
using SKCE.Examination.Services.ViewModels.QPSettings;

namespace SKCE.Examination.API.Controllers.Common
{
    [Route("api/[controller]")]
    [ApiController]
    public class AnswersheetController : ControllerBase
    {
        private readonly AnswersheetService _answersheetService;

        public AnswersheetController(AnswersheetService answersheetService)
        {
            _answersheetService = answersheetService;
        }

        [HttpGet("GetAnswersheets")]
        public async Task<ActionResult<IEnumerable<Answersheet>>> GetAnswersheets()
        {
            var result = await _answersheetService.GetAllAnswersheetsAsync();
            return Ok(result);
        }

        [HttpGet("GetAnswersheetDetails")]
        public async Task<ActionResult<IEnumerable<AnswerManagementDto>>> GetAnswersheetDetails(
            [FromQuery] long? institutionId, [FromQuery] long? courseId, [FromQuery] long? allocatedToUserId)
        {
            var result = await _answersheetService.GetAnswersheetDetailsAsync(institutionId, courseId, allocatedToUserId);
            return Ok(result);
        }


        /// <summary>
        /// Upload an Excel file and import QP data.
        /// </summary>
        [HttpPost("ImportDummyNumberByExcel")]
        public async Task<IActionResult> ImportDummyNumberByExcel(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("No file uploaded.");

            using var stream = file.OpenReadStream();
            var importedInfo = await _answersheetService.ImportDummyNumberByExcel(stream, 1);

            return Ok(new { Message = importedInfo });
        }

        [HttpGet("GetConsolidatedExamAnswersheets")]
        public async Task<ActionResult<List<AnswersheetQuestionAnswerDto>>>
             GetExamConsolidatedAnswersheets(long institutionId)
        {

            var result = await _answersheetService.GetExamConsolidatedAnswersheetsAsync(institutionId);
            return Ok(result);
        }




    }
}
