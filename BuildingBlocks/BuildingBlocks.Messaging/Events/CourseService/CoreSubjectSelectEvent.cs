using BaseService.Common.ApiEntities;

namespace BuildingBlocks.Messaging.Events.CourseService;

public class CoreSubjectSelectEvent
{
    public List<string> SubjectCodes { get; set; } = new();
}

public record CoreSubjectSelectEventResponse : AbstractApiResponse<List<CoreSubjectSelectEventResponseEntity>>
{
    public override List<CoreSubjectSelectEventResponseEntity> Response { get; set; }
}

public class CoreSubjectSelectEventResponseEntity
{
    public string SubjectCode { get; set; }
}

