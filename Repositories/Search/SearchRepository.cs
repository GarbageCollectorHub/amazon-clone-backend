using AMZN.Data;
using AMZN.Data.Entities;
using AMZN.Shared.Helpers.Search;
using Microsoft.EntityFrameworkCore;

namespace AMZN.Repositories.Search
{
    public class SearchRepository : ISearchRepository
    {
        private readonly DataContext _db;

        public SearchRepository(DataContext db)
        {
            _db = db;
        }

        public Task<List<Product>> GetProductSuggestionsAsync(string query, int take, CancellationToken ct = default)
        {
            var tokens = SearchQueryHelper.SplitTokens(query);

            IQueryable<Product> productsQuery = _db.Products
                .AsNoTracking()
                .Include(p => p.Brand)
                .Include(p => p.Category);

            foreach (var token in tokens)
            {
                productsQuery = productsQuery.Where(p =>
                    p.Title.Contains(token) ||
                    p.Brand.Name.Contains(token) ||
                    p.Category.Name.Contains(token));
            }

            return productsQuery
                .OrderByDescending(p => p.Title == query)
                .ThenByDescending(p => p.Title.StartsWith(query))
                .ThenByDescending(p => p.Brand.Name == query)
                .ThenByDescending(p => p.Brand.Name.StartsWith(query))
                .ThenByDescending(p => p.Category.Name == query)
                .ThenByDescending(p => p.Category.Name.StartsWith(query))
                .ThenByDescending(p => p.RatingCount)
                .ThenByDescending(p => p.CreatedAt)
                .ThenBy(p => p.Title)
                .Take(take)
                .ToListAsync(ct);
        }

        public Task<List<Category>> GetCategorySuggestionsAsync(string query, int take, CancellationToken ct = default)
        {
            var tokens = SearchQueryHelper.SplitTokens(query);

            IQueryable<Category> categoriesQuery = _db.Categories.AsNoTracking();

            foreach (var token in tokens)
            {
                categoriesQuery = categoriesQuery.Where(c => c.Name.Contains(token));
            }

            return categoriesQuery
                .OrderByDescending(c => c.Name == query)
                .ThenByDescending(c => c.Name.StartsWith(query))
                .ThenBy(c => c.Name)
                .Take(take)
                .ToListAsync(ct);
        }

        public Task<List<Brand>> GetBrandSuggestionsAsync(string query, int take, CancellationToken ct = default)
        {
            var tokens = SearchQueryHelper.SplitTokens(query);

            IQueryable<Brand> brandsQuery = _db.Brands.AsNoTracking();

            foreach (var token in tokens)
            {
                brandsQuery = brandsQuery.Where(b => b.Name.Contains(token));
            }

            return brandsQuery
                .OrderByDescending(b => b.Name == query)
                .ThenByDescending(b => b.Name.StartsWith(query))
                .ThenBy(b => b.Name)
                .Take(take)
                .ToListAsync(ct);
        }
    }
}
