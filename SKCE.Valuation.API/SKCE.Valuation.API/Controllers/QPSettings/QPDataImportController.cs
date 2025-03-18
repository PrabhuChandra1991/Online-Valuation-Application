using SKCE.Examination.Services.Helpers;
using Microsoft.AspNetCore.Mvc;
using SKCE.Examination.Services.ViewModels.QPSettings;

namespace SKCE.Examination.API.Controllers.QPSettings
{
    [Route("api/[controller]")]
    [ApiController]
    public class QPDataImportController : ControllerBase
    {
        private readonly QPDataImportHelper _qPDataImportHelper;

        public QPDataImportController(QPDataImportHelper qPDataImportHelper)
        {
            _qPDataImportHelper = qPDataImportHelper;
        }

        /// <summary>
        /// Upload an Excel file and import QP data.
        /// </summary>
        [HttpPost("importQPDataByExcel")]
        public async Task<IActionResult> ImportQPDataByExcel(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("No file uploaded.");

            using var stream = file.OpenReadStream();
            var importedInfo = await _qPDataImportHelper.ImportQPDataByExcel(stream, file);

            return Ok(new { Message = importedInfo });
        }

        /// <summary>
        /// Get import history list.
        /// </summary>
        [HttpGet("GetImportHistories")]
        public async Task<ActionResult<IEnumerable<ImportHistoryVM>>> GetImportHistories()
        {
            return Ok(await _qPDataImportHelper.GetImportHistories());
        }
    }
}
