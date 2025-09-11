using System.ComponentModel.DataAnnotations;
using BuildingBlocks.CQRS;

namespace QuizService.Application.Applications.StudentTests.Commands;

public record StudentTestInsertCommand : ICommand<StudentTestInsertResponse>
{
    [Required(ErrorMessage = "TestId is required")]
    public Guid TestId { get; set; }
    
    [Required(ErrorMessage = "StartedAt is required")]
    public DateTime StartedAt { get; set; }
    
    [Required(ErrorMessage = "Answers are required")]
    public List<StudentAnswerRequest> Answers { get; set; }
}

public record StudentAnswerRequest
{
    [Required(ErrorMessage = "QuestionId is required")]
    public Guid QuestionId { get; set; }

    [Required(ErrorMessage = "AnswerId is required")]
    public Guid AnswerId { get; set; }
}