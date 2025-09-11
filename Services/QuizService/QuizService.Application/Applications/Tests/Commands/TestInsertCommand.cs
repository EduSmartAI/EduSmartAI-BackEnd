using System.ComponentModel.DataAnnotations;
using BuildingBlocks.CQRS;

namespace QuizService.Application.Applications.Tests.Commands;

public record TestInsertCommand : ICommand<TestInsertResponse>
{
    [Required(ErrorMessage = "TestName is required")]
    public string TestName { get; set; } = null!;

    [Required(ErrorMessage = "Description is required")]
    public string? Description { get; set; }

    [Required(ErrorMessage = "Quizzes are required")]
    public List<Quizzes> Quizzes { get; set; } = null!;
}

public record Quizzes
{
    [Required(ErrorMessage = "Title is required")]
    public string Title { get; set; } = null!;

    [Required(ErrorMessage = "Description is required")]
    public string? Description { get; set; }
    
    [Required(ErrorMessage = "SubjectCode is required")]
    public Guid SubjectCode { get; set; }
    
    [Required(ErrorMessage = "Questions are required")]
    public List<Questions> Questions { get; set; } = null!;
}

public record Questions
{
    [Required(ErrorMessage = "QuestionText is required")]
    public string QuestionText { get; set; } = null!;
    
    public string? Explanation { get; set; }
    
    [Required(ErrorMessage = "Answers are required")]
    public List<Answers> Answers { get; set; } = null!;
}

public record Answers
{
    [Required(ErrorMessage = "AnswerText is required")]
    public string AnswerText { get; set; } = null!;

    [Required(ErrorMessage = "IsCorrect is required")]
    public bool IsCorrect { get; set; }
}