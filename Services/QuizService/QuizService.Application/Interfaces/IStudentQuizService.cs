using QuizService.Application.Applications.StudentSurveys;
using QuizService.Application.Applications.StudentSurveys.Commands;
using QuizService.Application.Applications.StudentSurveys.Queries;

namespace QuizService.Application.Interfaces;

public interface IStudentQuizService
{
    Task<StudentSurveyInsertResponse> InsertStudentSurveyAsync(StudentSurveyInsertCommand request, CancellationToken cancellationToken);
    
    Task<StudentSurveySelectResponse> SelectStudentSurveyAsync(StudentSurveySelectQuery request);
}