using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Threading.Channels;
using BaseService.Application.Interfaces.Repositories;
using Microsoft.Extensions.DependencyInjection;
using StudentService.Application.Applications.LearningPaths.Queries;
using StudentService.Application.Applications.LearningPaths.Queries.SelectLearningPaths;
using StudentService.Application.Interfaces;
using StudentService.Domain.ReadModels;

namespace StudentService.Infrastructure.Realtime;

public class LearningPathRealtimeNotifier : ILearningPathRealtimeNotifier
{
    private readonly ConcurrentDictionary<Guid, ConcurrentDictionary<Guid, Channel<LearningPathSelectResponse>>> _subscriptions = new();
    private readonly IServiceProvider _serviceProvider;

    public LearningPathRealtimeNotifier(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public IAsyncEnumerable<LearningPathSelectResponse> SubscribeAsync(Guid pathId, CancellationToken cancellationToken = default)
    {
        if (pathId == Guid.Empty)
        {
            return EmptyAsyncEnumerable(cancellationToken);
        }

        var subscriptionId = Guid.NewGuid();
        var channel = Channel.CreateUnbounded<LearningPathSelectResponse>();
        var group = _subscriptions.GetOrAdd(pathId, _ => new ConcurrentDictionary<Guid, Channel<LearningPathSelectResponse>>());
        group[subscriptionId] = channel;

        cancellationToken.Register(() =>
        {
            RemoveSubscription(pathId, subscriptionId);
            channel.Writer.TryComplete();
        });

        return ReadChannelAsync(pathId, subscriptionId, channel, cancellationToken);
    }

    public Task PublishAsync(Guid pathId, LearningPathSelectResponse payload, CancellationToken cancellationToken = default)
    {
        if (pathId == Guid.Empty || payload == null)
        {
            return Task.CompletedTask;
        }

        if (!_subscriptions.TryGetValue(pathId, out var subscribers) || subscribers.IsEmpty)
        {
            return Task.CompletedTask;
        }

        foreach (var subscriber in subscribers.Values)
        {
            subscriber.Writer.TryWrite(payload);
        }

        return Task.CompletedTask;
    }

    private async IAsyncEnumerable<LearningPathSelectResponse> ReadChannelAsync(
        Guid pathId,
        Guid subscriptionId,
        Channel<LearningPathSelectResponse> channel,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        try
        {
            await foreach (var payload in channel.Reader.ReadAllAsync())
            {
                yield return payload;
            }
        }
        finally
        {
            RemoveSubscription(pathId, subscriptionId);
        }
    }

    private static async IAsyncEnumerable<LearningPathSelectResponse> EmptyAsyncEnumerable(
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
        yield break;
    }

    private void RemoveSubscription(Guid pathId, Guid subscriptionId)
    {
        if (!_subscriptions.TryGetValue(pathId, out var subscribers))
        {
            return;
        }

        subscribers.TryRemove(subscriptionId, out _);

        if (subscribers.IsEmpty)
        {
            _subscriptions.TryRemove(pathId, out _);
        }
    }
    
    public async Task PublishLearningPathSnapshotAsync(Guid pathId, Guid? studentId, CancellationToken cancellationToken)
    {
        if (pathId == Guid.Empty)
        {
            return;
        }

        using var scope = _serviceProvider.CreateScope();
        var learningPathService = scope.ServiceProvider.GetRequiredService<ILearningPathService>();
        var learningPathQueryRepository = scope.ServiceProvider.GetService<IQueryRepository<LearningPathCollection>>();

        var effectiveStudentId = studentId;
        if ((effectiveStudentId == null || effectiveStudentId == Guid.Empty) && learningPathQueryRepository != null)
        {
            var readModel = await learningPathQueryRepository.FirstOrDefaultAsync(x => x.PathId == pathId && x.IsActive);
            effectiveStudentId = readModel?.StudentId;
        }

        if (!effectiveStudentId.HasValue || effectiveStudentId == Guid.Empty)
        {
            return;
        }
        
        var snapshot = await learningPathService.GetLearningPathById(
            new LearningPathSelectsQuery { LearningPathId = pathId },
            effectiveStudentId.Value,
            true,
            cancellationToken);

        if (snapshot.Success)
        {
            await PublishAsync(pathId, snapshot, cancellationToken);
        }
    }
}

