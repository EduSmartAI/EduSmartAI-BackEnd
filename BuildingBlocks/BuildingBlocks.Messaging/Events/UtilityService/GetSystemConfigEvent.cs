using BaseService.Common.ApiEntities;

namespace BuildingBlocks.Messaging.Events.UtilityService;

public class GetSystemConfigEvent
{
    public string ConfigId { get; set; } = null!;
}

public record GetSystemConfigEventResponse : AbstractApiResponse<string>
{
    public override string Response { get; set; } = string.Empty;
}
