using BaseService.Common.ApiEntities;

namespace QuizService.Application.Applications.PracticeTest;

public record PracticeTestSubmitInsertResponse : AbstractApiResponse<PracticeTestSubmitInsertResponseEntity>
{
    public override PracticeTestSubmitInsertResponseEntity Response { get; set; }
}

public class PracticeTestSubmitInsertResponseEntity
{
    public Guid SubmissionId { get; set; }
    public string Status { get; set; }
    public int PassedTests { get; set; }
    public int TotalTests { get; set; }
    public int AverageTimeMs { get; set; }
    public List<SubmissionTestResultResponse> TestResults { get; set; }
}
public class SubmissionTestResultResponse
{
    public Guid TestCaseId { get; set; }
    public bool IsPublic { get; set; }
    public string InputData { get; set; }
    public string ExpectedOutput { get; set; }
    public string ActualOutput { get; set; }
    public bool Passed { get; set; }
    public string Status { get; set; }
}