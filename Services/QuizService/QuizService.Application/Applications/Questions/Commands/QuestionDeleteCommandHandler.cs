using BuildingBlocks.CQRS;
using QuizService.Application.Interfaces;

namespace QuizService.Application.Applications.Questions.Commands;

/// <summary>
/// Handler cho delete question command
/// </summary>
public class QuestionDeleteCommandHandler : ICommandHandler<QuestionDeleteCommand, QuestionDeleteResponse>
{
    private readonly IQuestionService _questionService;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="questionService"></param>
    public QuestionDeleteCommandHandler(IQuestionService questionService)
    {
        _questionService = questionService;
    }

    /// <summary>
    /// Handle delete question và tất cả answers của nó
    /// </summary>
    /// <param name="request"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<QuestionDeleteResponse> Handle(QuestionDeleteCommand request, CancellationToken cancellationToken)
    {
        return await _questionService.DeleteQuestionAsync(request, cancellationToken);
    }
}
