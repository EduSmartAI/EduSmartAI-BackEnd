namespace BuildingBlocks.Messaging.Events.AIService.UpdateExternalMajorEvent
{
    public sealed record UpdateExternalMajorEvent(
        Guid LearningPathId,
        string CurrentUserEmail,
        string MajorCode,
        string Reason,
        List<StepExternalMajorItem> Steps
    );

    public sealed record StepExternalMajorItem(
        int Order,
        string Title,
        int DurationWeeks,
        IReadOnlyList<string> Objectives,
        IReadOnlyList<StepCourseItem> SuggestedCourses
    );

    public sealed record StepCourseItem(
        string Title,
        string Link,
        string Provider,
        string Reason,
        string Duration,
        string Level
    );
}
