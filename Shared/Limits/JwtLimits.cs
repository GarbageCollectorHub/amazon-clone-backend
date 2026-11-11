namespace AMZN.Shared.Limits;

public static class JwtLimits
{
    public const int MinSigningKeyBytes = 32; // HS256 requires at least 256 bits.
    public const int RefreshTokenEntropyBytes = 32;
    public const int RefreshTokenRequestMaxLength = 512;

    public const int AccessTokenDefaultMinutes = 30;
    public const int RefreshTokenDefaultDays = 7;
    public const int RefreshTokenMinDays = 1;
    public const int RefreshTokenMaxDays = 365;
    public const int ClockSkewSeconds = 30;
}
