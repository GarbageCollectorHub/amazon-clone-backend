using AMZN.Shared.Validation.Attributes;
using AMZN.Shared.Limits;
using AMZN.Shared.Validation.Files;
using System.ComponentModel.DataAnnotations;

namespace AMZN.Models.Product
{
    public class ProductEditFormModel : IValidatableObject
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


        [Range(typeof(decimal), ProductLimits.PriceMin, ProductLimits.PriceMax, ErrorMessage = "CurrentPrice must be between {1} and {2}")]
        public decimal CurrentPrice { get; set; }


        [Range(typeof(decimal), ProductLimits.PriceMin, ProductLimits.PriceMax, ErrorMessage = "OriginalPrice must be between {1} and {2}")]
        public decimal? OriginalPrice { get; set; }


        [ImageFile(ImageUploadLimits.MaxFileBytes, ErrorMessage = "Primary image is invalid")]
        public IFormFile? NewPrimaryImage { get; set; }

        // Новые gallery images, Добавляются после существующих
        public List<IFormFile>? NewGalleryImages { get; set; }

        // Порядок существующих gallery images после delete / reorder
        public List<Guid> ExistingGalleryImageIdsInOrder { get; set; } = new();


        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (OriginalPrice is > 0 && OriginalPrice < CurrentPrice)
            {
                yield return new ValidationResult(
                    "OriginalPrice must be >= CurrentPrice",
                    new[] { nameof(OriginalPrice), nameof(CurrentPrice) });
            }

            if (NewGalleryImages == null || NewGalleryImages.Count == 0)
                yield break;

            if (NewGalleryImages.Count > ImageUploadLimits.MaxGalleryImages)
            {
                yield return new ValidationResult(
                    $"Too many gallery images (max {ImageUploadLimits.MaxGalleryImages})",
                    new[] { nameof(NewGalleryImages) });

                yield break;
            }

            foreach (var file in NewGalleryImages)
            {
                if (file == null)
                {
                    yield return new ValidationResult("Invalid file", new[] { nameof(NewGalleryImages) });
                    continue;
                }

                if (!ImageFileRules.TryValidate(file, ImageUploadLimits.MaxFileBytes, out var error))
                    yield return new ValidationResult(error, new[] { nameof(NewGalleryImages) });
            }
        }
    }
}
