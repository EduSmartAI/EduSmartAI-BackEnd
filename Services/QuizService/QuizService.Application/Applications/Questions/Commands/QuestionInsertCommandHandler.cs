using BuildingBlocks.CQRS;
using QuizService.Application.Interfaces;

namespace QuizService.Application.Applications.Questions.Commands;

/// <summary>
/// Handler cho insert question với answers command
/// </summary>
public class QuestionInsertCommandHandler : ICommandHandler<QuestionInsertCommand, QuestionInsertResponse>
{
    private readonly IQuestionService _questionService;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="questionService"></param>
    public QuestionInsertCommandHandler(IQuestionService questionService)
    {
        _questionService = questionService;
    }

    /// <summary>
    /// Handle insert question với answers vào quiz
    /// </summary>
    /// <param name="request"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<QuestionInsertResponse> Handle(QuestionInsertCommand request, CancellationToken cancellationToken)
    {
        return await _questionService.InsertQuestionWithAnswersAsync(request, cancellationToken);
    }
}
