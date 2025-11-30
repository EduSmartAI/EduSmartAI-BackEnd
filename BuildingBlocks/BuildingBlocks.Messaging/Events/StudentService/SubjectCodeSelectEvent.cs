using BaseService.Common.ApiEntities;

namespace BuildingBlocks.Messaging.Events.StudentService;

public class SubjectCodeSelectEvent
{
    
}

public record SubjectCodeSelectEventResponse : AbstractApiResponse<List<SubjectCodeSelectEventResponseEntity>>
{
    public override List<SubjectCodeSelectEventResponseEntity> Response { get; set; }
}

public class SubjectCodeSelectEventResponseEntity
{
    public string SubjectCode { get; set; } = null!;
}