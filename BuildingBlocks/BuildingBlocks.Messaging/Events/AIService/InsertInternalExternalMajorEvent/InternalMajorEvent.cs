namespace BuildingBlocks.Messaging.Events.AIService.InsertInternalExternalMajorEvent
{
    public sealed record InternalMajorEvent(
        Guid LearningPathId,
        IReadOnlyList<InternalMajorItem> Majors
    );

    public sealed record InternalMajorItem(
        string MajorCode,
        string Reason,
        int SupportScore
    );
}
