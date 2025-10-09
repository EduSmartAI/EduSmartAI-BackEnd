namespace BuildingBlocks.Messaging.Events.AIService.UpdateExternalMajorEvent
{
    public sealed record UpdateBatchExternalMajorEvent(
        Guid LearningPathId,
        string CurrentUserEmail,
        List<ExternalMajorItem> Majors
    );

    public sealed record ExternalMajorItem(
        string MajorCode,
        string Reason,
        List<StepExternalMajorItem> Steps
    );
}

