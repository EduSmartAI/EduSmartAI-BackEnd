using BaseService.Common.ApiEntities;

namespace BuildingBlocks.Messaging.Events.InsertUserEvents;

public record SendKeyEventResponse : AbstractApiResponse<string>
{
    public override string Response { get; set; }
}