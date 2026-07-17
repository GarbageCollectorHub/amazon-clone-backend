namespace AMZN.DTOs.Auth
{
    public class AuthResponseDto
    {
        public string AccessToken { get; set; } = "";
        public int  ExpiresInSeconds { get; set; }
        public string RefreshToken { get; set; } = "";
        public int RefreshExpiresInSeconds { get; set; }
        public string TokenType { get; set; } = "Bearer";

        public UserResponseDto User { get; set; } = null!;
    }
}
