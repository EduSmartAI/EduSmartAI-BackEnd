namespace BuildingBlocks.Messaging.Events.AIService.InsertLearningPathEvent
{
    public sealed record InsertLearningPathEvent(
        Guid LearningPathId,
        string PathName,
        Guid StudentId,
        string CurrentUserEmail
    );
}
