using BaseService.Common.ApiEntities;

namespace QuizService.Application.Applications.StudentSurveys.Queries;

public record StudentSurveySelectDetailResponse : AbstractApiResponse<StudentSurveySelectDetailResponseEntity>
{
    public override StudentSurveySelectDetailResponseEntity Response { get; set; } = null!;
}

public record StudentSurveySelectDetailResponseEntity
{
    public Guid StudentSurveyId { get; set; }
    
    public Guid SurveyId { get; set; }
    
    public string SurveyTitle { get; set; } = null!;
    
    public string? SurveyDescription { get; set; }
    
    public string? SurveyCode { get; set; }
    
    public DateTime? CreatedAt { get; set; }
    
    public List<SurveyQuestionDetailResponseEntity> Questions { get; set; } = new();
}

public class SurveyQuestionDetailResponseEntity
{
    public Guid QuestionId { get; set; }
    
    public string QuestionText { get; set; } = null!;
    
    public short QuestionType { get; set; }
    
    public List<SurveyAnswerDetailResponse> Answers { get; set; } = new();
}

public record SurveyAnswerDetailResponse
{
    public Guid? AnswerId { get; set; }
    
    public bool SelectedByStudent { get; set; }
    
    public string? AnswerText { get; set; }
}
