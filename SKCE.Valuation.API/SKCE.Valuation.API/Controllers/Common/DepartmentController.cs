using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SKCE.Examination.Models.DbModels.Common;
using SKCE.Examination.Services.Common;

namespace SKCE.Examination.API.Controllers.Common
{
    [Route("api/[controller]")]
    [ApiController]
    public class DepartmentController : ControllerBase
    {
        private readonly DepartmentService _departmentService;

        public DepartmentController(DepartmentService departmentService)
        {
            _departmentService = departmentService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Department>>> GetDepartments()
        {
            return Ok(await _departmentService.GetAllDepartmentsAsync());
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Department>> GetDepartment(int id)
        {
            var department = await _departmentService.GetDepartmentByIdAsync(id);
            if (department == null) return NotFound();
            return Ok(department);
        }

        [HttpPost]
        public async Task<ActionResult<Department>> AddDepartment([FromBody] Department department)
        {
            var newDepartment = await _departmentService.AddDepartmentAsync(department);
            return CreatedAtAction(nameof(GetDepartment), new { id = newDepartment.DepartmentId }, newDepartment);
        }

        [HttpPut("{id}")]
        public async Task<ActionResult> UpdateDepartment(int id, [FromBody] Department department)
        {
            if (!await _departmentService.UpdateDepartmentAsync(id, department))
                return NotFound();

            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteDepartment(int id)
        {
            if (!await _departmentService.DeleteDepartmentAsync(id))
                return NotFound();

            return NoContent();
        }
    }
}
