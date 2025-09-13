using QuizService.Application.Applications.Quizzes.Queries;
using QuizService.Application.Applications.Surveys.Commands;
using QuizService.Application.Applications.Surveys.Queries;

namespace QuizService.Application.Interfaces;

public interface IQuizService
{
    Task<Guid> InsertQuizAsync(Guid testId, string title, string? description, Guid subjectCode, string userEmail);

    Task<QuizSelectsResponse> SelectQuizzesAsync(QuizSelectsQuery request);
    
    Task<SurveyInsertResponse> InsertSurveyAsync(SurveyInsertCommand request, CancellationToken cancellationToken);
    
    Task<SurveyDetailSelectResponse> SelectSurveyDetailAsync(SurveyDetailSelectQuery request);
    
    Task<SurveySelectsResponse> SelectSurveyAsync(SurveySelectsQuery request);
}