using AMZN.Data.Entities;

namespace AMZN.Repositories.Promotions;

public interface IPromotionRepository
{
    void AddProductPromotion(Promotion promotion);

    void AddCategoryPromotion(Promotion promotion);

    void ApplyProductPromotion(Promotion promotion);

    void ApplyCategoryPromotion(Promotion promotion);

    void RemoveProductPromotion(Promotion promotion);

    void RemoveCategoryPromotion(Promotion promotion);

    bool IsValid(Promotion promotion);

    List<Promotion> GetPromotions();
}
