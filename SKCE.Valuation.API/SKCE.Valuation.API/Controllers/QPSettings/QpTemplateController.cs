using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SKCE.Examination.Models.DbModels.Common;
using SKCE.Examination.Services.Common;
using SKCE.Examination.Services.QPSettings;
using SKCE.Examination.Services.ServiceContracts;
using SKCE.Examination.Services.ViewModels.QPSettings;

namespace SKCE.Examination.API.Controllers.QPSettings
{
    [Route("api/[controller]")]
    [ApiController]
    public class QpTemplateController : ControllerBase
    {
        private readonly QpTemplateService _qpTemplateService;
        private readonly IMapper _mapper;

        public QpTemplateController(QpTemplateService qpTemplateService, IMapper mapper)
        {
            _qpTemplateService = qpTemplateService;
            _mapper = mapper;
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
        public async Task<IActionResult> UpdateQpTemplate(long qpTemplateId,[FromBody] QPTemplateVM viewModel)
        {
            if (viewModel == null)
                return BadRequest("Invalid input data");

            var updatedTemplate = await _qpTemplateService.UpdateQpTemplateAsync(viewModel);
            return Ok(updatedTemplate);
        }
        [HttpGet]
        public async Task<ActionResult<IEnumerable<QPTemplateVM>>> GetQPTemplatesAsync()
        {
            return Ok( await _qpTemplateService.GetQPTemplatesAsync());
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
        [HttpGet("AssignQPForGeneration/{userId}/{qpTemplateId}")]
        public async Task<ActionResult<bool>> AssignQPForGeneration(long userId, long qpTemplateId)
        {
            var userQPTemplate = await _qpTemplateService.AssignTemplateForQPGenerationAsync(userId, qpTemplateId);
            if (userQPTemplate == null) return NotFound();
            return Ok(userQPTemplate);
        }
        [HttpGet("SubmitGeneratedQP/{userId}/{qpTemplateId}/{documentId}")]
        public async Task<ActionResult<bool>> SubmitGeneratedQP(long userId, long qpTemplateId, long documentId)
        {
            var userQPTemplate = await _qpTemplateService.SubmitGeneratedQPAsync(userId, qpTemplateId, documentId);
            if (userQPTemplate == null) return NotFound();
            return Ok(userQPTemplate);
        }
        [HttpGet("AssignQPForScrutinity/{userId}/{qpTemplateId}")]
        public async Task<ActionResult<bool>> AssignQPForScrutinity(long userId, long qpTemplateId)
        {
            var userQPTemplate = await _qpTemplateService.AssignTemplateForQPScrutinyAsync(userId, qpTemplateId);
            if (userQPTemplate == null) return NotFound();
            return Ok(userQPTemplate);
        }
        [HttpGet("SubmitScrutinizedQP/{userId}/{qpTemplateId}/{documentId}")]
        public async Task<ActionResult<bool>> SubmitScrutinizedQP(long userId, long qpTemplateId, long documentId)
        {
            var userQPTemplate = await _qpTemplateService.SubmitScrutinizedQPAsync(userId, qpTemplateId, documentId);
            if (userQPTemplate == null) return NotFound();
            return Ok(userQPTemplate);
        }

        [HttpGet("PrintSelectedQP/{qpTemplateId}/{qpCode}/{isForPrint}")]
        public async Task<ActionResult<bool>> PrintSelectedQP(long qpTemplateId,string qpCode,bool isForPrint)
        {
            var result = await _qpTemplateService.PrintSelectedQPAsync(qpTemplateId, qpCode,isForPrint);
            return Ok(result);
        }
    }
}
