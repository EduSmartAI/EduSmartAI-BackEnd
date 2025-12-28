using BaseService.Common.ApiEntities;

namespace QuizService.Application.Applications.Admin.Queries.PracticeTests;

public record AdminPracticeTestsSelectResponse : AbstractApiResponse<AdminPracticeTestsSelectResponseEntity>
{
    public override AdminPracticeTestsSelectResponseEntity Response { get; set; }
}

public class AdminPracticeTestsSelectResponseEntity
{
    public List<AdminPracticeTestItem> PracticeTests { get; set; } = new();
    
    public int TotalCount { get; set; }
    
    public int PageNumber { get; set; }
    
    public int PageSize { get; set; }
}

public class AdminPracticeTestItem
{
    public Guid ProblemId { get; set; }
    
    public string Title { get; set; } = null!;
    
    public string Description { get; set; } = null!;
    
    public string Difficulty { get; set; } = null!;
    
    public int TotalTestCases { get; set; }
    
    public int TotalExamples { get; set; }
    
    public int TotalTemplates { get; set; }
    
    public int TotalSubmissions { get; set; }
    
    public DateTime CreatedAt { get; set; }
}

