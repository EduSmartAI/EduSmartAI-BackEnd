using System.ComponentModel.DataAnnotations;
using MediatR;

namespace QuizService.Application.Applications.PracticeTest;

public class PracticeTestCodeCheckRequest : IRequest<PracticeTestCodeCheckResponse>
{
    [Required(ErrorMessage = "ProblemId is required")]
    public Guid ProblemId { get; set; }
    
    [Required(ErrorMessage = "SourceCode is required")]
    public string SourceCode { get; set; } = null!;
    
    [Required(ErrorMessage = "LanguageId is required")]
    public int LanguageId { get; set; }
    
    [Required(ErrorMessage = "Input is required")]
    public string Input { get; set; } = null!;
}

