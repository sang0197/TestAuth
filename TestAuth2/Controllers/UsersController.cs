using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TestAuth2.Data;
using TestAuth2.DTOs.Auth;

namespace TestAuth2.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class UsersController : ControllerBase
    {
        private readonly AppDbContext _dbContext;

        public UsersController(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        [HttpGet("me")]
        public async Task<IActionResult> GetMe()
        {
            var userIdValue = User.FindFirstValue(
                ClaimTypes.NameIdentifier);

            if (!int.TryParse(userIdValue, out var userId))
            {
                return Unauthorized();
            }

            var user = await _dbContext.Users
                .AsNoTracking()
                .Where(u => u.Id == userId)
                .Select(u => new UserResponse
                {
                    Id = u.Id,
                    Username = u.Username,
                    Email = u.Email,
                    Roles = u.UserRoles
                        .Select(ur => ur.Role.Name)
                        .ToList()
                })
                .FirstOrDefaultAsync();

            if (user is null)
            {
                return NotFound();
            }

            return Ok(user);
        }

        [HttpGet("admin")]
        [Authorize(Roles = "Admin")]
        public IActionResult AdminOnly()
        {
            return Ok(new
            {
                message = "Welcome, Admin!"
            });
        }

        [HttpGet("user-area")]
        [Authorize(Roles = "User")]
        public IActionResult UserArea()
        {
            return Ok(new
            {
                message = "Welcome to the User area!"
            });
        }

        [HttpGet("admin-or-user")]
        [Authorize(Roles = "Admin,User")]
        public IActionResult AdminOrUser()
        {
            return Ok(new
            {
                message = "You have an allowed role."
            });
        }
    }
}
