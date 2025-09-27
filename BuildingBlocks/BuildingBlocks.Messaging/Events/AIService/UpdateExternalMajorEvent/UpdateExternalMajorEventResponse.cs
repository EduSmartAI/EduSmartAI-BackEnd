using BaseService.Common.ApiEntities;

namespace BuildingBlocks.Messaging.Events.AIService.UpdateExternalMajorEvent
{
    public record UpdateExternalMajorEventResponse : AbstractApiResponse<string>
    {
        public override string Response { get; set; } = string.Empty;
    }
}
