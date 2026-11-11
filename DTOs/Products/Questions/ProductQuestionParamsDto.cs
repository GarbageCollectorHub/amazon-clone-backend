using System.ComponentModel.DataAnnotations;
using AMZN.Shared.Limits;

namespace AMZN.DTOs.Products.Questions
{
    public class ProductQuestionParamsDto
    {

        [Range(1, int.MaxValue)]
        public int Page { get; set; } = 1;

        [Range(1, ProductFeedbackLimits.QuestionOrAnswerPageSizeMax)]
        public int PageSize { get; set; } = 10;
    }
}
