using BaseService.Common.ApiEntities;

namespace QuizService.Application.Applications.Admin.Queries.StudentTests;

public record AdminStudentTestsSelectResponse : AbstractApiResponse<AdminStudentTestsSelectResponseEntity>
{
    public override AdminStudentTestsSelectResponseEntity Response { get; set; }
}

public class AdminStudentTestsSelectResponseEntity
{
    public List<AdminStudentTestItem> StudentTests { get; set; } = new();
    
    public int TotalCount { get; set; }
    
    public int PageNumber { get; set; }
    
    public int PageSize { get; set; }
}

public class AdminStudentTestItem
{
    public Guid StudentTestId { get; set; }
    
    public Guid StudentId { get; set; }
    
    public string StudentName { get; set; } = null!;
    
    public string StudentEmail { get; set; } = null!;
    
    public Guid TestId { get; set; }
    
    public string TestName { get; set; } = null!;
    
    public int TotalQuizzes { get; set; }
    
    public int TotalQuestions { get; set; }
    
    public int TotalCorrectAnswers { get; set; }
    
    public int StudentLevel { get; set; }
    
    public DateTime? StartedAt { get; set; }
    
    public DateTime? FinishedAt { get; set; }
    
    public TimeSpan? Duration { get; set; }
}

