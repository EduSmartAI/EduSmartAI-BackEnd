using BaseService.Common.ApiEntities;

namespace QuizService.Application.Applications.Quizzes.Queries;

public record QuizSelectsResponse : AbstractApiResponse<List<QuizSelectsResponseEntity>>
{
    public override List<QuizSelectsResponseEntity> Response { get; set; }
}

public record QuizSelectsResponseEntity
{
    public Guid QuizId { get; set; }
    
    public string Title { get; set; } = null!;

    public string? Description { get; set; }
    
    public Guid SubjectCode { get; set; }
}