using DocumentFormat.OpenXml.Office2010.Word;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SKCE.Examination.Services.Helpers;
using System.Security.Claims;

namespace SKCE.Examination.API.Controllers.Common
{
    [Route("api/[controller]")]
    [ApiController]
    public class BlobStorageController : ControllerBase
    {
        private readonly AzureBlobStorageHelper _blobStorageHelper;
        //private readonly string _userId;

        public BlobStorageController(AzureBlobStorageHelper blobStorageHelper, IHttpContextAccessor httpContextAccessor)
        {
            _blobStorageHelper = blobStorageHelper;
            //_userId = httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                      //?? throw new ArgumentNullException("User ID not found in the context.");
        }

        /// <summary>
        /// Uploads a file to Azure Blob Storage.
        /// </summary>
        [HttpPost("upload")]
        public async Task<IActionResult> UploadFile(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("File is required.");

            using (var stream = file.OpenReadStream())
            {
                long? documentId = await _blobStorageHelper.UploadFileAsync(stream, file.FileName, file.ContentType);
                if (documentId == -1 || documentId == null)
                    return StatusCode(500, "File upload failed.");

                return Ok(new { DocumentId = documentId });
            }
        }

        /// <summary>
        /// Downloads a file from Azure Blob Storage.
        /// </summary>
        [HttpGet("download/{documentId}")]
        public async Task<IActionResult> DownloadFile(long documentId)
        {
            var (fileStream, fileName) = await _blobStorageHelper.DownloadFileAsync(documentId);
            if (fileStream == null)
                return NotFound("File not found.");

            return File(fileStream, "application/octet-stream", fileName);
        }

        /// <summary>
        /// Lists all files in the blob container.
        /// </summary>
        [HttpGet("list")]
        public async Task<IActionResult> ListFiles()
        {
            var files = await _blobStorageHelper.ListFilesAsync();
            return Ok(files);
        }

        /// <summary>
        /// Deletes a file from Azure Blob Storage.
        /// </summary>
        [HttpDelete("delete/{fileName}")]
        public async Task<IActionResult> DeleteFile(string fileName)
        {
            var result = await _blobStorageHelper.DeleteFileAsync(fileName);
            if (result)
                return Ok("File deleted successfully.");
            else
                return NotFound("File not found.");
        }
    }
}
