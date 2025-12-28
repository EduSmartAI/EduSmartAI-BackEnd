using System.ComponentModel.DataAnnotations;
using BuildingBlocks.CQRS;

namespace QuizService.Application.Applications.Admin.Queries.PracticeTests;

/// <summary>
/// Query to select a specific practice test for admin
/// </summary>
public record AdminPracticeTestSelectQuery : IQuery<AdminPracticeTestSelectResponse>
{
    [Required(ErrorMessage = "ProblemId là bắt buộc")]
    public Guid ProblemId { get; init; }
}

