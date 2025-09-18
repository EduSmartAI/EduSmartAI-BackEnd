using BaseService.Common.ApiEntities;

namespace BuildingBlocks.Messaging.Events.QuizService.MajorSelectsEvents;

public record MajorSelectsEventResponse : AbstractApiResponse<List<MajorSelectsEventResponseEntity>>
{
    public override List<MajorSelectsEventResponseEntity> Response { get; set; }
}

public class MajorSelectsEventResponseEntity
{
    public Guid MajorId { get; set; }
    
    public string MajorName { get; set; }
    
    public Guid? ParentMajorId { get; set; }
}