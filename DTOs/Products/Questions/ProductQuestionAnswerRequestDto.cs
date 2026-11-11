using System.ComponentModel.DataAnnotations;
using AMZN.Shared.Limits;

namespace AMZN.DTOs.Products.Questions
{
    public class ProductQuestionAnswerRequestDto
    {
        [Required(ErrorMessage = "Text is required")]
        [StringLength(ProductFeedbackLimits.AnswerTextMaxLength, MinimumLength = ProductFeedbackLimits.QuestionOrAnswerTextMinLength, ErrorMessage = "Text must be between {2} and {1} characters")]
        public string Text { get; set; } = null!;
    }
}
