namespace BuildingBlocks.Messaging.Events.AIService.AiFeedback
{
    public record UpdateModuleFeedbackEvent(Guid ModuleId, Guid StudentId, string markdownFeedBack);
}
