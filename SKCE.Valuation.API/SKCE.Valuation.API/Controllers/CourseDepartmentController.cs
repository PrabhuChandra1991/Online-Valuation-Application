using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SKCE.Examination.Services.Common;

namespace SKCE.Examination.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CourseDepartmentController : ControllerBase
    {
        private readonly CourseDepartmentService _service;

        public CourseDepartmentController(CourseDepartmentService service)
        {
            _service = service;
        }

        //[HttpGet]
        //public async Task<ActionResult<IEnumerable<CourseDepartmentViewModel>>> GetAll() => Ok(await _service.GetAllCourseDepartmentsAsync());
    }
}
