using System.ComponentModel.DataAnnotations;
using AMZN.Shared.Limits;
using AMZN.Shared.Validation;

namespace AMZN.DTOs.Products
{
    public class SetProductRatingRequestDto
    {
        [Range(ProductFeedbackLimits.RatingMin, ProductFeedbackLimits.RatingMax, ErrorMessage = ValidationMessages.RatingRange)]
        public byte Rating { get; set; }
    }
}
