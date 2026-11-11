using System.ComponentModel.DataAnnotations;
using AMZN.Shared.Limits;

namespace AMZN.DTOs.Products.Questions
{
    public class ProductQuestionRequestDto
    {
        [Required(ErrorMessage = "Text is required")]
        [StringLength(ProductFeedbackLimits.QuestionTextMaxLength, MinimumLength = ProductFeedbackLimits.QuestionOrAnswerTextMinLength, ErrorMessage = "Text must be between {2} and {1} characters")]
        public string Text { get; set; } = null!;
    }
}
