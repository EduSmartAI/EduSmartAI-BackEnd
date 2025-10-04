using QuizService.Application.Applications.Quizzes.Queries;

namespace QuizService.Application.Interfaces;

public interface IQuizService
{
    Task<QuizSelectsResponse> SelectQuizzesAsync(QuizSelectsQuery request);
}