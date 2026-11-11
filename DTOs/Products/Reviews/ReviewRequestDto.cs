using System.ComponentModel.DataAnnotations;
using AMZN.Shared.Limits;
using AMZN.Shared.Validation;

namespace AMZN.DTOs.Products.Reviews
{
    public class ReviewRequestDto
    {
        [Range(ProductFeedbackLimits.RatingMin, ProductFeedbackLimits.RatingMax, ErrorMessage = ValidationMessages.RatingRange)]
        public byte Rating { get; set; }

        [Required(ErrorMessage = "Title is required")]
        [MaxLength(ProductFeedbackLimits.ReviewTitleMaxLength, ErrorMessage = "Title is too long")]
        public string Title { get; set; } = null!;

        [Required(ErrorMessage = "Text is required")]
        [MaxLength(ProductFeedbackLimits.ReviewTextMaxLength, ErrorMessage = "Text is too long")]
        public string Text { get; set; } = null!;
    }
}
