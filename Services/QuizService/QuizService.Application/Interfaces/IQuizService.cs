using QuizService.Application.Applications.Quizzes.Queries;
using QuizService.Application.Applications.Surveys.Commands;
using QuizService.Application.Applications.Surveys.Queries;
using QuizService.Domain.WriteModels;

namespace QuizService.Application.Interfaces;

public interface IQuizService
{
    Task<QuizSelectsResponse> SelectQuizzesAsync(QuizSelectsQuery request);
    
    Task<SurveyInsertResponse> InsertSurveyAsync(SurveyInsertCommand request, CancellationToken cancellationToken);
    
    Task<SurveyDetailSelectResponse> SelectSurveyDetailAsync(SurveyDetailSelectQuery request);
    
    Task<SurveySelectsResponse> SelectSurveyAsync(SurveySelectsQuery request);
}