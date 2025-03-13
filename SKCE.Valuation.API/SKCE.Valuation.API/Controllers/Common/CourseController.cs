using Microsoft.AspNetCore.Mvc;
using SKCE.Examination.Models.DbModels.Common;
using SKCE.Examination.Services.Common;
using SKCE.Examination.Services.ViewModels.Common;

namespace SKCE.Examination.API.Controllers.Common
{
    [Route("api/[controller]")]
    [ApiController]
    public class CourseController : ControllerBase
    {
        private readonly CourseService _courseService;

        public CourseController(CourseService courseService)
        {
            _courseService = courseService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Course>>> GetCourses()
        {
            return Ok(await _courseService.GetAllCoursesAsync());
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<CourseVM>> GetCourse(long id)
        {
            var course = await _courseService.GetCourseByIdAsync(id);
            if (course == null) return NotFound();
            return Ok(course);
        }

        [HttpPost]
        public async Task<ActionResult<Course>> AddCourse([FromBody] Course course)
        {
            var newCourse = await _courseService.AddCourseAsync(course);
            if (newCourse == null) return BadRequest(new ResultModel() { Message = "Course creation failed." });
            return CreatedAtAction(nameof(GetCourse), new { id = newCourse.CourseId }, newCourse);
        }

        [HttpPut("{id}")]
        public async Task<ActionResult> UpdateCourse(int id, [FromBody] Course course)
        {
            if (!await _courseService.UpdateCourseAsync(id, course))
                return NotFound();

            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteCourse(int id)
        {
            if (!await _courseService.DeleteCourseAsync(id))
                return NotFound();

            return NoContent();
        }
    }
}
