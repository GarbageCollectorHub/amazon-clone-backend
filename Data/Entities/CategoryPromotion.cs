namespace AMZN.Data.Entities;

public class CategoryPromotion
{
    public Guid Id { get; set; }
    public Guid CategoryId { get; set; }
    public Guid PromotionId { get; set; }
    public Category Category { get; set; } = null!;
    public Promotion Promotion { get; set; } = null!;
}