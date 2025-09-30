using BaseService.Common.ApiEntities;

namespace BuildingBlocks.Messaging.Events.StudentService;

public record CourseSelectsEventResponse : AbstractApiResponse<List<CourseSelectsEventResponseEntity>>
{
    public override List<CourseSelectsEventResponseEntity> Response { get; set; }
}

public class CourseSelectsEventResponseEntity
{
    public Guid MajorCodeId { get; set; }
    
    public List<Guid> CourseCodeIds { get; set; }
}
