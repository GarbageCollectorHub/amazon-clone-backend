using AMZN.DTOs.Auth;

namespace AMZN.Services.Auth
{
    public interface IAuthService
    {
        Task<AuthResponseDto> RegisterAsync(RegisterRequestDto dto);
        Task<AuthResponseDto> LoginAsync(LoginRequestDto dto);
        Task<AuthResponseDto> RefreshAsync(RefreshRequestDto dto);
        Task RevokeRefreshTokenAsync(string? refreshToken);
    }
}
