using Microsoft.AspNetCore.Mvc;

namespace AMZN.Models.Promotions;

public class PromotionFormModel
{
    [FromForm(Name = "promotion-name")]
    public string Name { get; set; } = null!;
    [FromForm(Name = "promotion-description")]
    public string? Description { get; set; } = null!;
    [FromForm(Name = "apply-to")]
    public string ApplyTo { get; set; } = null!;
    [FromForm(Name = "product-title")]
    public string ProductTitle { get; set; } = null!;
    [FromForm(Name = "promotion-discount-percent")]
    public int DiscountPercent { get; set; }
    [FromForm(Name = "promotion-start-date")]
    public DateTime StartDate { get; set; }
    [FromForm(Name = "promotion-end-date")]
    public DateTime EndDate { get; set; }
}
