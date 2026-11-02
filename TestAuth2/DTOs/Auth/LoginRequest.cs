using System.ComponentModel.DataAnnotations;

namespace TestAuth2.DTOs.Auth
{
    public class LoginRequest
    {
        [Required]
        public string Login { get; set; } = string.Empty;

        [Required]
        public string Password { get; set; } = string.Empty;
    }
}
