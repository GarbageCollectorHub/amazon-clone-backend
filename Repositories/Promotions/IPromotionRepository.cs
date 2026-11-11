using AMZN.Data.Entities;

namespace AMZN.Repositories.Promotions;

public interface IPromotionRepository
{
    Task<bool> NameExistsAsync(string name);

    Task AddProductPromotionAsync(Promotion promotion);
    Task AddCategoryPromotionAsync(Promotion promotion);
    Task ApplyDiscountAsync(Promotion promotion);
    Task RemoveDiscountAsync(Promotion promotion);
    Task<List<Promotion>> GetAppliedPromotionsOutsideScheduleAsync(DateTime now);
    Task<List<Promotion>> GetActivePromotionsAsync(DateTime now, bool includeApplied);
}
