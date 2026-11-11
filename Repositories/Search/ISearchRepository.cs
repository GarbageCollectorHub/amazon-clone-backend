using AMZN.Data.Entities;

namespace AMZN.Repositories.Search
{
    public interface ISearchRepository
    {
        Task<List<Product>> GetProductSuggestionsAsync(string query, int take, CancellationToken ct = default);
        Task<List<Category>> GetCategorySuggestionsAsync(string query, int take, CancellationToken ct = default);
        Task<List<Brand>> GetBrandSuggestionsAsync(string query, int take, CancellationToken ct = default);
    }
}
