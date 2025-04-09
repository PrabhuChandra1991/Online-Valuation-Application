using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SKCE.Examination.Models.DbModels.Common;
using SKCE.Examination.Services.Common;

namespace SKCE.Examination.API.Controllers.Common
{
    [Route("api/[controller]")]
    [ApiController]
    public class AnswersheetController : ControllerBase
    {
        private readonly AnswersheetService _answersheetService;

        public AnswersheetController(AnswersheetService answersheetService)
        {
            _answersheetService = answersheetService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Answersheet>>> GetAnswersheets()
        {
            var result = await _answersheetService.GetAllAnswersheetsAsync();
            return Ok(result);
        }
         
    }
}
