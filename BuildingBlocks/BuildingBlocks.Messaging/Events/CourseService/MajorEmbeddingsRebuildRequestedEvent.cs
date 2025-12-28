namespace BuildingBlocks.Messaging.Events.CourseService;

/// <summary>
/// Event published when major embeddings need to be rebuilt.
/// This is a fire-and-forget event, consumed asynchronously.
/// </summary>
public record MajorEmbeddingsRebuildRequestedEvent : IntegrationEvent
{
    /// <summary>
    /// Maximum number of rows to process. If null, process all rows.
    /// </summary>
    public int? MaxRows { get; init; }
    
    /// <summary>
    /// Reason for rebuild (e.g., "MajorCreated", "MajorUpdated")
    /// </summary>
    public string? Reason { get; init; }
}

