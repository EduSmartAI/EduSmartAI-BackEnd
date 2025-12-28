using StudentService.Application.Applications.LearningPaths.Queries.SelectLearningPaths;

namespace StudentService.Application.Interfaces;

/// <summary>
/// Abstraction for pushing learning path updates to connected SSE clients.
/// </summary>
public interface ILearningPathRealtimeNotifier
{
    /// <summary>
    /// Subscribe to updates for a specific learning path.
    /// </summary>
    IAsyncEnumerable<LearningPathSelectResponse> SubscribeAsync(Guid pathId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Publish a learning path snapshot to all subscribers of the given path.
    /// </summary>
    Task PublishAsync(Guid pathId, LearningPathSelectResponse payload, CancellationToken cancellationToken = default);

    Task PublishLearningPathSnapshotAsync(Guid pathId, Guid? studentId, CancellationToken cancellationToken);
}

