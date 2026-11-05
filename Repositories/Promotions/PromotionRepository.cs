using AMZN.Data;
using AMZN.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace AMZN.Repositories.Promotions;

public class PromotionRepository : IPromotionRepository
{
    private readonly DataContext _dataContext;

    public PromotionRepository(DataContext dataContext)
    {
        _dataContext = dataContext;
    }

    public Task<bool> NameExistsAsync(string name)
    {
        return _dataContext.Promotions.AnyAsync(p => p.Name == name);
    }
    
    public void AddProductPromotion(Promotion promotion)
    {
        _dataContext.Promotions.Add(promotion);
        var product = _dataContext.Products.FirstOrDefault(p => p.Title == promotion.ProductTitle);
        if (product != null)
        {
            _dataContext.ProductPromotions.Add(new()
            {
                ProductId = product.Id,
                PromotionId = promotion.Id
            });
        }

        _dataContext.SaveChanges();
    }

    public void AddCategoryPromotion(Promotion promotion)
    {
        _dataContext.Promotions.Add(promotion);
        var category = _dataContext.Categories.FirstOrDefault(c => c.Name == promotion.ProductTitle);
        if (category != null)
        {
            _dataContext.CategoryPromotions.Add(new()
            {
                CategoryId = category.Id,
                PromotionId = promotion.Id
            });
        }
        
        _dataContext.SaveChanges();
    }

    public void ApplyProductPromotion(Promotion promotion)
    {
        var productPromotion = _dataContext.ProductPromotions.FirstOrDefault(pa => pa.Promotion.Id == promotion.Id);
        if (productPromotion != null)
        {
            var product = _dataContext.Products.FirstOrDefault(p => p.Id == productPromotion.ProductId);
            if (product != null)
            {
                if (product.OriginalPrice == null)
                {
                    product.OriginalPrice = product.CurrentPrice;
                    product.CurrentPrice *= (decimal)((100 - promotion.DiscountPercent) * 0.01);
                }
                else
                {
                    product.CurrentPrice = (decimal)product.OriginalPrice * (decimal)((100 - promotion.DiscountPercent) * 0.01);
                }
                _dataContext.Products.Update(product);
                promotion.IsApplied = true;
                _dataContext.SaveChanges();
            }
        }
    }

    public void ApplyCategoryPromotion(Promotion promotion)
    {
        var categoryPromotion = _dataContext.CategoryPromotions.FirstOrDefault(ca => ca.Promotion.Id == promotion.Id);
        if (categoryPromotion != null)
        {
            var category = _dataContext.Categories.FirstOrDefault(c => c.Id == categoryPromotion.CategoryId);
            if (category != null)
            {
                var products = _dataContext.Products.Where(p => p.CategoryId == category.Id);
                if (products.Count() != 0)
                {
                    foreach (var product in products)
                    {
                        if (product.OriginalPrice == null)
                        {
                            product.OriginalPrice = product.CurrentPrice;
                            product.CurrentPrice *= (decimal)((100 - promotion.DiscountPercent) * 0.01);
                        }
                        else
                        {
                            product.CurrentPrice = (decimal)product.OriginalPrice * (decimal)((100 - promotion.DiscountPercent) * 0.01);
                        }
                        _dataContext.Products.Update(product);
                    }
                    promotion.IsApplied = true;
                    _dataContext.SaveChanges();
                }
            }
        }
    }

    public void RemoveProductPromotion(Promotion promotion)
    {
        var productPromotion = _dataContext.ProductPromotions.FirstOrDefault(pa => pa.Promotion.Id == promotion.Id);
        if (productPromotion != null)
        {
            var product = _dataContext.Products.FirstOrDefault(p => p.Id == productPromotion.ProductId);
            if (product != null)
            {
                if (product.OriginalPrice != null)
                {
                    product.CurrentPrice = (decimal)product.OriginalPrice;
                    product.OriginalPrice = null;
                    _dataContext.Products.Update(product);
                }
            }
            promotion.IsApplied = false;
            _dataContext.SaveChanges();
        }
    }

    public void RemoveCategoryPromotion(Promotion promotion)
    {
        var categoryPromotion = _dataContext.CategoryPromotions.FirstOrDefault(ca => ca.Promotion.Id == promotion.Id);
        if (categoryPromotion != null)
        {
            var category = _dataContext.Categories.FirstOrDefault(c => c.Id == categoryPromotion.CategoryId);
            if (category != null)
            {
                var products = _dataContext.Products.Where(p => p.CategoryId == category.Id);
                if (products.Count() != 0)
                {
                    foreach (var product in products)
                    {
                        if (product.OriginalPrice != null)
                        {
                            product.CurrentPrice = (decimal)product.OriginalPrice;
                            product.OriginalPrice = null;
                            _dataContext.Products.Update(product);
                        }
                    }
                }
            }
            promotion.IsApplied = false;
            _dataContext.SaveChanges();
        }
    }

    public bool IsValid(Promotion promotion)
    {
        DateTime now = DateTime.UtcNow;
        return promotion.StartDate <= now && promotion.EndDate > now;
    }

    public List<Promotion> GetPromotions()
    {
        return _dataContext.Promotions.ToList();
    }
}
