using Microsoft.AspNetCore.Mvc;
using SKCE.Examination.Services.Common;

namespace SKCE.Examination.API.Controllers.Common
{
    [Route("api/[controller]")]
    [ApiController]
    public class DropdownController : ControllerBase
    {
        private readonly DropdownService _dropdownService;

        public DropdownController(DropdownService dropdownService)
        {
            this._dropdownService = dropdownService;
        }

        [HttpGet("GetExamYears")]
        public async Task<ActionResult<List<string>>> GetExamYears()
        {
            var result = await _dropdownService.GetExamYearsAsync();
            return Ok(result);
        }


        [HttpGet("GetExamMonths")]
        public async Task<ActionResult<List<string>>> GetExamMonths()
        {
            var result = await _dropdownService.GetExamMonthsAsync();
            return Ok(result);
        }
         
    }
}
