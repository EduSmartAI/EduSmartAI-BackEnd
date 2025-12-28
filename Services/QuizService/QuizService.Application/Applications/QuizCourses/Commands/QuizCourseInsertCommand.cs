using System.ComponentModel.DataAnnotations;
using BuildingBlocks.CQRS;

namespace QuizService.Application.Applications.QuizCourses.Commands;

public class QuizCourseInsertCommand : ICommand<QuizCourseInsertResponse>
{
    [Required(ErrorMessage = "UserEmail is required")]
    public string UserEmail { get; set; } = null!;
    
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
    public List<QuizCourseQuestionsInsert> Questions { get; set; }
}

public record QuizCourseQuestionsInsert
{
    [Required(ErrorMessage = "QuestionText is required")]
    public string QuestionText { get; set; }
    
    public short QuestionType { get; set; }
    
    public string? Explanation { get; set; } 
    
    [Required(ErrorMessage = "Answers are required")]
    public List<QuizCourseAnswersInsert> Answers { get; set; }
}

public record QuizCourseAnswersInsert
{
    [Required(ErrorMessage = "AnswerText is required")]
    public string AnswerText { get; set; }

    [Required(ErrorMessage = "IsCorrect is required")]
    public bool IsCorrect { get; set; } 
}