using BaseService.Common.ApiEntities;

namespace BuildingBlocks.Messaging.Events.CourseService;

public class LearningPathCourseSelectEvent
{
    public required Guid StudentId { get; set; }
}

public record LearningPathCourseSelectEventResponse : AbstractApiResponse<List<Guid>?>
{
    public override List<Guid>? Response { get; set; }
}