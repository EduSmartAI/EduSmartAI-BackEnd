using BuildingBlocks.CQRS;

namespace QuizService.Application.Applications.Questions.Commands;

/// <summary>
/// Command để insert question và answers vào quiz
/// </summary>
public record QuestionInsertCommand : ICommand<QuestionInsertResponse>
{
    public Guid QuizId { get; set; }
    
    public string QuestionText { get; set; } = null!;
    
    public string? Explanation { get; set; }
    
    public List<InsertAnswers> Answers { get; set; } = null!;
}

public record InsertAnswers
{
    public string AnswerText { get; set; } = null!;
    
    public bool IsCorrect { get; set; }
}
