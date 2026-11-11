namespace AMZN.Shared.Validation;

public static class ValidationMessages
{
    public const string Required = "{0} is required";
    public const string MaxLength = "{0} must be {1} characters or less";
    public const string LengthRange = "{0} must be {2}-{1} characters";
    public const string InvalidFormat = "Invalid {0} format";
    public const string PersonNameFormat = "{0} may contain letters, spaces, hyphens and apostrophes";
    public const string RatingRange = "Rating must be between {1} and {2}";
}
