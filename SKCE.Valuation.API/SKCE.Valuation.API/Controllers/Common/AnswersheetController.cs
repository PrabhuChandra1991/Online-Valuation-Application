using Microsoft.AspNetCore.Mvc;
using SKCE.Examination.Models.DbModels.Common;
using SKCE.Examination.Services.Common;
using SKCE.Examination.Services.ViewModels.Common;

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

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Answersheet>>> GetAnswersheets()
        {
            var result = await _answersheetService.GetAllAnswersheetsAsync();
            return Ok(result);
        }


        [HttpGet("{id}")]
        public async Task<ActionResult<Answersheet>> GetAnswersheet(long id)
        {
            var result = await _answersheetService.GetAnswersheetAsync(id);
            if (result == null)
                return NotFound();
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


        [HttpGet("GetAnswersheetDetails")]
        public async Task<ActionResult<IEnumerable<AnswerManagementDto>>> GetAnswersheetDetails(
            [FromQuery] string? examYear, [FromQuery] string? examMonth, [FromQuery] string? examType, [FromQuery] long? courseId, [FromQuery] long? allocatedToUserId, [FromQuery] long? answersheetId)
        {
            var result = await _answersheetService.GetAnswersheetDetailsAsync(examYear, examMonth, examType, courseId, allocatedToUserId, answersheetId);
            return Ok(result);
        }

        [HttpGet("GetAnswersheetDetailsById")]
        public async Task<ActionResult<IEnumerable<AnswerManagementDto>>> GetAnswersheetDetailsById([FromQuery] long answersheetId)
        {
            if (answersheetId != 0)
            {
                var result = await _answersheetService.GetAnswersheetDetailsByIdAsync(answersheetId);
                return Ok(result);
            }
            return Ok();
        }

        [HttpGet("GetQuestionAndAnswersByAnswersheetId")]
        public async Task<ActionResult<List<AnswersheetQuestionAnswerDto>>> 
            GetQuestionAndAnswersByAnswersheetId(long answersheetId)
        {
            var result = await _answersheetService
                .GetQuestionAndAnswersByAnswersheetIdAsync(answersheetId);
            return Ok(result);
        }


        [HttpGet("GetQuestionAndAnswerImagesByAnswersheetId")]
        public async Task<ActionResult<List<AnswersheetQuestionAnswerDto>>>
            GetQuestionAndAnswerImagesByAnswersheetId(long answersheetId, int QuestionNumber, int QuestionNumberSubNum)
        {
            var result = await _answersheetService
                .GetQuestionAndAnswerImagesByAnswersheetIdAsync(answersheetId, QuestionNumber, QuestionNumberSubNum);
            return Ok(result);
        }

        [HttpGet("GetConsolidatedExamAnswersheets")]
        public async Task<ActionResult<List<AnswersheetQuestionAnswerDto>>>
            GetExamConsolidatedAnswersheets(string examYear, string examMonth, string examType)
        {
            var result = await _answersheetService.GetExamConsolidatedAnswersheetsAsync(examYear, examMonth, examType);
            return Ok(result);
        }


        [HttpGet("GetAnswerSheetAvailable")]
        public async Task<ActionResult<bool>> GetAnswerSheetAvailable(long answersheetId)
        {
            var result = await _answersheetService.GetAnswerSheetAvailable(answersheetId);
            return Ok(result);
        }

        // GET: /api/Answersheet/GetAnswersheetMark
        [HttpGet("GetAnswersheetMark")]
        public async Task<IActionResult> GetAnswersheetMark([FromQuery] long answersheetId)
        {
            try
            {
                var response = await _answersheetService.GetAnswersheetMarkAsync(answersheetId);
                return Ok(response);
            }
            catch (Exception ex)
            {
                return BadRequest(new ResultModel() { Message = "Error while saving Mark." });
            }
        }

        // POST: /api/Answersheet/SaveAnswersheetMark
        [HttpPost("SaveAnswersheetMark")]
        public async Task<IActionResult> SaveAnswersheetMark(AnswersheetQuestionwiseMark entity)
        {
            try
            {
                var result = await _answersheetService.SaveAnswersheetMarkAsync(entity);
                return Ok(new { Message = "Success", TotalMarksObtained = result });
            }
            catch (Exception ex)
            {
                return BadRequest(new ResultModel() { Message = "Error while saving Mark." });
            }
        }

        // POST: /api/Answersheet/CompleteEvaluation
        [HttpPost("CompleteEvaluation")]
        public async Task<IActionResult> CompleteEvaluation(long answersheetId, long evaluatedByUserId)
        {
            try
            {
                var response = await _answersheetService.CompleteEvaluationSync(answersheetId, evaluatedByUserId);
                return Ok(new { Message = (response) ? "Success" : "Failed" });
            }
            catch (Exception ex)
            {
                return BadRequest(new ResultModel() { Message = "Error on CompleteEvaluationSync." });
            }
        }

        // POST: /api/Answersheet/AllocateAnswerSheetsToUser
        [HttpPost("AllocateAnswerSheetsToUser")]
        public async Task<IActionResult> AllocateAnswerSheetsToUser(AnswersheetAllocateInputModel inputData)
        {
            try
            {
                var response = await _answersheetService.AllocateAnswerSheetsToUser(inputData);
                return Ok(new { Message = (response) ? "Success" : "Failed" });
            }
            catch (Exception ex)
            {
                return BadRequest(new ResultModel() { Message = "Error on AllocateAnswerSheetsToUser." });
            }
        }


        [HttpGet("GetCoursesHavingAnswersheet")]
        public async Task<ActionResult<IEnumerable<CourseWithAnswersheet>>> GetCoursesHavingAnswersheet(string examYear, string examMonth, string examType)
        {
            return Ok(await _answersheetService.GetCoursesHavingAnswersheetAsync(examYear, examMonth, examType));
        }

        [HttpGet("GetCoursesHavingAnswersheetForExportMark")]
        public async Task<ActionResult<IEnumerable<CourseWithAnswersheet>>> GetCoursesHavingAnswersheet(
            long institutionId, string examYear, string examMonth, string examType)
        {
            return Ok(await _answersheetService.GetCoursesHavingAnswersheetAsync(examYear, examMonth, examType, institutionId));
        }


        [HttpGet("ExportMarks")]
        public async Task<IActionResult> ExportMarks(
            [FromQuery] long institutionId, [FromQuery] long courseId,
            [FromQuery] string examYear, [FromQuery] string examMonth, [FromQuery] string examType)
        {
            try
            {
                var (stream, fileName) = await _answersheetService
                .ExportMarksAsync(institutionId, courseId, examYear, examMonth, examType);

                return Ok(new
                {
                    FileName = fileName,
                    ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    Base64Content = Convert.ToBase64String(stream.ToArray())
                });
            }
            catch (Exception ex)
            {
                if (ex.Message.ToString().Contains("EVALUATION-NOT-COMPLETED"))
                    return BadRequest(new ResultModel() { Message = ex.Message });
                else
                    throw ex;
            }


        }

        // GET: /api/Answersheet/RevertEvaluation
        [HttpGet("RevertEvaluation")]
        public async Task<IActionResult> RevertEvaluation([FromQuery] long answersheetId)
        {
            try
            {
                var response = await _answersheetService.RevertEvaluation(answersheetId);
                return Ok(response);
            }
            catch (Exception ex)
            {
                return BadRequest(new ResultModel() { Message = "Error while saving Mark." });
            }
        }

        [HttpGet("EvaluationHistory")]
        public async Task<IActionResult> EvaluationHistory([FromQuery] long answersheetId, [FromQuery] long questionNumber, [FromQuery] long questionNumberSubNum)
        {
            try
            {
                var response = await _answersheetService.EvaluationHistory(answersheetId,questionNumber,questionNumberSubNum);
                return Ok(response);
            }
            catch (Exception ex)
            {
                return BadRequest(new ResultModel() { Message = "Error while saving Mark." });
            }
        }
    }
}
