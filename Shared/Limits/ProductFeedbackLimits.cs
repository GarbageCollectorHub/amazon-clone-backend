namespace AMZN.Shared.Limits;

public static class ProductFeedbackLimits
{
    public const int RatingMin = 1;
    public const int RatingMax = 5;
    public const int ReviewTitleMaxLength = 120;
    public const int ReviewTextMaxLength = 4000;
    public const int QuestionTextMaxLength = 1024;
    public const int AnswerTextMaxLength = 2048;
    public const int QuestionOrAnswerTextMinLength = 5;
    public const int QuestionOrAnswerPageSizeMax = 50;
}
