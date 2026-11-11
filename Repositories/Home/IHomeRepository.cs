using AMZN.Data.Entities;
using AMZN.DTOs.Home;

namespace AMZN.Repositories.Home
{
    public interface IHomeRepository
    {
        Task<List<Category>> GetAllCategoriesAsync(CancellationToken ct = default);
        Task<List<CategoryProductCountsDto>> GetCategoryProductCountsAsync(decimal minRating, CancellationToken ct = default);

        Task<List<Product>> GetTopRatedProductsForCategoriesAsync(List<Guid> categoryIds, decimal minRating, int take, CancellationToken ct = default);

        Task<List<Product>> GetTopRatedProductsAsync(decimal minRating, int take, CancellationToken ct = default);

        Task<List<Product>> GetTopRatedProductsUnderPriceAsync(decimal maxPrice, decimal minRating, int take, CancellationToken ct = default);

        Task<List<Product>> GetProductsByIdsAsync(List<Guid> productIds, CancellationToken ct = default);
    }
}
