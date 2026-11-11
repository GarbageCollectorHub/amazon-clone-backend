using AMZN.Data.Entities;
using AMZN.DTOs.Products.Questions;

namespace AMZN.Repositories.Products.Questions
{
    public interface IProductQuestionRepository
    {
        Task SaveChangesAsync();

        Task<ProductQuestion?> GetQuestionByIdAsync(Guid productId, Guid questionId, CancellationToken ct = default);

        Task<int> CountQuestionsAsync(Guid productId, CancellationToken ct = default);
        Task<List<ProductQuestionDto>> GetQuestionsPageAsync(Guid productId, int skip, int take, CancellationToken ct = default);
        Task<Dictionary<Guid, List<ProductQuestionAnswerDto>>> GetPreviewAnswersMapAsync(List<Guid> questionIds, int takePerQuestion, CancellationToken ct = default);
        Task<ProductQuestionDto?> GetQuestionDtoByIdAsync(Guid productId, Guid questionId);

        void AddQuestion(ProductQuestion question);

        Task<int> CountAnswersAsync(Guid questionId, CancellationToken ct = default);
        Task<List<ProductQuestionAnswerDto>> GetAnswersPageAsync(Guid questionId, int skip, int take, CancellationToken ct = default);
        Task<ProductQuestionAnswerDto?> GetAnswerDtoByIdAsync(Guid questionId, Guid answerId);

        void AddAnswer(ProductQuestionAnswer answer);
    }

}
