namespace BaseService.API.Sse;

public interface IServerSentEventsClient
{
    Task SendEventAsync(string eventName, object? data, CancellationToken cancellationToken = default);

    Task SendCommentAsync(string comment, CancellationToken cancellationToken = default);

    Task SendRetryAsync(int retryMilliseconds, CancellationToken cancellationToken = default);
}












