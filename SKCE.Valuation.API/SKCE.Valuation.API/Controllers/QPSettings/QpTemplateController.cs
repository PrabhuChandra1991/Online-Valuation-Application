using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SKCE.Examination.Models.DbModels.Common;
using SKCE.Examination.Services.Common;
using SKCE.Examination.Services.ServiceContracts;
using SKCE.Examination.Services.ViewModels.QPSettings;

namespace SKCE.Examination.API.Controllers.QPSettings
{
    [Route("api/[controller]")]
    [ApiController]
    public class QpTemplateController : ControllerBase
    {
        private readonly IQpTemplateService _qpTemplateService;
        private readonly IMapper _mapper;

        public QpTemplateController(IQpTemplateService qpTemplateService, IMapper mapper)
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
        public async Task<ActionResult<IEnumerable<Course>>> GetQPTemplates()
        {
            return Ok(await _qpTemplateService.GetQPTemplatesAsync());
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
    }
}
