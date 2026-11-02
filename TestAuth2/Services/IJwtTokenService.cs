using TestAuth2.Entities;

namespace TestAuth2.Services
{
    public interface IJwtTokenService
    {
        (string Token, DateTime ExpiresAtUtc) GenerateToken(User user);
    }
}
