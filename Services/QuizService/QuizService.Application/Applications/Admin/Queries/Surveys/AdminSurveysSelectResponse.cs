using BaseService.Common.ApiEntities;
using QuizService.Domain.ReadModels;

namespace QuizService.Application.Applications.Admin.Queries.Surveys;

public record AdminSurveysSelectResponse : AbstractApiResponse<AdminSurveysSelectResponseEntity>
{
    public override AdminSurveysSelectResponseEntity Response { get; set; }
}

public class AdminSurveysSelectResponseEntity
{
    public List<AdminSurveyItem> Surveys { get; set; }
    
    public int TotalCount { get; set; }
    
    public int PageNumber { get; set; }
    
    public int PageSize { get; set; }
}

public class AdminSurveyItem
{
    public Guid SurveyId { get; set; }
    
    public short SurveyType { get; set; }
    
    public SurveyQuizSettingDto SurveyQuizSetting { get; set; } = null!;

    public List<QuestionDto> Questions { get; set; } = null!;

    public int TotalQuestions { get; set; }
    
    public int TotalStudentsTaken { get; set; }
    
    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public string CreatedBy { get; set; } = null!;

    public string UpdatedBy { get; set; } = null!;
}

public class SurveyQuizSettingDto
{
    public short SurveyTypeId { get; set; }
    
    public string SurveyTypeName { get; set; } = null!;
    
    public string SurveyCode { get; set; } = null!;
    
    public string Title { get; set; } = null!;
    
    public string? Description { get; set; }
}

public class QuestionDto
{
    public Guid QuestionId { get; set; }
    
    public string QuestionText { get; set; } = null!;
    
    public string? Explanation { get; set; }
    
    public short QuestionType { get; set; }
    
    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public List<AnswerDto>? Answers { get; set; }
}

public class AnswerDto
{
    public Guid AnswerId { get; set; }
    
    public string AnswerText { get; set; } = null!;
    
    public bool IsCorrect { get; set; }
}

public class AnswerRuleDto
{
    public Guid AnswerRuleId { get; set; }
    
    public string NextQuestion { get; set; } = null!;
}

