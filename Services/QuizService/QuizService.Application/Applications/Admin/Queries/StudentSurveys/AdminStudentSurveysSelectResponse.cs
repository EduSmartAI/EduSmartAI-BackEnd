using BaseService.Common.ApiEntities;

namespace QuizService.Application.Applications.Admin.Queries.StudentSurveys;

public record AdminStudentSurveysSelectResponse : AbstractApiResponse<AdminStudentSurveysSelectResponseEntity>
{
    public override AdminStudentSurveysSelectResponseEntity Response { get; set; }
}

public class AdminStudentSurveysSelectResponseEntity
{
    public List<AdminStudentSurveyItem> StudentSurveys { get; set; } = new();
    
    public int TotalCount { get; set; }
    
    public int PageNumber { get; set; }
    
    public int PageSize { get; set; }
}

public class AdminStudentSurveyItem
{
    public Guid StudentQuizId { get; set; }
    
    public Guid StudentId { get; set; }
    
    public string StudentName { get; set; } = null!;
    
    public string StudentEmail { get; set; } = null!;
    
    public Guid SurveyId { get; set; }
    
    public string SurveyTitle { get; set; } = null!;
    
    public string SurveyCode { get; set; } = null!;
    
    public int TotalQuestions { get; set; }
    
    public int TotalAnswers { get; set; }
    
    public DateTime CreatedAt { get; set; }
}

