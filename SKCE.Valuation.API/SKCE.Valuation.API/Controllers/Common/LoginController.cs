﻿using SKCE.Examination.Services.Common;
using Microsoft.AspNetCore.Mvc;
using SKCE.Examination.Models.ViewModels.Common;
using System.Threading.Tasks;
using SKCE.Examination.API.Model;

namespace SKCE.Examination.API.Controllers.Common
{
    [Route("api/[controller]")]
    [ApiController]
    public class LoginController : ControllerBase
    {
        private readonly LoginServices _loginManager;

        public LoginController(LoginServices loginManager)
        {
            _loginManager = loginManager;
        }

        [HttpPost("request-temp-password")]
        public async Task<IActionResult> RequestTempPassword([FromBody] LoginVM request)
        {
            var tempPassword = await _loginManager.GenerateTempPasswordAsync(request.Email);


            if (tempPassword == null)
                return BadRequest(new ResultModel { Message = "Invalid email ID." });

            return Ok(new ResultModel { Message= "Temporary password sent to your email." });
        }

        [HttpPost("validate-temp-password")]
        public async Task<IActionResult> ValidateTempPassword([FromBody] LoginVM request, [FromQuery] string tempPassword)
        {
            var user = await _loginManager.ValidateTempPassword(request.Email, tempPassword);
            if (user != null)
                return Ok(user);

            return BadRequest(new ResultModel { Message = "Invalid or expired password." });
        }
    }
}
