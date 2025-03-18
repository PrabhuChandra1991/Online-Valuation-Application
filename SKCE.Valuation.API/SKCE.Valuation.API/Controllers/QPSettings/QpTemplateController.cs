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

        [HttpPost]
        public async Task<IActionResult> CreateQpTemplate([FromBody] QPTemplateVM viewModel)
        {
            if (viewModel == null)
                return BadRequest("Invalid input data");

            var result = await _qpTemplateService.CreateQpTemplateAsync(viewModel);
            return CreatedAtAction(nameof(CreateQpTemplate), new { id = result.QPTemplateId }, result);
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<QPTemplateVM>>> GetQPTemplatesAsync()
        {
            return Ok( await _qpTemplateService.GetQPTemplatesAsync());
        }

        [HttpGet("{qpTemplateId}")]
        public async Task<ActionResult<Course>> GetQPTemplate(long qpTemplateId)
        {
            var course = await _qpTemplateService.GetQPTemplateAsync(qpTemplateId);
            if (course == null) return NotFound();
            return Ok(course);
        }
        [HttpGet("GetQPTemplatesByUserId/{userId}")]
        public async Task<ActionResult<Course>> GetQPTemplatesByUserId(long userId)
        {
            var course = await _qpTemplateService.GetQPTemplatesByUserIdAsync(userId);
            if (course == null) return NotFound();
            return Ok(course);
        }
        [HttpGet("GetUserQPTemplates/{userId}")]
        public async Task<ActionResult<IEnumerable<UserQPTemplateVM>>> GetUserQPTemplatesAsync(long userId)
        {
            return Ok(await _qpTemplateService.GetQPTemplatesByUserIdAsync(userId));
        }
        [HttpGet("AssignQPTemplate/{userId}/{qpTemplateId}")]
        public async Task<ActionResult<bool>> AssignQPTemplate(long userId, long qpTemplateId)
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
    }
}
