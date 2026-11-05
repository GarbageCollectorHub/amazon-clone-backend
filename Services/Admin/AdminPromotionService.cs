using AMZN.Data.Entities;
using AMZN.Models.Promotions;
using AMZN.Repositories.Promotions;
using AMZN.Repositories.Categories;
using AMZN.Repositories.Products;

namespace AMZN.Services.Admin;

public class AdminPromotionService
{
    private readonly IPromotionRepository _promotions;
    private readonly IProductRepository _products;
    private readonly ICategoryRepository _categories;
    public AdminPromotionService(IPromotionRepository promotions,
        IProductRepository products,
        ICategoryRepository categories)
    {
        _promotions = promotions;
        _products = products;
        _categories = categories;
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
        if (formModel.ApplyTo == "product")
        {
            _promotions.AddProductPromotion(promotion);
            if (_promotions.IsValid(promotion))
                _promotions.ApplyProductPromotion(promotion);
        }
        else
        {
            _promotions.AddCategoryPromotion(promotion);
            if (_promotions.IsValid(promotion))
                _promotions.ApplyCategoryPromotion(promotion);
        }
        return errors;
    }

    public void CheckPromotionsValidity()
    {
        var promotions = _promotions.GetPromotions()
            .OrderBy(a => a.CreatedAt)
            .ThenBy(a => a.Id)
            .ToList();
        var inactivePromotions = promotions.Where(a => a.IsApplied && !_promotions.IsValid(a)).ToList();

        foreach (var promotion in inactivePromotions)
        {
            _promotions.RemoveProductPromotion(promotion);
            _promotions.RemoveCategoryPromotion(promotion);
        }

        foreach (var promotion in promotions.Where(a => _promotions.IsValid(a)))
        {
            if (!promotion.IsApplied || inactivePromotions.Count > 0)
            {
                _promotions.ApplyProductPromotion(promotion);
                _promotions.ApplyCategoryPromotion(promotion);
            }
        }
    }
}
