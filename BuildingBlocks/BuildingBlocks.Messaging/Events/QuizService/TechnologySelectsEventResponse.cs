using BaseService.Common.ApiEntities;

namespace BuildingBlocks.Messaging.Events.QuizService;

public record TechnologySelectsEventResponse : AbstractApiResponse<List<TechnologySelectsEventResponseEntity>>
{
    public override List<TechnologySelectsEventResponseEntity> Response { get; set; }
}

public record TechnologySelectsEventResponseEntity
{
    public Guid TechnologyId { get; set; }
    
    public string TechnologyName { get; set; }
    
    public short TechnologyType { get; set; }
}