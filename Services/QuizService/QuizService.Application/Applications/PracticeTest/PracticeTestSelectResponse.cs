using BaseService.Common.ApiEntities;

namespace QuizService.Application.Applications.PracticeTest;

public record PracticeTestSelectResponse : AbstractApiResponse<PracticeTestSelectResponseEntity>
{
    public override PracticeTestSelectResponseEntity Response { get; set; }
}

public class PracticeTestSelectResponseEntity
{
    public Guid ProblemId { get; set; }

    public string Title { get; set; } = null!;

    public string Description { get; set; } = null!;

    public string Difficulty { get; set; } = null!;
    
    public List<PracticeTestProblemExample> Examples { get; set; }
    
    public List<PracticeTestSelectTestCaseResponse> TestCases { get; set; }
}

public class PracticeTestSelectTestCaseResponse
{
    public Guid TestcaseId { get; set; }

    public Guid? ProblemId { get; set; }

    public string InputData { get; set; } = null!;

    public string ExpectedOutput { get; set; } = null!;
}

public class PracticeTestProblemExample
{
    public Guid ExampleId { get; set; }
    
    public int ExampleOrder { get; set; }

    public string InputData { get; set; } = null!;

    public string OutputData { get; set; } = null!;

    public string? Explanation { get; set; }
}
