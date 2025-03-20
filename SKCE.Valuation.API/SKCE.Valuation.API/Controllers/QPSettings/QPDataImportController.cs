using SKCE.Examination.Services.Helpers;
using Microsoft.AspNetCore.Mvc;
using SKCE.Examination.Services.ViewModels.QPSettings;
using System.IO;

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

        /// <summary>
        /// Upload an Syllabus Documents for QP data.
        /// </summary>
        [HttpPost("importSyllabusDocuments")]
        public async Task<IActionResult> importSyllabusDocuments(List<IFormFile> files)
        {
            if (files == null || files.Count == 0)
                return BadRequest("No file uploaded.");

            var documentMissingCourses = await _qPDataImportHelper.ImportSyllabusDocuments(files);
            return Ok(new { Message = documentMissingCourses });
        }

        /// <summary>
        /// Upload an QP and QPAK Documents for QP data.
        /// </summary>
        [HttpPost("ImportQPDocuments")]
        public async Task<IActionResult> ImportQPDocuments(List<IFormFile> files)
        {
            List<QPDocumentValidationVM> qPDocumentValidationVMs = new List<QPDocumentValidationVM>();
            if (files == null || files.Count == 0)
                return BadRequest("No file uploaded.");

            var documentMissingCourses = await _qPDataImportHelper.ImportQPDocuments(files, qPDocumentValidationVMs);
            return Ok(new { Message = documentMissingCourses });
        }
    }
}
