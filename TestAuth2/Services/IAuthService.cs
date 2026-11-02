using TestAuth2.DTOs.Auth;

namespace TestAuth2.Services
{
    public interface IAuthService
    {
        Task<AuthResponse?> LoginAsync(LoginRequest request);

        Task<bool> RegisterAsync(RegisterRequest request);
    }
}
