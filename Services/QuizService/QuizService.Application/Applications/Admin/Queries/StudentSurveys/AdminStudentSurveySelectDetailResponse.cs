using BaseService.Common.ApiEntities;

namespace QuizService.Application.Applications.Admin.Queries.StudentSurveys;

public record AdminStudentSurveySelectDetailResponse : AbstractApiResponse<AdminStudentSurveySelectDetailResponseEntity>
{
    public override AdminStudentSurveySelectDetailResponseEntity Response { get; set; } = null!;
}

public record AdminStudentSurveySelectDetailResponseEntity
{
    public Guid StudentQuizId { get; set; }
    
    public Guid StudentId { get; set; }
    
    public string StudentName { get; set; } = null!;
    
    public string StudentEmail { get; set; } = null!;
    
    public Guid SurveyId { get; set; }
    
    public string SurveyTitle { get; set; } = null!;
    
    public string? SurveyDescription { get; set; }
    
    public string SurveyCode { get; set; } = null!;
    
    public DateTime CreatedAt { get; set; }
    
    public List<SurveyQuestionResultResponseEntity> QuestionResults { get; set; } = new();
}

public class SurveyQuestionResultResponseEntity
{
    public Guid QuestionId { get; set; }
    
    public string QuestionText { get; set; } = null!;
    
    public short QuestionType { get; set; }
    
    public List<AdminSurveyAnswerDetailResponse> Answers { get; set; } = new();
}

public record AdminSurveyAnswerDetailResponse
{
    public Guid? AnswerId { get; set; }
    
    public string? AnswerText { get; set; }
    
    public bool SelectedByStudent { get; set; }
}

