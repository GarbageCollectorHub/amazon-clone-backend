using AMZN.Data.Entities;
using AMZN.DTOs.Brands;
using AMZN.DTOs.Common;
using AMZN.DTOs.Products;
using AMZN.DTOs.Products.Reviews;
using AMZN.Repositories.Categories;
using AMZN.Repositories.Products;
using AMZN.Repositories.Products.Queries;
using AMZN.Shared.Exceptions;
using AMZN.Shared.Exceptions.Errors;
using AMZN.Shared.Helpers.Search;
using AMZN.Shared.Mapping;
using AMZN.Shared.Transactions;

namespace AMZN.Services.Products
{
    public class ProductService
    {
        private readonly IProductRepository _productRepository;
        private readonly ICategoryRepository _categoryRepository;
        private readonly ITransactionManager _transactionManager;


        public ProductService(IProductRepository productRepository, ICategoryRepository categoryRepository, ITransactionManager transactionManager)
        {
            _productRepository = productRepository;
            _categoryRepository = categoryRepository;
            _transactionManager = transactionManager;
        }


        public async Task<ProductDetailsDto> GetByIdAsync(Guid id)
        {
            Product? product = await _productRepository.GetDetailsByIdAsync(id);

            if (product == null)
                throw new ApiException(ErrorCodes.ProductNotFound, "Product not found", StatusCodes.Status404NotFound);

            return product.ToDetailsDto();
        }

        public async Task<PagedResult<ProductCardDto>> GetCatalogPageAsync(ProductListQueryDto q)
        {
            int skip = (q.Page - 1) * q.PageSize;
            string? normalizedSearch = SearchQueryHelper.NormalizeQuery(q.Search);

            ProductListQueryParams queryParams = new ProductListQueryParams
            {
                CategoryId = q.CategoryId,
                BrandIds = q.BrandIds.ToList(),
                Search = string.IsNullOrEmpty(normalizedSearch) ? null : normalizedSearch,
                MinPrice = q.MinPrice,
                MaxPrice = q.MaxPrice,
                MinRating = q.MinRating,
                Sort = q.Sort
            };

            List<Guid>? categoryIds = null;

            if (q.CategoryId != null)
                categoryIds = await _categoryRepository.GetCategoryTreeIdsAsync(q.CategoryId.Value);

            int total = await _productRepository.CountCatalogProductsAsync(queryParams, categoryIds);
            List<Product> items = await _productRepository.GetCatalogProductsAsync(queryParams, skip, q.PageSize, categoryIds);

            return new PagedResult<ProductCardDto>
            {
                Items = items.Select(x => x.ToCardDto()).ToList(),
                Page = q.Page,
                PageSize = q.PageSize,
                TotalItems = total
            };
        }

        public async Task<List<BrandDto>> GetCatalogBrandsAsync(Guid? categoryId)
        {
            List<Guid>? categoryIds = null;

            if (categoryId != null)
                categoryIds = await _categoryRepository.GetCategoryTreeIdsAsync(categoryId.Value);

            List<Brand> brands = await _productRepository.GetCatalogBrandsAsync(categoryIds);

            return brands.Select(b => b.ToBrandDto()).ToList();
        }

        //  Rating
        public Task<ProductRatingResponseDto> SetRatingAsync(Guid productId, Guid userId, byte rating)
        {

            return _transactionManager.ExecuteAsync(async () =>
            {
                Product product = await GetExistingProductForUpdateAsync(productId);

                if (product.SellerId == userId)
                    throw new ApiException(ErrorCodes.CannotRateOwnProduct, "You cannot rate your own product", StatusCodes.Status403Forbidden);

                bool ratingChanged = await SetUserRatingAsync(productId, userId, rating);

                int ratingSum = product.RatingSum;
                int ratingCount = product.RatingCount;

                if (ratingChanged)
                {
                    await _productRepository.SaveChangesAsync();
                    (ratingSum, ratingCount) = await _productRepository.UpdateProductRatingAsync(productId);
                }

                return new ProductRatingResponseDto
                {
                    AverageRating = ProductMapper.CalcRatingHalfStep(ratingSum, ratingCount),
                    RatingsCount = ratingCount,
                    UserRating = rating
                };
            });
        }

        // Review
        public async Task<PagedResult<ReviewDto>> GetReviewsAsync(Guid productId, ReviewParamsDto q)
        {
            bool exists = await _productRepository.ExistsAsync(productId);
            if (!exists)
                throw new ApiException(ErrorCodes.ProductNotFound, "Product not found", StatusCodes.Status404NotFound);

            int skip = (q.Page - 1) * q.PageSize;

            int total = await _productRepository.CountReviewsAsync(productId);
            List<ReviewDto> items = await _productRepository.GetReviewsPageAsync(productId, q.Sort, skip, q.PageSize);

            return new PagedResult<ReviewDto>
            {
                Items = items,
                Page = q.Page,
                PageSize = q.PageSize,
                TotalItems = total
            };
        }

        public Task<ReviewDto> CreateOrUpdateReviewAsync(Guid productId, Guid userId, ReviewRequestDto request)
        {
            return _transactionManager.ExecuteAsync(async () =>
            {
                Product product = await GetExistingProductForUpdateAsync(productId);

                if (product.SellerId == userId)
                    throw new ApiException(ErrorCodes.CannotReviewOwnProduct, "You cannot review your own product", StatusCodes.Status403Forbidden);

                string normalizedTitle = request.Title.Trim();
                string normalizedText = request.Text.Trim();

                ProductReview? existingReview = await _productRepository.GetUserReviewAsync(productId, userId);

                bool reviewChanged = false;

                if (existingReview == null)
                {
                    var review = new ProductReview
                    {
                        Id = Guid.NewGuid(),
                        ProductId = productId,
                        UserId = userId,
                        Title = normalizedTitle,
                        Text = normalizedText,
                        CreatedAt = DateTime.UtcNow
                    };

                    _productRepository.AddReview(review);
                    reviewChanged = true;
                }
                else if (existingReview.Title != normalizedTitle || existingReview.Text != normalizedText)
                {
                    existingReview.Title = normalizedTitle;
                    existingReview.Text = normalizedText;
                    existingReview.UpdatedAt = DateTime.UtcNow;
                    reviewChanged = true;
                }

                bool ratingChanged = await SetUserRatingAsync(productId, userId, request.Rating);

                if (reviewChanged || ratingChanged)
                    await _productRepository.SaveChangesAsync();

                if (ratingChanged)
                    await _productRepository.UpdateProductRatingAsync(productId);

                ReviewDto? dto = await _productRepository.GetUserReviewDtoAsync(productId, userId);

                if (dto == null)
                    throw new InvalidOperationException("Review was not saved correctly");

                return dto;
            });
        }


        // Helpers
        private async Task<Product> GetExistingProductForUpdateAsync(Guid productId)
        {
            Product? product = await _productRepository.GetByIdForUpdateAsync(productId);

            if (product == null)
                throw new ApiException(ErrorCodes.ProductNotFound, "Product not found", StatusCodes.Status404NotFound);

            return product;
        }

        private async Task<bool> SetUserRatingAsync(Guid productId, Guid userId, byte rating)
        {
            ProductRating? existingRating = await _productRepository.GetUserRatingAsync(productId, userId);

            if (existingRating == null)
            {
                var newRating = new ProductRating
                {
                    Id = Guid.NewGuid(),
                    ProductId = productId,
                    UserId = userId,
                    Value = rating
                };

                _productRepository.AddRating(newRating);
                return true;
            }

            if (existingRating.Value == rating)
                return false;

            existingRating.Value = rating;
            return true;
        }


    }
}
