using BaseService.Application.Common;
using BaseService.Common.ApiEntities;

namespace QuizService.Application.Applications.PracticeTest;

public record PracticeTestSelectsResponse : AbstractApiResponse<PagedResult<PracticeTestSelectsResponseEntity>>
{
    public override PagedResult<PracticeTestSelectsResponseEntity> Response { get; set; }
}

public class PracticeTestSelectsResponseEntity
{
    public Guid ProblemId { get; set; }

    public string Title { get; set; } = null!;

    public string Description { get; set; } = null!;

    public string Difficulty { get; set; } = null!;
}