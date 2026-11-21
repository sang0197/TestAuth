namespace TestAuth2.DTOs.Auth
{
    public class UserResponse
    {
        public int Id { get; set; }

        public string Username { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public List<string> Roles { get; set; } = new();
        public List<string> Permissions { get; set; } = new();
    }
}
