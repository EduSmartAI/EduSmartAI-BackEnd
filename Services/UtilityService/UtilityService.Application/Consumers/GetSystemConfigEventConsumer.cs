using BuildingBlocks.Messaging.Events.UtilityService;
using MassTransit;
using UtilityService.Application.Interfaces;

namespace UtilityService.Application.Consumers;

public class GetSystemConfigEventConsumer(ITavilyConfigService _tavilyConfigService) : IConsumer<GetSystemConfigEvent>
{
    public async Task Consume(ConsumeContext<GetSystemConfigEvent> context)
    {
        var evt = context.Message;
        var response = new GetSystemConfigEventResponse
        {
            Success = false,
            Response = string.Empty
        };

        try
        {
            if (string.IsNullOrWhiteSpace(evt.ConfigId))
            {
                response.Message = "ConfigId is required";
                await context.RespondAsync(response);
                return;
            }

            var configValue = await _tavilyConfigService.GetConfigValueByIdAsync(evt.ConfigId, context.CancellationToken);

            if (string.IsNullOrEmpty(configValue))
            {
                response.Message = $"System config with Id '{evt.ConfigId}' not found or is inactive";
                await context.RespondAsync(response);
                return;
            }

            response.Success = true;
            response.Response = configValue;
            await context.RespondAsync(response);
        }
        catch (Exception ex)
        {
            response.Message = $"Error retrieving system config: {ex.Message}";
            await context.RespondAsync(response);
        }
    }
}
