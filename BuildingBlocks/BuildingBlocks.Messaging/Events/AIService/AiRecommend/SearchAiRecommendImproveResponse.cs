using BaseService.Common.ApiEntities;

namespace BuildingBlocks.Messaging.Events.AIService.AiRecommend
{
    public record SearchAiRecommendImproveResponse : AbstractApiResponse<string>
    {
        public override string Response { get; set; } = string.Empty;
    }
}
