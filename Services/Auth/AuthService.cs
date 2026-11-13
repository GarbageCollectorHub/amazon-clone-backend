using AMZN.Data.Entities;
using AMZN.DTOs.Auth;
using AMZN.Repositories.Users;
using AMZN.Security.Passwords;
using AMZN.Security.Tokens;
using AMZN.Shared.Exceptions;
using AMZN.Shared.Exceptions.Errors;
using AMZN.Shared.Limits;
using AMZN.Shared.Mapping;
using AMZN.Shared.Transactions;
using Microsoft.Data.SqlClient;

namespace AMZN.Services.Auth
{
    public class AuthService : IAuthService
    {
        private const int SqlDatabaseUnavailableErrorNumber = 40613;

        private readonly IUserRepository _users;
        private readonly IUserRefreshTokenRepository _refreshTokens;
        private readonly IPasswordHasher _passwordHasher;
        private readonly IJwtService _jwt;
        private readonly IConfiguration _config;
        private readonly ILogger<AuthService> _logger;
        private readonly ITransactionManager _transactionManager;


        public AuthService(
            IUserRepository users,
            IUserRefreshTokenRepository refreshTokens,
            IPasswordHasher passwordHasher,
            IJwtService jwt,
            IConfiguration config,
            ILogger<AuthService> logger,
            ITransactionManager transactionManager)
        {
            _users = users;
            _refreshTokens = refreshTokens;
            _passwordHasher = passwordHasher;
            _jwt = jwt;
            _config = config;
            _logger = logger;
            _transactionManager = transactionManager;
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

            try
            {
                User? user = await _users.GetByEmailAsync(email);

                if (user is null || !_passwordHasher.VerifyPassword(dto.Password, user.PasswordHash))
                {
                    _logger.LogWarning("Login failed: invalid credentials, email={Email}", email);
                    throw new ApiException(ErrorCodes.AuthInvalidCredentials, "Invalid credentials", StatusCodes.Status401Unauthorized);
                }

                AuthResponseDto response = await IssueTokenPairAsync(user);
                _logger.LogInformation("Login success: userId={UserId}, email={Email}", user.Id, email);
                return response;
            }
            // На бесплатном тарифе Azure SQL база демо может уходить на паузу после простоя.
            // Если повторы подключения не помогли и SQL вернул 40613, отдаём 503,
            // чтобы форма входа подсказала подождать и попробовать снова.
            catch (Exception exception) when (IsSqlDatabaseUnavailable(exception))
            {
                _logger.LogWarning(exception, "The database was unavailable during sign-in");
                throw new ApiException(ErrorCodes.DatabaseUnavailable, "The database is temporarily unavailable.", StatusCodes.Status503ServiceUnavailable);
            }
        }


        public async Task<AuthResponseDto> RefreshAsync(RefreshRequestDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.RefreshToken))
                throw new ApiException(ErrorCodes.AuthInvalidRefreshToken, "Invalid refresh token", StatusCodes.Status401Unauthorized);

            _logger.LogInformation("Refresh Token attempt");

            string refreshToken = dto.RefreshToken.Trim();
            string tokenHash = _jwt.HashRefreshToken(refreshToken);

            return await _transactionManager.ExecuteAsync(async () =>
            {
                UserRefreshToken? storedToken = await _refreshTokens.GetValidByHashAsync(tokenHash);
                if (storedToken == null || !await _refreshTokens.TryRevokeAsync(tokenHash))
                {
                    _logger.LogWarning("Refresh failed: invalid refresh token");
                    throw new ApiException(ErrorCodes.AuthInvalidRefreshToken, "Invalid refresh token", StatusCodes.Status401Unauthorized);
                }

                AuthResponseDto response = await IssueTokenPairAsync(storedToken.User);
                _logger.LogInformation("Refresh success: userId={UserId}", storedToken.UserId);

                return response;
            });
        }


        public async Task RevokeRefreshTokenAsync(string? refreshToken)
        {
            if (string.IsNullOrWhiteSpace(refreshToken))
                return;

            string tokenHash = _jwt.HashRefreshToken(refreshToken.Trim());
            await _refreshTokens.TryRevokeAsync(tokenHash);
        }




        private static bool IsSqlDatabaseUnavailable(Exception? exception)
        {
            while (exception != null)
            {
                if (exception is SqlException sqlException)
                {
                    return sqlException.Number == SqlDatabaseUnavailableErrorNumber;
                }

                // Show the retry notice only if all failed connection attempts report the same SQL error.
                if (exception is AggregateException aggregateException)
                {
                    var attempts = aggregateException.Flatten().InnerExceptions;
                    return attempts.Count > 0 && attempts.All(IsSqlDatabaseUnavailable);
                }

                exception = exception.InnerException;
            }

            return false;
        }

        private static string NormalizeEmail(string email)
        {
            return email.Trim().ToLowerInvariant();
        }

        private int GetRefreshDays()
        {
            var valueText = _config[JwtConfiguration.RefreshDaysKey];

            int days;
            if (!int.TryParse(valueText, out days))
            {
                days = JwtLimits.RefreshTokenDefaultDays;
            }

            return Math.Clamp(days, JwtLimits.RefreshTokenMinDays, JwtLimits.RefreshTokenMaxDays);
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
                RefreshExpiresInSeconds = (int)TimeSpan.FromDays(refreshDays).TotalSeconds,
                TokenType = "Bearer",
                User = user.ToResponseDto()
            };

        }



    }
}
