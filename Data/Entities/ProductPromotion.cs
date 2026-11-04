namespace AMZN.Data.Entities;

public class ProductPromotion
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public Guid PromotionId { get; set; }
    
    public Product Product { get; set; } = null!;
    public Promotion Promotion { get; set; } = null!;
}