using BaseService.Common.ApiEntities;

namespace QuizService.Application.Applications.StudentSurveys.Queries;

public record StudentSurveySelectResponse : AbstractApiResponse<List<StudentSurveySelectResponseEntity>>
{
    public override List<StudentSurveySelectResponseEntity> Response { get; set; }
}

public record StudentSurveySelectResponseEntity
{
    public Guid StudentSurveyId { get; set; }
    
    public StudentSurveySelectQuizResponseEntity Survey { get; set; }
}

public record StudentSurveySelectQuizResponseEntity
{
    public string Title { get; set; } = null!;

    public string? Description { get; set; }
    
    public List<StudentSurveySelectQuestionResponseEntity> Questions { get; set; }
}

public class StudentSurveySelectQuestionResponseEntity
{
    public Guid QuestionId { get; set; }
    
    public string QuestionText { get; set; }
    
    public List<StudentSurveySelectAnswerResponseEntity> Answers { get; set; }
}

public class StudentSurveySelectAnswerResponseEntity
{
    public Guid? AnswerId { get; set; }
    
    public bool IsCorrect { get; set; }

    public string? AnswerText { get; set; }
}