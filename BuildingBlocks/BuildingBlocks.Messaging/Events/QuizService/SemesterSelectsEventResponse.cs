using BaseService.Common.ApiEntities;

namespace BuildingBlocks.Messaging.Events.QuizService.SemesterSelectsEvents;

public record SemesterSelectsEventResponse : AbstractApiResponse<List<SemesterSelectsEventResponseEntity>>
{
    public override List<SemesterSelectsEventResponseEntity> Response { get; set; }
}

public class SemesterSelectsEventResponseEntity
{
    public Guid SemesterId { get; set; }
    
    public string SemesterName { get; set; }
    
    public short SemesterNumber { get; set; }
}
