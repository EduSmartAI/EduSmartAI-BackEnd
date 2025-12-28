using System.ComponentModel.DataAnnotations;
using BaseService.Common.ApiEntities;
using BuildingBlocks.CQRS;

namespace QuizService.Application.Applications.Tests.Commands;

/// <summary>
/// Delete quiz from test
/// </summary>
public class TestQuizDeleteCommand : ICommand<TestQuizDeleteResponse>
{
    [Required(ErrorMessage = "TestId is required")]
    public Guid TestId { get; set; }
    
    [Required(ErrorMessage = "QuizId is required")]
    public Guid QuizId { get; set; }
}

public record TestQuizDeleteResponse : AbstractApiResponse<string>
{
    public override string Response { get; set; }
}

