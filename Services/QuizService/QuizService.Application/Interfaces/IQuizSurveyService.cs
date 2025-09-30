using QuizService.Application.Applications.Surveys.Commands;
using QuizService.Application.Applications.Surveys.Queries;

namespace QuizService.Application.Interfaces;

public interface IQuizSurveyService
{
    Task<SurveyInsertResponse> InsertSurveyAsync(SurveyInsertCommand request, CancellationToken cancellationToken);
    
    Task<SurveyDetailSelectResponse> SelectSurveyDetailAsync(SurveyDetailSelectQuery request);
    
    Task<SurveySelectsResponse> SelectSurveyAsync(SurveySelectsQuery request);
}