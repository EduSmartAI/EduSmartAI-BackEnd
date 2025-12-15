using BaseService.Common.ApiEntities;

namespace QuizService.Application.Applications.PracticeTest;

public record StudentPracticeTestSubmissionsByIdsSelectResponse : AbstractApiResponse<StudentPracticeTestSubmissionsByIdsSelectResponseEntity>
{
    public override StudentPracticeTestSubmissionsByIdsSelectResponseEntity Response { get; set; }
}

public class StudentPracticeTestSubmissionsByIdsSelectResponseEntity
{
    public List<StudentPracticeTestSubmissionDetailItem> Submissions { get; set; } = new();
}

public class StudentPracticeTestSubmissionDetailItem
{
    public Guid SubmissionId { get; set; }
    public Guid ProblemId { get; set; }
    public string ProblemTitle { get; set; } = null!;
    public string ProblemDifficulty { get; set; } = null!;
    public string LanguageName { get; set; } = null!;
    public int LanguageId { get; set; }
    public string Status { get; set; } = null!;
    public int PassedTests { get; set; }
    public int TotalTests { get; set; }
    public int RuntimeMs { get; set; }
    public DateTime SubmittedAt { get; set; }
    public string SourceCode { get; set; } = null!;
    public List<SubmissionTestResultDetailItem> TestResults { get; set; } = new();
}

public class SubmissionTestResultDetailItem
{
    public Guid TestCaseId { get; set; }
    public bool IsPublic { get; set; }
    public string? InputData { get; set; }
    public string? ExpectedOutput { get; set; }
    public string? ActualOutput { get; set; }
    public bool Passed { get; set; }
}

