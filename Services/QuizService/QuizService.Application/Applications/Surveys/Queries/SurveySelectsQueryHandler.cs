using BuildingBlocks.CQRS;
using QuizService.Application.Interfaces;

namespace QuizService.Application.Applications.Surveys.Queries;

public class SurveySelectsQueryHandler : IQueryHandler<SurveySelectsQuery, SurveySelectsResponse>
{
    private readonly IQuizService _quizService;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="quizService"></param>
    public SurveySelectsQueryHandler(IQuizService quizService)
    {
        _quizService = quizService;
    }

    /// <summary>
    /// Handler survey selects
    /// </summary>
    /// <param name="request"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<SurveySelectsResponse> Handle(SurveySelectsQuery request, CancellationToken cancellationToken)
    {
        return await _quizService.SelectSurveyAsync(request);
    }
}