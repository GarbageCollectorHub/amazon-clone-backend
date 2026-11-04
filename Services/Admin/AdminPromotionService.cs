using AMZN.Data.Entities;
using AMZN.Models;
using AMZN.Models.Promotions;
using AMZN.Repositories.Promotions;
using AMZN.Repositories.Categories;
using AMZN.Repositories.Products;

namespace AMZN.Services.Admin;

public class AdminPromotionService
{
    private readonly IPromotionRepository _promotions;
    private readonly FormsValidators _formsValidator;
    private readonly IProductRepository _products;
    private readonly ICategoryRepository _categories;
    public AdminPromotionService(IPromotionRepository promotions,
        FormsValidators formsValidator,
        IProductRepository products,
        ICategoryRepository categories)
    {
        _promotions = promotions;
        _formsValidator = formsValidator;
        _products = products;
        _categories = categories;
    }

    public (bool, object) AddPromotion(PromotionFormModel formModel)
    {
        Dictionary<string, string> errors = _formsValidator.ValidatePromotion(formModel);
        if (errors.Count == 0)
        {
            Guid id = Guid.NewGuid();
            Promotion promotion = new()
            {
                Id = id,
                Name = formModel.Name,
                ProductTitle = formModel.ProductTitle,
                DiscountPercent = formModel.DiscountPercent,
                StartDate = formModel.StartDate,
                EndDate = formModel.EndDate,
                CreatedAt = DateTime.UtcNow,
            };
            if (formModel.Description != null)
            {
                promotion.Description = formModel.Description;
            }
            if (formModel.ApplyTo == "product")
            {
                _promotions.AddProductPromotion(promotion);
                if (_promotions.IsValid(promotion))
                {
                    _promotions.ApplyProductPromotion(promotion);
                }
            }
            if (formModel.ApplyTo == "category")
            {
                _promotions.AddCategoryPromotion(promotion);
                if (_promotions.IsValid(promotion))
                {
                    _promotions.ApplyCategoryPromotion(promotion);
                }
            }
            return (true, "Promotion added successfully");
        }
        else
        {
            return (false, errors.Values);
        }
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
