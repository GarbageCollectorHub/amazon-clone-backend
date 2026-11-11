namespace AMZN.Shared.Limits;

public static class CatalogLimits
{
    public const int BrandNameFormMaxLength = 64;
    public const int BrandNameStorageMaxLength = 128;

    public const int CategoryNameMinLength = 2;
    public const int CategoryNameMaxLength = 20;
    public const int CategoryDescriptionMinLength = 2;
    public const int CategoryDescriptionMaxLength = 100;

    public const int PromotionNameMinLength = 2;
    public const int PromotionNameMaxLength = 20;
    public const int PromotionDescriptionMinLength = 2;
    public const int PromotionDescriptionMaxLength = 100;
    public const int DiscountPercentMin = 1;
    public const int DiscountPercentMax = 99;

    public const int SearchMinLength = 2;
    public const int SearchMaxLength = 100;
    public const int ProductPageSizeMax = 100;
    public const int BrandFilterCountMax = 50;
}
