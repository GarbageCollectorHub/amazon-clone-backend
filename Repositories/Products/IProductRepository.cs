using AMZN.Data.Entities;
using AMZN.DTOs.Common;
using AMZN.DTOs.Products;
using AMZN.DTOs.Products.Reviews;
using AMZN.Repositories.Products.Queries;

namespace AMZN.Repositories.Products
{

    public interface IProductRepository
    {
        void Add(Product product);
        void Update(Product product);
        void Remove(Product product);
        Task SaveChangesAsync(CancellationToken cancellationToken = default);

        Task<List<Product>> GetByCategoryAsync(Guid categoryId, int skip, int take);

        Task<Product?> GetByIdAsync(Guid id);
        Task<Product?> GetByIdWithImagesAsync(Guid id);
        Task<Product?> GetDetailsByIdAsync(Guid id, CancellationToken ct = default);

        Task<bool> ExistsAsync(Guid id, CancellationToken ct = default);
        Task<bool> TitleExistsAsync(string title);

        // MyProduct View
        Task<PagedResult<Product>> GetSellerProductsAsync(Guid sellerId, string search, string sort, int page, int pageSize, CancellationToken ct);
        void RemoveProductImages(IEnumerable<ProductImage> productImages);
        void AddProductImages(IEnumerable<ProductImage> productImages);

        // Catalog Page
        Task<int> CountCatalogProductsAsync(ProductListQueryParams queryParams, List<Guid>? categoryIds, CancellationToken ct = default);
        Task<List<Product>> GetCatalogProductsAsync(ProductListQueryParams queryParams,int skip, int take, List<Guid>? categoryIds, CancellationToken ct = default);
        Task<List<Brand>> GetCatalogBrandsAsync(List<Guid>? categoryIds, CancellationToken ct = default);

        // Product Rating
        Task<ProductRating?> GetUserRatingAsync(Guid productId, Guid userId);
        void AddRating(ProductRating rating);
        Task<Product?> GetByIdForUpdateAsync(Guid id);
        Task<(int ratingSum, int ratingCount)> UpdateProductRatingAsync(Guid productId);

        // Product Reviews
        Task<int> CountReviewsAsync(Guid productId, CancellationToken ct = default);
        Task<List<ReviewDto>> GetReviewsPageAsync(Guid productId, string? sort, int skip, int take, CancellationToken ct = default);
        Task<ProductReview?> GetUserReviewAsync(Guid productId, Guid userId);
        Task<ReviewDto?> GetUserReviewDtoAsync(Guid productId, Guid userId);
        void AddReview(ProductReview review);
        Task<List<Product>> GetProductsForUpdateAsync(Guid[] productIds);

    }

}
