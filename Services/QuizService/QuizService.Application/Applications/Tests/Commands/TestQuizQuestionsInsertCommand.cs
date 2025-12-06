using System.ComponentModel.DataAnnotations;
using BaseService.Common.ApiEntities;
using BuildingBlocks.CQRS;

namespace QuizService.Application.Applications.Tests.Commands;

/// <summary>
/// Insert questions into quiz
/// </summary>
public class TestQuizQuestionsInsertCommand : ICommand<TestQuizQuestionsInsertResponse>
{
    [Required(ErrorMessage = "TestId is required")]
    public Guid TestId { get; set; }
    
    [Required(ErrorMessage = "QuizId is required")]
    public Guid QuizId { get; set; }
    
    [Required(ErrorMessage = "Questions is required")]
    [MinLength(1, ErrorMessage = "At least one question is required")]
    public List<QuestionInsertDto> Questions { get; set; } = new();
}

public record TestQuizQuestionsInsertResponse : AbstractApiResponse<string>
{
    public override string Response { get; set; } = string.Empty;
}

