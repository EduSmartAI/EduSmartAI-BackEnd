using BaseService.Common.ApiEntities;

namespace BuildingBlocks.Messaging.Events.StudentService;

public record CourseSelectsBySubjectCodeEventResponse : AbstractApiResponse<List<CourseSelectsBySubjectCodeEventResponseEntity>>
{
    public override List<CourseSelectsBySubjectCodeEventResponseEntity> Response { get; set; }
}

public class CourseSelectsBySubjectCodeEventResponseEntity
{
    public Guid CourseId { get; set; }
}