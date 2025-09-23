using System.ComponentModel.DataAnnotations;

namespace BuildingBlocks.Messaging.Events.CourseService.QuizCourseInsertEvents;

public class QuizCourseInsertEvent
{
    [Required(ErrorMessage = "UserEmail is required")]
    public string UserEmail { get; set; } = null!;

    [Required(ErrorMessage = "Title is required")]
    public string Title { get; set; } = null!;

    [Required(ErrorMessage = "Description is required")]
    public string? Description { get; set; }

    [Required(ErrorMessage = "DurationMinutes is required")]
    public int DurationMinutes { get; set; }

    [Required(ErrorMessage = "PassingScorePercentage is required")]
    public int PassingScorePercentage { get; set; }

    [Required(ErrorMessage = "ShuffleQuestions is required")]
    public bool ShuffleQuestions { get; set; }

    [Required(ErrorMessage = "ShowResultsImmediately is required")]
    public bool ShowResultsImmediately { get; set; }

    [Required(ErrorMessage = "AllowRetake is required")]
    public bool AllowRetake { get; set; }

    [Required(ErrorMessage = "Questions are required")]
    public List<Questions> Questions { get; set; }
}

public record Questions
{
    [Required(ErrorMessage = "QuestionText is required")]
    public string QuestionText { get; set; }

    [Range(1, 3, ErrorMessage = "QuestionType must be between 1 and 3")]
    public short QuestionType { get; set; }

    public string? Explanation { get; set; }
    
    [Required(ErrorMessage = "Answers are required")]
    public List<Answers> Answers { get; set; }
}

public record Answers
{
    [Required(ErrorMessage = "AnswerText is required")]
    public string AnswerText { get; set; }

    [Required(ErrorMessage = "IsCorrect is required")]
    public bool IsCorrect { get; set; }
}