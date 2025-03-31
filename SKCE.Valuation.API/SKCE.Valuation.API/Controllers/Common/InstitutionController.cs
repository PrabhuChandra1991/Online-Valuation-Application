using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SKCE.Examination.Models.DbModels.Common;
using SKCE.Examination.Services.Common;

namespace SKCE.Examination.API.Controllers.Common
{
    [Route("api/[controller]")]
    [ApiController]
    public class InstitutionController : ControllerBase
    {
        private readonly InstitutionService _institutionService;

        public InstitutionController(InstitutionService institutionService)
        {
            _institutionService = institutionService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Institution>>> GetAllInstitutes()
        {
            return Ok(await _institutionService.GetAllInstitutionsAsync());
        }
    }
}
