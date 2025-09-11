using QuizService.Application.Applications.Questions.Commands;

namespace QuizService.Application.Interfaces;

public interface IQuestionService
{
    Task<Guid> InsertQuestionAsync(Guid quizId, string text, string explanation, string email);

    /// <summary>
    /// Insert question with answers
    /// </summary>
    /// <param name="request"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<QuestionInsertResponse> InsertQuestionWithAnswersAsync(QuestionInsertCommand request, CancellationToken cancellationToken);

    /// <summary>
    /// Update question and its answers
    /// </summary>
    /// <param name="request"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<QuestionUpdateResponse> UpdateQuestionAsync(QuestionUpdateCommand request, CancellationToken cancellationToken);

    /// <summary>
    /// Delete question with all its answers
    /// </summary>
    /// <param name="request"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<QuestionDeleteResponse> DeleteQuestionAsync(QuestionDeleteCommand request, CancellationToken cancellationToken);
}