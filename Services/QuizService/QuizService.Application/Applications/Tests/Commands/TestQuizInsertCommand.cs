using System.ComponentModel.DataAnnotations;
using BaseService.Common.ApiEntities;
using BaseService.Common.Utils.Const;
using BuildingBlocks.CQRS;

namespace QuizService.Application.Applications.Tests.Commands;

public class TestQuizInsertCommand : ICommand<TestQuizInsertResponse>
{
    [Required(ErrorMessage = "TestId is required")]
    public Guid TestId { get; set; }
    
    [Required(ErrorMessage = "Quizzes is required")]
    [MinLength(1, ErrorMessage = "At least one quiz is required")]
    public List<QuizInsertDto> Quizzes { get; set; } = new();
}

public class QuizInsertDto
{
    [Required(ErrorMessage = "SubjectCode is required")]
    public Guid SubjectCode { get; set; }
    
    [Required(ErrorMessage = "Title is required")]
    public string Title { get; set; } = null!;
    
    public string? Description { get; set; }
    
    [Required(ErrorMessage = "Questions is required")]
    [MinLength(1, ErrorMessage = "At least one question is required")]
    public List<QuestionInsertDto> Questions { get; set; } = new();
}

public class QuestionInsertDto
{
    [Required(ErrorMessage = "QuestionText is required")]
    public string QuestionText { get; set; } = null!;
    
    [Required(ErrorMessage = "QuestionType is required")]
    public ConstantEnum.QuestionType QuestionType { get; set; }
    
    public short? DifficultyLevel { get; set; }
    
    [Required(ErrorMessage = "Answers is required")]
    [MinLength(1, ErrorMessage = "At least one answer is required")]
    public List<AnswerInsertDto> Answers { get; set; } = new();
}

public class AnswerInsertDto
{
    [Required(ErrorMessage = "AnswerText is required")]
    public string AnswerText { get; set; } = null!;
    
    public bool IsCorrect { get; set; }
}

public record TestQuizInsertResponse : AbstractApiResponse<string>
{
    public override string Response { get; set; }
}

