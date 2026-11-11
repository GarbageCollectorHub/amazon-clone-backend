using AMZN.DTOs.Common;
using AMZN.DTOs.Search;
using AMZN.Repositories.Search;
using AMZN.Shared.Helpers.Search;
using AMZN.Shared.Limits;
using AMZN.Shared.Mapping;

namespace AMZN.Services.Search
{
    public class SearchService
    {
        private const int ProductSuggestionsTake = 6;
        private const int CategorySuggestionsTake = 4;
        private const int BrandSuggestionsTake = 4;

        private readonly ISearchRepository _searchRepository;

        public SearchService(ISearchRepository searchRepository)
        {
            _searchRepository = searchRepository;
        }

        public async Task<SearchSuggestionsResponseDto> GetSuggestionsAsync(string? query, CancellationToken ct)
        {
            var normalizedQuery = SearchQueryHelper.NormalizeQuery(query);

            if (normalizedQuery.Length < CatalogLimits.SearchMinLength)
                return new SearchSuggestionsResponseDto();

            var products = await _searchRepository.GetProductSuggestionsAsync(normalizedQuery, ProductSuggestionsTake, ct);
            var categories = await _searchRepository.GetCategorySuggestionsAsync(normalizedQuery, CategorySuggestionsTake, ct);
            var brands = await _searchRepository.GetBrandSuggestionsAsync(normalizedQuery, BrandSuggestionsTake, ct);

            return new SearchSuggestionsResponseDto
            {
                Products = products.Select(p => p.ToSearchSuggestionDto()).ToList(),
                Categories = categories.Select(c => new CategoryDto
                {
                    Id = c.Id,
                    Name = c.Name
                }).ToList(),
                Brands = brands.Select(b => b.ToBrandDto()).ToList()
            };
        }
    }
}
