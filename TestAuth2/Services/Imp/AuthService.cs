using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TestAuth2.Data;
using TestAuth2.DTOs.Auth;
using TestAuth2.Entities;

namespace TestAuth2.Services
{
    public class AuthService : IAuthService
    {
        private readonly AppDbContext _dbContext;
        private readonly IJwtTokenService _jwtTokenService;
        private readonly IPasswordHasher<User> _passwordHasher;

        public AuthService(
            AppDbContext dbContext,
            IJwtTokenService jwtTokenService,
            IPasswordHasher<User> passwordHasher)
        {
            _dbContext = dbContext;
            _jwtTokenService = jwtTokenService;
            _passwordHasher = passwordHasher;
        }

        public async Task<bool> RegisterAsync(RegisterRequest request)
        {
            var username = request.Username.Trim();
            var email = request.Email.Trim();

            var usernameExists = await _dbContext.Users
                .AnyAsync(u => u.Username == username);

            if (usernameExists)
                return false;

            var emailExists = await _dbContext.Users
                .AnyAsync(u => u.Email == email);

            if (emailExists)
                return false;

            var defaultRole = await _dbContext.Roles
                .SingleOrDefaultAsync(r => r.Name == "User");

            if (defaultRole is null)
            {
                throw new InvalidOperationException(
                    "Default role 'User' has not been seeded.");
            }

            var user = new User
            {
                Username = username,
                Email = email,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            user.PasswordHash = _passwordHasher.HashPassword(
                user,
                request.Password);

            user.UserRoles.Add(new UserRole
            {
                User = user,
                Role = defaultRole
            });

            _dbContext.Users.Add(user);

            await _dbContext.SaveChangesAsync();

            return true;
        }

        public async Task<AuthResponse?> LoginAsync(LoginRequest request)
        {
            var login = request.Login.Trim();

            var user = await _dbContext.Users
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u =>
                    u.Username == login || u.Email == login);

            if (user is null || !user.IsActive)
                return null;

            var verificationResult = _passwordHasher.VerifyHashedPassword(
                user,
                user.PasswordHash,
                request.Password);

            if (verificationResult == PasswordVerificationResult.Failed)
                return null;

            // Rehash with current settings when needed.
            if (verificationResult == PasswordVerificationResult
                    .SuccessRehashNeeded)
            {
                user.PasswordHash = _passwordHasher.HashPassword(
                    user,
                    request.Password);

                await _dbContext.SaveChangesAsync();
            }

            var (token, expiresAtUtc) =
                _jwtTokenService.GenerateToken(user);

            return new AuthResponse
            {
                AccessToken = token,
                TokenType = "Bearer",
                ExpiresAtUtc = expiresAtUtc
            };
        }
    }
}
