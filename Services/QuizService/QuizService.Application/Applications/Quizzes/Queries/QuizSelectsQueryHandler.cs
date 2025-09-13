using BuildingBlocks.CQRS;
using QuizService.Application.Interfaces;

namespace QuizService.Application.Applications.Quizzes.Queries;

public class QuizSelectsQueryHandler : IQueryHandler<QuizSelectsQuery, QuizSelectsResponse>
{
    private readonly IQuizService _quizService;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="quizService"></param>
    public QuizSelectsQueryHandler(IQuizService quizService)
    {
        _quizService = quizService;
    }

    /// <summary>
    /// Handles the retrieval of quiz selects
    /// </summary>
    /// <param name="request"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<QuizSelectsResponse> Handle(QuizSelectsQuery request, CancellationToken cancellationToken)
    {
        return await _quizService.SelectQuizzesAsync(request);
    }
}