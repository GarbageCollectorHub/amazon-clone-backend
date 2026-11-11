using AMZN.Data.Entities;
using AMZN.Models.Promotions;
using AMZN.Repositories.Promotions;
using AMZN.Repositories.Categories;
using AMZN.Repositories.Products;
using AMZN.Shared.Transactions;

namespace AMZN.Services.Admin;

public class AdminPromotionService
{
    private readonly IPromotionRepository _promotions;
    private readonly IProductRepository _products;
    private readonly ICategoryRepository _categories;
    private readonly ITransactionManager _transactionManager;
    public AdminPromotionService(
        IPromotionRepository promotions,
        IProductRepository products,
        ICategoryRepository categories,
        ITransactionManager transactionManager)
    {
        _promotions = promotions;
        _products = products;
        _categories = categories;
        _transactionManager = transactionManager;
    }

    public async Task<Dictionary<string, string>> AddPromotionAsync(PromotionFormModel formModel)
    {
        string name = formModel.Name.Trim();
        string target = formModel.ProductTitle.Trim();
        Dictionary<string, string> errors = new();
        if (await _promotions.NameExistsAsync(name))
            errors.Add(nameof(formModel.Name), "Name already taken");
        if (formModel.ApplyTo == "product" && !await _products.TitleExistsAsync(target))
            errors.Add(nameof(formModel.ProductTitle), "Product does not exist");
        if (formModel.ApplyTo == "category" && !await _categories.NameExistsAsync(target))
            errors.Add(nameof(formModel.ProductTitle), "Category does not exist");
        if (errors.Count > 0)
            return errors;

        Promotion promotion = new()
        {
            Id = Guid.NewGuid(),
            Name = name,
            Description = formModel.Description?.Trim(),
            ProductTitle = target,
            DiscountPercent = formModel.DiscountPercent.GetValueOrDefault(),
            StartDate = formModel.StartDate.GetValueOrDefault(),
            EndDate = formModel.EndDate.GetValueOrDefault(),
            CreatedAt = DateTime.UtcNow,
        };
        await _transactionManager.ExecuteAsync(async () =>
        {
            if (formModel.ApplyTo == "product")
                await _promotions.AddProductPromotionAsync(promotion);
            else
                await _promotions.AddCategoryPromotionAsync(promotion);

            if (IsValid(promotion, DateTime.UtcNow))
                await _promotions.ApplyDiscountAsync(promotion);
        });
        return errors;
    }

    public Task CheckPromotionsValidityAsync()
    {
        return _transactionManager.ExecuteAsync(async () =>
        {
            DateTime now = DateTime.UtcNow;
            var promotionsToRemove = await _promotions.GetAppliedPromotionsOutsideScheduleAsync(now);

            foreach (var promotion in promotionsToRemove)
                await _promotions.RemoveDiscountAsync(promotion);

            // Снятие акции возвращает исходную цену. Применяем активные скидки заново,
            // чтобы сохранить их на товарах, где акции пересекались.
            var promotionsToApply = await _promotions.GetActivePromotionsAsync(now, includeApplied: promotionsToRemove.Count > 0);
            foreach (var promotion in promotionsToApply)
                await _promotions.ApplyDiscountAsync(promotion);
        });
    }

    private static bool IsValid(Promotion promotion, DateTime now)
    {
        return promotion.StartDate <= now && now < promotion.EndDate;
    }
}
