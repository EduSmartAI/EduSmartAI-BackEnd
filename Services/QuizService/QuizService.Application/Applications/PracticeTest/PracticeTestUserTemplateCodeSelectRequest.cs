using System.ComponentModel.DataAnnotations;
using MediatR;

namespace QuizService.Application.Applications.PracticeTest;

public class PracticeTestUserTemplateCodeSelectRequest : IRequest<PracticeTestUserTemplateCodeSelectResponse>
{
    [Required(ErrorMessage = "ProblemId is required")]
    public Guid ProblemId { get; set; }

    [Required(ErrorMessage = "LanguageId is required")]
    public int LanguageId { get; set; }
}