using System.ComponentModel.DataAnnotations;
using AMZN.Shared.Limits;

namespace AMZN.Models.Brands
{
    public class BrandCreateViewModel
    {


        [Required(ErrorMessage = "Name is required")]
        [StringLength(CatalogLimits.BrandNameFormMaxLength, ErrorMessage = "Name is too long")]
        public string Name { get; set; } = "";

    }
}
