using AMZN.DTOs.Common;
using AMZN.DTOs.Products.Questions;
using AMZN.Extensions;
using AMZN.Services.Products.Questions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AMZN.Controllers.Api
{
    [Route("api/products/{productId:guid}/questions")]
    [ApiController]
    public class ProductQuestionsController : ControllerBase
    {
        private readonly ProductQuestionService _productQuestionService;

        public ProductQuestionsController(ProductQuestionService productQuestionService)
        {
            _productQuestionService = productQuestionService;
        }


        [HttpGet]
        [AllowAnonymous]
        public async Task<ActionResult<PagedResult<ProductQuestionDto>>> GetQuestions([FromRoute]Guid productId, [FromQuery]ProductQuestionParamsDto q, CancellationToken ct)
        {
            var dto = await _productQuestionService.GetQuestionsAsync(productId, q, ct);
            return Ok(dto);
        }


        [HttpPost]
        [Authorize]
        public async Task<ActionResult<ProductQuestionDto>> CreateQuestion([FromRoute]Guid productId, [FromBody]ProductQuestionRequestDto request)
        {
            var userId = User.GetRequiredUserId();

            var dto = await _productQuestionService.CreateQuestionAsync(productId, userId, request);
            return StatusCode(StatusCodes.Status201Created, dto);
        }


        [HttpGet("{questionId:guid}/answers")]
        [AllowAnonymous]
        public async Task<ActionResult<PagedResult<ProductQuestionAnswerDto>>> GetAnswers(
            [FromRoute] Guid productId,
            [FromRoute] Guid questionId,
            [FromQuery] ProductQuestionAnswerParamsDto q,
            CancellationToken ct
            )
        {
            var dto = await _productQuestionService.GetAnswersAsync(productId, questionId, q, ct);
            return Ok(dto);
        }


        [HttpPost("{questionId:guid}/answers")]
        [Authorize]
        public async Task<ActionResult<ProductQuestionAnswerDto>> CreateAnswer(
            [FromRoute] Guid productId,
            [FromRoute] Guid questionId,
            [FromBody] ProductQuestionAnswerRequestDto request
            )
        {
            var userId = User.GetRequiredUserId();

            var dto = await _productQuestionService.CreateAnswerAsync(productId, questionId, userId, request);
            return StatusCode(StatusCodes.Status201Created, dto);
        }

    }
}
