using SKCE.Examination.Services.Helpers;
using Microsoft.AspNetCore.Mvc;

namespace SKCE.Examination.API.Controllers.Common
{
    [Route("api/[controller]")]
    [ApiController]
    public class S3Controller : ControllerBase
    {
        private readonly S3Helper _s3Helper;

        public S3Controller(S3Helper s3Helper)
        {
            _s3Helper = s3Helper;
        }

        /// <summary>
        /// Uploads a file to AWS S3 and returns Document ID.
        /// </summary>
        [HttpPost("upload")]
        public async Task<IActionResult> UploadFile(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("No file uploaded.");

            using var stream = file.OpenReadStream();
            int? documentId = await _s3Helper.UploadFileAsync(file.FileName, stream, file.ContentType);

            if (documentId == -1 || documentId == null)
                return StatusCode(500, "File upload failed.");

            return Ok(new { DocumentId = documentId });
        }

        /// <summary>
        /// Downloads a file from AWS S3 using Document ID.
        /// </summary>
        [HttpGet("download/{documentId}")]
        public async Task<IActionResult> DownloadFile(int documentId)
        {
            var (fileStream, fileName) = await _s3Helper.DownloadFileAsync(documentId);
            if (fileStream == null)
                return NotFound("File not found.");

            return File(fileStream, "application/octet-stream", fileName);
        }
    }
}
