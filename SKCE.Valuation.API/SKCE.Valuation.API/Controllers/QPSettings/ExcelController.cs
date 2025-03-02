using SKCE.Examination.Services.Helpers;
using Microsoft.AspNetCore.Mvc;

namespace SKCE.Examination.API.Controllers.QPSettings
{
    [Route("api/[controller]")]
    [ApiController]
    public class ExcelController : ControllerBase
    {
        private readonly ExcelHelper _excelHelper;

        public ExcelController(ExcelHelper excelHelper)
        {
            _excelHelper = excelHelper;
        }

        /// <summary>
        /// Upload an Excel file and import data.
        /// </summary>
        [HttpPost("importCourseDetailsBySyllabus")]
        public async Task<IActionResult> ImportCourseDetailsBySyllabus(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("No file uploaded.");

            using var stream = file.OpenReadStream();
            var users = await _excelHelper.ImportCourseDetailsBySyllabusFromExcelAsync(stream);

            return Ok(new { Message = "Users imported successfully", Count = users.Count });
        }
    }
}
