namespace BuildingBlocks.Messaging.Events.AIService.AiFeedback
{
    public record InsertAiFeedbackEvents(Guid CourseId, Guid StudentId, string markdownFeedBack);
}
