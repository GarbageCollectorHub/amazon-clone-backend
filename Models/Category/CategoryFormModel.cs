using System.ComponentModel.DataAnnotations;
using AMZN.Shared.Limits;
using AMZN.Shared.Validation.Attributes;

namespace AMZN.Models.Category;

public class CategoryFormModel
{
    [Required(ErrorMessage = "Name is required")]
    [StringLength(CatalogLimits.CategoryNameMaxLength, MinimumLength = CatalogLimits.CategoryNameMinLength, ErrorMessage = "Name must be between {2} and {1} characters")]
    public string Name { get; set; } = null!;
    public string? ParentCategory { get; set; }
    [Required(ErrorMessage = "Description is required")]
    [StringLength(CatalogLimits.CategoryDescriptionMaxLength, MinimumLength = CatalogLimits.CategoryDescriptionMinLength, ErrorMessage = "Description must be between {2} and {1} characters")]
    public string Description { get; set; } = null!;
    [RequiredFile(ErrorMessage = "Image is required")]
    [ImageFile(ImageUploadLimits.MaxFileBytes)]
    public IFormFile? Image { get; set; }
}
