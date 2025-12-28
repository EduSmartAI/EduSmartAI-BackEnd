using BaseService.Common.ApiEntities;

namespace BuildingBlocks.Messaging.Events.AuthService.InsertUserEvents;

public record StudentInsertEventResponse : AbstractApiResponse<string>
{
    public override string Response { get; set; }
}

