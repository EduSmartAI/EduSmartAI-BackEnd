namespace BuildingBlocks.Messaging.Events.AIService.InsertInternalExternalMajorEvent
{
    public sealed record ExternalMajorEvent(
        Guid LearningPathId,
        IReadOnlyList<ExternalMajorItem> Majors
    );

    public sealed record ExternalMajorItem(
        string MajorCode,
        string Reason,
        string Description,
        string WhyForYou
    );
}
