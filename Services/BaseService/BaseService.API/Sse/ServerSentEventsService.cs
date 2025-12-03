using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace BaseService.API.Sse;

public class ServerSentEventsService : IServerSentEventsService
{
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
        await streamAction(client, cancellationToken);
        await response.Body.FlushAsync(cancellationToken);
    }

    private static void PrepareHeaders(HttpResponse response)
    {
        response.Headers["Cache-Control"] = "no-cache";
        response.Headers["Connection"] = "keep-alive";
        response.Headers["X-Accel-Buffering"] = "no";
        response.ContentType = "text/event-stream";
    }
}




