using AMZN.Shared.Limits;

namespace AMZN.Security.Tokens;

internal static class JwtConfiguration
{
    public const string IssuerKey = "Jwt:Issuer";
    public const string AudienceKey = "Jwt:Audience";
    public const string ExpiresMinutesKey = "Jwt:ExpiresMinutes";
    public const string RefreshDaysKey = "Jwt:RefreshDays";
    public const string AccessTokenCookieName = "amzn_access";
    public const string RefreshTokenCookieName = "amzn_refresh";

    // Token issuance and bearer authentication must accept the same signing keys.
    public static byte[] ReadSigningKey(IConfiguration configuration)
    {
        const string keySetting = "Jwt:Key";
        var keyBase64 = configuration[keySetting] ?? throw new InvalidOperationException($"{keySetting} is missing");

        byte[] keyBytes;
        try
        {
            keyBytes = Convert.FromBase64String(keyBase64);
        }
        catch (FormatException ex)
        {
            throw new InvalidOperationException($"{keySetting} must be a valid Base64 string.", ex);
        }

        if (keyBytes.Length < JwtLimits.MinSigningKeyBytes)
            throw new InvalidOperationException($"{keySetting} is too short. Need at least {JwtLimits.MinSigningKeyBytes} bytes for HS256.");

        return keyBytes;
    }
}
