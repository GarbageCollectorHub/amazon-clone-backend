using System.ComponentModel.DataAnnotations;
using AMZN.Shared.Validation.Attributes;

namespace AMZN.Models.Category;

public class CategoryFormModel
{
    [Required(ErrorMessage = "Name is required")]
    [StringLength(20, MinimumLength = 2, ErrorMessage = "Name must be between 2 and 20 characters")]
    public string Name { get; set; } = null!;
    public string? ParentCategory { get; set; }
    [Required(ErrorMessage = "Description is required")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Description must be between 2 and 100 characters")]
    public string Description { get; set; } = null!;
    [RequiredFile(ErrorMessage = "Image is required")]
    [ImageFile(5 * 1024 * 1024)]
    public IFormFile? Image { get; set; }
}
