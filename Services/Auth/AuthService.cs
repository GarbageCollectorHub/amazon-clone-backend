using AMZN.Data.Entities;
using AMZN.DTOs.Auth;
using AMZN.Repositories.Users;
using AMZN.Security.Passwords;
using AMZN.Security.Tokens;
using AMZN.Shared.Exceptions;
using AMZN.Shared.Exceptions.Errors;
using AMZN.Shared.Mapping;

namespace AMZN.Services.Auth
{
    public class AuthService : IAuthService
    {
        // refresh token TTL берём из конфига Jwt:RefreshDays.  + ограничиваем на 1 - 365 дней (защита от кривого конфига)  // ps: в идеале валидировать конфиг на старте...
        private const int RefreshDaysDefault = 7;
        private const int RefreshDaysMin = 1;
        private const int RefreshDaysMax = 365;


        private readonly IUserRepository _users;
        private readonly IUserRefreshTokenRepository _refreshTokens;
        private readonly IPasswordHasher _passwordHasher;
        private readonly IJwtService _jwt;
        private readonly IConfiguration _config;
        private readonly ILogger<AuthService> _logger;


        public AuthService(
            IUserRepository users,
            IUserRefreshTokenRepository refreshTokens,
            IPasswordHasher passwordHasher,
            IJwtService jwt,
            IConfiguration config,
            ILogger<AuthService> logger)
        {
            _users = users;
            _refreshTokens = refreshTokens;
            _passwordHasher = passwordHasher;
            _jwt = jwt;
            _config = config;
            _logger = logger;
        }

        public async Task<AuthResponseDto> RegisterAsync(RegisterRequestDto dto)
        {
            string email = NormalizeEmail(dto.Email);

            _logger.LogInformation("Register attempt: email={Email}", email);

            bool isTaken = await _users.IsEmailTakenAsync(email);
            if(isTaken)
            {
                _logger.LogWarning("Register failed: email taken, email={Email}", email);
                throw new ApiException(ErrorCodes.AuthEmailTaken, "Email already taken", StatusCodes.Status409Conflict);
            }

            var user = new User
            {
                Id = Guid.NewGuid(),
                FirstName = dto.FirstName.Trim(),
                LastName = dto.LastName.Trim(),
                Email = email,
                PasswordHash = _passwordHasher.HashPassword(dto.Password),
                Role = UserRole.User,
                CreatedAt = DateTime.UtcNow,
            };

            await _users.AddUserAsync(user);

            _logger.LogInformation("Register success: userId={UserId}, email={Email}", user.Id, email);
            return await IssueTokenPairAsync(user);

        }


        // Login
        public async Task<AuthResponseDto> LoginAsync(LoginRequestDto dto)
        {
            string email = NormalizeEmail(dto.Email);
            _logger.LogInformation("Login attempt: email={Email}", email);

            User? user = await _users.GetByEmailAsync(email);

            if (user is null || !_passwordHasher.VerifyPassword(dto.Password, user.PasswordHash))
            {
                _logger.LogWarning("Login failed: invalid credentials, email={Email}", email);
                throw new ApiException(ErrorCodes.AuthInvalidCredentials, "Invalid credentials", StatusCodes.Status401Unauthorized);
            }

            _logger.LogInformation("Login success: userId={UserId}, email={Email}", user.Id, email);
            return await IssueTokenPairAsync(user);
        }


        public async Task<AuthResponseDto> RefreshAsync(RefreshRequestDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.RefreshToken))
                throw new ApiException(ErrorCodes.AuthInvalidRefreshToken, "Invalid refresh token", StatusCodes.Status401Unauthorized);

            _logger.LogInformation("Refresh Token attempt");

            string refreshToken = dto.RefreshToken.Trim();
            string tokenHash = _jwt.HashRefreshToken(refreshToken);

            UserRefreshToken? storedToken = await _refreshTokens.GetValidByHashAsync(tokenHash);
            if (storedToken == null)
            {
                _logger.LogWarning("Refresh failed: invalid refresh token");
                throw new ApiException(ErrorCodes.AuthInvalidRefreshToken, "Invalid refresh token", StatusCodes.Status401Unauthorized);
            }

            await _refreshTokens.RevokeAsync(storedToken);
            _logger.LogInformation("Refresh success: userId={UserId}", storedToken.UserId);

            return await IssueTokenPairAsync(storedToken.User);
        }




        private static string NormalizeEmail(string email)
        {
            return email.Trim().ToLowerInvariant();
        }

        private int GetRefreshDays()
        {
            var valueText = _config["Jwt:RefreshDays"];

            int days;
            if (!int.TryParse(valueText, out days))
            {
                days = RefreshDaysDefault;
            }

            if (days < RefreshDaysMin) return RefreshDaysMin;
            if (days > RefreshDaysMax) return RefreshDaysMax;

            return days;
        }


        private async Task<AuthResponseDto> IssueTokenPairAsync(User user)
        {
            (string accessToken, int expiresInSeconds) = _jwt.GenerateAccessToken(user);

            var refreshDays = GetRefreshDays();
            var refreshToken = _jwt.GenerateRefreshToken();
            var refreshHash = _jwt.HashRefreshToken(refreshToken);

            var refreshTokenEntity = new UserRefreshToken
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                TokenHash = refreshHash,
                ExpiresAt = DateTime.UtcNow.AddDays(refreshDays),
                IsRevoked = false,
            };

            await _refreshTokens.AddRefreshTokenAsync(refreshTokenEntity);

            return new AuthResponseDto
            {
                AccessToken = accessToken,
                ExpiresInSeconds = expiresInSeconds,
                RefreshToken = refreshToken,
                TokenType = "Bearer",
                User = user.ToResponseDto()
            };

        }



    }
}
