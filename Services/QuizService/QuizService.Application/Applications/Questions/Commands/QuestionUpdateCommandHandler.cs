using BuildingBlocks.CQRS;
using QuizService.Application.Interfaces;

namespace QuizService.Application.Applications.Questions.Commands;

/// <summary>
/// Handler cho update question command
/// </summary>
public class QuestionUpdateCommandHandler : ICommandHandler<QuestionUpdateCommand, QuestionUpdateResponse>
{
    private readonly IQuestionService _questionService;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="questionService"></param>
    public QuestionUpdateCommandHandler(IQuestionService questionService)
    {
        _questionService = questionService;
    }

    /// <summary>
    /// Handle update question và answers
    /// </summary>
    /// <param name="request"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<QuestionUpdateResponse> Handle(QuestionUpdateCommand request, CancellationToken cancellationToken)
    {
        return await _questionService.UpdateQuestionAsync(request, cancellationToken);
    }
}
