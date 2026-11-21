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
        private readonly IRefreshTokenService _refreshTokenService;

        public AuthService(
            AppDbContext dbContext,
            IJwtTokenService jwtTokenService,
            IPasswordHasher<User> passwordHasher,
            IRefreshTokenService refreshTokenService)
        {
            _dbContext = dbContext;
            _jwtTokenService = jwtTokenService;
            _passwordHasher = passwordHasher;
            _refreshTokenService = refreshTokenService;
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
                    .ThenInclude(rp => rp.RolePermissions)
                    .ThenInclude(rp => rp.Permission)
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

            var refreshToken = _refreshTokenService.GenerateToken();
            var refreshTokenHash = _refreshTokenService.HashToken(refreshToken);
            var refreshTokenEntity = new RefreshToken
            {
                TokenHash = refreshTokenHash,
                UserId = user.Id,
                ExpiresAt = DateTime.UtcNow.AddDays(
                    _jwtTokenService.GetRefreshTokenExpirationDays())
            };

            _dbContext.RefreshTokens.Add(refreshTokenEntity);
            await _dbContext.SaveChangesAsync();

            return new AuthResponse
            {
                AccessToken = token,
                TokenType = "Bearer",
                ExpiresAtUtc = expiresAtUtc,
                RefreshToken = refreshToken,
                RefreshTokenExpiresAt = refreshTokenEntity.ExpiresAt
            };
        }

        public async Task<AuthResponse?> RefreshAsync(string refreshToken)
        {
            var refreshTokenHash = _refreshTokenService.HashToken(refreshToken);
            var refreshTokenEntity = await _dbContext.RefreshTokens
                .Include(rt => rt.User)
                    .ThenInclude(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                    .ThenInclude(rp => rp.RolePermissions)
                    .ThenInclude(rp => rp.Permission)
                .FirstOrDefaultAsync(rt =>
                    rt.TokenHash == refreshTokenHash);
            if (refreshTokenEntity is null ||
                !refreshTokenEntity.IsActive ||
                !refreshTokenEntity.User.IsActive)
            {
                return null;
            }

            // Generate a new access token and refresh token
            var user = refreshTokenEntity.User;
            var (newAccessToken, newAccessTokenExpiresAt) =
                _jwtTokenService.GenerateToken(user);
            var newRefreshToken = _refreshTokenService.GenerateToken();
            var newRefreshTokenHash = _refreshTokenService.HashToken(newRefreshToken);
            var newRefreshTokenEntity = new RefreshToken
            {
                TokenHash = newRefreshTokenHash,
                UserId = user.Id,
                ExpiresAt = DateTime.UtcNow.AddDays(
                    _jwtTokenService.GetRefreshTokenExpirationDays())
            };

            // Revoke the old refresh token
            refreshTokenEntity.RevokedAt = DateTime.UtcNow;
            refreshTokenEntity.ReplacedByTokenHash = newRefreshTokenHash;
            _dbContext.RefreshTokens.Add(newRefreshTokenEntity);
            await _dbContext.SaveChangesAsync();
            return new AuthResponse
            {
                AccessToken = newAccessToken,
                TokenType = "Bearer",
                ExpiresAtUtc = newAccessTokenExpiresAt,
                RefreshToken = newRefreshToken,
                RefreshTokenExpiresAt = newRefreshTokenEntity.ExpiresAt
            };
        }

        public async Task LogoutAsync(string refreshToken)
        {
            var tokenHash = _refreshTokenService.HashToken(refreshToken);

            var storedToken = await _dbContext.RefreshTokens
                    .FirstOrDefaultAsync(x => x.TokenHash == tokenHash);

            if (storedToken == null || storedToken.RevokedAt.HasValue)
            {
                return;
            }

            storedToken.RevokedAt = DateTime.UtcNow;

            await _dbContext.SaveChangesAsync();
        }

        public async Task LogoutAllAsync(int userId)
        {
            var tokens =
                await _dbContext.RefreshTokens
                    .Where(x =>
                        x.UserId == userId &&
                        x.RevokedAt == null)
                    .ToListAsync();

            foreach (var token in tokens)
            {
                token.RevokedAt =
                    DateTime.UtcNow;
            }

            await _dbContext.SaveChangesAsync();
        }
    }
}
