namespace BuildingBlocks.Messaging.Events.AIService.GetLessonInfoEvent
{
    public sealed record GetLessonInfoEvent(Guid LessonId, Guid StudentId);
}