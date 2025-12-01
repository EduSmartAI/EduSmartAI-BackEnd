using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Threading.Channels;
using StudentService.Application.Applications.LearningPaths.Queries.SelectLearningPaths;
using StudentService.Application.Interfaces;

namespace StudentService.Infrastructure.Realtime;

public class LearningPathRealtimeNotifier : ILearningPathRealtimeNotifier
{
    private readonly ConcurrentDictionary<Guid, ConcurrentDictionary<Guid, Channel<LearningPathSelectResponse>>> _subscriptions = new();

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
            await foreach (var payload in channel.Reader.ReadAllAsync(cancellationToken))
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
}

