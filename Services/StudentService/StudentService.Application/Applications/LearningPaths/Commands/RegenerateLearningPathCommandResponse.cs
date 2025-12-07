using BaseService.Common.ApiEntities;

namespace StudentService.Application.Applications.LearningPaths.Commands;

public record RegenerateLearningPathCommandResponse : AbstractApiResponse<Guid?>
{
    public override Guid? Response { get; set; }
}