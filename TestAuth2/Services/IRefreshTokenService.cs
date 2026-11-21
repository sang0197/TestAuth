namespace TestAuth2.Services
{
    public interface IRefreshTokenService
    {
        string GenerateToken();
        string HashToken(string token);
    }
}
