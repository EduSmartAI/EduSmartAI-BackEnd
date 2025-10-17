using System.ComponentModel.DataAnnotations;
using BuildingBlocks.CQRS;

namespace QuizService.Application.Applications.QuizCourses.Commands;

public class QuizCourseUpdateCommand : ICommand<QuizCourseUpdateResponse>
{
    [Required(ErrorMessage = "QuizId is required")]
    public Guid QuizId { get; set; }
    public int? DurationMinutes { get; set; }
    public int? PassingScorePercentage { get; set; }
    public bool? ShuffleQuestions { get; set; }
    public bool? ShowResultsImmediately { get; set; }
    public bool? AllowRetake { get; set; }
    public List<QuestionUpdateRequest>? Questions { get; set; }
}

public record QuestionUpdateRequest
{
    public Guid QuestionId { get; set; }
    public string? QuestionText { get; set; }
    public short? QuestionType { get; set; }
    public string? Explanation { get; set; }
    public List<AnswerUpdateRequest> Answers { get; set; } = null!;
}

public record AnswerUpdateRequest
{
    public Guid AnswerId { get; set; }
    
    [Required(ErrorMessage = "AnswerText is required")]
    public string AnswerText { get; set; } = null!;

    [Required(ErrorMessage = "IsCorrect is required")]
    public bool IsCorrect { get; set; }
}

