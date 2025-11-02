using System.ComponentModel.DataAnnotations;
using BuildingBlocks.CQRS;

namespace QuizService.Application.Applications.Admin.Queries.StudentTests;

public class AdminStudentTestSelectDetailQuery : IQuery<AdminStudentTestSelectDetailResponse>
{
    [Required(ErrorMessage = "StudentTestId is required.")]
    public Guid StudentTestId { get; set; }
}