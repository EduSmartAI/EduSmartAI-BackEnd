using BaseService.Common.ApiEntities;

namespace BuildingBlocks.Messaging.Events.AIService.AiFeedback
{
    public record InsertAiFeedbackResponse : AbstractApiResponse<string>
    {
        public override string Response { get; set; } = string.Empty;
    }
}
