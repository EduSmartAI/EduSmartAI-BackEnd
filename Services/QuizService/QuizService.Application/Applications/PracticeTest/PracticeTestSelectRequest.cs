using System.ComponentModel.DataAnnotations;
using MediatR;

namespace QuizService.Application.Applications.PracticeTest;

public class PracticeTestSelectRequest : IRequest<PracticeTestSelectResponse>
{
    [Required(ErrorMessage = "ProblemId là bắt buộc")]
    public Guid ProblemId { get; set; }
}