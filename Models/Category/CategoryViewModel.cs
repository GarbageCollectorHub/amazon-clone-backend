namespace AMZN.Models.Category;

public class CategoryViewModel
{
    public CategoryFormModel Form { get; set; } = new();
    public List<Data.Entities.Category> Categories { get; set; } = new();
}
