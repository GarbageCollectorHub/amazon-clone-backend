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

    public async Task AddProductPromotionAsync(Promotion promotion)
    {
        _dataContext.Promotions.Add(promotion);
        var productId = await _dataContext.Products.Where(p => p.Title == promotion.ProductTitle)
            .Select(p => (Guid?)p.Id).FirstOrDefaultAsync();
        if (productId.HasValue)
        {
            _dataContext.ProductPromotions.Add(new()
            {
                ProductId = productId.Value,
                PromotionId = promotion.Id
            });
        }

        await _dataContext.SaveChangesAsync();
    }

    public async Task AddCategoryPromotionAsync(Promotion promotion)
    {
        _dataContext.Promotions.Add(promotion);
        var categoryId = await _dataContext.Categories.Where(c => c.Name == promotion.ProductTitle)
            .Select(c => (Guid?)c.Id).FirstOrDefaultAsync();
        if (categoryId.HasValue)
        {
            _dataContext.CategoryPromotions.Add(new()
            {
                CategoryId = categoryId.Value,
                PromotionId = promotion.Id
            });
        }

        await _dataContext.SaveChangesAsync();
    }

    public async Task ApplyDiscountAsync(Promotion promotion)
    {
        decimal priceMultiplier = (100 - promotion.DiscountPercent) / 100m;

        // Повторное применение считает скидку от исходной цены, а не от уже сниженной.
        int updated = await GetProductsForPromotion(promotion.Id).ExecuteUpdateAsync(setters => setters
            .SetProperty(p => p.OriginalPrice, p => p.OriginalPrice ?? p.CurrentPrice)
            .SetProperty(p => p.CurrentPrice, p => (p.OriginalPrice ?? p.CurrentPrice) * priceMultiplier));

        if (updated == 0)
            return;

        promotion.IsApplied = true;
        await _dataContext.SaveChangesAsync();
    }

    public async Task RemoveDiscountAsync(Promotion promotion)
    {
        await GetProductsForPromotion(promotion.Id)
            .Where(p => p.OriginalPrice != null)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(p => p.CurrentPrice, p => p.OriginalPrice ?? p.CurrentPrice)
                .SetProperty(p => p.OriginalPrice, (decimal?)null));

        promotion.IsApplied = false;
        await _dataContext.SaveChangesAsync();
    }

    public Task<List<Promotion>> GetAppliedPromotionsOutsideScheduleAsync(DateTime now)
    {
        return _dataContext.Promotions
            .Where(p => p.IsApplied && (now < p.StartDate || now >= p.EndDate))
            .OrderBy(p => p.CreatedAt)
            .ThenBy(p => p.Id)
            .ToListAsync();
    }

    public Task<List<Promotion>> GetActivePromotionsAsync(DateTime now, bool includeApplied)
    {
        var query = _dataContext.Promotions.Where(p => p.StartDate <= now && now < p.EndDate);
        if (!includeApplied)
            query = query.Where(p => !p.IsApplied);

        return query.OrderBy(p => p.CreatedAt).ThenBy(p => p.Id).ToListAsync();
    }

    private IQueryable<Product> GetProductsForPromotion(Guid promotionId)
    {
        var directProductIds = _dataContext.ProductPromotions
            .Where(link => link.PromotionId == promotionId)
            .Select(link => link.ProductId);
        var categoryIds = _dataContext.CategoryPromotions
            .Where(link => link.PromotionId == promotionId)
            .Select(link => link.CategoryId);

        return _dataContext.Products.Where(p => directProductIds.Contains(p.Id) || categoryIds.Contains(p.CategoryId));
    }
}
