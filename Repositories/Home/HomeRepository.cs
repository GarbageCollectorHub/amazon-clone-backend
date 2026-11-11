using AMZN.Data;
using AMZN.Data.Entities;
using AMZN.DTOs.Home;
using Microsoft.EntityFrameworkCore;

namespace AMZN.Repositories.Home
{
    public class HomeRepository : IHomeRepository
    {
        private readonly DataContext _db;

        public HomeRepository(DataContext db)
        {
            _db = db;
        }

        public Task<List<Category>> GetAllCategoriesAsync(CancellationToken ct = default)
        {
            return _db.Categories
                .AsNoTracking()
                .OrderBy(x => x.Name)
                .ToListAsync(ct);
        }

        public Task<List<CategoryProductCountsDto>> GetCategoryProductCountsAsync(decimal minRating, CancellationToken ct = default)
        {
            return _db.Products
                .AsNoTracking()
                .Where(x => x.StockQuantity > 0)
                .Where(x => x.RatingCount > 0)
                .Where(x => (decimal)x.RatingSum / x.RatingCount > minRating)
                .GroupBy(x => x.CategoryId)
                .Select(group => new CategoryProductCountsDto
                {
                    CategoryId = group.Key,
                    ProductCount = group.Count(),
                    RatingCount = group.Sum(x => x.RatingCount)
                })
                .ToListAsync(ct);
        }

        public Task<List<Product>> GetTopRatedProductsForCategoriesAsync(List<Guid> categoryIds, decimal minRating, int take, CancellationToken ct = default)
        {
            if (categoryIds.Count == 0)
                return Task.FromResult(new List<Product>());

            return _db.Products
                .AsNoTracking()
                .Where(x => categoryIds.Contains(x.CategoryId))
                .Where(x => x.StockQuantity > 0)
                .Where(x => x.RatingCount > 0)
                .Where(x => (decimal)x.RatingSum / x.RatingCount > minRating)
                .OrderByDescending(x => x.RatingCount)
                .ThenByDescending(x => (decimal)x.RatingSum / x.RatingCount)
                .ThenByDescending(x => x.CreatedAt)
                .ThenBy(x => x.Title)
                .Take(take)
                .ToListAsync(ct);
        }

        public Task<List<Product>> GetTopRatedProductsAsync(decimal minRating, int take, CancellationToken ct = default)
        {
            return _db.Products
                .AsNoTracking()
                .Where(x => x.StockQuantity > 0)
                .Where(x => x.RatingCount > 0)
                .Where(x => (decimal)x.RatingSum / x.RatingCount > minRating)
                .OrderByDescending(x => x.RatingCount)
                .ThenByDescending(x => (decimal)x.RatingSum / x.RatingCount)
                .ThenByDescending(x => x.CreatedAt)
                .ThenBy(x => x.Title)
                .Take(take)
                .ToListAsync(ct);
        }

        public Task<List<Product>> GetTopRatedProductsUnderPriceAsync(
            decimal maxPrice,
            decimal minRating,
            int take,
            CancellationToken ct = default)
        {
            return _db.Products
                .AsNoTracking()
                .Where(x => x.StockQuantity > 0)
                .Where(x => x.CurrentPrice <= maxPrice)
                .Where(x => x.RatingCount > 0)
                .Where(x => (decimal)x.RatingSum / x.RatingCount > minRating)
                .OrderByDescending(x => x.RatingCount)
                .ThenByDescending(x => (decimal)x.RatingSum / x.RatingCount)
                .ThenByDescending(x => x.CreatedAt)
                .ThenBy(x => x.Title)
                .Take(take)
                .ToListAsync(ct);
        }

        public Task<List<Product>> GetProductsByIdsAsync(List<Guid> productIds, CancellationToken ct = default)
        {
            if (productIds.Count == 0)
                return Task.FromResult(new List<Product>());

            return _db.Products
                .AsNoTracking()
                .Where(x => productIds.Contains(x.Id) && x.StockQuantity > 0)
                .ToListAsync(ct);
        }
    }
}
