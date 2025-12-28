using BaseService.Common.ApiEntities;

namespace QuizService.Application.Applications.PracticeTest;

public record StudentSubmissionsSelectResponse : AbstractApiResponse<StudentSubmissionsSelectResponseEntity>
{
    public override StudentSubmissionsSelectResponseEntity Response { get; set; }
}

public class StudentSubmissionsSelectResponseEntity
{
    public Guid StudentId { get; set; }
    public int TotalRecords { get; set; }
    public int TotalPages { get; set; }
    public int CurrentPage { get; set; }
    public int PageSize { get; set; }
    public List<AdminStudentSubmissionItem> Submissions { get; set; } = new();
}

public class AdminStudentSubmissionItem
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
    public List<AdminSubmissionTestResultItem> TestResults { get; set; } = new();
}

public class AdminSubmissionTestResultItem
{
    public Guid TestCaseId { get; set; }
    public bool IsPublic { get; set; }
    public string InputData { get; set; } = null!;
    public string ExpectedOutput { get; set; } = null!;
    public string? ActualOutput { get; set; }
    public bool Passed { get; set; }
}

