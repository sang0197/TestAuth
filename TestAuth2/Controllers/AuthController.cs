using Microsoft.AspNetCore.Mvc;
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
    }
}
