using AMZN.Data.Entities;
using AMZN.DTOs.Common;
using AMZN.DTOs.Products.Questions;
using AMZN.Repositories.Products;
using AMZN.Repositories.Products.Questions;
using AMZN.Shared.Exceptions;
using AMZN.Shared.Exceptions.Errors;

namespace AMZN.Services.Products.Questions
{
    public class ProductQuestionService
    {
        private const int PreviewAnswersTake = 3;

        private readonly IProductQuestionRepository _productQuestionRepository;
        private readonly IProductRepository _productRepository;

        public ProductQuestionService(IProductQuestionRepository productQuestionRepository, IProductRepository productRepository)
        {
            _productQuestionRepository = productQuestionRepository;
            _productRepository = productRepository;
        }


        public async Task<PagedResult<ProductQuestionDto>> GetQuestionsAsync(Guid productId, ProductQuestionParamsDto q, CancellationToken ct)
        {
            var exists = await _productRepository.ExistsAsync(productId, ct);
            if (!exists)
                throw new ApiException(ErrorCodes.ProductNotFound, "Product not found", StatusCodes.Status404NotFound);

            var skip = (q.Page - 1) * q.PageSize;

            var total = await _productQuestionRepository.CountQuestionsAsync(productId, ct);
            var items = await _productQuestionRepository.GetQuestionsPageAsync(productId, skip, q.PageSize, ct);

            if (items.Count > 0)
            {
                var questionIds = items.Select(x => x.Id).ToList();
                var previewAnswersMap = await _productQuestionRepository.GetPreviewAnswersMapAsync(questionIds, PreviewAnswersTake, ct);

                foreach (var item in items)
                {
                    if (previewAnswersMap.TryGetValue(item.Id, out var previewAnswers))
                        item.PreviewAnswers = previewAnswers;
                }
            }

            return new PagedResult<ProductQuestionDto>
            {
                Items = items,
                Page = q.Page,
                PageSize = q.PageSize,
                TotalItems = total
            };
        }

        public async Task<ProductQuestionDto> CreateQuestionAsync(Guid productId, Guid userId, ProductQuestionRequestDto request)
        {
            var exists = await _productRepository.ExistsAsync(productId);
            if (!exists)
                throw new ApiException(ErrorCodes.ProductNotFound, "Product not found", StatusCodes.Status404NotFound);

            var normalizedText = request.Text.Trim();

            var question = new ProductQuestion
            {
                Id = Guid.NewGuid(),
                ProductId = productId,
                UserId = userId,
                Text = normalizedText,
                CreatedAt = DateTime.UtcNow
            };

            _productQuestionRepository.AddQuestion(question);
            await _productQuestionRepository.SaveChangesAsync();

            var dto = await _productQuestionRepository.GetQuestionDtoByIdAsync(productId, question.Id);

            if (dto == null)
                throw new InvalidOperationException("Question was not saved correctly");

            return dto;
        }

        public async Task<PagedResult<ProductQuestionAnswerDto>> GetAnswersAsync(Guid productId, Guid questionId, ProductQuestionAnswerParamsDto q, CancellationToken ct)
        {
            await GetExistingQuestionAsync(productId, questionId, ct);

            var skip = (q.Page - 1) * q.PageSize;

            var total = await _productQuestionRepository.CountAnswersAsync(questionId, ct);
            var items = await _productQuestionRepository.GetAnswersPageAsync(questionId, skip, q.PageSize, ct);

            return new PagedResult<ProductQuestionAnswerDto>
            {
                Items = items,
                Page = q.Page,
                PageSize = q.PageSize,
                TotalItems = total
            };
        }

        public async Task<ProductQuestionAnswerDto> CreateAnswerAsync(Guid productId, Guid questionId, Guid userId, ProductQuestionAnswerRequestDto request)
        {
            await GetExistingQuestionAsync(productId, questionId);

            var normalizedText = request.Text.Trim();

            var answer = new ProductQuestionAnswer
            {
                Id = Guid.NewGuid(),
                QuestionId = questionId,
                UserId = userId,
                Text = normalizedText,
                CreatedAt = DateTime.UtcNow
            };

            _productQuestionRepository.AddAnswer(answer);
            await _productQuestionRepository.SaveChangesAsync();

            var dto = await _productQuestionRepository.GetAnswerDtoByIdAsync(questionId, answer.Id);

            if (dto == null)
                throw new InvalidOperationException("Answer was not saved correctly");

            return dto;
        }


        //
        private async Task<ProductQuestion> GetExistingQuestionAsync(Guid productId, Guid questionId, CancellationToken ct = default)
        {
            var question = await _productQuestionRepository.GetQuestionByIdAsync(productId, questionId, ct);

            if (question == null)
            {
                throw new ApiException(
                    ErrorCodes.ProductQuestionNotFound,
                    "Question not found",
                    StatusCodes.Status404NotFound);
            }

            return question;
        }


    }
}
