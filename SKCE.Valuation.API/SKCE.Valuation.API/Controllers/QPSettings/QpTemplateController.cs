using AutoMapper;
using DocumentFormat.OpenXml.Office2010.Word;
using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.AspNetCore.Mvc;
using SKCE.Examination.Models.DbModels.Common;
using SKCE.Examination.Services.Helpers;
using SKCE.Examination.Services.QPSettings;
using SKCE.Examination.Services.ViewModels.Common;
using SKCE.Examination.Services.ViewModels.QPSettings;


namespace SKCE.Examination.API.Controllers.QPSettings
{
    [Route("api/[controller]")]
    [ApiController]
    public class QpTemplateController : ControllerBase
    {
        private readonly QpTemplateService _qpTemplateService;
        private readonly IMapper _mapper;
        private readonly AzureBlobStorageHelper _azureBlobStorageHelper;
        public QpTemplateController(QpTemplateService qpTemplateService, IMapper mapper, AzureBlobStorageHelper azureBlobStorageHelper)
        {
            _qpTemplateService = qpTemplateService;
            _mapper = mapper;
            _azureBlobStorageHelper = azureBlobStorageHelper;
        }

        [HttpGet("GetQPTemplateByCourseId/{courseId}")]
        public async Task<ActionResult<QPTemplateVM>> GetQPTemplateByCourseId(long courseId)
        {
            var qpTemplate = await _qpTemplateService.GetQPTemplateByCourseIdAsync(courseId);
            if (qpTemplate == null) return NotFound();
            return Ok(qpTemplate);
        }

        [HttpPost("CreateQpTemplate")]
        public async Task<IActionResult> CreateQpTemplate([FromBody] QPTemplateVM viewModel)
        {
            if (viewModel == null)
                return BadRequest("Invalid input data");

            var result = await _qpTemplateService.CreateQpTemplateAsync(viewModel);
            return CreatedAtAction(nameof(CreateQpTemplate), new { id = result.QPTemplateId }, result);
        }
        [HttpPost("UpdateQpTemplate/{qpTemplateId}")]
        public async Task<IActionResult> UpdateQpTemplate(long qpTemplateId, [FromBody] QPTemplateVM viewModel)
        {
            if (viewModel == null)
                return BadRequest("Invalid input data");

            var updatedTemplate = await _qpTemplateService.UpdateQpTemplateAsync(viewModel);
            return Ok(updatedTemplate);
        }
        [HttpGet("GetQPTemplates/{institutionId}")]
        public async Task<ActionResult<IEnumerable<QPTemplateVM>>> GetQPTemplates(long institutionId)
        {
            return Ok(await _qpTemplateService.GetQPTemplatesAsync(institutionId));
        }

        [HttpGet("{qpTemplateId}")]
        public async Task<ActionResult<QPTemplateVM>> GetQPTemplate(long qpTemplateId)
        {
            var qPTemplate = await _qpTemplateService.GetQPTemplateAsync(qpTemplateId);
            if (qPTemplate == null) return NotFound();
            return Ok(qPTemplate);
        }

        [HttpGet("GetQPTemplatesByStatusId/{statusId}")]
        public async Task<ActionResult<IEnumerable<QPTemplateVM>>> GetQPTemplatesByStatusId(long statusId)
        {
            return Ok(await _qpTemplateService.GetQPTemplateByStatusIdAsync(statusId));
        }

        [HttpGet("GetUserQPTemplates/{userId}")]
        public async Task<ActionResult<IEnumerable<UserQPTemplateVM>>> GetUserQPTemplatesAsync(long userId)
        {
            return Ok(await _qpTemplateService.GetQPTemplatesByUserIdAsync(userId));
        }
        
        
        [HttpGet("GetExpertsForQPAssignment")]
        public async Task<ActionResult<IEnumerable<QPAssignmentExpertVM>>> GetExpertsForQPAssignment()
        {
            return Ok(await _qpTemplateService.GetExpertsForQPAssignmentAsync());
        }

        [HttpPost("ValidateGeneratedQP/{userQPTemplateId}")]
        public async Task<IActionResult> ValidateGeneratedQP(long userQPTemplateId, IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("Invalid file. Please upload a valid Word document.");

            try
            {
                // Save uploaded file to MemoryStream
                using var stream = new MemoryStream();
                await file.CopyToAsync(stream);
                stream.Position = 0;

                // Load Word document from stream
                Spire.Doc.Document doc = new Spire.Doc.Document(stream);
                // Get and validate bookmarks
               var validationResult = await _qpTemplateService.ValidateGeneratedQPAsync(userQPTemplateId, doc);

                return Ok(new ResultModel() { Message = validationResult.message, InValid= validationResult.inValidForSubmission });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error processing document: {ex.Message}");
            }
        }

        [HttpPost("GeneratedQPPreview/{userQPTemplateId}")]
        public async Task<IActionResult> GeneratedQPPreview(long userQPTemplateId, IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("Invalid file. Please upload a valid Word document.");

            try
            {
                // Save uploaded file to MemoryStream
                using var stream = new MemoryStream();
                await file.CopyToAsync(stream);
                stream.Position = 0;
                // Load Word document from stream
                var documentId = _azureBlobStorageHelper.UploadFileAsync(stream, file.FileName, file.ContentType).Result;
                // Get and validate bookmarks
                var validationResult = await _qpTemplateService.PreviewGeneratedQP(userQPTemplateId, documentId);

                return Ok();
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error processing document: {ex.Message}");
            }
        }

        [HttpPost("SubmitGeneratedQP/{userQPTemplateId}")]
        public async Task<ActionResult<bool>> SubmitGeneratedQP(long userQPTemplateId, QPSubmissionVM qPSubmissionVM)
        {
            // Save uploaded file to MemoryStream
            using var stream = new MemoryStream();
            await qPSubmissionVM.file.CopyToAsync(stream);
            stream.Position = 0;
            // Load Word document from stream
            var documentId = _azureBlobStorageHelper.UploadFileAsync(stream, qPSubmissionVM.file.FileName, qPSubmissionVM.file.ContentType).Result;

            var userQPTemplate = await _qpTemplateService.SubmitGeneratedQPAsync(userQPTemplateId, documentId, qPSubmissionVM);
            if (userQPTemplate == null) return NotFound();
            return Ok(userQPTemplate);
        }

        [HttpGet("AssignQPForScrutinity/{userId}/{userQPTemplateId}")]
        public async Task<ActionResult<bool>> AssignQPForScrutinity(long userId, long userQPTemplateId)
        {
            var userQPTemplate = await _qpTemplateService.AssignTemplateForQPScrutinyAsync(userId, userQPTemplateId);
            if (userQPTemplate == null) return NotFound();
            return Ok(userQPTemplate);
        }
        [HttpGet("SubmitScrutinizedQP/{userQPTemplateId}")]
        public async Task<ActionResult<bool>> SubmitScrutinizedQP(long userQPTemplateId, QPSubmissionVM qPSubmissionVM)
        {
            long documentId = 0;
            if (qPSubmissionVM.file != null)
            {
                // Save uploaded file to MemoryStream
                using var stream = new MemoryStream();
                await qPSubmissionVM.file.CopyToAsync(stream);
                stream.Position = 0;
                // Load Word document from stream
                documentId = _azureBlobStorageHelper.UploadFileAsync(stream, qPSubmissionVM.file.FileName, qPSubmissionVM.file.ContentType).Result;
            }
            var userQPTemplate = await _qpTemplateService.SubmitScrutinizedQPAsync(userQPTemplateId, documentId, qPSubmissionVM);
            if (userQPTemplate == null) return NotFound();
            return Ok(userQPTemplate);
        }

        //[HttpGet("PrintSelectedQP/{qpTemplateId}/{qpCode}/{isForPrint}")]
        //public async Task<ActionResult<bool>> PrintSelectedQP(long qpTemplateId, string qpCode, bool isForPrint)
        //{
        //    var result = await _qpTemplateService.PrintSelectedQPAsync(qpTemplateId, qpCode, isForPrint);
        //    return Ok(result);
        //}

    }
}
