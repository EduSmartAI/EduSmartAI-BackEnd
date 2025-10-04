namespace BuildingBlocks.Messaging.Events.AIService.InsertInternalExternalMajorEvent
{
    public sealed record InternalMajorEvent(
        Guid LearningPathId,
        string LimitTime,
        string CurrentUserEmail,
        IReadOnlyList<InternalMajorItem> Majors,
        Guid SemesterId);
        
    public sealed record InternalMajorItem(
        string MajorCode,
        string Reason
    );
}
