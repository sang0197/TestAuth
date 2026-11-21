using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TestAuth2.DTOs.Auth;
using TestAuth2.Services;

namespace TestAuth2.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register(
            RegisterRequest request)
        {
            var success = await _authService.RegisterAsync(request);

            if (!success)
            {
                return Conflict(new
                {
                    message = "Username or email already exists."
                });
            }

            return StatusCode(StatusCodes.Status201Created, new
            {
                message = "Registration successful."
            });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginRequest request)
        {
            var result = await _authService.LoginAsync(request);

            if (result is null)
            {
                return Unauthorized(new
                {
                    message = "Invalid login credentials."
                });
            }

            return Ok(result);
        }

        [HttpPost("refresh")]
        public async Task<IActionResult> Refresh(RefreshRequest request)
        {
            var result =
                await _authService.RefreshAsync(
                    request.RefreshToken);

            if (result == null)
            {
                return Unauthorized(new
                {
                    message = "Invalid or expired refresh token."
                });
            }

            return Ok(result);
        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout(RefreshRequest request)
        {
            await _authService.LogoutAsync(request.RefreshToken);
            return NoContent();
        }

        [HttpPost("logout-all")]
        public async Task<IActionResult> LogoutAll()
        {
            var userId = int.Parse(
                User.FindFirst(
                    ClaimTypes.NameIdentifier)!.Value);
            await _authService.LogoutAllAsync(userId);
            return NoContent();
        }

    }
}
