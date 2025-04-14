using SKCE.Examination.Models.DbModels.Common;
using SKCE.Examination.Services.ServiceContracts;
using Microsoft.AspNetCore.Mvc;
using SKCE.Examination.Services.ViewModels.Common;
using DocumentFormat.OpenXml.Spreadsheet;

namespace SKCE.Examination.API.Controllers.Common
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly IUserService _userService;
        public UserController(IUserService  userService)
        {
            _userService = userService;
        }

        // GET: api/user
        [HttpGet]
        public async Task<ActionResult<IEnumerable<User>>> GetUsers()
        {
            return Ok(await _userService.GetUsersAsync());
        }

        // GET: api/user/5
        [HttpGet("{id}")]
        public async Task<ActionResult<User>> GetUser(int id)
        {
            var user = await _userService.GetUserByIdAsync(id);
            if (user == null) return NotFound();
            return Ok(user);
        }

        // POST: api/user
        [HttpPost]
        public async Task<ActionResult<User>> AddUser(User user)
        {
            try
            {
                var newUser = await _userService.AddUserAsync(user);
                return CreatedAtAction(nameof(GetUser), new { id = newUser.UserId }, newUser);
            }
            catch (Exception ex)
            {
                return BadRequest(new ResultModel() { Message = "User is already exists." });
            }
           
        }

        // PUT: api/user/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateUser(int id, User user)
        {
            try
            {
                if (id != user.UserId) return BadRequest();
                var duplicateUserCheck = await _userService.CheckSameNameOtherUserExists(user, user.UserId);
                if (!duplicateUserCheck)
                {
                    var updatedUser = await _userService.UpdateUserAsync(user);
                    return Ok(updatedUser);
                }
                else
                {
                    return BadRequest(new ResultModel() { Message = "User is already exists." });
                }
            }
            catch (Exception ex)
            {
                return BadRequest(new ResultModel() { Message = "User update action is failed." });
            }
        }

        // DELETE: api/user/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            var deleted = await _userService.DeleteUserAsync(id);
            if (!deleted) return NotFound();
            return NoContent();
        }
    }
}
