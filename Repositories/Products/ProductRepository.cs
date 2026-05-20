using AMZN.Data;
using AMZN.Data.Entities;
using AMZN.DTOs.Products.Reviews;
using AMZN.Repositories.Products.Queries;
using AMZN.Shared.Helpers.Search;
using Microsoft.EntityFrameworkCore;

namespace AMZN.Repositories.Products
{
    public class ProductRepository : IProductRepository
    {
        private readonly DataContext _db;

        public ProductRepository(DataContext db)
        {
            _db = db;
        }

        public void Add(Product product)
        {
            _db.Products.Add(product);
        }

        public void Update(Product product)
        {
            _db.Products.Update(product);
        }

        public void Remove(Product product)
        {
            _db.Products.Remove(product);
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return _db.SaveChangesAsync(cancellationToken);
        }

        public Task<List<Product>> GetByCategoryAsync(Guid categoryId, int skip, int take)
        {
            return _db.Products
                .AsNoTracking()
                .Where(x => x.CategoryId == categoryId)
                .OrderByDescending(x => x.CreatedAt)
                .Skip(skip)
                .Take(take)
                .ToListAsync();
        }

        public Task<Product?> GetByIdAsync(Guid id)
        {
            return _db.Products
                .FirstOrDefaultAsync(x => x.Id == id);
        }

        public Task<Product?> GetByIdWithImagesAsync(Guid id)
        {
            return _db.Products
                .Include(x => x.Images)
                .FirstOrDefaultAsync(x => x.Id == id);
        }

        public Task<bool> ExistsAsync(Guid id)
        {
            return _db.Products.AnyAsync(x => x.Id == id);
        }

        public Task<Product?> GetDetailsByIdAsync(Guid id)
        {
            return _db.Products
                .AsNoTracking()
                .Include(p => p.Category)
                .Include(p => p.Brand)
                .Include(p => p.Images)
                .FirstOrDefaultAsync(p => p.Id == id);
        }

        // MyProduct View
        public Task<List<Product>> GetSellerProductsAsync(Guid sellerId)
        {
            return _db.Products
                .AsNoTracking()
                .Where(x => x.SellerId == sellerId)
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync();
        }

        public void RemoveProductImages(IEnumerable<ProductImage> productImages)
        {
            _db.ProductImages.RemoveRange(productImages);
        }
        public void AddProductImages(IEnumerable<ProductImage> productImages)
        {
            _db.ProductImages.AddRange(productImages);
        }

        // Catalog Page
        // кол-во Products подходящих под фильтры запроса
        public Task<int> CountCatalogProductsAsync(ProductListQueryParams queryParams, List<Guid>? categoryIds)
        {
            return BuildCatalogQuery(queryParams, categoryIds).CountAsync();
        }

        // Catalog Products
        public Task<List<Product>> GetCatalogProductsAsync(
            ProductListQueryParams queryParams,
            int skip,
            int take,
            List<Guid>? categoryIds
            )
        {
            IQueryable<Product> query = BuildCatalogQuery(queryParams, categoryIds);
            query = ApplySort(query, queryParams);

            return query
                .Skip(skip)
                .Take(take)
                .ToListAsync();
        }

        public Task<List<Brand>> GetCatalogBrandsAsync(List<Guid>? categoryIds)
        {
            IQueryable<Product> productQuery = _db.Products.AsNoTracking();

            if (categoryIds != null && categoryIds.Count > 0)
                productQuery = productQuery.Where(p => categoryIds.Contains(p.CategoryId));

            IQueryable<Guid> brandIdsQuery = productQuery
                .Select(p => p.BrandId)
                .Distinct();

            return _db.Brands
                .AsNoTracking()
                .Where(b => brandIdsQuery.Contains(b.Id))
                .OrderBy(b => b.Name)
                .ToListAsync();
        }

        // 
        private IQueryable<Product> BuildCatalogQuery(ProductListQueryParams q, List<Guid>? categoryIds)
        {
            var query = _db.Products.AsNoTracking();

            if (categoryIds != null && categoryIds.Count > 0)
                query = query.Where(p => categoryIds.Contains(p.CategoryId));

            if (q.BrandIds.Count > 0)
                query = query.Where(p => q.BrandIds.Contains(p.BrandId));

            if (!string.IsNullOrWhiteSpace(q.Search))
            {
                var tokens = SearchQueryHelper.SplitTokens(q.Search);

                foreach (var token in tokens)
                {
                    query = query.Where(p =>
                        p.Title.Contains(token) ||
                        p.Brand.Name.Contains(token) ||
                        p.Category.Name.Contains(token));
                }
            }

            if (q.MinPrice != null)
                query = query.Where(p => p.CurrentPrice >= q.MinPrice.Value);

            if (q.MaxPrice != null)
                query = query.Where(p => p.CurrentPrice <= q.MaxPrice.Value);

            if (q.MinRating != null && q.MinRating > 0)
            {
                var minRating = q.MinRating.Value;

                query = query.Where(p =>
                    p.RatingCount > 0 &&
                    ((decimal)p.RatingSum / p.RatingCount) >= minRating);
            }

            return query;
        }

        private static IQueryable<Product> ApplySort(IQueryable<Product> query, ProductListQueryParams q)
        {
            var sort = q.Sort?.Trim().ToLowerInvariant();

            if (string.IsNullOrWhiteSpace(sort))
            {
                if (!string.IsNullOrWhiteSpace(q.Search))
                    return ApplySearchRelevanceSort(query, q.Search);

                return query
                    .OrderByDescending(p => p.RatingCount)
                    .ThenByDescending(p => p.CreatedAt);
            }

            return sort switch
            {
                "featured" => query
                    .OrderByDescending(p => p.RatingCount)
                    .ThenByDescending(p => p.CreatedAt),

                "price_asc" => query
                    .OrderBy(p => p.CurrentPrice),

                "price_desc" => query
                    .OrderByDescending(p => p.CurrentPrice),

                "rating" or "rating_desc" => query
                    .OrderByDescending(p => p.RatingCount > 0
                        ? ((decimal)p.RatingSum / p.RatingCount)
                        : 0m)
                    .ThenByDescending(p => p.RatingCount),

                "newest" => query
                    .OrderByDescending(p => p.CreatedAt),

                _ => query
                    .OrderByDescending(p => p.RatingCount)
                    .ThenByDescending(p => p.CreatedAt)
            };
        }

        private static IQueryable<Product> ApplySearchRelevanceSort(IQueryable<Product> query, string? search)
        {
            if (string.IsNullOrWhiteSpace(search))
            {
                return query
                    .OrderByDescending(p => p.RatingCount)
                    .ThenByDescending(p => p.CreatedAt);
            }

            return query
                .OrderByDescending(p => p.Title == search)
                .ThenByDescending(p => p.Title.StartsWith(search))
                .ThenByDescending(p => p.Brand.Name == search)
                .ThenByDescending(p => p.Brand.Name.StartsWith(search))
                .ThenByDescending(p => p.Category.Name == search)
                .ThenByDescending(p => p.Category.Name.StartsWith(search))
                .ThenByDescending(p => p.RatingCount)
                .ThenByDescending(p => p.CreatedAt)
                .ThenBy(p => p.Title);
        }

        // Prodcut Ratings
        public Task<ProductRating?> GetUserRatingAsync(Guid productId, Guid userId)
        {
            return _db.ProductRatings.FirstOrDefaultAsync(x => x.ProductId == productId && x.UserId == userId);
        }

        public void AddRating(ProductRating rating)
        {
            _db.ProductRatings.Add(rating);
        }



        // Блокирует запись в БД (product) для изменений рейтинга и отзывов внутри транзакции
        public Task<Product?> GetByIdForUpdateAsync(Guid id)
        {
            return _db.Products
                .FromSqlInterpolated($"SELECT * FROM Products WHERE Id = {id} FOR UPDATE")
                .FirstOrDefaultAsync();
        }



        public async Task<(int ratingSum, int ratingCount)> UpdateProductRatingAsync(Guid productId)
        {
            var totals = await _db.ProductRatings
                .Where(x => x.ProductId == productId)
                .GroupBy(x => x.ProductId)
                .Select(g => new
                {
                    RatingSum = g.Sum(x => (int)x.Value),
                    RatingCount = g.Count()
                })
                .FirstOrDefaultAsync();

            int ratingSum = totals?.RatingSum ?? 0;
            int ratingCount = totals?.RatingCount ?? 0;

            await _db.Products
                .Where(x => x.Id == productId)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(x => x.RatingSum, ratingSum)
                    .SetProperty(x => x.RatingCount, ratingCount));

            return (ratingSum, ratingCount);
        }

        // Product Review
        public void AddReview(ProductReview review)
        {
            _db.ProductReviews.Add(review);
        }

        public Task<ProductReview?> GetUserReviewAsync(Guid productId, Guid userId)
        {
            return _db.ProductReviews.FirstOrDefaultAsync(x => x.ProductId == productId && x.UserId == userId);
        }

        public Task<ReviewDto?> GetUserReviewDtoAsync(Guid productId, Guid userId)
        {
            return BuildReviewDtoQuery(productId, userId).FirstOrDefaultAsync();
        }

        public Task<int> CountReviewsAsync(Guid productId)
        {
            return _db.ProductReviews
                .AsNoTracking()
                .CountAsync(x => x.ProductId == productId);
        }

        public Task<List<ReviewDto>> GetReviewsPageAsync(Guid productId, string? sort, int skip, int take)
        {
            var query = BuildReviewDtoQuery(productId);
            query = ApplyReviewSort(query, sort);

            return query
                .Skip(skip)
                .Take(take)
                .ToListAsync();
        }


        // Helpers Review
        private IQueryable<ReviewDto> BuildReviewDtoQuery(Guid productId, Guid? userId = null)
        {
            var reviewsQuery = _db.ProductReviews.AsNoTracking().Where(x => x.ProductId == productId);

            if (userId != null)
                reviewsQuery = reviewsQuery.Where(x => x.UserId == userId.Value);

            return reviewsQuery
                .Select(review => new ReviewDto
                {
                    Id = review.Id,
                    Rating = _db.ProductRatings
                        .Where(r => r.ProductId == review.ProductId && r.UserId == review.UserId)
                        .Select(r => r.Value)
                        .FirstOrDefault(),

                    Title = review.Title,
                    Text = review.Text,
                    AuthorName = review.User.FirstName + " " + review.User.LastName,
                    CreatedAt = review.CreatedAt,
                    UpdatedAt = review.UpdatedAt
                });
        }

        private static IQueryable<ReviewDto> ApplyReviewSort(IQueryable<ReviewDto> query, string? sort)
        {
            if (string.IsNullOrWhiteSpace(sort))
                sort = "top";
            else
                sort = sort.Trim().ToLowerInvariant();

            return sort switch
            {
                "recent" => query
                    .OrderByDescending(x => x.CreatedAt)
                    .ThenByDescending(x => x.Id),

                "top" => query
                    .OrderByDescending(x => x.Rating)
                    .ThenByDescending(x => x.CreatedAt)
                    .ThenByDescending(x => x.Id),

                _ => query
                    .OrderByDescending(x => x.Rating)
                    .ThenByDescending(x => x.CreatedAt)
                    .ThenByDescending(x => x.Id)
            };
        }

        public void PurchaseProduct(Guid productId, int quantity)
        {
            var product = _db.Products.FirstOrDefault(p => p.Id == productId);
            if (product == null)
                throw new ArgumentException("Product not found", nameof(productId));
            product.StockQuantity -= quantity;
            _db.SaveChanges();
        }
    }
}
