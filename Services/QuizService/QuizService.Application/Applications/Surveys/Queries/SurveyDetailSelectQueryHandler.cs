using BuildingBlocks.CQRS;
using QuizService.Application.Interfaces;

namespace QuizService.Application.Applications.Surveys.Queries;

public class SurveyDetailSelectQueryHandler : IQueryHandler<SurveyDetailSelectQuery, SurveyDetailSelectResponse>
{
    private readonly IQuizSurveyService _quizService;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="quizService"></param>
    public SurveyDetailSelectQueryHandler(IQuizSurveyService quizService)
    {
        _quizService = quizService;
    }

    /// <summary>
    /// Handle select surveys
    /// </summary>
    /// <param name="request"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<SurveyDetailSelectResponse> Handle(SurveyDetailSelectQuery request, CancellationToken cancellationToken)
    {
        return await _quizService.SelectSurveyDetailAsync(request);
    }
}