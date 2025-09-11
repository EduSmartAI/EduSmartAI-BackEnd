using BaseService.Common.ApiEntities;

namespace QuizService.Application.Applications.StudentTests.Queries;

public record StudentTestSelectResponse : AbstractApiResponse<StudentTestSelectResponseEntity>
{
    public override StudentTestSelectResponseEntity Response { get; set; }
}

public record StudentTestSelectResponseEntity
{
    public Guid StudentTestId { get; set; }
    public Guid TestId { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? FinishedAt { get; set; }
    
    public List<StudentAnswerSelectResponseEntity> Answers { get; set; }
}

public record StudentAnswerSelectResponseEntity
{
    public Guid QuestionId { get; set; }
    public Guid? AnswerId { get; set; }
    
    public bool? IsCorrect { get; set; }
    
    public string? Explanation { get; set; }
}