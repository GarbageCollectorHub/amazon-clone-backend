using System.ComponentModel.DataAnnotations;

namespace AMZN.Models.Promotions;

public class PromotionFormModel : IValidatableObject
{
    [Required(ErrorMessage = "Name is required")]
    [StringLength(20, MinimumLength = 2, ErrorMessage = "Name must be between 2 and 20 characters")]
    public string Name { get; set; } = null!;
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Description must be between 2 and 100 characters")]
    public string? Description { get; set; } = null!;
    [Required(ErrorMessage = "Application type is required")]
    [RegularExpression("^(product|category)$", ErrorMessage = "Application type is not valid")]
    public string ApplyTo { get; set; } = null!;
    [Required(ErrorMessage = "Product / Category name is required")]
    public string ProductTitle { get; set; } = null!;
    [Range(1, 99, ErrorMessage = "Discount must be between 1 and 99")]
    [Required(ErrorMessage = "Discount is required")]
    [Display(Name = "Discount (%)")]
    public int? DiscountPercent { get; set; }

    [Required(ErrorMessage = "Start date is required")]
    [Display(Name = "Start date")]
    public DateTime? StartDate { get; set; }

    [Required(ErrorMessage = "End date is required")]
    [Display(Name = "End date")]
    public DateTime? EndDate { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (StartDate == DateTime.MinValue)
            yield return new ValidationResult("Start date is required", new[] { nameof(StartDate) });

        if (EndDate == DateTime.MinValue)
            yield return new ValidationResult("End date is required", new[] { nameof(EndDate) });
        else if (StartDate >= EndDate)
            yield return new ValidationResult("End date must be after start date", new[] { nameof(EndDate) });
        else if (EndDate <= DateTime.UtcNow)
            yield return new ValidationResult("End date must be in the future", new[] { nameof(EndDate) });
    }
}
