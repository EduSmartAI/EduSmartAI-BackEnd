using BaseService.Common.ApiEntities;

namespace QuizService.Application.Applications.Admin.Queries.Quizzes;

public record AdminQuizzesSelectResponse : AbstractApiResponse<AdminQuizzesSelectResponseEntity>
{
    public override AdminQuizzesSelectResponseEntity Response { get; set; }
}

public class AdminQuizzesSelectResponseEntity
{
    public List<AdminQuizItem> Quizzes { get; set; } = new();
    
    public int TotalCount { get; set; }
    
    public int PageNumber { get; set; }
    
    public int PageSize { get; set; }
}

public class AdminQuizItem
{
    public Guid QuizId { get; set; }
    
    public short QuizType { get; set; }
    
    public string QuizTypeName { get; set; } = null!;
    
    public string? Title { get; set; } = null!;
    
    public string? Description { get; set; }
    
    public Guid? SubjectCode { get; set; }
    
    public string? SubjectCodeName { get; set; }
    
    public string? SurveyCode { get; set; }
    
    public int TotalQuestions { get; set; }
    
    public int TotalStudentsTaken { get; set; }
    
    public DateTime CreatedAt { get; set; }
    
    // Settings based on QuizType
    public PlacementTestQuizSettingDto? PlacementTestQuizSetting { get; set; }
    
    public CourseQuizSettingDto? CourseQuizSetting { get; set; }
    
    public SurveyQuizSettingDto? SurveyQuizSetting { get; set; }
}

public class PlacementTestQuizSettingDto
{
    public Guid SubjectCode { get; set; }
    public string SubjectCodeName { get; set; } = null!;
    public string Title { get; set; } = null!;
    public string? Description { get; set; }
}

public class CourseQuizSettingDto
{
    public Guid QuizId { get; set; }
    public int DurationMinutes { get; set; }
    public int PassingScorePercentage { get; set; }
    public bool? ShuffleQuestions { get; set; }
    public bool? ShowResultsImmediately { get; set; }
    public bool? AllowRetake { get; set; }
}

public class SurveyQuizSettingDto
{
    public short SurveyTypeId { get; set; }
    public string SurveyCode { get; set; } = null!;
    public string SurveyTypeName { get; set; } = null!;
    public string Title { get; set; } = null!;
    public string? Description { get; set; }
}

