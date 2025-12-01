using System.IO;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;

namespace BaseService.API.Sse;

internal sealed class ServerSentEventsClient : IServerSentEventsClient
{
    private readonly HttpResponse _response;
    private readonly JsonSerializerOptions _serializerOptions;

    public ServerSentEventsClient(HttpResponse response, JsonSerializerOptions serializerOptions)
    {
        _response = response;
        _serializerOptions = serializerOptions;
    }

    public async Task SendEventAsync(string eventName, object? data, CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return;
        }

        var payloadBuilder = new StringBuilder();

        if (!string.IsNullOrWhiteSpace(eventName))
        {
            payloadBuilder.Append("event: ").Append(eventName).Append('\n');
        }

        payloadBuilder.Append(SerializeData(data));

        await WriteAsync(payloadBuilder.ToString(), cancellationToken);
    }

    public Task SendCommentAsync(string comment, CancellationToken cancellationToken = default)
        => WriteAsync($": {comment}\n\n", cancellationToken);

    public Task SendRetryAsync(int retryMilliseconds, CancellationToken cancellationToken = default)
        => WriteAsync($"retry: {retryMilliseconds}\n\n", cancellationToken);

    private async Task WriteAsync(string data, CancellationToken cancellationToken)
    {
        await _response.WriteAsync(data, cancellationToken);
        await _response.Body.FlushAsync(cancellationToken);
    }

    private string SerializeData(object? data)
    {
        var serialized = data switch
        {
            null => "{}",
            string str => str,
            _ => JsonSerializer.Serialize(data, _serializerOptions)
        };

        var builder = new StringBuilder();
        using var reader = new StringReader(serialized);
        string? line;

        while ((line = reader.ReadLine()) != null)
        {
            builder.Append("data: ").Append(line).Append('\n');
        }

        builder.Append('\n');
        return builder.ToString();
    }
}

