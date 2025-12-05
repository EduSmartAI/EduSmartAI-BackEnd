using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace BaseService.API.Sse;

public class ServerSentEventsService : IServerSentEventsService
{
    private static readonly TimeSpan HeartbeatInterval = TimeSpan.FromSeconds(25);
    private readonly JsonSerializerOptions _serializerOptions;

    public ServerSentEventsService(IOptions<JsonOptions>? jsonOptions = null)
    {
        _serializerOptions = jsonOptions?.Value?.JsonSerializerOptions ?? new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };
    }

    public async Task StreamAsync(HttpResponse response, Func<IServerSentEventsClient, CancellationToken, Task> streamAction, CancellationToken cancellationToken = default)
    {
        if (response == null) throw new ArgumentNullException(nameof(response));
        if (streamAction == null) throw new ArgumentNullException(nameof(streamAction));

        PrepareHeaders(response);

        var client = new ServerSentEventsClient(response, _serializerOptions);
        using var heartbeatCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var heartbeatTask = RunHeartbeatAsync(client, heartbeatCts.Token);

        try
        {
            await streamAction(client, cancellationToken);
        }
        catch (OperationCanceledException) when (IsCancellationExpected(response, cancellationToken))
        {
            // Ignore cooperative cancellation to prevent premature connection teardown.
        }
        catch (IOException) when (IsCancellationExpected(response, cancellationToken))
        {
            // Ignore IO errors caused by client disconnects.
        }
        finally
        {
            heartbeatCts.Cancel();
            try
            {
                await heartbeatTask;
            }
            catch (OperationCanceledException)
            {
                // ignored
            }

            try
            {
                await response.Body.FlushAsync(CancellationToken.None);
            }
            catch (OperationCanceledException) when (IsCancellationExpected(response, cancellationToken))
            {
                // ignored
            }
            catch (IOException) when (IsCancellationExpected(response, cancellationToken))
            {
                // ignored
            }
        }
    }

    private static void PrepareHeaders(HttpResponse response)
    {
        response.Headers["Cache-Control"] = "no-cache";
        response.Headers["Connection"] = "keep-alive";
        response.Headers["X-Accel-Buffering"] = "no";
        response.ContentType = "text/event-stream";
    }

    private static async Task RunHeartbeatAsync(IServerSentEventsClient client, CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(HeartbeatInterval, cancellationToken);
                await client.SendCommentAsync("heartbeat", cancellationToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    private static bool IsCancellationExpected(HttpResponse response, CancellationToken cancellationToken)
    {
        return cancellationToken.IsCancellationRequested || (response.HttpContext?.RequestAborted.IsCancellationRequested ?? false);
    }
}





