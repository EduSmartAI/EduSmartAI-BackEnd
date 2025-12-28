using BaseService.Common.ApiEntities;

namespace QuizService.Application.Applications.PracticeTest;

public record PracticeTestCodeCheckResponse : AbstractApiResponse<PracticeTestCodeCheckResponseEntity>
{
    public override PracticeTestCodeCheckResponseEntity Response { get; set; } = null!;
}

public class PracticeTestCodeCheckResponseEntity
{
    public string OverallStatus { get; set; } = null!;
    public int TotalTests { get; set; }
    public int PassedTests { get; set; }
    public List<TestCaseExecutionResult> TestCaseResults { get; set; } = null!;
}

public class TestCaseExecutionResult
{
    public int TestCaseNumber { get; set; }
    public string Input { get; set; } = null!;
    
    public string? ExpectedOutput { get; set; }
    public string Status { get; set; } = null!;
    public string Output { get; set; } = null!;
    public string? Error { get; set; }
    public double? ExecutionTime { get; set; }
    public int? Memory { get; set; }
}

