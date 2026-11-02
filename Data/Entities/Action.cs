namespace AMZN.Data.Entities;

public class Action
{
    public Guid Id { get; set; }
    //public Guid ProductId { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public string ProductTitle { get; set; } = null!;
    public int Amount { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public bool IsApplied { get; set; }
    //public Product Product { get; set; } = null!;
}
