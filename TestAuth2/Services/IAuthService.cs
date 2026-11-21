using TestAuth2.DTOs.Auth;

namespace TestAuth2.Services
{
    public interface IAuthService
    {
        Task<AuthResponse?> LoginAsync(LoginRequest request);

        Task<bool> RegisterAsync(RegisterRequest request);

        Task<AuthResponse?> RefreshAsync(string refreshToken);

        Task LogoutAsync(string refreshToken);

        Task LogoutAllAsync(int userId);
    }
}
