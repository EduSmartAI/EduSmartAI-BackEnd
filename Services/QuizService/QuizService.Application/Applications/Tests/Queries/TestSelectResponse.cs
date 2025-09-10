using BaseService.Common.ApiEntities;

namespace QuizService.Application.Applications.Tests.Queries;

public record TestSelectResponse : AbstractApiResponse<TestSelectResponseEntity>
{
    public override TestSelectResponseEntity Response { get; set; }
}

public record TestSelectResponseEntity
{
    public Guid TestId { get; set; }
    
    public string TestName { get; set; } = null!;

    public string? Description { get; set; }

    public int DurationMinutes { get; set; }

    public List<QuizzDetailResponse> Quizzes { get; set; } = null!;
}

public record QuizzDetailResponse
{
    public Guid QuizId { get; set; }
    
    public string Title { get; set; } = null!;

    public string? Description { get; set; }
    
    public Guid SubjectCode { get; set; }
    
    public List<QuestionDetailResponse> Questions { get; set; } = null!;
}

public record QuestionDetailResponse
{
    public Guid QuestionId { get; set; }
    
    public string QuestionText { get; set; } = null!;

    public short QuestionType { get; set; }
    
    public List<AnswerDetailResponse> Answers { get; set; } = null!;
}

public record AnswerDetailResponse
{
    public Guid AnswerId { get; set; }
    
    public string AnswerText { get; set; } = null!;
}