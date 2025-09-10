using BuildingBlocks.CQRS;

namespace QuizService.Application.Applications.Tests.Commands;

public record TestInsertCommand : ICommand<TestInsertResponse>
{
    public string TestName { get; set; } = null!;

    public string? Description { get; set; }

    public List<Quizzes> Quizzes { get; set; } = null!;
}

public record Quizzes
{
    public string Title { get; set; } = null!;

    public string? Description { get; set; }
    
    public Guid SubjectCode { get; set; }
    
    public List<Questions> Questions { get; set; } = null!;
}

public record Questions
{
    public string QuestionText { get; set; } = null!;

    public short QuestionType { get; set; }
    
    public List<Answers> Answers { get; set; } = null!;
}

public record Answers
{
    public string AnswerText { get; set; } = null!;

    public bool? IsCorrect { get; set; }
}