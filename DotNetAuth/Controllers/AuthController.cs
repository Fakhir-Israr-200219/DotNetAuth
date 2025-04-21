using DotNetAuth.Domain.Constracts;
using DotNetAuth.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DotNetAuth.Controllers
{
    [Route("api/")]
    public class AuthController :ControllerBase
    {

        private readonly IUserService _userService;
        public AuthController(IUserService userService)
        {
            _userService = userService;
        }


        [HttpPost("registor")]
        [AllowAnonymous]
        public async Task<IActionResult> Register([FromBody] UserRegisterRequest request)
        {
            var responce = await _userService.RegisterAsync(request);
            return Ok(responce);
        }

        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] UserLoginRequest request)
        {
            var responce = await _userService.LoginAsync(request);
            return Ok(responce);
        }

        [HttpGet("user/{id}")]
        [Authorize]
        public async Task<IActionResult> GetById(Guid id)
        {
            var respoce = await _userService.GetByIdAsync(id);
            return Ok(respoce);
        }

        [HttpPost("refresh-token")]
        [Authorize]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
        {
            var responce = await _userService.RefreshTokenAsync(request);
            return Ok(responce);
        }

        //revoke refresh token 
        [HttpPost("revoke-refresh-token")]
        [Authorize]
        public async Task<IActionResult> RevokeRefreshToken([FromBody] RefreshTokenRequest request)
        {
            var responce = await _userService.RevokeRefreshToken(request);
            if (responce != null && responce.Message == "Refresh token revoke succsessfully")
            {
                return Ok(responce);
            }
            return BadRequest(responce);
        }

        [HttpGet("current-user")]
        [Authorize]
        public async Task<IActionResult> GetCurrentUser()
        {
            var responce = await _userService.GetCurrentUserAsync();
            return Ok(responce);
        }
    }
}
