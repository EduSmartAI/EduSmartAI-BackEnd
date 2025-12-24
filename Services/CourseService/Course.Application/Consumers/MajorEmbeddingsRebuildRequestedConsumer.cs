using BuildingBlocks.Messaging.Events.CourseService;
using NLog;

namespace Course.Application.Consumers;

/// <summary>
/// Consumer for MajorEmbeddingsRebuildRequestedEvent.
/// Handles async rebuild of major embeddings via external API.
/// This consumer is fire-and-forget, so failures won't affect the main operation.
/// </summary>
public class MajorEmbeddingsRebuildRequestedConsumer : IConsumer<MajorEmbeddingsRebuildRequestedEvent>
{
    private static readonly Logger _logger = LogManager.GetCurrentClassLogger();
    private readonly IMajorEmbeddingBuilderClient _embeddingBuilderClient;

    public MajorEmbeddingsRebuildRequestedConsumer(IMajorEmbeddingBuilderClient embeddingBuilderClient)
    {
        _embeddingBuilderClient = embeddingBuilderClient;
    }

    public async Task Consume(ConsumeContext<MajorEmbeddingsRebuildRequestedEvent> context)
    {
        var evt = context.Message;
        
        try
        {
            _logger.Info($"Starting major embeddings rebuild. EventId: {evt.EventId}, Reason: {evt.Reason ?? "Unknown"}, MaxRows: {evt.MaxRows ?? -1}");
            
            // Call the embedding builder service
            var result = await _embeddingBuilderClient.RebuildAllAsync(evt.MaxRows, context.CancellationToken);
            
            // Log success (result contains details about the operation)
            if (result.TryGetProperty("ok", out var okProperty) && okProperty.GetBoolean())
            {
                var selectedRows = result.TryGetProperty("selected_rows", out var rowsProperty) 
                    ? rowsProperty.GetInt32() 
                    : 0;
                var elapsed = result.TryGetProperty("elapsed_sec", out var elapsedProperty) 
                    ? elapsedProperty.GetDouble() 
                    : 0;
                    
                _logger.Info($"Major embeddings rebuild completed successfully. Rows processed: {selectedRows}, Elapsed: {elapsed}s");
            }
            else
            {
                var error = result.TryGetProperty("error", out var errorProperty) 
                    ? errorProperty.GetString() 
                    : "Unknown error";
                _logger.Warn($"Major embeddings rebuild completed with errors. Error: {error}");
            }
        }
        catch (Exception ex)
        {
            // Log error but don't throw - this is fire-and-forget
            _logger.Error(ex, $"Failed to rebuild major embeddings. EventId: {evt.EventId}, Reason: {evt.Reason ?? "Unknown"}");
            
            // Optionally: could publish a failure event or retry, but for now just log
        }
    }
}

