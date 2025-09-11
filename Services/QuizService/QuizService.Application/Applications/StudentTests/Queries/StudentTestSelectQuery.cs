using System.ComponentModel.DataAnnotations;
using BuildingBlocks.CQRS;

namespace QuizService.Application.Applications.StudentTests.Queries;

public record StudentTestSelectQuery : IQuery<StudentTestSelectResponse>
{
    [Required(ErrorMessage = "StudentTestId is required")]
    public Guid StudentTestId { get; set; }
}