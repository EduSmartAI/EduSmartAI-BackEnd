using BuildingBlocks.CQRS;

namespace QuizService.Application.Applications.Questions.Commands;

public record QuestionUpdateCommand : ICommand<QuestionUpdateResponse>
{
    public Guid QuestionId { get; set; }
    
    public string QuestionText { get; set; } = null!;
    
    public string? Explanation { get; set; }
    
    public List<UpdateAnswers> Answers { get; set; } = null!;
}

public record UpdateAnswers
{
    public Guid? AnswerId { get; set; }
    
    public string AnswerText { get; set; } = null!;
    
    public bool IsCorrect { get; set; }
}
