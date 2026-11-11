namespace AMZN.Shared.Limits;

public static class ProductLimits
{
    public const int TitleMaxLength = 256;
    public const int DescriptionMaxLength = 4000;
    public const int ImageUrlMaxLength = 2048;

    // RangeAttribute for decimal uses string bounds. Keep creation stricter than editing.
    public const string CreatePriceMin = "1";
    public const string PriceMin = "0";
    public const string PriceMax = "1000000";
    public const int StockMin = 0;
}
