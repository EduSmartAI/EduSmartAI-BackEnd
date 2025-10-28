using BaseService.Common.ApiEntities;

namespace BuildingBlocks.Messaging.Events.AuthService.InsertUserEvents;

public record LecturerInsertEventResponse : AbstractApiResponse<string>
{
    public override string Response { get; set; }
}

