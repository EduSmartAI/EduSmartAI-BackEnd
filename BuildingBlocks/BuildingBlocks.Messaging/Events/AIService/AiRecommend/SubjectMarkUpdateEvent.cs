namespace BuildingBlocks.Messaging.Events.AIService.AiRecommend;

public record SubjectMarkUpdateEvent
{
    public required Guid LearningPathId { get; init; }
    public required List<SubjectMarkUpdateItem> SubjectMarks { get; init; }
}

public record SubjectMarkUpdateItem
{
    public required string SubjectCode { get; init; }
    public required string SubjectName { get; init; }
    public double? OldMark { get; init; }
    public required double NewMark { get; init; }
    public required string NewAnalysis { get; init; }
    public required string CareerGoal { get; init; }
}

