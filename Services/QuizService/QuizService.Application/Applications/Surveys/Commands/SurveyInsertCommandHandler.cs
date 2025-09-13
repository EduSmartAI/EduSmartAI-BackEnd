using BuildingBlocks.CQRS;
using QuizService.Application.Interfaces;

namespace QuizService.Application.Applications.Surveys.Commands;

public class SurveyInsertCommandHandler : ICommandHandler<SurveyInsertCommand, SurveyInsertResponse>
{
    private readonly IQuizService _quizService;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="quizService"></param>
    public SurveyInsertCommandHandler(IQuizService quizService)
    {
        _quizService = quizService;
    }

    /// <summary>
    /// Handle servey insert
    /// </summary>
    /// <param name="request"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<SurveyInsertResponse> Handle(SurveyInsertCommand request, CancellationToken cancellationToken)
    {
        return await _quizService.InsertSurveyAsync(request, cancellationToken);
    }
}