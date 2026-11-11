using System.ComponentModel.DataAnnotations;
using AMZN.Shared.Limits;
using AMZN.Shared.Validation.Attributes;
using AMZN.Shared.Validation.Files;

namespace AMZN.Models.Product
{
    public class ProductCreateFormModel : IValidatableObject
    {

        [Required(ErrorMessage = "Title is required")]
        [StringLength(ProductLimits.TitleMaxLength, ErrorMessage = "Title is too long")]
        public string Title { get; set; } = null!;


        [StringLength(ProductLimits.DescriptionMaxLength, ErrorMessage = "Description is too long")]
        public string? Description { get; set; }

        [Range(ProductLimits.StockMin, int.MaxValue, ErrorMessage = "StockQuantity must be >= {1}")]
        public int StockQuantity { get; set; }

        [Required(ErrorMessage = "Brand is required")]
        public Guid? BrandId { get; set; }

        [Required(ErrorMessage = "Category is required")]
        public Guid? CategoryId { get; set; }


        [Range(typeof(decimal), ProductLimits.CreatePriceMin, ProductLimits.PriceMax, ErrorMessage = "CurrentPrice must be between {1} and {2}")]
        public decimal CurrentPrice { get; set; }


        [Range(typeof(decimal), ProductLimits.CreatePriceMin, ProductLimits.PriceMax, ErrorMessage = "OriginalPrice must be between {1} and {2}")]
        public decimal? OriginalPrice { get; set; }


        [RequiredFile(ErrorMessage = "Primary image is required")]
        [ImageFile(ImageUploadLimits.MaxFileBytes, ErrorMessage = "Primary image is invalid")]
        public IFormFile? PrimaryImage { get; set; }


        public List<IFormFile>? Images { get; set; }



        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            // OriginalPrice >= CurrentPrice
            if (OriginalPrice is > 0 && OriginalPrice < CurrentPrice)
            {
                yield return new ValidationResult(
                    "OriginalPrice must be >= CurrentPrice",
                    new[] { nameof(OriginalPrice), nameof(CurrentPrice) }
                );
            }

            if (Images == null || Images.Count == 0)
                yield break;

            if (Images.Count > ImageUploadLimits.MaxGalleryImages)
            {
                yield return new ValidationResult($"Too many gallery images (max {ImageUploadLimits.MaxGalleryImages})", new[] { nameof(Images) });
                yield break;
            }

            // Проверка файлов галереи
            foreach (var file in Images)
            {
                if (file == null)
                {
                    yield return new ValidationResult("Invalid file", new[] { nameof(Images) });
                    continue;
                }

                if (!ImageFileRules.TryValidate(file, ImageUploadLimits.MaxFileBytes, out var error))
                    yield return new ValidationResult(error, new[] { nameof(Images) });
            }


        }

    }
}
