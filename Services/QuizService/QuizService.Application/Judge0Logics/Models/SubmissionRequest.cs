namespace QuizService.Application.Judge0Logics.Models;

public class SubmissionRequest
{
    public string SourceCode { get; set; } = null!;
    public int LanguageId { get; set; }
    public string? Stdin { get; set; }
    public string? ExpectedOutput { get; set; }
    public double? CpuTimeLimit { get; set; } = 2.0;
    public double? WallTimeLimit { get; set; } = 5.0;
    public int? MemoryLimit { get; set; } = 128000;
}