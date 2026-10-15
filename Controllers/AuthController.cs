using System.Security.Claims;
using MetrixVox.Application.DTOs;
using MetrixVox.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MetrixVox.Api.Controllers
{
    [ApiController]
    [Route("api/v1/auth")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        /// <summary>POST /api/v1/auth/login</summary>
        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] LoginRequestDto request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _authService.LoginAsync(request);
            if (result == null)
                return Unauthorized(new { error = "Credenciales inválidas." });

            return Ok(result);
        }

        /// <summary>POST /api/v1/auth/refresh</summary>
        [HttpPost("refresh")]
        [AllowAnonymous]
        public async Task<IActionResult> Refresh([FromBody] RefreshRequestDto request)
        {
            var result = await _authService.RefreshAsync(request.RefreshToken);
            if (result == null)
                return Unauthorized(new { error = "Refresh token inválido o expirado." });

            return Ok(result);
        }

        /// <summary>POST /api/v1/auth/logout</summary>
        [HttpPost("logout")]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? User.FindFirstValue("sub");
            if (!Guid.TryParse(userIdStr, out var userId))
                return Unauthorized();

            await _authService.LogoutAsync(userId);
            return Ok(new { message = "Sesión cerrada." });
        }

        /// <summary>GET /api/v1/auth/me</summary>
        [HttpGet("me")]
        [Authorize]
        public async Task<IActionResult> Me()
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? User.FindFirstValue("sub");
            if (!Guid.TryParse(userIdStr, out var userId))
                return Unauthorized();

            var user = await _authService.GetCurrentUserAsync(userId);
            return user == null ? NotFound() : Ok(user);
        }
    }
}
