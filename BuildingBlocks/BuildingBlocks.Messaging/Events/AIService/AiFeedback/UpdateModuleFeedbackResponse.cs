using BaseService.Common.ApiEntities;

namespace BuildingBlocks.Messaging.Events.AIService.AiFeedback
{
    public record UpdateModuleFeedbackResponse : AbstractApiResponse<string>
    {
        public override string Response { get; set; } = string.Empty;
    }
}
