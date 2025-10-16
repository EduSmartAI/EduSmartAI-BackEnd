using System.ComponentModel.DataAnnotations;
using BuildingBlocks.CQRS;

namespace QuizService.Application.Applications.QuizCourses.Commands;

/// <summary>
/// Command to add new questions to an existing quiz
/// </summary>
public class QuizCourseAddQuestionsCommand : ICommand<QuizCourseAddQuestionsResponse>
{
    [Required(ErrorMessage = "QuizId is required")]
    public Guid QuizId { get; set; }
    
    [Required(ErrorMessage = "Questions are required")]
    [MinLength(1, ErrorMessage = "At least one question is required")]
    public List<QuestionAddRequest> Questions { get; set; } = null!;
}

public record QuestionAddRequest
{
    [Required(ErrorMessage = "QuestionText is required")]
    public string QuestionText { get; set; } = null!;
    
    [Range(1, 4, ErrorMessage = "QuestionType must be between 1 and 4")]
    public short QuestionType { get; set; }
    
    public string? Explanation { get; set; }
    
    [Required(ErrorMessage = "Answers are required")]
    [MinLength(2, ErrorMessage = "At least 2 answers are required")]
    public List<AnswerAddRequest> Answers { get; set; } = null!;
}

public record AnswerAddRequest
{
    [Required(ErrorMessage = "AnswerText is required")]
    public string AnswerText { get; set; } = null!;

    [Required(ErrorMessage = "IsCorrect is required")]
    public bool IsCorrect { get; set; }
}

