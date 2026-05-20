using AMZN.Data.Entities;
using AMZN.DTOs.Home;
using AMZN.DTOs.Products;
using AMZN.Repositories.Home;
using AMZN.Shared.Mapping;

namespace AMZN.Services.Home
{
    public class HomeService
    {
        private const decimal HomeMinRating = 2.5m;
        private const decimal MoreProductsMinRating = HomeMinRating;
        private const decimal UnderTwentyMaxPrice = 20m;

        private const int TopCategoriesCount = 3;                    // Топ 3 больш. карточки
        private const int CategoryBlocksCount = 4;                   // 4 блока с 2х2 продуктс
        private const int CategoryBlockProductsCount = 4;            // 2х2 products

        private const int PopularProductsCount = 3;                 // popular products
        private const int PopularCategoriesCount = 3;               // popular categories
        private const int ProductsUnderTwentyCount = 10;
        private const int MoreProductsCount = 7;

        private readonly IHomeRepository _homeRepository;

        public HomeService(IHomeRepository homeRepository)
        {
            _homeRepository = homeRepository;
        }


        public async Task<HomeResponseDto> GetHomeAsync()
        {
            // Все категории и прямые ProductCount/RatingCount
            List<Category> categories = await _homeRepository.GetAllCategoriesAsync();
            List<CategoryProductCountsDto> categoryProductCounts = await _homeRepository.GetCategoryProductCountsAsync(HomeMinRating);

            // ProductCount/RatingCount по всей ветке категорий
            Dictionary<Guid, List<Category>> subcategoriesByParentId = BuildSubcategoriesByParentId(categories);
            Dictionary<Guid, CategoryProductCountsDto> categoryCountsById = BuildCategoryCountsById(categories, subcategoriesByParentId, categoryProductCounts);

            // Top 3 root categories
            List<HomeCategoryCardDto> topCategories = BuildTopCategories(categories, categoryCountsById);
            List<Guid> topCategoryIds = topCategories.Select(x => x.Id).ToList();

            // Категории для 2x2 блоков и товары для них
            List<Category> blockCategories = BuildBlockCategories(categories, categoryCountsById, topCategoryIds);
            List<HomeCategoryBlockDto> categoryBlocks = await BuildCategoryBlocksAsync(blockCategories, subcategoriesByParentId);

            // Отдельные товарные секции
            List<Product> popularProducts = await _homeRepository.GetTopRatedProductsAsync(HomeMinRating, PopularProductsCount);
            List<Product> productsUnderTwenty = await _homeRepository.GetTopRatedProductsUnderPriceAsync(UnderTwentyMaxPrice, HomeMinRating, ProductsUnderTwentyCount);
            List<Product> moreProducts = await _homeRepository.GetTopRatedProductsAsync(MoreProductsMinRating, MoreProductsCount);

            // Popular categories без top 3
            List<HomeCategoryCardDto> popularCategories = BuildPopularCategories(categories, categoryCountsById, topCategoryIds);

            return new HomeResponseDto
            {
                TopCategories = topCategories,
                CategoryBlocks = categoryBlocks,
                PopularProducts = MapProductCards(popularProducts),
                PopularCategories = popularCategories,
                ProductsUnderTwenty = MapProductCards(productsUnderTwenty),
                MoreProducts = MapProductCards(moreProducts)
            };
        }


        public async Task<List<ProductCardDto>> GetLastViewedAsync(List<Guid> productIds)
        {
            if (productIds.Count == 0)
                return new List<ProductCardDto>();

            List<Product> products = await _homeRepository.GetProductsByIdsAsync(productIds);
            Dictionary<Guid, Product> productsById = products.ToDictionary(x => x.Id);
            List<ProductCardDto> result = new List<ProductCardDto>(productIds.Count);

            foreach (Guid productId in productIds)
            {
                if (productsById.TryGetValue(productId, out Product? product))
                    result.Add(product.ToCardDto());
            }

            return result;
        }

        // 2x2 блоки, категории + продукты
        private async Task<List<HomeCategoryBlockDto>> BuildCategoryBlocksAsync(List<Category> blockCategories, Dictionary<Guid, List<Category>> subcategoriesByParentId)
        {
            List<HomeCategoryBlockDto> result = new List<HomeCategoryBlockDto>();

            foreach (Category blockCategory in blockCategories)
            {
                List<Guid> categoryTreeIds = GetCategoryTreeIds(blockCategory.Id, subcategoriesByParentId);
                List<Product> blockProducts = await _homeRepository.GetTopRatedProductsForCategoriesAsync(categoryTreeIds, HomeMinRating, CategoryBlockProductsCount);

                if (blockProducts.Count == 0)
                    continue;

                result.Add(new HomeCategoryBlockDto
                {
                    CategoryId = blockCategory.Id,
                    CategoryName = blockCategory.Name,
                    Products = MapProductCards(blockProducts)
                });
            }

            return result;
        }


        // ParentId -> список прямых подкатегорий
        private static Dictionary<Guid, List<Category>> BuildSubcategoriesByParentId(List<Category> categories)
        {
            Dictionary<Guid, List<Category>> result = new Dictionary<Guid, List<Category>>();

            foreach (Category category in categories)
            {
                if (category.ParentId == null)
                    continue;

                Guid parentId = category.ParentId.Value;

                if (!result.TryGetValue(parentId, out List<Category>? subcategories))
                {
                    subcategories = new List<Category>();
                    result[parentId] = subcategories;
                }

                subcategories.Add(category);
            }

            return result;
        }

        // Считаем counts по всей ветке:
        // сама категория + все потомки
        private static Dictionary<Guid, CategoryProductCountsDto> BuildCategoryCountsById(
            List<Category> categories,
            Dictionary<Guid, List<Category>> subcategoriesByParentId,
            List<CategoryProductCountsDto> directCategoryCounts
            )
        {
            Dictionary<Guid, CategoryProductCountsDto> directCountsByCategoryId = directCategoryCounts.ToDictionary(x => x.CategoryId, x => x);

            Dictionary<Guid, CategoryProductCountsDto> result = new Dictionary<Guid, CategoryProductCountsDto>();

            foreach (Category category in categories)
            {
                if (result.ContainsKey(category.Id))
                    continue;

                CalculateCategoryCounts(category.Id, subcategoriesByParentId, directCountsByCategoryId, result);
            }

            return result;
        }


        private static CategoryProductCountsDto CalculateCategoryCounts(
            Guid categoryId,
            Dictionary<Guid, List<Category>> subcategoriesByParentId,
            Dictionary<Guid, CategoryProductCountsDto> directCountsByCategoryId,
            Dictionary<Guid, CategoryProductCountsDto> result
            )
        {
            if (result.TryGetValue(categoryId, out CategoryProductCountsDto? categoryCounts))
                return categoryCounts;

            int productCount = 0;
            int ratingCount = 0;

            if (directCountsByCategoryId.TryGetValue(categoryId, out CategoryProductCountsDto? directCategoryCounts))
            {
                productCount += directCategoryCounts.ProductCount;
                ratingCount += directCategoryCounts.RatingCount;
            }

            if (subcategoriesByParentId.TryGetValue(categoryId, out List<Category>? subcategories))
            {
                foreach (Category subcategory in subcategories)
                {
                    CategoryProductCountsDto subcategoryCounts = CalculateCategoryCounts(
                        subcategory.Id,
                        subcategoriesByParentId,
                        directCountsByCategoryId,
                        result);

                    productCount += subcategoryCounts.ProductCount;
                    ratingCount += subcategoryCounts.RatingCount;
                }
            }

            categoryCounts = new CategoryProductCountsDto
            {
                CategoryId = categoryId,
                ProductCount = productCount,
                RatingCount = ratingCount
            };

            result[categoryId] = categoryCounts;
            return categoryCounts;
        }


        private static CategoryProductCountsDto GetCategoryCounts(Guid categoryId, Dictionary<Guid, CategoryProductCountsDto> categoryCountsById)
        {
            if (categoryCountsById.TryGetValue(categoryId, out CategoryProductCountsDto? categoryCounts))
                return categoryCounts;

            return new CategoryProductCountsDto
            {
                CategoryId = categoryId,
                ProductCount = 0,
                RatingCount = 0
            };
        }


        // Топ 3 большие карточки
        private static List<HomeCategoryCardDto> BuildTopCategories(List<Category> categories, Dictionary<Guid, CategoryProductCountsDto> categoryCountsById)
        {
            return categories
                .Where(x => x.ParentId == null)
                .Select(category => new
                {
                    Category = category,
                    CategoryCounts = GetCategoryCounts(category.Id, categoryCountsById)
                })
                .Where(x => x.CategoryCounts.ProductCount > 0)
                .OrderByDescending(x => x.CategoryCounts.ProductCount)
                .ThenByDescending(x => x.CategoryCounts.RatingCount)
                .ThenBy(x => x.Category.Name)
                .Take(TopCategoriesCount)
                .Select(x => new HomeCategoryCardDto
                {
                    Id = x.Category.Id,
                    Name = x.Category.Name,
                    ImageUrl = x.Category.ImageUrl
                })
                .ToList();
        }


        // 4 категории для 2x2 блоков, кроме top 3
        private static List<Category> BuildBlockCategories(
            List<Category> categories,
            Dictionary<Guid, CategoryProductCountsDto> categoryCountsById,
            List<Guid> topCategoryIds
            )
        {
            return categories
                .Where(x => !topCategoryIds.Contains(x.Id))
                .Select(category => new
                {
                    Category = category,
                    CategoryCounts = GetCategoryCounts(category.Id, categoryCountsById)
                })
                .Where(x => x.CategoryCounts.ProductCount > 0)
                .OrderByDescending(x => x.CategoryCounts.ProductCount)
                .ThenByDescending(x => x.CategoryCounts.RatingCount)
                .ThenBy(x => x.Category.Name)
                .Take(CategoryBlocksCount)
                .Select(x => x.Category)
                .ToList();
        }

        // Popular categories = top категории, но без top 3 больших карточек
        private static List<HomeCategoryCardDto> BuildPopularCategories(
            List<Category> categories,
            Dictionary<Guid, CategoryProductCountsDto> categoryCountsById,
            List<Guid> topCategoryIds
            )
        {
            return categories
                .Where(x => !topCategoryIds.Contains(x.Id))
                .Select(category => new
                {
                    Category = category,
                    CategoryCounts = GetCategoryCounts(category.Id, categoryCountsById)
                })
                .Where(x => x.CategoryCounts.ProductCount > 0)
                .OrderByDescending(x => x.CategoryCounts.ProductCount)
                .ThenByDescending(x => x.CategoryCounts.RatingCount)
                .ThenBy(x => x.Category.Name)
                .Take(PopularCategoriesCount)
                .Select(x => new HomeCategoryCardDto
                {
                    Id = x.Category.Id,
                    Name = x.Category.Name,
                    ImageUrl = x.Category.ImageUrl
                })
                .ToList();
        }


        // Все id категории + все id потомков
        private static List<Guid> GetCategoryTreeIds(Guid categoryId, Dictionary<Guid, List<Category>> subcategoriesByParentId)
        {
            List<Guid> result = new List<Guid>();

            CollectCategoryTreeIds(categoryId, subcategoriesByParentId, result);

            return result;
        }

        private static void CollectCategoryTreeIds(
            Guid categoryId,
            Dictionary<Guid, List<Category>> subcategoriesByParentId,
            List<Guid> result
            )
        {
            result.Add(categoryId);

            if (!subcategoriesByParentId.TryGetValue(categoryId, out List<Category>? subcategories))
                return;

            foreach (Category subcategory in subcategories)
            {
                CollectCategoryTreeIds(subcategory.Id, subcategoriesByParentId, result);
            }               
        }

        private static List<ProductCardDto> MapProductCards(IEnumerable<Product> products)
        {
            return products.Select(x => x.ToCardDto()).ToList();
        }

    }
}