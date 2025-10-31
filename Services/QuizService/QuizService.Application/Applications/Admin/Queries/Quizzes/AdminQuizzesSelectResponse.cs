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
    
    public string Title { get; set; } = null!;
    
    public string? Description { get; set; }
    
    public Guid? SubjectCode { get; set; }
    
    public string? SubjectCodeName { get; set; }
    
    public string? SurveyCode { get; set; }
    
    public int TotalQuestions { get; set; }
    
    public int TotalStudentsTaken { get; set; }
    
    public bool IsActive { get; set; }
    
    public DateTime CreatedAt { get; set; }
}

