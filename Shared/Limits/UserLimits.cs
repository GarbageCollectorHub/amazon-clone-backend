namespace AMZN.Shared.Limits;

public static class UserLimits
{
    public const int EmailMaxLength = 128;
    // Kept separate because the existing sign-in contract is stricter.
    public const int LoginEmailMaxLength = 100;

    public const int PersonNameMinLength = 3;
    public const int PersonNameMaxLength = 64;
    public const string PersonNamePattern = @"^[\p{L}\p{M}]+(?:[ '\-][\p{L}\p{M}]+)*$";

    public const int PasswordMinLength = 6;
    public const int PasswordMaxLength = 72;

    public const int AdminSignUpNameMinLength = 2;
    public const int AdminSignUpNameMaxLength = 20;
    public const int AdminSignUpPasswordMinLength = 8;
    public const int AdminSignUpPasswordMaxLength = 20;

    public const int ProfilePhoneMaxLength = 15;
    public const int ProfileMinAgeYears = 18;
    public const string PhoneNumberPattern = @"^\+[1-9]\d{1,14}$";
}
