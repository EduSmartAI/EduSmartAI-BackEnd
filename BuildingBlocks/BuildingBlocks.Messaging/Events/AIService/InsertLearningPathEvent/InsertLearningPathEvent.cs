namespace BuildingBlocks.Messaging.Events.AIService.InsertLearningPathEvent
{
    public sealed record InsertLearningPathEventLearningPathEvent(
        Guid LearningPathId,
        string PathName,
        Guid StudentId,
        string CurrentUserEmail
    );
    public sealed record InsertLearningPathResponseEvent(
        bool Success
    );
}
