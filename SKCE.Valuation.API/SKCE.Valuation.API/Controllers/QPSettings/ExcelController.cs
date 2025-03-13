using SKCE.Examination.Services.Helpers;
using Microsoft.AspNetCore.Mvc;

namespace SKCE.Examination.API.Controllers.QPSettings
{
    [Route("api/[controller]")]
    [ApiController]
    public class ExcelController : ControllerBase
    {
        private readonly ExcelImportHelper _excelHelper;

        public ExcelController(ExcelImportHelper excelHelper)
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
            var importedInfo = await _excelHelper.ImportDataFromExcel(stream, file);

            return Ok(new { Message = importedInfo });
        }
    }
}
