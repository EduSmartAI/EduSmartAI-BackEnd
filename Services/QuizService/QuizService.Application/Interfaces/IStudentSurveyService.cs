using QuizService.Application.Applications.StudentSurveys.Commands;
using QuizService.Application.Applications.StudentSurveys.Queries;

namespace QuizService.Application.Interfaces;

public interface IStudentSurveyService
{
    Task<StudentSurveyInsertResponse> InsertStudentSurveyAsync(StudentSurveyInsertCommand request, CancellationToken cancellationToken);
    
    Task<StudentSurveySelectResponse> SelectStudentSurveyAsync(StudentSurveySelectQuery request);
    
    Task<StudentStudyTimeResponse> GetStudentStudyTimeAsync(StudentStudyTimeRequest request, CancellationToken contextCancellationToken);
}