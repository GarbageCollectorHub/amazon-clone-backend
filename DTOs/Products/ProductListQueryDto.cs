
using System.ComponentModel.DataAnnotations;
using AMZN.Shared.Limits;

namespace AMZN.DTOs.Products
{
    public class ProductListQueryDto : IValidatableObject
    {


        private static readonly string[] AllowedSorts =
        {
            "featured",
            "price_asc",
            "price_desc",
            "rating",
            "rating_desc",
            "newest"
        };


        public Guid? CategoryId { get; set; }
        public List<Guid> BrandIds { get; set; } = new();

        [StringLength(CatalogLimits.SearchMaxLength)]
        public string? Search { get; set; }

        [Range(typeof(decimal), ProductLimits.PriceMin, ProductLimits.PriceMax)]
        public decimal? MaxPrice { get; set; }

        [Range(typeof(decimal), ProductLimits.PriceMin, ProductLimits.PriceMax)]
        public decimal? MinPrice { get; set; }

        [Range(0, ProductFeedbackLimits.RatingMax)]
        public decimal? MinRating { get; set; }

        public string? Sort {  get; set; }

        [Range(1, int.MaxValue)]
        public int Page { get; set; } = 1;

        [Range(1, CatalogLimits.ProductPageSizeMax)]
        public int PageSize { get; set; } = 20;


        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (MinPrice.HasValue && MaxPrice.HasValue && MinPrice.Value > MaxPrice.Value)
                yield return new ValidationResult("MinPrice must be <= MaxPrice", new[] { nameof(MinPrice), nameof(MaxPrice) });

            if (BrandIds.Count > CatalogLimits.BrandFilterCountMax)
                yield return new ValidationResult($"Too many brands chosen (max {CatalogLimits.BrandFilterCountMax})", new[] { nameof(BrandIds) });

            if (!string.IsNullOrWhiteSpace(Sort) && !AllowedSorts.Contains(Sort.Trim(), StringComparer.OrdinalIgnoreCase))
                yield return new ValidationResult("Invalid sort value", new[] { nameof(Sort) });

            if (!string.IsNullOrWhiteSpace(Search) && Search.Trim().Length < CatalogLimits.SearchMinLength)
                yield return new ValidationResult($"Search must contain at least {CatalogLimits.SearchMinLength} characters", new[] { nameof(Search) });
        }


    }
}
