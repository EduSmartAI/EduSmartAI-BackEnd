using BaseService.Common.ApiEntities;

namespace StudentService.Application.Applications.LearningPaths.Commands;

public record LearningPathMajorInternalInsertResponse : AbstractApiResponse<Guid>
{
    public override Guid Response { get; set; }
    public List<Guid>? InsertedMajorIds { get; set; }
}