using BaseService.Common.ApiEntities;

namespace BuildingBlocks.Messaging.Events.AIService.UpdateExternalMajorEvent
{
    public sealed record UpdateBatchExternalMajorEventResponse : AbstractApiResponse<string>
    {
        public override string Response { get; set; } = string.Empty;
    }
}

