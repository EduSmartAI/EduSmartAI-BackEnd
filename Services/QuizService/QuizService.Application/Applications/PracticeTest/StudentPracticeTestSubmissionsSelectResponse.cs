using BaseService.Common.ApiEntities;

namespace QuizService.Application.Applications.PracticeTest;

public record StudentPracticeTestSubmissionsSelectResponse : AbstractApiResponse<StudentPracticeTestSubmissionsSelectResponseEntity>
{
    public override StudentPracticeTestSubmissionsSelectResponseEntity Response { get; set; }
}

public class StudentPracticeTestSubmissionsSelectResponseEntity
{
    public int TotalRecords { get; set; }
    public int TotalPages { get; set; }
    public int CurrentPage { get; set; }
    public int PageSize { get; set; }
    public List<StudentPracticeTestSubmissionItem> Submissions { get; set; } = new();
}

public class StudentPracticeTestSubmissionItem
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
    public int AverageTimeMs { get; set; }
    public DateTime SubmittedAt { get; set; }
    public List<SubmissionTestResultItem> TestResults { get; set; } = new();
}

public class SubmissionTestResultItem
{
    public Guid TestCaseId { get; set; }
    public bool IsPublic { get; set; }
    public string? InputData { get; set; }
    public string? ExpectedOutput { get; set; }
    public string? ActualOutput { get; set; }
    public bool Passed { get; set; }
}

