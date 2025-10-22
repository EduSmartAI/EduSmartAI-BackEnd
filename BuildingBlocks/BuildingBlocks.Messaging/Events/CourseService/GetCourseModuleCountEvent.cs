using BaseService.Common.ApiEntities;

namespace BuildingBlocks.Messaging.Events.CourseService;

public record GetCourseModuleCountEvent
{
    public Guid CourseId { get; init; }
}

public record GetCourseModuleCountEventResponse : AbstractApiResponse<GetCourseModuleCountEventResponseEntity>
{
    public override GetCourseModuleCountEventResponseEntity Response { get; set; }
}

public class GetCourseModuleCountEventResponseEntity
{
    public int TotalModules { get; init; }
}

