using System.ComponentModel.DataAnnotations;
using BaseService.Common.ApiEntities;
using BuildingBlocks.CQRS;

namespace QuizService.Application.Applications.Tests.Commands;

/// <summary>
/// Delete multiple questions from quiz
/// </summary>
public class TestQuizQuestionsDeleteCommand : ICommand<TestQuizQuestionsDeleteResponse>
{
    [Required(ErrorMessage = "TestId is required")]
    public Guid TestId { get; set; }
    
    [Required(ErrorMessage = "QuizId is required")]
    public Guid QuizId { get; set; }
    
    [Required(ErrorMessage = "QuestionIds is required")]
    [MinLength(1, ErrorMessage = "At least one question ID is required")]
    public List<Guid> QuestionIds { get; set; } = new();
}

public record TestQuizQuestionsDeleteResponse : AbstractApiResponse<string>
{
    public override string Response { get; set; }
}

