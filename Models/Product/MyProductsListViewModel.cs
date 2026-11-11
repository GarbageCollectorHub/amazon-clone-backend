namespace AMZN.Models.Product
{
    public class MyProductsListViewModel
    {
        public List<MyProductsListItemViewModel> Products { get; set; } = new();
        public string Search { get; set; } = "";
        public string Sort { get; set; } = "newest";
        public int Page { get; set; }
        public int TotalPages { get; set; }
        public int TotalItems { get; set; }
    }
}
