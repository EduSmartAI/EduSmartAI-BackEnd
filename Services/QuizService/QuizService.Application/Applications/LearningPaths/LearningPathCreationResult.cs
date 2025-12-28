using BaseService.Common.ApiEntities;

namespace QuizService.Application.Applications.LearningPaths;

public record LearningPathCreationResult : AbstractApiResponse<object>
{
    public override object Response { get; set; } = null!;
}

