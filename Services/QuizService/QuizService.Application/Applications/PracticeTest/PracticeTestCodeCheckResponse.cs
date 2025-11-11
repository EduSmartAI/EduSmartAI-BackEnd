using BaseService.Common.ApiEntities;

namespace QuizService.Application.Applications.PracticeTest;

public record PracticeTestCodeCheckResponse : AbstractApiResponse<PracticeTestCodeCheckResponseEntity>
{
    public override PracticeTestCodeCheckResponseEntity Response { get; set; } = null!;
}

public class PracticeTestCodeCheckResponseEntity
{
    public string Status { get; set; } = null!;
    public string Output { get; set; } = null!;
    public string? Error { get; set; }
    public double? ExecutionTime { get; set; }
    public int? Memory { get; set; }
}

