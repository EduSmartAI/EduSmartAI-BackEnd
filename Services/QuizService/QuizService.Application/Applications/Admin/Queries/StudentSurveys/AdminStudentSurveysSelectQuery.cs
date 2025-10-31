using BuildingBlocks.CQRS;

namespace QuizService.Application.Applications.Admin.Queries.StudentSurveys;

public class AdminStudentSurveysSelectQuery : IQuery<AdminStudentSurveysSelectResponse>
{
    public Guid? StudentId { get; set; }
    
    public Guid? SurveyId { get; set; }
    
    public int PageNumber { get; set; } = 1;
    
    public int PageSize { get; set; } = 10;
}